using ApiGateway.Web.DistributedCaches;
using Shared.Redis;
using Shared.Redis.BackgroundService;

namespace ApiGateway.Web.BackgroundServices
{
    public class RedisBackgroundServices : RedisConnectionBackgroundService
    {
        public RedisBackgroundServices(
            TokenBlacklistRedis tokenBlacklist,
            RateLimiterRedis rateLimiterRedis,
            ILogger<RedisBackgroundServices> logger)
            : base(
            redisConnections: new List<RedisConnection>()
            {
                tokenBlacklist,
                rateLimiterRedis
            },
            logger)
        {
        }
    }
}
