using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.RabbitMq.Helpers;
using Shared.RabbitMq.Helpers.Structs;
using Shared.Test.Generators;
using Shared.Test.Integration.RabbitMq.Helpers.Collections;
using Shared.Test.Integration.RabbitMq.Helpers.Fixtures;

namespace Shared.Test.Integration.RabbitMq.Helpers
{
    [Collection(nameof(RabbitMqCollection))]
    public class PublisherTest
    {
        private const string _normalExchange = "test.publisher.exchange";
        private const string _rejectExchange = "test.publisher.exchange.reject";
        private const string _noQueueExchange = "test.publisher.exchange.no-queue";

        private const string _normalRouting = "test.publisher.routing.normal";
        private const string _rejectRouting = "test.publisher.routing.reject";

        public const string _normalQueue = "test.publisher.queue";
        public const string _rejectQueue = "test.publisher.queue.reject";

        private readonly TimeSpan TimeoutSpan = TimeSpan.FromSeconds(30);

        private readonly RabbitMqFixture _rabbitMqFixture;

        public PublisherTest(RabbitMqFixture rabbitMqFixture)
        {
            _rabbitMqFixture = rabbitMqFixture;
        }

        [Fact]
        public async Task PublishMessageAsync_WhenMessageIsNotRouted_ShouldAddMessageToDroppedMessages()
        {
            // Arrange
            var message = StringGenerator.GeneratePrintableAscii();
            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString()
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
        }

        [Fact]
        public async Task PublishMessageAsync_WhenMessageIsNotAcknowledged_ShouldSetMessagePendingToFalse()
        {
            // Arrange
            var message = StringGenerator.GeneratePrintableAscii();
            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString()
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
        public async Task PublishMessageAsync_WhenChannelIsNotOpen_ShouldAddMessageToDroppedMessages()
        {
            // Arrange
            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            var publisher = new TestPublisher();
            await InitializePublisherAsync(publisher);

            var channelPropertyInfo = typeof(Publisher).GetProperty("Channel", BindingFlags.NonPublic | BindingFlags.Instance)!;
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

        private class TestPublisher : Publisher
        {
            public TestPublisher() : base("TestPublisher", 3)
            {
            }

            protected override async Task DeclareExchangesAsync(IChannel channel, CancellationToken cancellationToken = default)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: _normalExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                await channel.ExchangeDeclareAsync(
                    exchange: _rejectExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);
                
                await channel.ExchangeDeclareAsync(
                    exchange: _noQueueExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                // Declare queues for test
                await channel.QueueDeclareAsync(
                    queue: _normalQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
                await channel.QueueBindAsync(
                    queue: _normalQueue,
                    exchange: _normalExchange,
                    routingKey: _normalRouting);

                await channel.QueueDeclareAsync(
                    queue: _rejectQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?>()
                    {
                        { "x-max-length", 0 },
                        { "x-overflow", "reject-publish" }
                    });
                await channel.QueueBindAsync(
                    queue: _rejectQueue,
                    exchange: _rejectExchange,
                    routingKey: _rejectRouting);
            }
        }
    }
}
