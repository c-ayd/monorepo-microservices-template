using System.Diagnostics;
using System.Text.Json;
using AuthService.Application.Abstractions.Notifications;
using AuthService.Infrastructure.MessageBrokers;
using RabbitMQ.Client;
using Shared.RabbitMq.Notifications.Configurations;
using Shared.RabbitMq.Notifications.Messages;

namespace AuthService.Infrastructure.Notifications
{
    public class EmailService : IEmailService
    {
        private readonly RabbitMqConnectionService _rabbitMq;

        private readonly Lazy<Task<IChannel>> _channel;

        public EmailService(RabbitMqConnectionService rabbitMq)
        {
            _rabbitMq = rabbitMq;

            _channel = new Lazy<Task<IChannel>>(InitializeChannel);
        }

        private async Task<IChannel> InitializeChannel()
        {
            var channel = await _rabbitMq.Connection!.CreateChannelAsync(new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: false));

            await channel.ExchangeDeclareAsync(
                exchange: EmailConfiguration.ExchangeName,
                type: EmailConfiguration.ExchangeType,
                durable: true,
                autoDelete: false);
            await channel.ExchangeDeclareAsync(
                exchange: EmailConfiguration.DlxName,
                type: EmailConfiguration.DlxExchangeType,
                durable: true,
                autoDelete: false);

            channel.BasicReturnAsync += async (obj, args) =>
            {
                var channel = await _channel.Value;

                var properties = new BasicProperties(args.BasicProperties);
                await channel.BasicPublishAsync(
                    exchange: EmailConfiguration.DlxName,
                    routingKey: EmailConfiguration.DeadLetterRoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: args.Body,
                    cancellationToken: args.CancellationToken);
            };

            return channel;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var channel = await _channel.Value;

            var properties = new BasicProperties()
            {
                CorrelationId = Activity.Current?.TraceId.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: EmailConfiguration.ExchangeName,
                routingKey: EmailConfiguration.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: JsonSerializer.SerializeToUtf8Bytes(message),
                cancellationToken: cancellationToken);
        }
    }
}
