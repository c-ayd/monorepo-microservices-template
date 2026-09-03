using DotNet.Testcontainers.Builders;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Shared.Test.Integration.RabbitMq.Helpers.Fixtures
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

            Connection = await CreateConnectionFactory().CreateConnectionAsync();
        }

        public ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory()
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost",
                Port = _container.GetMappedPublicPort(5672)
            };
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
