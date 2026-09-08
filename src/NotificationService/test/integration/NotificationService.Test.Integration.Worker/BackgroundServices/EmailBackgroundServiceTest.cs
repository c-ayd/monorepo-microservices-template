using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NotificationService.Test.Integration.Worker.Collections;
using NotificationService.Test.Integration.Worker.Fixtures;
using NotificationService.Worker.BackgroundServices;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Entities;
using NotificationService.Worker.Services;
using RabbitMQ.Client;
using Shared.RabbitMq.Notifications.Configurations;
using Shared.RabbitMq.Notifications.Messages;
using Shared.Test.Generators;
using Shared.Test.Helpers.Fixtures;

namespace NotificationService.Test.Integration.Worker.BackgroundServices
{
    [Collection(nameof(WorkerCollection))]
    public class EmailBackgroundServiceTest : IClassFixture<EmailServiceFixture>, IClassFixture<LoggerFixture<EmailBackgroundService>>, IAsyncLifetime
    {
        private const int _timeoutInSeconds = 30;

        private readonly WorkerFixture _workerFixture;
        private readonly EmailBackgroundService _emailBackgroundService;
        private readonly TemplateService _templateServiceFixture;
        private readonly EmailServiceFixture _emailServiceFixture;

        public EmailBackgroundServiceTest(
            WorkerFixture workerFixture,
            EmailServiceFixture emailServiceFixture,
            LoggerFixture<EmailBackgroundService> loggerFixture)
        {
            _workerFixture = workerFixture;

            var scopeFactory = new ServiceCollection()
                .AddDbContext<TemplateDbContext>(_ => _.UseNpgsql(_workerFixture.GetTemplateDbConnectionString()))
                .BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();
            _templateServiceFixture = new TemplateService(scopeFactory);

            _emailServiceFixture = emailServiceFixture;

            _emailBackgroundService = new EmailBackgroundService(
                Options.Create(_workerFixture.GetRabbitMqOptions()),
                _templateServiceFixture,
                new SmtpService(Options.Create(emailServiceFixture.SmtpOptions)),
                loggerFixture
            );
        }

        public async Task InitializeAsync()
        {
            await _emailBackgroundService.StartAsync(default);
        }

        public async Task DisposeAsync()
        {
            await _emailBackgroundService.StopAsync(default);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReceivedAsync_WhenMessageIsNotSerialized_ShouldPutMessageInDLQ(bool isJson)
        {
            // Arrange
            var mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            var dlq = await _workerFixture.GetQueueInfo(EmailConfiguration.DlqName);
            if (mainQueue.MessageCount != 0 || dlq.MessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueue.MessageCount}, DLQ: {dlq.MessageCount}.");

            string message = isJson ? "{}" : "Test message";
            await _workerFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                JsonSerializer.SerializeToUtf8Bytes(message),
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                });

            // Act
            // _emailBackgroundService should be receiving the message already

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                dlq = await _workerFixture.GetQueueInfo(EmailConfiguration.DlqName);
                if (dlq.MessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the DLQ.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            var messageFromRabbitMq = await _workerFixture.GetMessageAsync(EmailConfiguration.DlqName);

            Assert.NotNull(messageFromRabbitMq);
            Assert.Equal(message, JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(messageFromRabbitMq.Body.ToArray())));

            mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            Assert.Equal((uint)0, mainQueue.MessageCount);
        }

