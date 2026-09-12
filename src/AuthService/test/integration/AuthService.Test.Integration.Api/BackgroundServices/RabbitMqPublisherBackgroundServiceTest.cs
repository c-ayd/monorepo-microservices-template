using System.Reflection;
using System.Text.Json;
using AuthService.Api.BackgroundServices;
using AuthService.Application.Abstractions.Crypto;
using AuthService.Persistence.DbContexts;
using AuthService.Test.Integration.Api.Collections;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared.Crypto;
using Shared.RabbitMq.Helpers.Structures;
using Shared.Test.Generators;

namespace AuthService.Test.Integration.Api.BackgroundServices
{
    [Collection(nameof(AuthApiCollection))]
    public class RabbitMqPublisherBackgroundServiceTest
    {
        private readonly AuthApiCollectionCluster _collectionCluster;

        public RabbitMqPublisherBackgroundServiceTest(AuthApiCollectionCluster collectionCluster)
        {
            _collectionCluster = collectionCluster;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SaveRejectedMessagesAsync_WhenThereAreMessages_ShouldSaveMessageInDb(bool isShuttingDown)
        {
            // Arrange
            var saveRejectedMessagesMethodInfo = typeof(RabbitMqPublisherBackgroundService).GetMethod("SaveRejectedMessagesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var backgroundService = _collectionCluster.AuthApiWebApp.GetBackgroundService<RabbitMqPublisherBackgroundService>();

            var rejectedMessages = new List<Message>()
            {
                new Message(
                    publisherName: StringGenerator.GenerateAlphanumeric(),
                    exchangeName: StringGenerator.GenerateAlphanumeric(),
                    routingKey: StringGenerator.GenerateAlphanumeric(),
                    properties: new BasicProperties()
                    {
                        CorrelationId = Guid.NewGuid().ToString()
                    },
                    body: JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GenerateAlphanumeric())),
                new Message(
                    publisherName: StringGenerator.GenerateAlphanumeric(),
                    exchangeName: StringGenerator.GenerateAlphanumeric(),
                    routingKey: StringGenerator.GenerateAlphanumeric(),
                    properties: new BasicProperties()
                    {
                        CorrelationId = Guid.NewGuid().ToString()
                    },
                    body: JsonSerializer.SerializeToUtf8Bytes(StringGenerator.GenerateAlphanumeric())),
            };

            // Act
            await (Task)saveRejectedMessagesMethodInfo.Invoke(backgroundService, [rejectedMessages, isShuttingDown, default])!;

            // Assert
            using var authRejectedMessagesDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<AuthRejectedMessagesDbContext>(AuthApiCollectionCluster.AuthRejectedMessagesDbName);
            var encrpytionVerions = _collectionCluster.AuthApiWebApp.GetService<IAesGcmEncryptionVersions>();

            var messagesFromDb = await authRejectedMessagesDbContext.RejectedMessages.ToListAsync();
            Assert.Equal(rejectedMessages.Count, messagesFromDb.Count);

            foreach (var rejectedMessage in rejectedMessages)
            {
                var message = messagesFromDb.FirstOrDefault(m => m.GetBasicProperties().CorrelationId == rejectedMessage.Properties.CorrelationId);
                if (message == null)
                    Assert.Fail("One of the messages is not saved to the DB.");

                var messageBodyDecrypted = AesGcmEncryption.Decrypt(message.BodyEncrypted, encrpytionVerions.GetEncryptionKey, out var _);
                Assert.Equal(rejectedMessage.Body, messageBodyDecrypted);
            }

            authRejectedMessagesDbContext.RejectedMessages.RemoveRange(messagesFromDb);
            await authRejectedMessagesDbContext.SaveChangesAsync();
        }
    }
}
