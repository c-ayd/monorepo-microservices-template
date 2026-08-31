using AuthService.Test.Utility.Fixtures;

namespace AuthService.Test.Integration.Persistence.Collections
{
    [CollectionDefinition(nameof(TokenBlacklistCollection))]
    public class TokenBlacklistCollection : ICollectionFixture<RedisFixture>
    {
    }
}
