using System.Net;
using ApiGateway.Web.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ApiGateway.Test.Integration.Web.Fixtures
{
    public class ApiGatewayWebFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory = null!;
        
        public HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddSingleton<IStartupFilter, TestConfiguration>();
                    });
                });

            Client = _factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }

        private class TestConfiguration : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.UseMiddleware<GlobalExceptionHandler>();
                    app.Map("/test/no-exception", config =>
                    {
                        config.Run(async (context) => context.Response.StatusCode = (int)HttpStatusCode.NoContent);
                    });
                    app.Map("/test/exception", config =>
                    {
                        config.Run(async (context) => throw new Exception("Test exception"));
                    });

                    next(app);
                };
            }
        }
    }
}
