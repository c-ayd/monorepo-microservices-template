using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.Http.Authentication;

namespace Shared.Test.Helpers.Fixtures
{
    /// <summary>
    /// Is a web application factory to centralize common functionalities for test cases.
    /// </summary>
    /// <typeparam name="TEntryPoint">Entry point of the web application, usually the main Program.cs</typeparam>
    public class WebAppFactoryFixture<TEntryPoint> : WebApplicationFactory<TEntryPoint>
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

        public HttpClient CreateHttpClientWithCredentials(
            string accountId,
            string preferredLanguage = SupportedLanguages.DefaultLanguage,
            Dictionary<string, string>? cookieValues = null)
        {
            var client = CreateHttpClient();
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, accountId);
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.PreferredLanguage.HeaderKey, preferredLanguage);
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.IssuedAt.HeaderKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            if (cookieValues != null)
            {
                string cookieString = "";
                foreach (var (key, value) in cookieValues)
                {
                    cookieString += $"{key}={value}; ";
                }

                client.DefaultRequestHeaders.Add("Cookie", cookieString.Substring(0, cookieString.Length - 2));
            }

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

        public override async ValueTask DisposeAsync()
        {
            // The disposal works on local developments, however, it randomly fails on GitHub in GitHub Actions.
            // Since this method is called once all tests have run, the current solution is to catch the exception
            // and do nothing. The finished job in GitHub Action will clean the resources if there is any.
            try
            {
                foreach (var client in HttpClients)
                {
                    client.Dispose();
                }

                await base.DisposeAsync();
            }
            catch { }
        }
    }
}
