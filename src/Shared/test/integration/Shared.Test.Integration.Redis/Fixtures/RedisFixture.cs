using DotNet.Testcontainers.Builders;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Shared.Test.Integration.Redis.Fixtures
{
    public class RedisFixture : IAsyncLifetime
    {
        private RedisContainer _container = null!;

        private ConnectionMultiplexer _connection = null!;
        public IDatabase Database { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            _container = new RedisBuilder("redis:8.10")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
                .Build();

            await _container.StartAsync();

            _connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
            Database = _connection.GetDatabase();
        }

        public async Task DisposeAsync()
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();

            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
