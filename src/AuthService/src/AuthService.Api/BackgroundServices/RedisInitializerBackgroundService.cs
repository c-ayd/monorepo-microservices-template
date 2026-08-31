using AuthService.Persistence.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Api.BackgroundServices
{
    public class RedisInitializerBackgroundServices : IHostedService
    {
        private readonly ConnectionStringsOptions _connectionStrings;
        private readonly ILogger<RedisInitializerBackgroundServices> _logger;

        public RedisInitializerBackgroundServices(
            IOptions<ConnectionStringsOptions> connectionStrings,
            ILogger<RedisInitializerBackgroundServices> logger)
        {
            _connectionStrings = connectionStrings.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                DataProtection.SetConnection(await ConnectionMultiplexer.ConnectAsync(_connectionStrings.AuthDataProtectionRedis));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (DataProtection.Connection != null)
            {
                await DataProtection.Connection.CloseAsync();
                await DataProtection.Connection.DisposeAsync();
            }
        }
        
        /// <summary>
        /// Microsoft's data protection extension methods do not have any overload allowing dependency injection.
        /// Therefore, the connection is defined as a static variable and the Redis DB is given in the implementation.
        /// </summary>
        public static class DataProtection
        {
            private static ConnectionMultiplexer? _connection;
            public static ConnectionMultiplexer? Connection => _connection;
            public static void SetConnection(ConnectionMultiplexer connection) => _connection = connection;
        }
    }
}
