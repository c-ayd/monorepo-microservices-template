using System.Text;
using System.Text.Json;
using NotificationService.Worker.Abstractions;
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
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogWarning("The email background service is exiting since the connection is not initialized.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (obj, args) =>
            {
                try
                {
                    // Deserialize message
                    var body = args.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<RabbitMqEmailMessage>(json, new JsonSerializerOptions()
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
                    queue: RabbitMqEmailConfiguration.QueueName,
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
                _logger.LogError(exception, exception.Message);
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
                exchange: RabbitMqEmailConfiguration.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            await _channel.ExchangeDeclareAsync(
                exchange: RabbitMqEmailConfiguration.DlxName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);
        }

        private async Task DeclareQueuesAsync()
        {
            await _channel!.QueueDeclareAsync(
                queue: RabbitMqEmailConfiguration.DlqName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            await _channel.QueueBindAsync(
                queue: RabbitMqEmailConfiguration.DlqName,
                exchange: RabbitMqEmailConfiguration.DlxName,
                routingKey: RabbitMqEmailConfiguration.DeadLetterRoutingKey);

            var arguments = new Dictionary<string, object?>()
            {
                { "x-queue-type", "quorum" },
                { "delivery-limit", RetryLimit },
                { "x-dead-letter-exchange", RabbitMqEmailConfiguration.DlxName },
                { "x-dead-letter-routing-key", RabbitMqEmailConfiguration.DeadLetterRoutingKey }
            };

            await _channel.QueueDeclareAsync(
                queue: RabbitMqEmailConfiguration.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);
            await _channel.QueueBindAsync(
                queue: RabbitMqEmailConfiguration.QueueName,
                exchange: RabbitMqEmailConfiguration.ExchangeName,
                routingKey: RabbitMqEmailConfiguration.RoutingKey);
        }
    }
}
