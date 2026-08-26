using AuthService.Test.Utility.Fixtures;

namespace AuthService.Test.Integration.Infrastructure.Collections
{
    [CollectionDefinition(nameof(RabbitMqCollection))]
    public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
    {
    }
}
