using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.RabbitMq.Helpers.BackgroundServices;
using Shared.Test.Generators;
using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.RabbitMq.Helpers.Collections;
using Shared.Test.Integration.RabbitMq.Helpers.Fixtures;

namespace Shared.Test.Integration.RabbitMq.Helpers.BackgroundServices
{
    [Collection(nameof(RabbitMqCollection))]
    public class ConsumerBackgroundServiceTest : IClassFixture<LoggerFixture<ConsumerBackgroundServiceTest>>
    {
        private readonly TimeSpan _timeoutTime = TimeSpan.FromSeconds(5);

        private const string _exchangeName = "test.consumer.background.exchange";
        private const string _routingKey = "test.consumer.background.routing";
        private const string _queueName = "test.consumer.background.queue";

        private readonly RabbitMqFixture _rabbitMqFixture;
        private readonly LoggerFixture<ConsumerBackgroundServiceTest> _loggerFixture;

        public ConsumerBackgroundServiceTest(
            RabbitMqFixture rabbitMqFixture,
            LoggerFixture<ConsumerBackgroundServiceTest> loggerFixture)
        {
            _rabbitMqFixture = rabbitMqFixture;
            _loggerFixture = loggerFixture;
        }

        [Fact]
        public async Task StartAsync_WhenMethodIsCalled_ShouldCreateConnnectionAndChannelAndConsumer()
        {
            // Arrange
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                TimeSpan.FromSeconds(60),
                _queueName,
                1,
                _loggerFixture);

            // Act
            await backgroundService.StartAsync(default);

            // Assert
            Assert.NotNull(GetConnection(backgroundService));
            Assert.NotNull(backgroundService.Channel);
            Assert.NotNull(GetConsumerTag(backgroundService));
        }

        [Fact]
        public async Task ExecuteAsync_WhenConnectionIsStable_ShouldCallReceivedEvent()
        {
            // Arrange
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                TimeSpan.FromSeconds(1),
                _queueName,
                1,
                _loggerFixture);

            await InitializeBackgroundService(backgroundService);

            var consumerTag = GetConsumerTag(backgroundService);

            var message = StringGenerator.GeneratePrintableAscii();
            await _rabbitMqFixture.PublishMessageAsync(
                _exchangeName,
                _routingKey,
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Act
            try
            {
                await ExecuteAsync(backgroundService).WaitAsync(_timeoutTime);
            }
            catch { }

            // Assert
            Assert.Equal(consumerTag, GetConsumerTag(backgroundService));
            Assert.Equal(message, backgroundService.Message);

            backgroundService.Message = null;
            await _rabbitMqFixture.ClearQueue(_queueName);
        }
        
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task ExecuteAsync_WhenConnectionOrChannelIsClosed_ShouldRenewConnectionOrChannel(bool isConnectionClosed, bool isChannelClosed)
        {
            // Arrange
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                TimeSpan.FromSeconds(1),
                _queueName,
                1,
                _loggerFixture);

            await InitializeBackgroundService(backgroundService);

            var consumerTag = GetConsumerTag(backgroundService);

            if (isConnectionClosed)
            {
                SetConnection(backgroundService, null);
            }
            else if (isChannelClosed)
            {
                SetChannel(backgroundService, null);
            }

            // Act
            try
            {
                await ExecuteAsync(backgroundService).WaitAsync(_timeoutTime);
            }
            catch { }

            // Assert
            Assert.NotEqual(consumerTag, GetConsumerTag(backgroundService));
            Assert.NotNull(GetConnection(backgroundService));
            Assert.NotNull(backgroundService.Channel);
        }

        [Fact]
        public async Task StopAsync_WhenApplicationShutsDown_ShouldCloseConnectionAndChannel()
        {
            // Arrange
            var backgroundService = new TestBackgroundService(
                _rabbitMqFixture.CreateConnectionFactory(),
                TimeSpan.FromSeconds(1),
                _queueName,
                1,
                _loggerFixture);

            await InitializeBackgroundService(backgroundService);

            // Act
            await backgroundService.StopAsync(default);

            // Assert
            Assert.False(GetConnection(backgroundService).IsOpen, "The connection is still open.");
            Assert.False(backgroundService.Channel!.IsOpen, "The channel is still open.");
        }

        private async Task InitializeBackgroundService(TestBackgroundService backgroundService)
        {
            var initializeConnectionMethodInfo = typeof(ConsumerBackgroundService).GetMethod("InitializeConnectionAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)initializeConnectionMethodInfo.Invoke(backgroundService, [default])!;
        }

        private IConnection GetConnection(TestBackgroundService backgroundService)
        {
            var fieldInfo = typeof(ConsumerBackgroundService).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (IConnection)fieldInfo.GetValue(backgroundService)!;
        }

        private void SetConnection(TestBackgroundService backgroundService, IConnection? value)
        {
            var fieldInfo = typeof(ConsumerBackgroundService).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!;
            fieldInfo.SetValue(backgroundService, value);
        }

        private void SetChannel(TestBackgroundService backgroundService, IChannel? value)
        {
            var propertyInfo = typeof(ConsumerBackgroundService).GetProperty("Channel", BindingFlags.Public | BindingFlags.Instance)!;
            propertyInfo.SetValue(backgroundService, value);
        }

        private string GetConsumerTag(TestBackgroundService backgroundService)
        {
            var fieldInfo = typeof(ConsumerBackgroundService).GetField("_consumerTag", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (string)fieldInfo.GetValue(backgroundService)!;
        }

        private Task ExecuteAsync(TestBackgroundService backgroundService)
        {
            var methodInfo = typeof(ConsumerBackgroundService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Task)methodInfo.Invoke(backgroundService, [default])!;
        }

        private class TestBackgroundService : ConsumerBackgroundService
        {
            public string? Message { get; set; }

            public TestBackgroundService(
                ConnectionFactory connectionFactory,
                TimeSpan connectionCheckTime,
                string queueName,
                ushort prefetchCount,
                ILogger logger)
                : base(connectionFactory, connectionCheckTime, queueName, prefetchCount, logger)
            {
            }

            protected override async Task DeclareExchangesAsync(CancellationToken cancellationToken)
            {
                await Channel!.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);
            }

            protected override async Task DeclareQueuesAsync(CancellationToken cancellationToken)
            {
                await Channel!.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
                await Channel.QueueBindAsync(
                    queue: _queueName,
                    exchange: _exchangeName,
                    routingKey: _routingKey);
            }

            protected override async Task ReceivedAsync(object obj, BasicDeliverEventArgs args)
            {
                Message = JsonSerializer.Deserialize<string>(args.Body.ToArray());

                await Channel!.BasicAckAsync(args.DeliveryTag, false);
            }
        }
    }
}
