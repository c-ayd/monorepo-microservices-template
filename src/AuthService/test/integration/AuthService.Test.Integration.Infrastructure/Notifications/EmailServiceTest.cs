using System.Reflection;
using System.Text.Json;
using AuthService.Infrastructure.MessageBrokers;
using AuthService.Infrastructure.Notifications;
using AuthService.Test.Integration.Infrastructure.Collections;
using AuthService.Test.Utility.Fixtures;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.RabbitMq.Notifications.Configurations;
using Shared.RabbitMq.Notifications.Messages;
using Shared.Test.Generators;

namespace AuthService.Test.Integration.Infrastructure.Notifications
{
    [Collection(nameof(RabbitMqCollection))]
    public class EmailServiceTest
    {
        private const int TimeoutInSeconds = 30;

        private readonly RabbitMqFixture _rabbitMqFixture;

        private readonly EmailService _emailService;
        private readonly IChannel _channel;
    
        public EmailServiceTest(RabbitMqFixture rabbitMqFixture)
        {
            _rabbitMqFixture = rabbitMqFixture;

            var rabbitMqOptions = rabbitMqFixture.GetRabbitMqOptions();
            var rabbitMqConnectionService = new RabbitMqConnectionService(Options.Create(rabbitMqOptions));
            rabbitMqConnectionService.ConnectAsync().GetAwaiter().GetResult();

            _emailService = new EmailService(rabbitMqConnectionService);
            var channelFieldInfo = typeof(EmailService).GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _channel = ((Lazy<Task<IChannel>>)channelFieldInfo.GetValue(_emailService)!).Value.GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Send_WhenNoErrorHappens_ShouldPutMessageInQueue()
        {
            // Arrange
            await _channel.QueueDeclareAsync(
                queue: RabbitMqEmailConfiguration.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            await _channel.QueueBindAsync(
                queue: RabbitMqEmailConfiguration.QueueName,
                exchange: RabbitMqEmailConfiguration.ExchangeName,
                routingKey: RabbitMqEmailConfiguration.RoutingKey);

            var to = new string[] {EmailGenerator.Generate() };
            var templateId = StringGenerator.GeneratePrintableAscii();
            var language = StringGenerator.GeneratePrintableAscii();
            var subjectParams = new string[] { StringGenerator.GeneratePrintableAscii() };
            var bodyParams = new string[] { StringGenerator.GeneratePrintableAscii(), StringGenerator.GeneratePrintableAscii() };
            var message = new RabbitMqEmailMessage(to, templateId, language, subjectParams, bodyParams);

            // Act
            await _emailService.SendAsync(message);

            // Assert
            var result = await _channel.BasicGetAsync(
                queue: RabbitMqEmailConfiguration.QueueName,
                autoAck: true);

            await _channel.QueueDeleteAsync(
                queue: RabbitMqEmailConfiguration.QueueName,
                ifUnused: false,
                ifEmpty: false);

            Assert.NotNull(result);

            var resultMessage = JsonSerializer.Deserialize<RabbitMqEmailMessage>(result.Body.Span)!;
            Assert.True(to.SequenceEqual(resultMessage.To), "The target email addresses are different.");
            Assert.Equal(templateId, resultMessage.TemplateId);
            Assert.Equal(language, resultMessage.Language);
            Assert.True(subjectParams.SequenceEqual(resultMessage.SubjectParameters), "The subject parameters are different.");
            Assert.True(bodyParams.SequenceEqual(resultMessage.BodyParameters), "The body parameters are different.");
        }

        [Fact]
        public async Task Send_WhenErrorHappens_ShouldPutMessageInDlq()
        {
            // Arrange
            await _channel.QueueDeclareAsync(
                queue: RabbitMqEmailConfiguration.DlqName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            await _channel.QueueBindAsync(
                queue: RabbitMqEmailConfiguration.DlqName,
                exchange: RabbitMqEmailConfiguration.DlxName,
                routingKey: RabbitMqEmailConfiguration.DeadLetterRoutingKey);

            var to = new string[] { EmailGenerator.Generate() };
            var templateId = StringGenerator.GeneratePrintableAscii();
            var language = StringGenerator.GeneratePrintableAscii();
            var subjectParams = new string[] { StringGenerator.GeneratePrintableAscii() };
            var bodyParams = new string[] { StringGenerator.GeneratePrintableAscii(), StringGenerator.GeneratePrintableAscii() };
            var message = new RabbitMqEmailMessage(to, templateId, language, subjectParams, bodyParams);

            // Act
            await _emailService.SendAsync(message);

            // Assert
            BasicGetResult? result = null;
            var elapsedTimeInSeconds = 0;
            while (true)
            {
                await Task.Delay(1000);
                
                result = await _channel.BasicGetAsync(
                    queue: RabbitMqEmailConfiguration.DlqName,
                    autoAck: true);

                if (result != null)
                    break;

                ++elapsedTimeInSeconds;
                if (elapsedTimeInSeconds >= TimeoutInSeconds)
                {
                    await _channel.QueueDeleteAsync(
                        queue: RabbitMqEmailConfiguration.DlqName,
                        ifUnused: false,
                        ifEmpty: false);
                    
                    Assert.Fail($"Timeout. The test case waited for {TimeoutInSeconds} seconds but the message did not arrive the DLQ.");
                }
            }

            await _channel.QueueDeleteAsync(
                queue: RabbitMqEmailConfiguration.DlqName,
                ifUnused: false,
                ifEmpty: false);

            var resultMessage = JsonSerializer.Deserialize<RabbitMqEmailMessage>(result.Body.Span)!;
            Assert.True(to.SequenceEqual(resultMessage.To), "The target email addresses are different.");
            Assert.Equal(templateId, resultMessage.TemplateId);
            Assert.Equal(language, resultMessage.Language);
            Assert.True(subjectParams.SequenceEqual(resultMessage.SubjectParameters), "The subject parameters are different.");
            Assert.True(bodyParams.SequenceEqual(resultMessage.BodyParameters), "The body parameters are different.");
        }
    }
}
