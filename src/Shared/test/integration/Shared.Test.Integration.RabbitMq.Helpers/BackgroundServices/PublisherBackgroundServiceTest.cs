using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.RabbitMq.Helpers;
using Shared.RabbitMq.Helpers.BackgroundServices;
using Shared.RabbitMq.Helpers.Exceptions;
using Shared.RabbitMq.Helpers.Structs;
using Shared.Test.Generators;
using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.RabbitMq.Helpers.Collections;
using Shared.Test.Integration.RabbitMq.Helpers.Fixtures;

namespace Shared.Test.Integration.RabbitMq.Helpers.BackgroundServices
{
    [Collection(nameof(RabbitMqCollection))]
    public class PublisherBackgroundServiceTest : IClassFixture<LoggerFixture<PublisherBackgroundServiceTest>>
    {
        private readonly TimeSpan TimeoutSpan = TimeSpan.FromSeconds(5);
        private readonly TimeSpan RetryTime = TimeSpan.FromSeconds(1);

        private const int _maxRetry = 3;

        private const string _exchangeName = "test.publisher.background.exchange";
        private const string _routingKey = "test.publisher.background.routing";
        private const string _queueName = "test.publisher.background.queue";

        private readonly RabbitMqFixture _rabbitMqFixture;
        private readonly LoggerFixture<PublisherBackgroundServiceTest> _logger;

        public PublisherBackgroundServiceTest(
            RabbitMqFixture rabbitMqFixture,
            LoggerFixture<PublisherBackgroundServiceTest> logger)
        {
            _rabbitMqFixture = rabbitMqFixture;
            _logger = logger;
        }

        [Fact]
        public async Task StartAsync_WhenMethodIsCalled_ShouldCreateConnnectionAndChannels()
        {
            // Arrange
            var publisher = new TestPublisher();
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                new List<Publisher>() { publisher },
                RetryTime,
                _logger
            );

            // Act
            await backgroundService.StartAsync(default);

            // Assert
            Assert.NotNull(GetConnection(backgroundService));
            Assert.NotNull(publisher.Channel);
        }

