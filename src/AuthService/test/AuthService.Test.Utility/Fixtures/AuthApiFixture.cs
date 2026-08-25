using AuthService.Infrastructure.Options;
using AuthService.Persistence.DbContexts;
using AuthService.Persistence.Options;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.Test.Helpers;
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
            Factory = new AuthApiFactory(_dbContainer.GetConnectionString(),
                _rabbitMqContainer.GetMappedPublicPort(5672).ToString());
            Client = Factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();
        }

        private class AuthApiFactory : WebApplicationFactory<Program>
        {
            private readonly string _authDbConnString;
            private readonly string _rabbitMqPort;

            public AuthApiFactory(string authDbConnString, string rabbitMqPort)
            {
                _authDbConnString = authDbConnString;
                _rabbitMqPort = rabbitMqPort;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Test");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddConfiguration(ConfigurationHelper.CreateConfigurationFromTestSettings());

                    config.AddInMemoryCollection([
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthDb)}",
                            _authDbConnString),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Port)}",
                            _rabbitMqPort)
                    ]);
                });
            }
        }
    }
}
