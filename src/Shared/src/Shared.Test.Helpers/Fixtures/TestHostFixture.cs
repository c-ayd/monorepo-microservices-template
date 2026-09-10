using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shared.Test.Helpers.Fixtures
{
    /// <summary>
    /// Is a test host for test cases that do not have a web project to start but require a web host.
    /// </summary>
    public class TestHostFixture
    {
        public IHost Host { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync(
            Action<IConfigurationBuilder>? addConfiguration,
            Action<IServiceCollection, IConfiguration>? configureServices,
            Action<IApplicationBuilder>? configureApp,
            Action<IEndpointRouteBuilder>? configureEndpoints)
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    addConfiguration?.Invoke(builder);
                })
                .ConfigureWebHostDefaults(config =>
                {
                    config.UseTestServer();
                    config.ConfigureServices((context, services) =>
                    {
                        configureServices?.Invoke(services, context.Configuration);
                    });
                    config.Configure(app =>
                    {
                        app.UseRouting();

                        configureApp?.Invoke(app);
                        app.UseEndpoints(endpoints =>
                        {
                            configureEndpoints?.Invoke(endpoints);
                        });
                    });
                })
                .Build();
            await Host.StartAsync();

            Client = Host.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            if (Client != null)
            {
                Client.Dispose();
            }

            if (Host != null)
            {
                await Host.StopAsync();
                Host.Dispose();
            }
        }
    }
}
