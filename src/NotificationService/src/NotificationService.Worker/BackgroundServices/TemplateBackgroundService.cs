using NotificationService.Worker.Services;

namespace NotificationService.Worker.BackgroundServices
{
    public class TemplateBackgroundService : BackgroundService
    {
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);

        private readonly TemplateService _templateService;
        private readonly ILogger<TemplateBackgroundService> _logger;

        public TemplateBackgroundService(
            TemplateService templateService,
            ILogger<TemplateBackgroundService> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _templateService.RecacheAllTemplatesAsync(cancellationToken);

                _logger.LogInformation("All templates have been recached.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The template background service initialization has been canceled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong while caching the templates. Message: {Message}",
                    exception.Message);
            }

            _logger.LogInformation("The template background service will recache templates every {cacheDuration} hour(s).",
                _cacheDuration.TotalHours);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay((int)_cacheDuration.TotalMilliseconds, stoppingToken);

                try
                {
                    await _templateService.RecacheAllTemplatesAsync(stoppingToken);

                    _logger.LogInformation("All templates have been recached.");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("The template recache operation has been cancelled.");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Something went wrong while recaching the templates. Message: {Message}",
                        exception.Message);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("The template background service has been stopped.");

            return base.StopAsync(cancellationToken);
        }
    }
}
