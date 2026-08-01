namespace NotificationService.Abstractions
{
    /// <summary>
    /// Provides a method to send emails.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email to given target emails.
        /// </summary>
        /// <param name="subject">Subject line of the email</param>
        /// <param name="body">Body of the email</param>
        /// <param name="isBodyHtml">Whether the body of the email includes html elements</param>
        /// <param name="to">Email address to send to</param>
        Task Send(string subject, string body, bool isBodyHtml, params string[] to);
    }
}
