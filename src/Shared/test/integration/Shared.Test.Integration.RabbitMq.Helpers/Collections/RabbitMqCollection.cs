using Shared.Test.Integration.RabbitMq.Helpers.Fixtures;

namespace Shared.Test.Integration.RabbitMq.Helpers.Collections
{
    [CollectionDefinition(nameof(RabbitMqCollection))]
    public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
    {
    }
}
