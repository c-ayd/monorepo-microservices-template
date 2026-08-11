using System.Net;
using AuthService.Api.Middlewares;
using AuthService.Persistence.DbContexts;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AuthService.Test.Utility.Fixtures
{
    public class AuthApiFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory = null!;
        private PostgreSqlContainer _dbContainer = null!;
        
        public HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            _dbContainer = new PostgreSqlBuilder("postgres:18.4")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
                .Build();
                
            await _dbContainer.StartAsync();

            using var dbContext = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(_dbContainer.GetConnectionString())
                .Options);
            await dbContext.Database.MigrateAsync();

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.AddSingleton<IStartupFilter, TestConfiguration>();

                        var dbContext = services.FirstOrDefault(s => s.ServiceType == typeof(AuthDbContext));
                        if (dbContext != null)
                        {
                            services.Remove(dbContext);
                        }
                        services.AddDbContext<AuthDbContext>(_ => _.UseNpgsql(_dbContainer.GetConnectionString()));
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

                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test/no-exception", context => Results.NoContent().ExecuteAsync(context));
                        endpoints.MapGet("/test/exception", context => throw new Exception("Test exception"));
                    });

                    next(app);
                };
            }
        }
    }
}
