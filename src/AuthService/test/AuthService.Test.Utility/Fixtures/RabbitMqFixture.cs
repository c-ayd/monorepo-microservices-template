using AuthService.Application.Options;
using DotNet.Testcontainers.Builders;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace AuthService.Test.Utility.Fixtures
{
    public class RabbitMqFixture : IAsyncLifetime
    {
        private RabbitMqContainer _container = null!;

        public IConnection Connection { get; private set; } = null!;

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

            Connection = await factory.CreateConnectionAsync();
        }

        public RabbitMqOptions GetRabbitMqOptions()
        {
            return new RabbitMqOptions()
            {
                Username = "guest",
                Password = "guest",
                Host = _container.Hostname,
                Port = _container.GetMappedPublicPort(5672),
            };
        }

        public async Task DisposeAsync()
        {
            await Connection.CloseAsync();
            await Connection.DisposeAsync();

            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
