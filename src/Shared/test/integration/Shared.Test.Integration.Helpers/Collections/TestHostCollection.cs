using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.Helpers.Options;

namespace Shared.Test.Integration.Helpers.Collections
{
    [CollectionDefinition(nameof(TestHostCollection))]
    public class TestHostCollection : ICollectionFixture<TestHostCollectionCluster>
    {
    }

    public class TestHostCollectionCluster : IAsyncLifetime
    {
        public TestHostFixture TestHostFixture { get; private set; }

        public TestHostCollectionCluster()
        {
            TestHostFixture = new TestHostFixture();
        }

        public async Task InitializeAsync()
        {
            await TestHostFixture.InitializeAsync(
                addConfiguration: builder =>
                {
                    AddOptionsTest.AddConfiguration(builder);
                },
                configureServices: (services, configuration) =>
                {
                    AddOptionsTest.ConfigureServices(services, configuration);
                },
                configureApp: null,
                configureEndpoints: null
            );
        }

        public async Task DisposeAsync()
        {
            await TestHostFixture.DisposeAsync();
        }
    }
}
