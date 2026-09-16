using ApiGateway.Web.Options;
using Microsoft.Extensions.Options;
using Shared.Redis.Extensions;
using StackExchange.Redis;

namespace ApiGateway.Web.Services
{
    public class TokenBlacklist : IDisposable, IAsyncDisposable
    {
        private readonly ConnectionStringsOptions _connectionStrings;

        private ConnectionMultiplexer? _connection;
        private SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);

        private IDatabase? _database;

        public TokenBlacklist(IOptions<ConnectionStringsOptions> connectionStrings)
        {
            _connectionStrings = connectionStrings.Value;
        }

        public async Task<DateTimeOffset?> GetBlacklistTimeAsync(string accountId)
        {
            var result = await _database!.LoadFromStringAsync<long>(accountId);
            if (result.isKeyFound)
                return DateTimeOffset.FromUnixTimeSeconds(result.value);

            return null;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            // This method should be called only once. This semaphore is used as a safeguard.
            await _connectionSemaphore.WaitAsync(cancellationToken);

            if (_connection != null)
            {
                _connectionSemaphore.Release();
                return;
            }

            // Since the connection to the Redis DB is required for this service, the code below is not
            // wrapped with a catch block and should throw an exception if something goes wrong.
            try
            {
                _connection = await ConnectionMultiplexer.ConnectAsync(_connectionStrings.AuthTokenBlacklistRedis);
                _database = _connection.GetDatabase();
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}
