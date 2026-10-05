using AuthService.Application.Options;
using AuthService.Infrastructure.Authentication;
using AuthService.Persistence.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
                    AuthTokenBlacklistRedis = TokenBlacklistRedisFixture.GetConnectionString()
                },
                new RabbitMqOptions()
                {
                    Username = rabbitMqOptions.Username,
                    Password = rabbitMqOptions.Password,
                    Host = rabbitMqOptions.Host,
                    Port = rabbitMqOptions.Port
                },
                new JwtKeysOptions()
                {
                    CurrentKeyId = "k2",
                    Keys = new List<JwtKeysOptions.KeyInfo>()
                    {
                        new JwtKeysOptions.KeyInfo()
                        {
                            KeyId = "k1",
                            PrivateKey = File.ReadAllText("./test_jwt_k1_private.txt"),
                            PublicKey = File.ReadAllText("./test_jwt_k1_public.txt")
                        },
                        new JwtKeysOptions.KeyInfo()
                        {
                            KeyId = "k2",
                            PrivateKey = File.ReadAllText("./test_jwt_k2_private.txt"),
                            PublicKey = File.ReadAllText("./test_jwt_k2_public.txt")
                        }
                    }
                });
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(
                PostgreSqlFixture.DisposeAsync(),
                RabbitMqFixture.DisposeAsync(),
                DataProtectionRedisFixture.DisposeAsync(),
                TokenBlacklistRedisFixture.DisposeAsync()
            );

            await AuthApiWebApp.DisposeAsync();
        }

        public class AuthApiWebAppFactory : WebAppFactoryFixture<Program>
        {
            private readonly ConnectionStringsOptions _connectionStringsOptions;
            private readonly RabbitMqOptions _rabbitMqOptions;
            private readonly JwtKeysOptions _jwtKeysOptions;

            public AuthApiWebAppFactory(
                ConnectionStringsOptions connectionStringsOptions,
                RabbitMqOptions rabbitMqOptions,
                JwtKeysOptions jwtKeysOptions)
            {
                _connectionStringsOptions = connectionStringsOptions;
                _rabbitMqOptions = rabbitMqOptions;
                _jwtKeysOptions = jwtKeysOptions;
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

                        new KeyValuePair<string, string?>($"{JwtKeysOptions.Key}:{nameof(JwtKeysOptions.CurrentKeyId)}",
                            _jwtKeysOptions.CurrentKeyId),
                        .._jwtKeysOptions.Keys.Select((k, i) => new KeyValuePair<string, string?>($"{JwtKeysOptions.Key}:{nameof(JwtKeysOptions.Keys)}:{i}:{nameof(JwtKeysOptions.KeyInfo.KeyId)}",
                            k.KeyId)),
                        .._jwtKeysOptions.Keys.Select((k, i) => new KeyValuePair<string, string?>($"{JwtKeysOptions.Key}:{nameof(JwtKeysOptions.Keys)}:{i}:{nameof(JwtKeysOptions.KeyInfo.PrivateKey)}",
                            k.PrivateKey)),
                        .._jwtKeysOptions.Keys.Select((k, i) => new KeyValuePair<string, string?>($"{JwtKeysOptions.Key}:{nameof(JwtKeysOptions.Keys)}:{i}:{nameof(JwtKeysOptions.KeyInfo.PublicKey)}",
                            k.PublicKey))
                    ]);
                });
            }
        }
    }
}
