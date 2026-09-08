using NotificationService.Worker.Abstractions;
using NotificationService.Worker.Services;

namespace NotificationService.Worker.BackgroundServices
{
    public class TemplateBackgroundService : BackgroundService
    {
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

        private readonly TemplateService _templateService;
        private readonly ILogger<TemplateBackgroundService> _logger;

        public TemplateBackgroundService(
            ITemplateService templateService,
            ILogger<TemplateBackgroundService> logger)
        {
            _templateService = (TemplateService)templateService;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _templateService.RecacheTemplatesAsync(cancellationToken);

                await base.StartAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);
                
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_cacheDuration, stoppingToken);

                try
                {
                    await _templateService.RecacheTemplatesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("The template recache operation has been cancelled.");

                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Something went wrong while recaching the templates. The process will rerun in {RetryTime} in hours. Message: {Message}",
                        _cacheDuration.TotalHours,
                        exception.Message);
                }
            }
        }
    }
}
