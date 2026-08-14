using Microsoft.Extensions.Options;
using NotificationService.Test.Integration.Worker.Fixtures;
using NotificationService.Worker.Services;
using Shared.TestGenerators;

namespace NotificationService.Test.Integration.Worker.Services
{
    public class SmtpServiceTest : IClassFixture<EmailServiceFixture>
    {
        private readonly EmailServiceFixture _emailServiceFixture;
        private readonly SmtpService _smtp;

        public SmtpServiceTest(EmailServiceFixture emailServiceFixture)
        {
            _emailServiceFixture = emailServiceFixture;
            _smtp = new SmtpService(Options.Create(emailServiceFixture.SmtpOptions));
        }

        [Fact]
        public async Task SendAsync_WhenSendSingleEmail_ShouldSendSingleEmail()
        {
            // Arrange
            var to = EmailGenerator.Generate();
            var subject = StringGenerator.GenerateAlphanumeric();
            var body = StringGenerator.GenerateAlphanumeric();

            // Act
            await _smtp.SendAsync([to], subject, body, isBodyHtml: false);
            
            // Assert
            var emails = await _emailServiceFixture.GetEmails();
            Assert.Single(emails);

            Assert.Equal($"\"{_emailServiceFixture.SmtpOptions.SenderDisplayName}\" <{_emailServiceFixture.SmtpOptions.SenderEmail}>",
                emails[0].From);

            Assert.Single(emails[0].To);
            Assert.Equal(to, emails[0].To[0]);

            Assert.Equal(subject, emails[0].Subject);
            Assert.Equal(body, emails[0].Body!.TrimEnd());
        }

        [Fact]
        public async Task SendAsync_WhenSendMultipleEmails_ShouldSendMultipleEmails()
        {
            // Arrange
            var to = new string[]
            {
                EmailGenerator.Generate(),
                EmailGenerator.Generate(),
                EmailGenerator.Generate()
            };
            var subject = StringGenerator.GenerateAlphanumeric();
            var body = StringGenerator.GenerateAlphanumeric();

            // Act
            await _smtp.SendAsync(to, subject, body, isBodyHtml: false);

            // Assert
            var emails = await _emailServiceFixture.GetEmails();
            Assert.Single(emails);

            Assert.Equal($"\"{_emailServiceFixture.SmtpOptions.SenderDisplayName}\" <{_emailServiceFixture.SmtpOptions.SenderEmail}>",
                emails[0].From);

            foreach (var item in emails[0].To)
            {
                if (!to.Contains(item))
                    Assert.Fail("The email is not sent to one of the given email addresses");
            }

            Assert.Equal(subject, emails[0].Subject);
            Assert.Equal(body, emails[0].Body!.TrimEnd());
        }
    }
}
