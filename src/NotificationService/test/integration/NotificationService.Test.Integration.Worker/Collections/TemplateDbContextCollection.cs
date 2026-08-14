using NotificationService.Test.Integration.Worker.Fixtures;

namespace NotificationService.Test.Integration.Worker.Collections
{
    [CollectionDefinition(nameof(TemplateDbContextCollection))]
    public class TemplateDbContextCollection : ICollectionFixture<TemplateDbContextFixture>
    {
    }
}
