using System.Text.Json;
using DotNet.Testcontainers.Builders;
using NotificationService.Worker.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace NotificationService.Test.Integration.Worker.Fixtures
{
    public class RabbitMqFixture : IAsyncLifetime
    {
        private RabbitMqContainer _container = null!;

        private IConnection _connection = null!;
        private IChannel _channel = null!;

        public async Task InitializeAsync()
        {
            _container = new RabbitMqBuilder("rabbitmq:4.3.4-management")
                .WithPortBinding(5672, true)
                .WithPortBinding(15672, true)
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*Server startup complete.*"))
                .Build();
            await _container.StartAsync();

            var rabbitMqOptions = GetRabbitMqOptions();
            var factory = new ConnectionFactory()
            {
                UserName = rabbitMqOptions.Username,
                Password = rabbitMqOptions.Password,
                HostName = rabbitMqOptions.Host,
                Port = rabbitMqOptions.Port,
                VirtualHost = "/"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync(new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ));
        }

        public RabbitMqOptions GetRabbitMqOptions()
        {
            return new RabbitMqOptions()
            {
                Username = "guest",
                Password = "guest",
                Host = "localhost",
                Port = _container.GetMappedPublicPort(5672)
            };
        }

        public async Task PublishMessageAsync<T>(T message, string exchangeName, string routingKey, int timeoutInSeconds)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                body: JsonSerializer.SerializeToUtf8Bytes(message),
                cancellationToken: cts.Token
            );
        }

        public async Task<QueueDeclareOk> GetQueueInfo(string queueName)
        {
            return await _channel.QueueDeclarePassiveAsync(queueName);
        }

        public async Task ClearQueue(string queueName)
        {
            await _channel.QueuePurgeAsync(queueName);
        }

        public async Task DisposeAsync()
        {
            await _channel.CloseAsync();
            await _connection.CloseAsync();
            await _container.StopAsync();

            await _channel.DisposeAsync();
            await _connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }
}
