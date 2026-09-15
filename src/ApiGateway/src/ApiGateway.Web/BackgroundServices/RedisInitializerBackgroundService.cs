using ApiGateway.Web.Services;

namespace ApiGateway.Web.BackgroundServices
{
    public class RedisInitializerBackgroundServices : IHostedService
    {
        private readonly TokenBlacklist _tokenBlacklist;
        private readonly ILogger<RedisInitializerBackgroundServices> _logger;

        public RedisInitializerBackgroundServices(
            TokenBlacklist tokenBlacklist,
            ILogger<RedisInitializerBackgroundServices> logger)
        {
            _tokenBlacklist = tokenBlacklist;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _tokenBlacklist.ConnectAsync(cancellationToken);
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
