using Shared.RabbitMq.Notifications.Messages;

namespace AuthService.Application.Abstractions.Notifications
{
    /// <summary>
    /// Provides a method to send email messages to the message broker.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email message to the message broker.
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="cancellationToken">Token to cancel the publishing message process</param>
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
