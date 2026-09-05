using System.Text.Json;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.Exceptions;
using NotificationService.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.RabbitMq.Notifications.Configurations;
using Shared.RabbitMq.Notifications.Messages;

namespace NotificationService.Worker.BackgroundServices
{
    public class EmailBackgroundService : BackgroundService
    {
        private const int RetryLimit = 3;

        private readonly RabbitMqConnectionService _rabbitMq;
        private readonly TemplateService _templateService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailBackgroundService> _logger;

        private IChannel? _channel;

        public EmailBackgroundService(
            RabbitMqConnectionService rabbitMq,
            TemplateService templateService,
            IEmailService emailService,
            ILogger<EmailBackgroundService> logger)
        {
            _rabbitMq = rabbitMq;
            _templateService = templateService;
            _emailService = emailService;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _rabbitMq.ConnectAsync(cancellationToken);

                _channel = await _rabbitMq.Connection!.CreateChannelAsync(cancellationToken: cancellationToken);

                await DeclareExchangesAsync();
                await DeclareQueuesAsync();

                _logger.LogInformation("The email background service has been started.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The email background service initialization has been canceled.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);
                
                throw;
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                var exception = new EmailBackgroundServiceConnectionException($"{nameof(_channel)} is null.");

                _logger.LogError(exception, "The email background service is exiting since the connection is not initialized. Message: {Message}",
                    exception.Message);

                throw exception;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (obj, args) =>
            {
                try
                {
                    // Deserialize message
                    var message = JsonSerializer.Deserialize<EmailMessage>(args.Body.ToArray(), new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (message == null)
                    {
                        _logger.LogWarning("The message could not be deserialized. Correlation ID: {CorrelationId}, Timestamp: {Timestamp}",
                            args.BasicProperties.CorrelationId,
                            args.BasicProperties.Timestamp);

                        await _channel!.BasicNackAsync(
                            deliveryTag: args.DeliveryTag,
                            multiple: false,
                            requeue: false);
    
                        return;
                    }

                    // Get email template
                    var template = _templateService.GetEmailTemplateAsync(message.TemplateId, message.Language);
                    if (template == null)
                    {
                        _logger.LogWarning("The email template could not be found. Correlation ID: {CorrelationId}, Timestamp: {Timestamp}, Template ID: {TemplateId}",
                            args.BasicProperties.CorrelationId,
                            args.BasicProperties.Timestamp,
                            message.TemplateId);

                        await _channel!.BasicNackAsync(
                            deliveryTag: args.DeliveryTag,
                            multiple: false,
                            requeue: false);
                        
                        return;
                    }

                    // Send email
                    await _emailService.SendAsync(
                        message.To,
                        message.SubjectParameters?.Length > 0 ?
                            string.Format(template.Subject, message.SubjectParameters) : template.Subject,
                        message.BodyParameters?.Length > 0 ?
                            string.Format(template.Body, message.BodyParameters) : template.Body,
                        template.IsBodyHtml);

                    await _channel!.BasicAckAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false);

                    _logger.LogInformation("The message has been processed. Correlation ID: {CorrelationId}, Timestamp: {Timestamp}",
                        args.BasicProperties.CorrelationId,
                        args.BasicProperties.Timestamp);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Something went wrong while handing a message. Correlation ID: {correlationId}, Timestamp: {timestamp}, Message: {message}",
                        args.BasicProperties.CorrelationId,
                        args.BasicProperties.Timestamp,
                        exception.Message);

                    await _channel!.BasicNackAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            };

            try
            {
                await _channel!.BasicConsumeAsync(
                    queue: EmailConfiguration.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("A consumer has been started to process messages.");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The email background service has been canceled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }

            _logger.LogInformation("The email background service has been stopped.");

            await base.StopAsync(cancellationToken);
        }

        private async Task DeclareExchangesAsync()
        {
            await _channel!.ExchangeDeclareAsync(
                exchange: EmailConfiguration.ExchangeName,
                type: EmailConfiguration.ExchangeType,
                durable: true,
                autoDelete: false);

            await _channel.ExchangeDeclareAsync(
                exchange: EmailConfiguration.DlxName,
                type: EmailConfiguration.DlxExchangeType,
                durable: true,
                autoDelete: false);
        }

        private async Task DeclareQueuesAsync()
        {
            await _channel!.QueueDeclareAsync(
                queue: EmailConfiguration.DlqName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            await _channel.QueueBindAsync(
                queue: EmailConfiguration.DlqName,
                exchange: EmailConfiguration.DlxName,
                routingKey: EmailConfiguration.DeadLetterRoutingKey);

            var arguments = new Dictionary<string, object?>()
            {
                { "x-queue-type", "quorum" },
                { "delivery-limit", RetryLimit },
                { "x-dead-letter-exchange", EmailConfiguration.DlxName },
                { "x-dead-letter-routing-key", EmailConfiguration.DeadLetterRoutingKey }
            };

            await _channel.QueueDeclareAsync(
                queue: EmailConfiguration.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);
            await _channel.QueueBindAsync(
                queue: EmailConfiguration.QueueName,
                exchange: EmailConfiguration.ExchangeName,
                routingKey: EmailConfiguration.RoutingKey);
        }
    }
}
