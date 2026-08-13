using System.Text;
using System.Text.Json;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.RabbitMq.Notification.Configurations;
using Shared.RabbitMq.Notification.Messages;

namespace NotificationService.Workers
{
    public class EmailWorker : BackgroundService
    {
        private const int RetryLimit = 3;

        private readonly RabbitMqConnectionService _rabbitMq;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailWorker> _logger;

        private IChannel? _channel;

        public EmailWorker(
            RabbitMqConnectionService rabbitMq,
            IEmailService emailService,
            ILogger<EmailWorker> logger)
        {
            _emailService = emailService;
            _rabbitMq = rabbitMq;
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

                _logger.LogInformation("The email worker has been started.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The email worker initialization has been canceled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogWarning("The email worker is exiting since the connection is not initialized.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (obj, args) =>
            {
                try
                {
                    var body = args.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<EmailMessage>(json, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (message == null)
                    {
                        _logger.LogWarning("The message could not be deserialized. Correlation ID: {correlationId}, Timestamp: {timestamp}",
                            args.BasicProperties.CorrelationId,
                            args.BasicProperties.Timestamp);

                        await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    await _emailService.SendAsync(message.To, message.Subject, message.Body, message.IsBodyHtml);

                    await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);

                    _logger.LogInformation("The message has been processed. Correlation ID: {correlationId}, Timestamp: {timestamp}",
                        args.BasicProperties.CorrelationId,
                        args.BasicProperties.Timestamp);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Correlation ID: {correlationId}, Timestamp: {timestamp}, Message: {message}",
                        args.BasicProperties.CorrelationId,
                        args.BasicProperties.Timestamp,
                        exception.Message);

                    await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
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
                _logger.LogWarning("The email worker has been canceled.");
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

            _logger.LogInformation("The email worker has been stopped.");

            await base.StopAsync(cancellationToken);
        }

        private async Task DeclareExchangesAsync()
        {
            await _channel!.ExchangeDeclareAsync(
                exchange: EmailConfiguration.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            await _channel.ExchangeDeclareAsync(
                exchange: EmailConfiguration.DlxName,
                type: ExchangeType.Direct,
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