        [Fact]
        public async Task ExecuteAsync_WhenConnectionIsNull_ShouldThrowExceptionBeforeMainLoopStarts()
        {
            // Arrange
            var publisher = new TestPublisher();
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                new List<Publisher>() { publisher },
                RetryTime,
                _logger
            );

            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await ExecuteAsync(backgroundService).WaitAsync(TimeoutSpan);
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ConnectionNotEstablishedException>(exception);
        }

        [Fact]
        public async Task ExecuteAsync_WhenThereIsNotAcknowledgedAndDroppedMessages_ShouldRetryPublishing()
        {
            // Arrange
            var publisher = new TestPublisher();
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                new List<Publisher>() { publisher },
                RetryTime,
                _logger
            );

            var pendingMessage = new Message(
                "TestPublisher",
                _exchangeName,
                _routingKey,
                new BasicProperties() { CorrelationId = Guid.NewGuid().ToString() },
                JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GeneratePrintableAscii()));
            SetIsPending(pendingMessage, false);

            var droppedMessage = new Message(
                "TestPublisher",
                _exchangeName,
                _routingKey,
                new BasicProperties() { CorrelationId = Guid.NewGuid().ToString() },
                JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GeneratePrintableAscii()));

            GetPendingMessages(publisher).TryAdd(0, pendingMessage);
            GetDroppedMessages(publisher).TryAdd(droppedMessage.GetHashCode(), droppedMessage);

            await InitializeBackgroundServiceAndPublisher(backgroundService, publisher);

            // Act
            try
            {
                await ExecuteAsync(backgroundService).WaitAsync(TimeoutSpan);
            }
            catch { }

            // Assert
            Assert.Empty(GetPendingMessages(publisher));
            Assert.Empty(GetDroppedMessages(publisher));

            var channel = await _rabbitMqFixture.Connection.CreateChannelAsync();
            var queue = await channel.QueueDeclarePassiveAsync(_queueName);
            Assert.Equal((uint)2, queue.MessageCount);

            await channel.QueuePurgeAsync(_queueName);
        }

        [Fact]
        public async Task ExecuteAsync_WhenThereIsMessagesThatExceedRetryLimit_ShouldRemovedFromDictionaryAndPutIntoRejectedListAndCallSaveRejectedMessages()
        {
            // Arrange
            var publisher = new TestPublisher();
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                new List<Publisher>() { publisher },
                RetryTime,
                _logger
            );

            var pendingMessage = new Message(
                "TestPublisher",
                _exchangeName,
                _routingKey,
                new BasicProperties() { CorrelationId = Guid.NewGuid().ToString() },
                JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GeneratePrintableAscii()));
            SetIsPending(pendingMessage, false);
            SetRetryCount(pendingMessage, _maxRetry);

            var droppedMessage = new Message(
                "TestPublisher",
                _exchangeName,
                _routingKey,
                new BasicProperties() { CorrelationId = Guid.NewGuid().ToString() },
                JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GeneratePrintableAscii()));
            SetRetryCount(droppedMessage, _maxRetry);

            GetPendingMessages(publisher).TryAdd(0, pendingMessage);
            GetDroppedMessages(publisher).TryAdd(droppedMessage.GetHashCode(), droppedMessage);

            await InitializeBackgroundServiceAndPublisher(backgroundService, publisher);

            // Act
            try
            {
                await ExecuteAsync(backgroundService).WaitAsync(TimeoutSpan);
            }
            catch { }

            // Assert
            Assert.Empty(GetPendingMessages(publisher));
            Assert.Empty(GetDroppedMessages(publisher));

            Assert.Equal(2, backgroundService.RejectedMessages.Count);
            Assert.Contains(pendingMessage.GetHashCode(), backgroundService.RejectedMessages.Select(m => m.GetHashCode()));
            Assert.Contains(droppedMessage.GetHashCode(), backgroundService.RejectedMessages.Select(m => m.GetHashCode()));
            Assert.False(backgroundService.IsShuttingDown, "The shutdown parameter was given as true.");
        }

        [Fact]
        public async Task ExecuteAsync_WhenApplicationShutsDown_ShouldCombineLeftOverMessagesAndCallSaveRejectedMessagesAndCloseConnectionAndChannels()
        {
            // Arrange
            var publisher = new TestPublisher();
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                new List<Publisher>() { publisher },
                RetryTime,
                _logger
            );

            var pendingMessage = new Message(
                "TestPublisher",
                _exchangeName,
                _routingKey,
                new BasicProperties() { CorrelationId = Guid.NewGuid().ToString() },
                JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GeneratePrintableAscii()));
            var droppedMessage = new Message(
                "TestPublisher",
                _exchangeName,
                _routingKey,
                new BasicProperties() { CorrelationId = Guid.NewGuid().ToString() },
                JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GeneratePrintableAscii()));
            var rejectedMessage = new Message(
                "TestPublisher",
                _exchangeName,
                _routingKey,
                new BasicProperties() { CorrelationId = Guid.NewGuid().ToString() },
                JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GeneratePrintableAscii()));

            GetPendingMessages(publisher).TryAdd(0, pendingMessage);
            GetDroppedMessages(publisher).TryAdd(droppedMessage.GetHashCode(), droppedMessage);
            GetRejectedMessages(backgroundService).TryAdd(rejectedMessage.GetHashCode(), rejectedMessage);

            await InitializeBackgroundServiceAndPublisher(backgroundService, publisher);

            // Act
            await backgroundService.StopAsync(default);

            // Assert
            Assert.Equal(3, backgroundService.RejectedMessages.Count);
            Assert.Contains(pendingMessage.GetHashCode(), backgroundService.RejectedMessages.Select(m => m.GetHashCode()));
            Assert.Contains(droppedMessage.GetHashCode(), backgroundService.RejectedMessages.Select(m => m.GetHashCode()));
            Assert.Contains(rejectedMessage.GetHashCode(), backgroundService.RejectedMessages.Select(m => m.GetHashCode()));
            Assert.True(backgroundService.IsShuttingDown, "The shutdown parameter was given as true.");

            Assert.False(GetConnection(backgroundService).IsOpen, "The connection is still open.");
            Assert.False(publisher.Channel!.IsOpen, "The channel is still open.");
        }

        private async Task InitializeBackgroundServiceAndPublisher(TestBackgroundService backgroundService, TestPublisher publisher)
        {
            // Backgroung service
            var connFactoryfieldInfo = typeof(PublisherBackgroundService).GetField("_connectionFactory", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var connFactory = (ConnectionFactory)connFactoryfieldInfo.GetValue(backgroundService)!;
            var connection = await connFactory.CreateConnectionAsync();

            var connectionFieldInfo = typeof(PublisherBackgroundService).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!;
            connectionFieldInfo.SetValue(backgroundService, connection);

            // Publisher
            var channel = await connection.CreateChannelAsync(new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: false));

            var acknowledgeMethodInfo = typeof(Publisher).GetMethod("HandleAcknowledgedMessages", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var acknowledgeEvent = Delegate.CreateDelegate(typeof(AsyncEventHandler<BasicAckEventArgs>), publisher, acknowledgeMethodInfo);
            var notAcknowledgeMethodInfo = typeof(Publisher).GetMethod("HandleNotAcknowledgedMessages", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var notAcknowledgeEvent = Delegate.CreateDelegate(typeof(AsyncEventHandler<BasicNackEventArgs>), publisher, notAcknowledgeMethodInfo);
            var returnMethodInfo = typeof(Publisher).GetMethod("HandleReturnedMessages", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var returnEvent = Delegate.CreateDelegate(typeof(AsyncEventHandler<BasicReturnEventArgs>), publisher, returnMethodInfo);

            channel.BasicAcksAsync += (AsyncEventHandler<BasicAckEventArgs>)acknowledgeEvent;
            channel.BasicNacksAsync += (AsyncEventHandler<BasicNackEventArgs>)notAcknowledgeEvent;
            channel.BasicReturnAsync += (AsyncEventHandler<BasicReturnEventArgs>)returnEvent;

            var declareExchangeMethodInfo = typeof(Publisher).GetMethod("DeclareExchangesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)declareExchangeMethodInfo.Invoke(publisher, [channel, default])!;

            var channelPropertyInfo = typeof(Publisher).GetProperty("Channel", BindingFlags.NonPublic | BindingFlags.Instance)!;
            channelPropertyInfo.SetValue(publisher, channel);
        }

        private IConnection GetConnection(TestBackgroundService backgroundService)
        {
            var fieldInfo = typeof(PublisherBackgroundService).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (IConnection)fieldInfo.GetValue(backgroundService)!;
        }

        private Task ExecuteAsync(TestBackgroundService backgroundService)
        {
            var methodInfo = typeof(PublisherBackgroundService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Task)methodInfo.Invoke(backgroundService, [default])!;
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

        private void SetIsPending(Message message, bool value)
        {
            var propertyInfo = typeof(Message).GetProperty("IsPending", BindingFlags.NonPublic | BindingFlags.Instance)!;
            propertyInfo.SetValue(message, value);
        }

        private void SetRetryCount(Message message, int value)
        {
            var propertyInfo = typeof(Message).GetProperty("RetryCount", BindingFlags.NonPublic | BindingFlags.Instance)!;
            propertyInfo.SetValue(message, value);
        }

        private Dictionary<int, Message> GetRejectedMessages(TestBackgroundService backgroundService)
        {
            var fieldInfo = typeof(PublisherBackgroundService).GetField("_rejectedMessages", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Dictionary<int, Message>)fieldInfo.GetValue(backgroundService)!;
        }

        private class TestBackgroundService : PublisherBackgroundService
        {
            public List<Message> RejectedMessages { get; private set; }
            public bool IsShuttingDown { get; private set; }

            public TestBackgroundService(
                ConnectionFactory connectionFactory,
                List<Publisher> publishers,
                TimeSpan publishRetryTime,
                ILogger logger)
                : base(connectionFactory, publishers, publishRetryTime, logger)
            {
                RejectedMessages = new List<Message>();
            }

            protected override async Task SaveRejectedMessagesAsync(
                IEnumerable<Message> rejectedMessages,
                bool isShuttingDown,
                CancellationToken cancellationToken = default)
            {
                RejectedMessages.AddRange(rejectedMessages);
                IsShuttingDown = isShuttingDown;
            }
        }

        private class TestPublisher : Publisher
        {
            public TestPublisher() : base("TestPublisher", _maxRetry)
            {
            }

            protected override async Task DeclareExchangesAsync(IChannel channel, CancellationToken cancellationToken = default)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                // Declare queues for test
                await channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
                await channel.QueueBindAsync(
                    queue: _queueName,
                    exchange: _exchangeName,
                    routingKey: _routingKey);
            }
        }
    }
}
