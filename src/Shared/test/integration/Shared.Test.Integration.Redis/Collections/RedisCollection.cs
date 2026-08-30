using Shared.Test.Integration.Redis.Fixtures;

namespace Shared.Test.Integration.Redis.Collections
{
    [CollectionDefinition(nameof(RedisCollection))]
    public class RedisCollection : ICollectionFixture<RedisFixture>
    {
    }
}
