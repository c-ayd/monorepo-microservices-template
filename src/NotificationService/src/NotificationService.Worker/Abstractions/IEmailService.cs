namespace NotificationService.Worker.Abstractions
{
    /// <summary>
    /// Provides a method to send emails.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email to given target email adresses.
        /// </summary>
        /// <param name="to">Email addresses to send the email to</param>
        /// <param name="subject">Subject line of the email</param>
        /// <param name="body">Body of the email</param>
        /// <param name="isBodyHtml">Whether the body of the email includes HTML elements</param>
        Task SendAsync(string[] to, string subject, string body, bool isBodyHtml);
    }
}
