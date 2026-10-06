using AuthService.Application.Abstractions.DistributedCaches;
using Shared.Redis;
using Shared.Redis.BackgroundService;

namespace AuthService.Api.BackgroundServices
{
    public class RedisBackgroundServices : RedisConnectionBackgroundService
    {
        public RedisBackgroundServices(
            ITokenBlacklist tokenBlacklist,
            ILogger<RedisBackgroundServices> logger)
            : base(
            redisConnections: new List<RedisConnection>()
            {
                (RedisConnection)tokenBlacklist
            },
            logger)
        {
        }
    }
}
