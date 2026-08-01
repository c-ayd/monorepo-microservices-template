using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.Options;

namespace NotificationService.Worker.Services
{
    public class SmtpService : IEmailService
    {
        private readonly SmtpOptions _smtpOptions;

        public SmtpService(IOptions<SmtpOptions> smtpOptions)
        {
            _smtpOptions = smtpOptions.Value;
        }

        public async Task SendAsync(IEnumerable<string> to, string subject, string body, bool isBodyHtml)
        {
            using var client = new SmtpClient(_smtpOptions.Server, _smtpOptions.Port);
            client.Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password);
            client.EnableSsl = _smtpOptions.EnableSsl;

            var message = new MailMessage()
            {
                From = new MailAddress(_smtpOptions.SenderEmail, _smtpOptions.SenderDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml,
            };

            foreach (var email in to)
            {
                message.To.Add(email);
            }

            await client.SendMailAsync(message);
        }
    }
}
