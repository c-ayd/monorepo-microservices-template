using Shared.Test.Helpers.Fixtures;

namespace Shared.Test.Integration.RabbitMq.Helpers.Collections
{
    [CollectionDefinition(nameof(RabbitMqCollection))]
    public class RabbitMqCollection : ICollectionFixture<RabbitMqCollectionCluster>
    {
    }

    public class RabbitMqCollectionCluster : IAsyncLifetime
    {
        public RabbitMqFixture RabbitMqFixture { get; private set; }

        public RabbitMqCollectionCluster()
        {
            RabbitMqFixture = new RabbitMqFixture();
        }

        public async Task InitializeAsync()
        {
            await RabbitMqFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await RabbitMqFixture.DisposeAsync();
        }
    }
}
