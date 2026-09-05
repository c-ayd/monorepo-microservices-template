using AuthService.Application.Options;
using AuthService.Persistence.DbContexts;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Test.Helpers;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace AuthService.Test.Utility.Fixtures
{
    public class AuthApiFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _authDbContainer = null!;
        private RabbitMqContainer _rabbitMqContainer = null!;
        private RedisContainer _dataProtectionRedisContainer = null!;
        private RedisContainer _tokenBlacklistRedisContainer = null!;

        public WebApplicationFactory<Program> Factory { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            // PostgreSQL
            _authDbContainer = new PostgreSqlBuilder("postgres:18.4")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
                .Build();
                
            await _authDbContainer.StartAsync();

            using var authDbContext = CreateAuthDbContext();
            await authDbContext.Database.MigrateAsync();

            // RabbitMQ
            _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4.3.4-management")
                .WithPortBinding(5672, true)
                .WithPortBinding(15672, true)
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*Server startup complete.*"))
                .Build();
            
            await _rabbitMqContainer.StartAsync();

            // Data Protection Redis
            _dataProtectionRedisContainer = new RedisBuilder("redis:8.10")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
                .Build();

            await _dataProtectionRedisContainer.StartAsync();

            // Token Blacklist Redis
            _tokenBlacklistRedisContainer = new RedisBuilder("redis:8.10")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
                .Build();

            await _tokenBlacklistRedisContainer.StartAsync();

            // Web API
            Factory = new AuthApiFactory(
                _authDbContainer.GetConnectionString(),
                _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                _dataProtectionRedisContainer.GetConnectionString(),
                _tokenBlacklistRedisContainer.GetConnectionString());
            
            Client = Factory.CreateClient();
        }

        public AuthDbContext CreateAuthDbContext()
        {
            return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(_authDbContainer.GetConnectionString())
                .Options);
        }

        public T GetOptions<T>()
            where T : class
        {
            return Factory.Services.GetRequiredService<IOptions<T>>().Value;
        }

        public async Task DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();

            await _authDbContainer.StopAsync();
            await _rabbitMqContainer.StopAsync();
            await _dataProtectionRedisContainer.StopAsync();
            await _tokenBlacklistRedisContainer.StopAsync();

            await _authDbContainer.DisposeAsync();
            await _rabbitMqContainer.DisposeAsync();
            await _dataProtectionRedisContainer.DisposeAsync();
            await _tokenBlacklistRedisContainer.DisposeAsync();
        }

        private class AuthApiFactory : WebApplicationFactory<Program>
        {
            private readonly string _authDbConnString;
            private readonly string _rabbitMqPort;
            private readonly string _dataProtectionRedisConnString;
            private readonly string _tokenBlacklistRedisConnString;

            public AuthApiFactory(
                string authDbConnString,
                string rabbitMqPort,
                string dataProtectionRedisConnString,
                string tokenBlacklistRedisConnString)
            {
                _authDbConnString = authDbConnString;
                _rabbitMqPort = rabbitMqPort;
                _dataProtectionRedisConnString = dataProtectionRedisConnString;
                _tokenBlacklistRedisConnString = tokenBlacklistRedisConnString;
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
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthDataProtectionRedis)}",
                            _dataProtectionRedisConnString),
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthTokenBlacklistRedis)}",
                            _tokenBlacklistRedisConnString),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Port)}",
                            _rabbitMqPort)
                    ]);
                });
            }
        }
    }
}
