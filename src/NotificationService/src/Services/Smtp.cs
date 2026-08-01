using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using NotificationService.Abstractions;
using NotificationService.Options;

namespace NotificationService.Services
{
    public class Smtp : IEmailService
    {
        private readonly SmtpOptions _smtpOptions;
        private readonly ILogger<Smtp> _logger;

        public Smtp(
            IOptions<SmtpOptions> smtpOptions,
            ILogger<Smtp> logger)
        {
            _smtpOptions = smtpOptions.Value;
            _logger = logger;
        }

        public async Task Send(string subject, string body, bool isBodyHtml, params string[] to)
        {
            using var client = new SmtpClient(_smtpOptions.Server, _smtpOptions.Port);
            client.Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password);
            client.EnableSsl = true;

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
