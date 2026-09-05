using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.RabbitMq.Helpers;
using Shared.RabbitMq.Helpers.Structures;
using Shared.Test.Generators;
using Shared.Test.Integration.RabbitMq.Helpers.Collections;
using Shared.Test.Integration.RabbitMq.Helpers.Fixtures;

namespace Shared.Test.Integration.RabbitMq.Helpers
{
    [Collection(nameof(RabbitMqCollection))]
    public class PublisherTest
    {
        private const string _normalExchange = "test.publisher.exchange";
        private const string _normalRouting = "test.publisher.routing.normal";
        public const string _normalQueue = "test.publisher.queue";

        private const string _rejectExchange = "test.publisher.exchange.reject";
        private const string _rejectRouting = "test.publisher.routing.reject";
        public const string _rejectQueue = "test.publisher.queue.reject";

        private const string _noQueueExchange = "test.publisher.exchange.no-queue";

        private readonly Dictionary<string, object?> TestHeaders = new Dictionary<string, object?>
        {
            { "Key1", 10 },
            { "Key2", "Test value" },
            { "Key3", true },
            { "Key4", 10.10 },
            { "Key5", new int[] { 1, 2, 3, 4, 5 } },
            { "Key6", new byte[] { 5, 4, 3, 2, 1 } },
            { "Key7", new TestClass() },
            { "Key8", new Dictionary<int, string>() { { 1, "abc" }, { 2, "def" } } },
            { "Key9", null }
        };

        private readonly TimeSpan TimeoutSpan = TimeSpan.FromSeconds(30);

        private readonly RabbitMqFixture _rabbitMqFixture;

        public PublisherTest(RabbitMqFixture rabbitMqFixture)
        {
            _rabbitMqFixture = rabbitMqFixture;
        }

        [Fact]
        public async Task PublishMessageAsync_WhenMessageIsNotRouted_ShouldAddMessageToDroppedMessagesWithCorrectHeaders()
        {
            // Arrange
            var message = StringGenerator.GeneratePrintableAscii();
            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Headers = new Dictionary<string, object?>(TestHeaders)
            };

            var publisher = new TestPublisher();
            var returnEventTcs = new TaskCompletionSource<string>();
            await InitializePublisherAsync(publisher,
                returnEvent: async (obj, args) =>
                {
                    returnEventTcs.SetResult(Encoding.UTF8.GetString(args.Body.ToArray()));
                });

            // Act
            await publisher.PublishMessageAsync(
                _noQueueExchange,
                "routingKey",
                properties,
                Encoding.UTF8.GetBytes(message));

            // Assert
            var result = await returnEventTcs.Task.WaitAsync(TimeoutSpan);
            Assert.Equal(message, result);

            var droppedMessages = GetDroppedMessages(publisher);
            Assert.Single(droppedMessages);

            var headers = droppedMessages.Values.First().Properties.Headers;
            CheckHeaders(headers, true);
        }

        [Fact]
        public async Task PublishMessageAsync_WhenMessageIsNotAcknowledged_ShouldSetMessagePendingToFalseWithCorrectHeaders()
        {
            // Arrange
            var message = StringGenerator.GeneratePrintableAscii();
            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Headers = new Dictionary<string, object?>(TestHeaders)
            };

            var publisher = new TestPublisher();
            var notAcknowledgeEventTcs = new TaskCompletionSource<bool>();
            await InitializePublisherAsync(publisher,
                notAcknowledgeEvent: async (obj, args) =>
                {
                    notAcknowledgeEventTcs.SetResult(true);
                });

            // Act
            await publisher.PublishMessageAsync(
                _rejectExchange,
                _rejectRouting,
                properties,
                Encoding.UTF8.GetBytes(message));

            // Assert
            var result = await notAcknowledgeEventTcs.Task.WaitAsync(TimeoutSpan);
            Assert.True(result, "The not acknowledge event did not set the value to true.");

            var pendingMessages = GetPendingMessages(publisher);
            Assert.Single(pendingMessages);
            Assert.False(GetIsPending(pendingMessages.Values.First()), "The pending value is true.");

