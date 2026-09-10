using Shared.Test.Helpers.Fixtures;
using Shared.Test.Integration.Http.Authentication;
using Shared.Test.Integration.Http.DependencyInjection;
using Shared.Test.Integration.Http.Response.Middlewares;

namespace Shared.Test.Integration.Http.Collections
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
                addConfiguration: null,
                configureServices: (services, configuration) =>
                {
                    ApiGatewayAuthHandlerTest.ConfigureServices(services);
                    AddValidatorTest.ConfigureServices(services);
                    ValidationFiltersTest.ConfigureServices(services);
                },
                configureApp: app =>
                {
                    AuthErrorResponseMiddlewareTest.ConfigureApp(app);
                    ApiGatewayAuthHandlerTest.ConfigureApp(app);
                },
                configureEndpoints: endpoints =>
                {
                    AuthErrorResponseMiddlewareTest.ConfigureEndpoints(endpoints);
                    ApiGatewayAuthHandlerTest.ConfigureEndpoints(endpoints);
                    ValidationFiltersTest.ConfigureEndpoints(endpoints);
                }
            );
        }

        public async Task DisposeAsync()
        {
            await TestHostFixture.DisposeAsync();
        }
    }
}
