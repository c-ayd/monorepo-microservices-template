using AuthService.Application.Abstractions.DistributedCaches;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using Shared.Redis;
using Shared.Redis.Extensions;

namespace AuthService.Persistence.DistributedCaches
{
    public class TokenBlacklist : RedisConnection, ITokenBlacklist
    {
        public TokenBlacklist(
            IOptions<ConnectionStringsOptions> connectionStrings)
            : base(connectionStrings.Value.AuthTokenBlacklistRedis)
        {
        }

        public async Task AddAsync(string accountId, TimeSpan accessTokenLifespan)
        {
            await GetDatabase().SaveAsStringAsync(accountId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), accessTokenLifespan);
        }

        public async Task DeleteAsync(string accountId)
        {
            await GetDatabase().KeyDeleteAsync(accountId);
        }
    }
}
