using AuthService.Application.Abstractions.Crypto;
using AuthService.Application.Abstractions.MessageBrokers;
using AuthService.Application.Options;
using AuthService.Persistence.DbContexts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Crypto;
using Shared.RabbitMq.Helpers;
using Shared.RabbitMq.Helpers.BackgroundServices;
using Shared.RabbitMq.Helpers.EntityFramework;
using Shared.RabbitMq.Helpers.Structures;

namespace AuthService.Api.BackgroundServices
{
    public class RabbitMqPublisherBackgroundService : PublisherBackgroundService
    {
        private readonly AuthRejectedMessagesDbContext _authRejectedMessagesDbContext;
        private readonly IAesGcmEncryptionVersions _aesGcmEncryptionVersions;
        private readonly ILogger<RabbitMqPublisherBackgroundService> _logger;

        public RabbitMqPublisherBackgroundService(
            IOptions<RabbitMqOptions> rabbitMqOptions,
            AuthRejectedMessagesDbContext authRejectedMessagesDbContext,
            IAesGcmEncryptionVersions aesGcmEncryptionVersions,
            IEmailService emailService,
            ILogger<RabbitMqPublisherBackgroundService> logger)
            : base(
            connectionFactory: new ConnectionFactory()
            {
                UserName = rabbitMqOptions.Value.Username,
                Password = rabbitMqOptions.Value.Password,
                HostName = rabbitMqOptions.Value.Host,
                Port = rabbitMqOptions.Value.Port
            },
            publishers: new List<Publisher>()
            {
                (Publisher)emailService
            },
            retryPublishTime: TimeSpan.FromSeconds(5),
            logger)
        {
            _authRejectedMessagesDbContext = authRejectedMessagesDbContext;
            _aesGcmEncryptionVersions = aesGcmEncryptionVersions;
            _logger = logger;
        }

        protected override async Task SaveRejectedMessagesAsync(
            IEnumerable<Message> rejectedMessages,
            bool isShuttingDown,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<RejectedMessage>();
            foreach (var rejectedMessage in rejectedMessages)
            {
                messages.Add(new RejectedMessage(
                    rejectedMessage.PublisherName,
                    rejectedMessage.ExchangeName,
                    rejectedMessage.RoutingKey,
                    rejectedMessage.Properties,
                    AesGcmEncryption.Encrypt(rejectedMessage.Body, _aesGcmEncryptionVersions.CurrentVersion, _aesGcmEncryptionVersions.GetEncryptionKey)));
            }

            if (isShuttingDown)
            {
                await _authRejectedMessagesDbContext.AddRangeAsync(messages);
                await _authRejectedMessagesDbContext.SaveChangesAsync();
            }
            else
            {
                await _authRejectedMessagesDbContext.AddRangeAsync(messages, cancellationToken);
                await _authRejectedMessagesDbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
