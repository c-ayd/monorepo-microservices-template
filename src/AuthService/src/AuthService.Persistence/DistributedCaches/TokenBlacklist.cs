using AuthService.Application.Abstractions.DistributedCaches;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using Shared.Redis.Extensions;
using StackExchange.Redis;

namespace AuthService.Persistence.DistributedCaches
{
    public class TokenBlacklist : ITokenBlacklist, IDisposable, IAsyncDisposable
    {
        private readonly ConnectionStringsOptions _connectionStrings;

        private ConnectionMultiplexer? _connection;
        private SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);
        
        private IDatabase? _database;

        public TokenBlacklist(IOptions<ConnectionStringsOptions> connectionStrings)
        {
            _connectionStrings = connectionStrings.Value;
        }

        public async Task AddAsync(string accountId, TimeSpan accessTokenLifespan)
        {
            await _database!.SaveAsStringAsync(accountId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), accessTokenLifespan);
        }

        public async Task DeleteAsync(string accountId)
        {
            await _database!.KeyDeleteAsync(accountId);
        }

        public async Task ConnectAsync()
        {
            // This method should be called only once. This semaphore is used as a safeguard.
            await _connectionSemaphore.WaitAsync();

            if (_connection != null)
            {
                _connectionSemaphore.Release();
                return;
            }

            // Since the connection to the Redis DB is required for this service, the code below is not
            // wrapped with a catch block and should throw an exception if something goes wrong.
            try
            {
                _connection = await ConnectionMultiplexer.ConnectAsync(_connectionStrings.AuthBlacklistRedis);
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
