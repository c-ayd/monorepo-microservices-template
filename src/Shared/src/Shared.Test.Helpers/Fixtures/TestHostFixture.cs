using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
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
        private int _port;
        private string? _baseUrl;

        private List<HttpClient> _httpClients = new List<HttpClient>();

        public async Task InitializeAsync(
            Action<IConfigurationBuilder>? addConfiguration,
            Action<IServiceCollection, IConfiguration>? configureServices,
            Action<IApplicationBuilder>? configureApp,
            Action<IEndpointRouteBuilder>? configureEndpoints,
            int? port = null)
        {
            _port = port ?? Random.Shared.Next(6000, 7000);
            _baseUrl = $"http://localhost:{_port}";

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    addConfiguration?.Invoke(builder);
                })
                .ConfigureWebHostDefaults(config =>
                {
                    config.UseUrls(_baseUrl);

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
        }

        public string GetUrl()
        {
            return _baseUrl!;
        }

        public HttpClient CreateHttpClient()
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(_baseUrl!)
            };

            _httpClients.Add(client);

            return client;
        }

        public async Task DisposeAsync()
        {
            foreach (var client in _httpClients)
            {
                client.Dispose();
            }

            if (Host != null)
            {
                await Host.StopAsync();
                Host.Dispose();
            }
        }
    }
}
