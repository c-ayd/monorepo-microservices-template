using Shared.Test.Helpers.Fixtures;

namespace AuthService.Test.Integration.Persistence.Collections
{
    [CollectionDefinition(nameof(TokenBlacklistCollection))]
    public class TokenBlacklistCollection : ICollectionFixture<TokenBlacklistCollectionCluster>
    {
    }

    public class TokenBlacklistCollectionCluster : IAsyncLifetime
    {
        public RedisFixture TokenBlacklistRedisFixture { get; private set; }

        public TokenBlacklistCollectionCluster()
        {
            TokenBlacklistRedisFixture = new RedisFixture();
        }

        public async Task InitializeAsync()
        {
            await TokenBlacklistRedisFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await TokenBlacklistRedisFixture.DisposeAsync();
        }
    }
}