            var headers = pendingMessages.Values.First().Properties.Headers;
            CheckHeaders(headers, false);
        }

        [Fact]
        public async Task PublishMessageAsync_WhenMessageIsAcknowledged_ShouldRemoveMessageFromPendingMessages()
        {
            // Arrange
            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            var publisher = new TestPublisher();
            var acknowledgeEventTcs = new TaskCompletionSource<bool>();
            await InitializePublisherAsync(publisher,
                acknowledgeEvent: async (obj, args) =>
                {
                    acknowledgeEventTcs.SetResult(true);
                });

            // Act
            await publisher.PublishMessageAsync(
                _normalExchange,
                _normalRouting,
                properties,
                Encoding.UTF8.GetBytes(StringGenerator.GeneratePrintableAscii()));

            // Assert
            var result = await acknowledgeEventTcs.Task.WaitAsync(TimeoutSpan);
            Assert.True(result, "The acknowledge event did not set the value to true.");

            var pendingMessages = GetPendingMessages(publisher);
            var droppedMessages = GetDroppedMessages(publisher);
            Assert.Empty(pendingMessages);
            Assert.Empty(droppedMessages);

            var channel = await _rabbitMqFixture.Connection.CreateChannelAsync();
            var queue = await channel.QueueDeclarePassiveAsync(_normalQueue);
            Assert.Equal((uint)1, queue.MessageCount);

            await channel.QueuePurgeAsync(_normalQueue);
        }

        [Fact]
        public async Task PublishMessageAsync_WhenChannelIsNotOpen_ShouldAddMessageToDroppedMessagesWithCorrectHeaders()
        {
            // Arrange
            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Headers = new Dictionary<string, object?>(TestHeaders)
            };

            var publisher = new TestPublisher();
            await InitializePublisherAsync(publisher);

            var channelPropertyInfo = typeof(Publisher).GetProperty("Channel", BindingFlags.Public | BindingFlags.Instance)!;
            channelPropertyInfo.SetValue(publisher, null);

            // Act
            await publisher.PublishMessageAsync(
                _normalExchange,
                _normalRouting,
                properties,
                Encoding.UTF8.GetBytes(StringGenerator.GeneratePrintableAscii()));

            // Assert
            var pendingMessages = GetPendingMessages(publisher);
            var droppedMessages = GetDroppedMessages(publisher);
            Assert.Empty(pendingMessages);
            Assert.Single(droppedMessages);

            var headers = droppedMessages.Values.First().Properties.Headers;
            CheckHeaders(headers, false);
        }

        private async Task InitializePublisherAsync(TestPublisher publisher,
            AsyncEventHandler<BasicAckEventArgs>? acknowledgeEvent = null,
            AsyncEventHandler<BasicNackEventArgs>? notAcknowledgeEvent = null,
            AsyncEventHandler<BasicReturnEventArgs>? returnEvent = null)
        {
            var initializeAsyncMethodInfo = typeof(Publisher).GetMethod("InitializeAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)initializeAsyncMethodInfo.Invoke(publisher, [_rabbitMqFixture.Connection, default])!;

            publisher.Channel!.BasicAcksAsync += acknowledgeEvent;
            publisher.Channel.BasicNacksAsync += notAcknowledgeEvent;
            publisher.Channel.BasicReturnAsync += returnEvent;
        }

        private ConcurrentDictionary<ulong, Message> GetPendingMessages(TestPublisher publisher)
        {
            var propertyInfo = typeof(Publisher).GetProperty("PendingMessages", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (ConcurrentDictionary<ulong, Message>)propertyInfo.GetValue(publisher)!;
        }

        private ConcurrentDictionary<int, Message> GetDroppedMessages(TestPublisher publisher)
        {
            var propertyInfo = typeof(Publisher).GetProperty("DroppedMessages", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (ConcurrentDictionary<int, Message>)propertyInfo.GetValue(publisher)!;
        }

        private bool GetIsPending(Message message)
        {
            var propertyInfo = typeof(Message).GetProperty("IsPending", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (bool)propertyInfo.GetValue(message)!;
        }

        private void CheckHeaders(IDictionary<string, object?>? headers, bool isReturned)
        {
            Assert.NotNull(headers);

            int key1Value;
            string key2Value;
            bool key3Value;
            double key4Value;
            int[] key5Value;
            byte[] key6Value;
            TestClass key7Value;
            Dictionary<int, string> key8Value;
            string key9Value;

            if (isReturned)
            {
                key1Value = JsonSerializer.Deserialize<int>((byte[])headers["Key1"]!);
                key2Value = JsonSerializer.Deserialize<string>((byte[])headers["Key2"]!)!;
                key3Value = JsonSerializer.Deserialize<bool>((byte[])headers["Key3"]!);
                key4Value = JsonSerializer.Deserialize<double>((byte[])headers["Key4"]!);
                key5Value = JsonSerializer.Deserialize<int[]>((byte[])headers["Key5"]!)!;
                key6Value = JsonSerializer.Deserialize<byte[]>((byte[])headers["Key6"]!)!;
                key7Value = JsonSerializer.Deserialize<TestClass>((byte[])headers["Key7"]!)!;
                key8Value = JsonSerializer.Deserialize<Dictionary<int, string>>((byte[])headers["Key8"]!)!;
                key9Value = JsonSerializer.Deserialize<string>((byte[])headers["Key9"]!)!;
            }
            else
            {
                key1Value = JsonSerializer.Deserialize<int>((string)headers["Key1"]!);
                key2Value = JsonSerializer.Deserialize<string>((string)headers["Key2"]!)!;
                key3Value = JsonSerializer.Deserialize<bool>((string)headers["Key3"]!);
                key4Value = JsonSerializer.Deserialize<double>((string)headers["Key4"]!);
                key5Value = JsonSerializer.Deserialize<int[]>((string)headers["Key5"]!)!;
                key6Value = JsonSerializer.Deserialize<byte[]>((string)headers["Key6"]!)!;
                key7Value = JsonSerializer.Deserialize<TestClass>((string)headers["Key7"]!)!;
                key8Value = JsonSerializer.Deserialize<Dictionary<int, string>>((string)headers["Key8"]!)!;
                key9Value = JsonSerializer.Deserialize<string>((string)headers["Key9"]!)!;
            }

            Assert.Equal((int)TestHeaders["Key1"]!, key1Value);
            Assert.Equal((string)TestHeaders["Key2"]!, key2Value);
            Assert.Equal((bool)TestHeaders["Key3"]!, key3Value);
            Assert.Equal((double)TestHeaders["Key4"]!, key4Value);
            Assert.True(((int[])TestHeaders["Key5"]!).SequenceEqual(key5Value), "Key5 header differs.");
            Assert.True(((byte[])TestHeaders["Key6"]!).SequenceEqual(key6Value), "Key6 header differs.");
            Assert.Equal(((TestClass)TestHeaders["Key7"]!).IntValue, key7Value.IntValue);
            Assert.Equal(((TestClass)TestHeaders["Key7"]!).StrValue, key7Value.StrValue);
            Assert.Equal(((Dictionary<int, string>)TestHeaders["Key8"]!)[1], key8Value[1]);
            Assert.Equal(((Dictionary<int, string>)TestHeaders["Key8"]!)[2], key8Value[2]);
            Assert.Equal((string)TestHeaders["Key9"]!, key9Value);
        }

        private class TestPublisher : Publisher
        {
            public TestPublisher() : base("TestPublisher", 3)
            {
            }

            protected override async Task DeclareExchangesAsync(CancellationToken cancellationToken = default)
            {
                await Channel!.ExchangeDeclareAsync(
                    exchange: _normalExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                await Channel.ExchangeDeclareAsync(
                    exchange: _rejectExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);
                
                await Channel.ExchangeDeclareAsync(
                    exchange: _noQueueExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                // Declare queues for test
                await Channel.QueueDeclareAsync(
                    queue: _normalQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
                await Channel.QueueBindAsync(
                    queue: _normalQueue,
                    exchange: _normalExchange,
                    routingKey: _normalRouting);

                await Channel.QueueDeclareAsync(
                    queue: _rejectQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?>()
                    {
                        { "x-max-length", 0 },
                        { "x-overflow", "reject-publish" }
                    });
                await Channel.QueueBindAsync(
                    queue: _rejectQueue,
                    exchange: _rejectExchange,
                    routingKey: _rejectRouting);
            }
        }

        public class TestClass
        {
            public int IntValue { get; set; } = 5;
            public string StrValue { get; set; } = "StrValue";
        }
    }
}
