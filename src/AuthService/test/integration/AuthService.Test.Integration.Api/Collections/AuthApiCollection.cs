using AuthService.Application.Options;
using AuthService.Persistence.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Shared.Test.Helpers.Fixtures;

namespace AuthService.Test.Integration.Api.Collections
{
    [CollectionDefinition(nameof(AuthApiCollection))]
    public class AuthApiCollection : ICollectionFixture<AuthApiCollectionCluster>
    {
    }

    public class AuthApiCollectionCluster :  IAsyncLifetime
    {
        public const string AuthDbName = "auth-db";
        public const string AuthRejectedMessagesDbName = "auth-rejected-messages-db";

        public PostgreSqlFixture PostgreSqlFixture { get; private set; }
        public RabbitMqFixture RabbitMqFixture { get; private set; }
        public RedisFixture DataProtectionRedisFixture { get; private set; }
        public RedisFixture TokenBlacklistRedisFixture { get; private set; }

        public AuthApiWebAppFactory AuthApiWebApp { get; private set; } = null!;
        public HttpClient AuthApiClient { get; private set; } = null!;

        public AuthApiCollectionCluster()
        {
            PostgreSqlFixture = new PostgreSqlFixture();
            RabbitMqFixture = new RabbitMqFixture();
            DataProtectionRedisFixture = new RedisFixture();
            TokenBlacklistRedisFixture = new RedisFixture();
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                PostgreSqlFixture.InitializeAsync(new Dictionary<string, Type>()
                {
                    { AuthDbName, typeof(AuthDbContext) },
                    { AuthRejectedMessagesDbName, typeof(AuthRejectedMessagesDbContext) }
                }),
                RabbitMqFixture.InitializeAsync(),
                DataProtectionRedisFixture.InitializeAsync(),
                TokenBlacklistRedisFixture.InitializeAsync()
            );

            var rabbitMqOptions = RabbitMqFixture.GetRabbitMqOptions();
            AuthApiWebApp = new AuthApiWebAppFactory(
                new ConnectionStringsOptions()
                {
                    AuthDb = PostgreSqlFixture.GetConnectionString(AuthDbName),
                    AuthRejectedMessagesDb = PostgreSqlFixture.GetConnectionString(AuthRejectedMessagesDbName),
                    AuthDataProtectionRedis = DataProtectionRedisFixture.GetConnectionString(),
                    AuthTokenBlacklistRedis = TokenBlacklistRedisFixture.GetConnectionString()
                },
                new RabbitMqOptions()
                {
                    Username = rabbitMqOptions.Username,
                    Password = rabbitMqOptions.Password,
                    Host = rabbitMqOptions.Host,
                    Port = rabbitMqOptions.Port
                });
            
            AuthApiClient = AuthApiWebApp.CreateClient();
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(
                PostgreSqlFixture.DisposeAsync(),
                RabbitMqFixture.DisposeAsync(),
                DataProtectionRedisFixture.DisposeAsync(),
                TokenBlacklistRedisFixture.DisposeAsync()
            );

            AuthApiClient.Dispose();
            await AuthApiWebApp.DisposeAsync();
        }

        public class AuthApiWebAppFactory : WebApplicationFixture<Program>
        {
            private readonly ConnectionStringsOptions _connectionStringsOptions;
            private readonly RabbitMqOptions _rabbitMqOptions;

            public AuthApiWebAppFactory(
                ConnectionStringsOptions connectionStringsOptions,
                RabbitMqOptions rabbitMqOptions)
            {
                _connectionStringsOptions = connectionStringsOptions;
                _rabbitMqOptions = rabbitMqOptions;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection([
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthDb)}",
                            _connectionStringsOptions.AuthDb),
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthRejectedMessagesDb)}",
                            _connectionStringsOptions.AuthRejectedMessagesDb),
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthDataProtectionRedis)}",
                            _connectionStringsOptions.AuthDataProtectionRedis),
                        new KeyValuePair<string, string?>($"{ConnectionStringsOptions.Key}:{nameof(ConnectionStringsOptions.AuthTokenBlacklistRedis)}",
                            _connectionStringsOptions.AuthTokenBlacklistRedis),
                        
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Username)}",
                            _rabbitMqOptions.Username),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Password)}",
                            _rabbitMqOptions.Password),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Host)}",
                            _rabbitMqOptions.Host),
                        new KeyValuePair<string, string?>($"{RabbitMqOptions.Key}:{nameof(RabbitMqOptions.Port)}",
                            _rabbitMqOptions.Port.ToString()),
                    ]);
                });
            }
        }
    }
}
