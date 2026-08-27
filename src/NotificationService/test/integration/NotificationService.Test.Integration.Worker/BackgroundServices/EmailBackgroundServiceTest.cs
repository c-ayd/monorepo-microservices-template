using Microsoft.Extensions.Options;
using NotificationService.Test.Integration.Worker.Collections;
using NotificationService.Test.Integration.Worker.Fixtures;
using NotificationService.Worker.Services;
using NotificationService.Worker.BackgroundServices;
using Shared.RabbitMq.Notifications.Configurations;
using Shared.RabbitMq.Notifications.Messages;
using Shared.Test.Generators;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Worker.DbContexts;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Entities;

namespace NotificationService.Test.Integration.Worker.BackgroundServices
{
    [Collection(nameof(WorkerCollection))]
    public class EmailBackgroundServiceTest : IClassFixture<EmailServiceFixture>, IClassFixture<LoggerFixture<EmailBackgroundService>>, IAsyncLifetime
    {
        private const int TimeoutInSeconds = 30;

        private readonly WorkerFixture _workerFixture;
        private readonly EmailServiceFixture _emailServiceFixture;

        private readonly TemplateService _templateService;
        private readonly EmailBackgroundService _backgroundService;

        private readonly CancellationTokenSource _cts;

        public EmailBackgroundServiceTest(
            WorkerFixture workerFixture,
            EmailServiceFixture emailServiceFixture,
            LoggerFixture<EmailBackgroundService> loggerFixture)
        {
            _workerFixture = workerFixture;
            _emailServiceFixture = emailServiceFixture;

            var scopeFactory = new ServiceCollection()
                .AddDbContext<TemplateDbContext>(_ => _.UseNpgsql(_workerFixture.GetTemplateDbConnectionString()))
                .BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();

            _templateService = new TemplateService(scopeFactory);
            _backgroundService = new EmailBackgroundService(
                new RabbitMqConnectionService(Options.Create(_workerFixture.GetRabbitMqOptions())),
                _templateService,
                new SmtpService(Options.Create(_emailServiceFixture.SmtpOptions)),
                loggerFixture);
            
            _cts = new CancellationTokenSource();
        }

        public async Task InitializeAsync()
        {
            await _backgroundService.StartAsync(_cts.Token);
        }

        public async Task DisposeAsync()
        {
            await _backgroundService.StopAsync(_cts.Token);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMessageWithoutPlaceholdersIsInQueue_ShouldSendEmail()
        {
            // Arrange
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                false
            );

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();
            await _templateService.RecacheAllTemplatesAsync();

            var to = EmailGenerator.Generate();

            await _workerFixture.PublishMessageAsync(
                new RabbitMqEmailMessage(
                    [to],
                    emailTemplate.TemplateId,
                    emailTemplate.Language),
                RabbitMqEmailConfiguration.ExchangeName,
                RabbitMqEmailConfiguration.RoutingKey,
                TimeoutInSeconds);

            // Act
            // _backgroundService starts ExecuteAsync automatically when StartAsync is called

            // Assert
            var elapsedTimeInSeconds = 0;
            List<EmailServiceFixture.MailHogDto> emails = new List<EmailServiceFixture.MailHogDto>();
            while (true)
            {
                emails = await _emailServiceFixture.GetEmails();
                if (emails.Count == 1)
                    break;

                await Task.Delay(1000);

                ++elapsedTimeInSeconds;
                if (elapsedTimeInSeconds >= TimeoutInSeconds)
                    Assert.Fail($"Timeout. The test case waited for {TimeoutInSeconds} seconds but the message was not processed.");
            }

            foreach (var item in emails[0].To)
            {
                if (!to.Contains(item))
                    Assert.Fail("The email is not sent to one of the given email addresses");
            }

            Assert.Equal(emailTemplate.Subject, emails[0].Subject);
            Assert.Equal(emailTemplate.Body, emails[0].Body!.TrimEnd());

            await _cts.CancelAsync();
        }

        [Fact]
        public async Task ExecuteAsync_WhenMessageWithPlaceholdersIsInQueue_ShouldSendEmail()
        {
            // Arrange
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                "{0}" + StringGenerator.GenerateAlpha() + "{1}",
                "{0}" + StringGenerator.GenerateAlpha() + "{1}" + "{0}",
                false
            );

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();
            await _templateService.RecacheAllTemplatesAsync();

            var to = EmailGenerator.Generate();
            var subjectParameters = new string[] { "abc", "def" };
            var bodyParameters = new string[] { "123", "456" };

            await _workerFixture.PublishMessageAsync(
                new RabbitMqEmailMessage(
                    [to],
                    emailTemplate.TemplateId,
                    emailTemplate.Language,
                    subjectParameters,
                    bodyParameters),
                RabbitMqEmailConfiguration.ExchangeName,
                RabbitMqEmailConfiguration.RoutingKey,
                TimeoutInSeconds);

            // Act
            // _backgroundService starts ExecuteAsync automatically when StartAsync is called

            // Assert
            var elapsedTimeInSeconds = 0;
            List<EmailServiceFixture.MailHogDto> emails = new List<EmailServiceFixture.MailHogDto>();
            while (true)
            {
                emails = await _emailServiceFixture.GetEmails();
                if (emails.Count == 1)
                    break;

                await Task.Delay(1000);

                ++elapsedTimeInSeconds;
                if (elapsedTimeInSeconds >= TimeoutInSeconds)
                    Assert.Fail($"Timeout. The test case waited for {TimeoutInSeconds} seconds but the message was not processed.");
            }

            foreach (var item in emails[0].To)
            {
                if (!to.Contains(item))
                    Assert.Fail("The email is not sent to one of the given email addresses");
            }

            Assert.Equal(string.Format(emailTemplate.Subject, subjectParameters), emails[0].Subject);
            Assert.Equal(string.Format(emailTemplate.Body, bodyParameters), emails[0].Body!.TrimEnd());

            await _cts.CancelAsync();
        }

        [Fact]
        public async Task ExecuteAsync_WhenMessageIsPoisoned_ShouldSendMessageToDlq()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var message = "{}";

            await _workerFixture.PublishMessageAsync(
                message,
                RabbitMqEmailConfiguration.ExchangeName,
                RabbitMqEmailConfiguration.RoutingKey,
                TimeoutInSeconds);

            // Act
            // _backgroundService starts ExecuteAsync automatically when StartAsync is called

            // Assert
            var elapsedTimeInSeconds = 0;
            while (true)
            {
                var dlqInfo = await _workerFixture.GetQueueInfo(RabbitMqEmailConfiguration.DlqName);
                if (dlqInfo.MessageCount == 1)
                    break;

                await Task.Delay(1000);

                ++elapsedTimeInSeconds;
                if (elapsedTimeInSeconds >= TimeoutInSeconds)
                    Assert.Fail($"Timeout. The test case waited for {TimeoutInSeconds} seconds but the message was not sent to DLQ.");
            }

            await cts.CancelAsync();
        }
    }
}
