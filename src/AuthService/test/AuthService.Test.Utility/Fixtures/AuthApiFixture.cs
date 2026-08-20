using AuthService.Api.Middlewares;
using AuthService.Infrastructure.Options;
using AuthService.Persistence.DbContexts;
using AuthService.Persistence.Options;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace AuthService.Test.Utility.Fixtures
{
    public class AuthApiFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _dbContainer = null!;
        private RabbitMqContainer _rabbitMqContainer = null!;
        
        public WebApplicationFactory<Program> Factory { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            // PostgreSQL
            _dbContainer = new PostgreSqlBuilder("postgres:18.4")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
                .Build();
                
            await _dbContainer.StartAsync();

            using var dbContext = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(_dbContainer.GetConnectionString())
                .Options);
            await dbContext.Database.MigrateAsync();

            // RabbitMQ
            _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4.3.4-management")
                .WithPortBinding(5672, true)
                .WithPortBinding(15672, true)
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*Server startup complete.*"))
                .Build();
            await _rabbitMqContainer.StartAsync();

            // Web API
            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test");

                    builder.ConfigureAppConfiguration((context, configBuilder) =>
                    {
                        configBuilder.AddConfiguration(ConfigurationHelper.CreateConfiguration());
                        
                        configBuilder.AddInMemoryCollection([
                            new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}", _dbContainer.GetConnectionString()),
                            new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Port)}",
                                _rabbitMqContainer.GetMappedPublicPort(5672).ToString())
                        ]);
                    });

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

            Client = Factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();
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
