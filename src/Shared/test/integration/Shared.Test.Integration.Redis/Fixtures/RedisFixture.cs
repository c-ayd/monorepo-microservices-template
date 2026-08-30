using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Testcontainers.Redis;

namespace Shared.Test.Integration.Redis.Fixtures
{
    public class RedisFixture : IAsyncLifetime
    {
        private RedisContainer _container = null!;

        public IDistributedCache Redis { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            _container = new RedisBuilder("redis:8.10")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
                .Build();

            await _container.StartAsync();

            Redis = new RedisCache(Options.Create(new RedisCacheOptions()
            {
                Configuration = _container.GetConnectionString()
            }));
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
