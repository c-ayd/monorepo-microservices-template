using Microsoft.Extensions.Options;
using NotificationService.Test.Integration.Worker.Collections;
using NotificationService.Test.Integration.Worker.Fixtures;
using NotificationService.Worker.Services;
using NotificationService.Workers;
using Shared.RabbitMq.Notification.Configurations;
using Shared.RabbitMq.Notification.Messages;
using Shared.TestGenerators;

namespace NotificationService.Test.Integration.Worker.Workers
{
    [Collection(nameof(RabbitMqCollection))]
    public class EmailWorkerTest : IClassFixture<EmailServiceFixture>, IClassFixture<LoggerFixture<EmailWorker>>, IAsyncLifetime
    {
        private const int TimeoutInSeconds = 30;

        private readonly RabbitMqFixture _rabbitMqFixture;
        private readonly EmailServiceFixture _emailServiceFixture;

        private readonly EmailWorker _worker;

        private readonly CancellationTokenSource _cts;

        public EmailWorkerTest(
            RabbitMqFixture rabbitMqFixture,
            EmailServiceFixture emailServiceFixture,
            LoggerFixture<EmailWorker> loggerFixture)
        {
            _rabbitMqFixture = rabbitMqFixture;
            _emailServiceFixture = emailServiceFixture;

            _worker = new EmailWorker(
                new RabbitMqConnectionService(Options.Create(_rabbitMqFixture.GetRabbitMqOptions())),
                new SmtpService(Options.Create(_emailServiceFixture.SmtpOptions)),
                loggerFixture);
            
            _cts = new CancellationTokenSource();
        }

        public async Task InitializeAsync()
        {
            await _worker.StartAsync(_cts.Token);
        }

        public async Task DisposeAsync()
        {
            await _worker.StopAsync(_cts.Token);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMessageInQueue_ShouldSendEmail()
        {
            // Arrange
            var to = EmailGenerator.Generate();
            var subject = StringGenerator.GenerateAlphanumeric();
            var body = StringGenerator.GenerateAlphanumeric();

            await _rabbitMqFixture.PublishMessageAsync(
                new EmailMessage([to], subject, body, IsBodyHtml: false),
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                TimeoutInSeconds);

            // Act
            // _worker starts ExecuteAsync automatically when StartAsync is called

            // Assert
            var elapsedTimeInSeconds = 0;
            while (true)
            {
                var emails = await _emailServiceFixture.GetEmails();
                if (emails.Count == 1)
                    break;

                await Task.Delay(1000);

                ++elapsedTimeInSeconds;
                if (elapsedTimeInSeconds >= TimeoutInSeconds)
                    Assert.Fail($"Timeout. The test case waited for {TimeoutInSeconds} seconds but the message was not processed.");
            }

            await _cts.CancelAsync();
        }

        [Fact]
        public async Task ExecuteAsync_WhenMessageIsPoisoned_ShouldSendMessageToDlq()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var message = "{}";

            await _rabbitMqFixture.PublishMessageAsync(
                message,
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                TimeoutInSeconds);

            // Act
            // _worker starts ExecuteAsync automatically when StartAsync is called

            // Assert
            var elapsedTimeInSeconds = 0;
            while (true)
            {
                var dlqInfo = await _rabbitMqFixture.GetQueueInfo(EmailConfiguration.DlqName);
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
