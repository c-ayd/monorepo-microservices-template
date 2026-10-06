using ApiGateway.Web.Services;
using Shared.Redis;
using Shared.Redis.BackgroundService;

namespace ApiGateway.Web.BackgroundServices
{
    public class RedisBackgroundServices : RedisConnectionBackgroundService
    {
        public RedisBackgroundServices(
            TokenBlacklist tokenBlacklist,
            ILogger<RedisBackgroundServices> logger)
            : base(
            redisConnections: new List<RedisConnection>()
            {
                tokenBlacklist
            },
            logger)
        {
        }
    }
}
