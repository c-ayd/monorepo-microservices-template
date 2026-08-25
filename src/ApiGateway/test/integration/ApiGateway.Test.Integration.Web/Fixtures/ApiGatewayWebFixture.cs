using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shared.Test.Helpers;

namespace ApiGateway.Test.Integration.Web.Fixtures
{
    public class ApiGatewayWebFixture : IAsyncLifetime
    {
        private ApiGatewayFactory _factory = null!;
        
        public HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            _factory = new ApiGatewayFactory();
            Client = _factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }

        private class ApiGatewayFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Test");
            }

            protected override IHost CreateHost(IHostBuilder builder)
            {
                builder.ConfigureHostConfiguration(config =>
                {
                    config.AddConfiguration(ConfigurationHelper.CreateConfigurationFromTestSettings());
                });

                return base.CreateHost(builder);
            }
        }
    }
}
