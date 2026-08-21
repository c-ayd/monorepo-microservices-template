using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shared.Test.Integration.Http.Fixtures
{
    public class TestHostFixture : IAsyncLifetime
    {
        public IHost? TestHost { get; private set; }
        public HttpClient? Client { get; private set; }

        public async Task InitializeAsync()
        {
        }

        public async Task BuildAsync(Action<IServiceCollection>? configureServices, Action<IApplicationBuilder>? configureApp)
        {
            TestHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(config =>
                {
                    config.UseTestServer();
                    config.ConfigureServices(services =>
                    {
                        configureServices?.Invoke(services);
                    });
                    config.Configure(app =>
                    {
                        configureApp?.Invoke(app);
                    });
                })
                .Build();
            
            await TestHost.StartAsync();

            Client = TestHost.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            Client?.Dispose();

            if (TestHost != null)
            {
                await TestHost.StopAsync();
                TestHost?.Dispose();
            }
        }
    }
}
