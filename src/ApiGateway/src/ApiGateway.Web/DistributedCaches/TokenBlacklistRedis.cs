using ApiGateway.Web.Options;
using Microsoft.Extensions.Options;
using Shared.Redis;
using Shared.Redis.Extensions;

namespace ApiGateway.Web.DistributedCaches
{
    public class TokenBlacklistRedis : RedisConnection
    {
        public TokenBlacklistRedis(
            IOptions<ConnectionStringsOptions> connectionStrings)
            : base(connectionStrings.Value.AuthTokenBlacklistRedis)
        {
        }

        public async Task<DateTimeOffset?> GetBlacklistTimeAsync(string accountId)
        {
            var result = await GetDatabase().LoadFromStringAsync<long>(accountId);
            if (result.isKeyFound)
                return DateTimeOffset.FromUnixTimeSeconds(result.value);

            return null;
        }
    }
}
