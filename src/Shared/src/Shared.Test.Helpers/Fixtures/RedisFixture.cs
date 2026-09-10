using DotNet.Testcontainers.Builders;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Shared.Test.Helpers.Fixtures
{
    /// <summary>
    /// Is a Redis handler for test cases requiring Redis.
    /// </summary>
    public class RedisFixture
    {
        private RedisContainer _container = null!;
        private ConnectionMultiplexer _connection = null!;

        public async Task InitializeAsync()
        {
            _container = new RedisBuilder("redis:8.10")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
                .Build();
            await _container.StartAsync();

            _connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
        }

        public string GetConnectionString()
        {
            return _container.GetConnectionString();
        }

        public IDatabase GetDatabase(int db = -1)
        {
            return _connection.GetDatabase(db);
        }
        
        public async Task DisposeAsync()
        {
            await _connection.CloseAsync();
            await _container.StopAsync();

            await _connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }
}
