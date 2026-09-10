using Shared.Test.Helpers.Fixtures;

namespace Shared.Test.Integration.Redis.Collections
{
    [CollectionDefinition(nameof(RedisCollection))]
    public class RedisCollection : ICollectionFixture<RedisCollectionCluster>
    {
    }

    public class RedisCollectionCluster : IAsyncLifetime
    {
        public RedisFixture RedisFixture { get; private set; }

        public RedisCollectionCluster()
        {
            RedisFixture = new RedisFixture();
        }

        public async Task InitializeAsync()
        {
            await RedisFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await RedisFixture.DisposeAsync();
        }
    }
}
