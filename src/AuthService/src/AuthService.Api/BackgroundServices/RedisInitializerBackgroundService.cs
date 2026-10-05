using AuthService.Application.Abstractions.DistributedCaches;
using AuthService.Persistence.DistributedCaches;

namespace AuthService.Api.BackgroundServices
{
    public class RedisInitializerBackgroundServices : IHostedService
    {
        private readonly ITokenBlacklist _tokenBlacklist;
        private readonly ILogger<RedisInitializerBackgroundServices> _logger;

        public RedisInitializerBackgroundServices(
            ITokenBlacklist tokenBlacklist,
            ILogger<RedisInitializerBackgroundServices> logger)
        {
            _tokenBlacklist = tokenBlacklist;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ((TokenBlacklist)_tokenBlacklist).ConnectAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);

                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
