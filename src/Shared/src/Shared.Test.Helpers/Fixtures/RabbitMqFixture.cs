using DotNet.Testcontainers.Builders;
using RabbitMQ.Client;
using Shared.Test.Helpers.Structures;
using Testcontainers.RabbitMq;

namespace Shared.Test.Helpers.Fixtures
{
    /// <summary>
    /// Is a RabbitMQ handler for test cases requiring RabbitMQ.
    /// </summary>
    public class RabbitMqFixture
    {
        private RabbitMqContainer _container = null!;

        public ConnectionFactory ConnectionFactory { get; private set; } = null!;
        public IConnection Connection { get; private set; } = null!;
        public IChannel Channel { get; private set; } = null!;

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
            ConnectionFactory = new ConnectionFactory()
            {
                UserName = rabbitMqOptions.Username,
                Password = rabbitMqOptions.Password,
                HostName = rabbitMqOptions.Host,
                Port = rabbitMqOptions.Port,
            };
            Connection = await ConnectionFactory.CreateConnectionAsync();
            Channel = await Connection.CreateChannelAsync(new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true));
        }

        public RabbitMqFixtureOptions GetRabbitMqOptions()
        {
            return new RabbitMqFixtureOptions(
                Username: "guest",
                Password: "guest",
                Host: _container.Hostname,
                Port: _container.GetMappedPublicPort(5672)
            );
        }

        public async Task PublishMessageAsync(string exchangeName, string routingKey, BasicProperties properties, byte[] body)
        {
            await Channel.BasicPublishAsync(exchangeName, routingKey, mandatory: true, properties, body);
        }

        public async Task<BasicGetResult?> GetNextMessageAsync(string queueName, bool autoAck = true)
        {
            return await Channel.BasicGetAsync(queueName, autoAck);
        }

        public async Task<uint> GetMessageCountAsync(string queueName)
        {
            var queueInfo = await Channel.QueueDeclarePassiveAsync(queueName);
            return queueInfo.MessageCount;
        }

        public async Task ClearMessagesAsync(string queueName)
        {
            await Channel.QueuePurgeAsync(queueName);
        }

        public async Task DisposeAsync()
        {
            if (Channel.IsOpen)
            {
                await Channel.CloseAsync();
            }
            if (Connection.IsOpen)
            {
                await Connection.CloseAsync();
            }
            await _container.StopAsync();

            await Channel.DisposeAsync();
            await Connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }
}
