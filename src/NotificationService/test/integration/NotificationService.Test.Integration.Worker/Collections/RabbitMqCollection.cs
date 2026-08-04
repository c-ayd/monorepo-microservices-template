using NotificationService.Test.Integration.Worker.Fixtures;

namespace NotificationService.Test.Integration.Worker.Collections
{
    [CollectionDefinition(nameof(RabbitMqCollection))]
    public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
    {
    }
}
