using Shared.RabbitMq.Notifications.Messages;

namespace AuthService.Application.Abstractions.MessageBrokers
{
    /// <summary>
    /// Provides a method to send email messages to RabbitMQ.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email message to RabbitMQ.
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="userId">ID of the user initiating this publishing</param>
        /// <param name="cancellationToken">Token to cancel the publishing message process</param>
        Task SendAsync(EmailMessage message, string? userId = null, CancellationToken cancellationToken = default);
    }
}