        [Fact]
        public async Task ReceivedAsync_WhenTemplatIsNotFoundForFirstTime_ShouldPutMessageInRetryQueueWithProperExpiration()
        {
            // Arrange
            var mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            var retryQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.RetryQueueName);
            if (mainQueue.MessageCount != 0 || retryQueue.MessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueue.MessageCount}, Retry: {retryQueue.MessageCount}.");

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                null,
                null);
            
            await _workerFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                JsonSerializer.SerializeToUtf8Bytes(message),
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                });

            // Act
            // _emailBackgroundService should be receiving the message already

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                retryQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.RetryQueueName);
                if (retryQueue.MessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the retry queue.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            var messageFromRabbitMq = await _workerFixture.GetMessageAsync(EmailConfiguration.RetryQueueName);

            Assert.NotNull(messageFromRabbitMq);
            Assert.Equal(((int)TemplateBackgroundService.CacheDuration.TotalMilliseconds).ToString(), messageFromRabbitMq.BasicProperties.Expiration);

            mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            Assert.Equal((uint)0, mainQueue.MessageCount);
        }

        [Fact]
        public async Task ReceivedAsync_WhenTemplatIsNotFoundForSecondTime_ShouldPutMessageInDlq()
        {
            // Arrange
            var mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            var dlq = await _workerFixture.GetQueueInfo(EmailConfiguration.DlqName);
            if (mainQueue.MessageCount != 0 || dlq.MessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueue.MessageCount}, DLQ: {dlq.MessageCount}.");

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                null,
                null);

            await _workerFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                JsonSerializer.SerializeToUtf8Bytes(message),
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Headers = new Dictionary<string, object?>()
                    {
                        { "Template-Not-Found", true }
                    }
                });

            // Act
            // _emailBackgroundService should be receiving the message already

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                dlq = await _workerFixture.GetQueueInfo(EmailConfiguration.DlqName);
                if (dlq.MessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the DLQ.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            await _workerFixture.ClearQueue(EmailConfiguration.DlqName);

            mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            Assert.Equal((uint)0, mainQueue.MessageCount);
        }

        [Fact]
        public async Task ReceivedAsync_WhenEmailIsSent_ShouldAcknowledgeMessage()
        {
            // Arrange
            var mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            if (mainQueue.MessageCount != 0)
                Assert.Fail($"The main queue is not empty.");

            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                "Subject: {0}",
                "Body: {0} {1}",
                false);

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();
            await _templateServiceFixture.RecacheTemplatesAsync();

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                emailTemplate.TemplateId,
                emailTemplate.Language,
                [StringGenerator.GeneratePrintableAscii()],
                [StringGenerator.GeneratePrintableAscii(), StringGenerator.GeneratePrintableAscii()]);

            await _workerFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                JsonSerializer.SerializeToUtf8Bytes(message),
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                });

            // Act
            // _emailBackgroundService should be receiving the message already

            // Assert
            EmailServiceFixture.MailHogDto? email = null;
            var elapsedTime = 0;
            while (true)
            {
                var emails = await _emailServiceFixture.GetEmails();
                if (emails.Count == 1)
                {
                    email = emails[0];
                    break;
                }

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the email was not sent.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            Assert.Equal((uint)0, mainQueue.MessageCount);

            Assert.Equal(message.To[0], email.To[0]);
            Assert.Equal("Subject: " + message.SubjectParameters![0], email.Subject);
            Assert.Equal("Body: " + message.BodyParameters![0] + " " + message.BodyParameters[1], email.Body!.TrimEnd());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task ReceivedAsync_WhenEmailIsNotSent_ShouldPutMessageInRetryQueueWithProperExpiration(int retryCount)
        {
            // Arrange
            var mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            var retryQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.RetryQueueName);
            if (mainQueue.MessageCount != 0 || retryQueue.MessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueue.MessageCount}, Retry: {retryQueue.MessageCount}.");

            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha() + "{10}",
                StringGenerator.GenerateAlpha() + "{10}",
                false);

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();
            await _templateServiceFixture.RecacheTemplatesAsync();

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                emailTemplate.TemplateId,
                emailTemplate.Language,
                [StringGenerator.GeneratePrintableAscii()],
                [StringGenerator.GeneratePrintableAscii()]);

            var properties = new BasicProperties()
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            if (retryCount != 0)
            {
                properties.Headers = new Dictionary<string, object?>()
                {
                    { "Retry-Count", retryCount }
                };
            }

            await _workerFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                JsonSerializer.SerializeToUtf8Bytes(message),
                properties);

            // Act
            // _emailBackgroundService should be receiving the message already

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                retryQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.RetryQueueName);
                if (retryQueue.MessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the retry queue.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            var messageFromRabbitMq = await _workerFixture.GetMessageAsync(EmailConfiguration.RetryQueueName);

            var retryDelayTimeFieldInfo = typeof(EmailBackgroundService).GetField("_retryDelayTime", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var retryDelayTime = (TimeSpan)retryDelayTimeFieldInfo.GetValue(_emailBackgroundService)!;

            Assert.NotNull(messageFromRabbitMq);
            Assert.Equal(retryCount + 1, (int)messageFromRabbitMq.BasicProperties.Headers!["Retry-Count"]!);
            Assert.Equal(((int)retryDelayTime.TotalMilliseconds * (retryCount + 1)).ToString(), messageFromRabbitMq.BasicProperties.Expiration);

            mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            Assert.Equal((uint)0, mainQueue.MessageCount);
        }

        [Fact]
        public async Task ReceivedAsync_WhenEmailIsNotSentAndMessageExceedsRetryLimit_ShouldPutMessageInDlq()
        {
            // Arrange
            var mainQueue = await _workerFixture.GetQueueInfo(EmailConfiguration.QueueName);
            var dlq = await _workerFixture.GetQueueInfo(EmailConfiguration.DlqName);
            if (mainQueue.MessageCount != 0 || dlq.MessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueue.MessageCount}, DLQ: {dlq.MessageCount}.");

            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha() + "{10}",
                StringGenerator.GenerateAlpha() + "{10}",
                false);

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();
            await _templateServiceFixture.RecacheTemplatesAsync();

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                emailTemplate.TemplateId,
                emailTemplate.Language,
                [StringGenerator.GeneratePrintableAscii()],
                [StringGenerator.GeneratePrintableAscii()]);

            var maxRetryFieldInfo = typeof(EmailBackgroundService).GetField("_maxRetry", BindingFlags.NonPublic | BindingFlags.Static)!;
            var maxRetry = (int)maxRetryFieldInfo.GetValue(null)!;

            await _workerFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                JsonSerializer.SerializeToUtf8Bytes(message),
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Headers = new Dictionary<string, object?>()
                    {
                        { "Retry-Count", maxRetry }
                    }
                });

            // Act
            // _emailBackgroundService should be receiving the message already

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                dlq = await _workerFixture.GetQueueInfo(EmailConfiguration.DlqName);
                if (dlq.MessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the retry queue.");

                await Task.Delay(1000);
                ++elapsedTime;
            }
        }
    }
}
