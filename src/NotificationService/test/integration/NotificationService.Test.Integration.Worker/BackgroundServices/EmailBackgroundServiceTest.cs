using System.Reflection;
using System.Text;
using System.Text.Json;
using NotificationService.Test.Integration.Worker.Collections;
using NotificationService.Worker.Abstractions;
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
    public class EmailBackgroundServiceTest
    {
        private const int _timeoutInSeconds = 5;

        private readonly WorkerCollectionCluster _collectionCluster;

        public EmailBackgroundServiceTest(WorkerCollectionCluster collectionCluster)
        {
            _collectionCluster = collectionCluster;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReceivedAsync_WhenMessageIsNotSerialized_ShouldPutMessageInDlq(bool isJson)
        {
            // Arrange
            var mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
            if (mainQueueMessageCount != 0 || dlqMessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueueMessageCount}, DLQ: {dlqMessageCount}.");

            string message = isJson ? "{}" : "Test message";
            await _collectionCluster.RabbitMqFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                },
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Act
            // EmailBackgroundService should automatically handle the message.

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
                if (dlqMessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the DLQ.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            var messageFromRabbitMq = await _collectionCluster.RabbitMqFixture.GetNextMessageAsync(EmailConfiguration.DlqName);
            Assert.NotNull(messageFromRabbitMq);
            Assert.Equal(message, JsonSerializer.Deserialize<string>(Encoding.UTF8.GetString(messageFromRabbitMq.Body.ToArray())));

            mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
            Assert.Equal((uint)0, mainQueueMessageCount);
            Assert.Equal((uint)0, retryQueueMessageCount);

            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.QueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.RetryQueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.DlqName);
        }

        [Fact]
        public async Task ReceivedAsync_WhenTemplateIsNotFoundForFirstTime_ShouldPutMessageInRetryQueueWithProperExpiration()
        {
            // Arrange
            var mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
            if (mainQueueMessageCount != 0 || retryQueueMessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueueMessageCount}, Retry: {retryQueueMessageCount}.");

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                null,
                null);
            
            await _collectionCluster.RabbitMqFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                },
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Act
            // EmailBackgroundService should automatically handle the message.

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
                if (retryQueueMessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the retry queue.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            var messageFromRabbitMq = await _collectionCluster.RabbitMqFixture.GetNextMessageAsync(EmailConfiguration.RetryQueueName);
            Assert.NotNull(messageFromRabbitMq);
            Assert.Equal(((int)TemplateBackgroundService.CacheDuration.TotalMilliseconds).ToString(), messageFromRabbitMq.BasicProperties.Expiration);

            mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
            Assert.Equal((uint)0, mainQueueMessageCount);
            Assert.Equal((uint)0, dlqMessageCount);

            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.QueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.RetryQueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.DlqName);
        }

        [Fact]
        public async Task ReceivedAsync_WhenTemplateIsNotFoundForSecondTime_ShouldPutMessageInDlq()
        {
            // Arrange
            var mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
            if (mainQueueMessageCount != 0 || dlqMessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueueMessageCount}, DLQ: {dlqMessageCount}.");

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                null,
                null);

            await _collectionCluster.RabbitMqFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Headers = new Dictionary<string, object?>()
                    {
                        { "Template-Not-Found", true }
                    }
                },
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Act
            // EmailBackgroundService should automatically handle the message.

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
                if (dlqMessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the DLQ.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
            Assert.Equal((uint)0, mainQueueMessageCount);
            Assert.Equal((uint)0, retryQueueMessageCount);

            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.QueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.RetryQueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.DlqName);
        }

        [Fact]
        public async Task ReceivedAsync_WhenEmailIsSent_ShouldAcknowledgeMessage()
        {
            // Arrange
            var mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            if (mainQueueMessageCount != 0)
                Assert.Fail($"The main queue is not empty. Message Count: {mainQueueMessageCount}.");

            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                "Subject: {0}",
                "Body: {0} {1}",
                false);

            using var templateDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<TemplateDbContext>(WorkerCollectionCluster.TemplateDbName);
            await templateDbContext.EmailTemplates.AddAsync(emailTemplate);
            await templateDbContext.SaveChangesAsync();

            var templateService = (TemplateService)_collectionCluster.NotificationWebApp.GetService<ITemplateService>();
            await templateService.RecacheTemplatesAsync();

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                emailTemplate.TemplateId,
                emailTemplate.Language,
                [StringGenerator.GenerateAlphanumeric()],
                [StringGenerator.GenerateAlphanumeric(), StringGenerator.GenerateAlphanumeric()]);

            await _collectionCluster.RabbitMqFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                },
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Act
            // EmailBackgroundService should automatically handle the message.

            // Assert
            SmtpFixture.SentEmail? email = null;
            var elapsedTime = 0;
            while (true)
            {
                var emails = await _collectionCluster.SmtpFixture.GetEmailsAsync();
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

            Assert.Equal(message.To[0], email.To[0]);
            Assert.Equal("Subject: " + message.SubjectParameters![0], email.Subject);
            Assert.Equal("Body: " + message.BodyParameters![0] + " " + message.BodyParameters[1], email.Body!.TrimEnd());

            mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
            var dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
            Assert.Equal((uint)0, mainQueueMessageCount);
            Assert.Equal((uint)0, retryQueueMessageCount);
            Assert.Equal((uint)0, dlqMessageCount);

            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.QueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.RetryQueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.DlqName);
            await _collectionCluster.SmtpFixture.ClearEmailsAsync();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task ReceivedAsync_WhenEmailIsNotSent_ShouldPutMessageInRetryQueueWithProperExpiration(int retryCount)
        {
            // Arrange
            var mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
            if (mainQueueMessageCount != 0 || retryQueueMessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueueMessageCount}, Retry: {retryQueueMessageCount}.");

            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha() + "{10}",
                StringGenerator.GenerateAlpha() + "{10}",
                false);

            using var templateDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<TemplateDbContext>(WorkerCollectionCluster.TemplateDbName);
            await templateDbContext.EmailTemplates.AddAsync(emailTemplate);
            await templateDbContext.SaveChangesAsync();

            var templateService = (TemplateService)_collectionCluster.NotificationWebApp.GetService<ITemplateService>();
            await templateService.RecacheTemplatesAsync();

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                emailTemplate.TemplateId,
                emailTemplate.Language,
                [StringGenerator.GenerateAlphanumeric()],
                [StringGenerator.GenerateAlphanumeric()]);

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

            await _collectionCluster.RabbitMqFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                properties,
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Act
            // EmailBackgroundService should automatically handle the message.

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
                if (retryQueueMessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the retry queue.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            var emailBackgroundService = _collectionCluster.NotificationWebApp.GetBackgroundService<EmailBackgroundService>();
            var retryDelayTimeFieldInfo = typeof(EmailBackgroundService).GetField("_retryDelayTime", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var retryDelayTime = (TimeSpan)retryDelayTimeFieldInfo.GetValue(emailBackgroundService)!;

            var messageFromRabbitMq = await _collectionCluster.RabbitMqFixture.GetNextMessageAsync(EmailConfiguration.RetryQueueName);
            Assert.NotNull(messageFromRabbitMq);
            Assert.Equal(retryCount + 1, (int)messageFromRabbitMq.BasicProperties.Headers!["Retry-Count"]!);
            Assert.Equal(((int)retryDelayTime.TotalMilliseconds * (retryCount + 1)).ToString(), messageFromRabbitMq.BasicProperties.Expiration);

            mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
            Assert.Equal((uint)0, mainQueueMessageCount);
            Assert.Equal((uint)0, dlqMessageCount);

            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.QueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.RetryQueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.DlqName);
        }

        [Fact]
        public async Task ReceivedAsync_WhenEmailIsNotSentAndMessageExceedsRetryLimit_ShouldPutMessageInDlq()
        {
            // Arrange
            var mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
            if (mainQueueMessageCount != 0 || dlqMessageCount != 0)
                Assert.Fail($"The queues are not empty. Main: {mainQueueMessageCount}, DLQ: {dlqMessageCount}.");

            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha() + "{10}",
                StringGenerator.GenerateAlpha() + "{10}",
                false);

            using var templateDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<TemplateDbContext>(WorkerCollectionCluster.TemplateDbName);
            await templateDbContext.EmailTemplates.AddAsync(emailTemplate);
            await templateDbContext.SaveChangesAsync();

            var templateService = (TemplateService)_collectionCluster.NotificationWebApp.GetService<ITemplateService>();
            await templateService.RecacheTemplatesAsync();

            var message = new EmailMessage(
                [EmailGenerator.Generate()],
                emailTemplate.TemplateId,
                emailTemplate.Language,
                [StringGenerator.GenerateAlphanumeric()],
                [StringGenerator.GenerateAlphanumeric()]);

            var maxRetryFieldInfo = typeof(EmailBackgroundService).GetField("_maxRetry", BindingFlags.NonPublic | BindingFlags.Static)!;
            var maxRetry = (int)maxRetryFieldInfo.GetValue(null)!;

            await _collectionCluster.RabbitMqFixture.PublishMessageAsync(
                EmailConfiguration.ExchangeName,
                EmailConfiguration.RoutingKey,
                new BasicProperties()
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Headers = new Dictionary<string, object?>()
                    {
                        { "Retry-Count", maxRetry }
                    }
                },
                JsonSerializer.SerializeToUtf8Bytes(message));

            // Act
            // EmailBackgroundService should automatically handle the message.

            // Assert
            var elapsedTime = 0;
            while (true)
            {
                dlqMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.DlqName);
                if (dlqMessageCount == 1)
                    break;

                if (elapsedTime >= _timeoutInSeconds)
                    Assert.Fail($"The test waited for {_timeoutInSeconds} seconds, but the message was not in the retry queue.");

                await Task.Delay(1000);
                ++elapsedTime;
            }

            mainQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.QueueName);
            var retryQueueMessageCount = await _collectionCluster.RabbitMqFixture.GetMessageCountAsync(EmailConfiguration.RetryQueueName);
            Assert.Equal((uint)0, mainQueueMessageCount);
            Assert.Equal((uint)0, retryQueueMessageCount);

            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.QueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.RetryQueueName);
            await _collectionCluster.RabbitMqFixture.ClearMessagesAsync(EmailConfiguration.DlqName);
        }
    }
}
