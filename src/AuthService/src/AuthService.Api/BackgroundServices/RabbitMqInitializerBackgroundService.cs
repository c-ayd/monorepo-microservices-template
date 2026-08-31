using AuthService.Infrastructure.MessageBrokers;

namespace AuthService.Api.BackgroundServices
{
    public class RabbitMqInitializerBackgroundService : IHostedService
    {
        private readonly RabbitMqConnectionService _rabbitMq;
        private readonly ILogger<RabbitMqInitializerBackgroundService> _logger;

        public RabbitMqInitializerBackgroundService(
            RabbitMqConnectionService rabbitMq,
            ILogger<RabbitMqInitializerBackgroundService> logger)
        {
            _rabbitMq = rabbitMq;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _rabbitMq.ConnectAsync(cancellationToken);

                _logger.LogInformation("The RabbitMQ connection has been established.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The RabbitMQ connection operation has been cancelled");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);

                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
