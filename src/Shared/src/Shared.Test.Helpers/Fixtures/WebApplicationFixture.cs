using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis.Configuration;

namespace Shared.Test.Helpers.Fixtures
{
    /// <summary>
    /// Is a web application factory to centralize common functionalities for test cases.
    /// </summary>
    /// <typeparam name="TEntryPoint">Entry point of the web application, usually the main Program.cs</typeparam>
    public class WebApplicationFixture<TEntryPoint> : WebApplicationFactory<TEntryPoint>
        where TEntryPoint : class
    {
        protected List<HttpClient> HttpClients { get; private set; } = new List<HttpClient>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddConfiguration(ConfigurationHelper.CreateConfigurationFromTestSettings());
            });
        }

        public HttpClient CreateHttpClient()
        {
            var client = CreateClient();
            HttpClients.Add(client);
            
            return client;
        }

        public TService GetService<TService>()
            where TService : notnull
        {
            return Services.GetRequiredService<TService>();
        }

        public TBackgroundService GetBackgroundService<TBackgroundService>()
        {
            return Services.GetServices<IHostedService>()
                .OfType<TBackgroundService>()
                .Single();
        }

        public TOptions GetOptions<TOptions>()
            where TOptions: class
        {
            return Services.GetRequiredService<IOptions<TOptions>>().Value;
        }

        public override ValueTask DisposeAsync()
        {
            foreach (var client in HttpClients)
            {
                client.Dispose();
            }
            
            return base.DisposeAsync();
        }
    }
}
