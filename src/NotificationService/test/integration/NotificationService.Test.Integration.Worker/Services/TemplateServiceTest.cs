using System.Collections;
using System.Reflection;
using NotificationService.Test.Integration.Worker.Collections;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Entities;
using NotificationService.Worker.Services;
using Shared.Constants;
using Shared.Test.Generators;

namespace NotificationService.Test.Integration.Worker.Services
{
    [Collection(nameof(WorkerCollection))]
    public class TemplateServiceTest
    {
        private readonly WorkerCollectionCluster _collectionCluster;

        public TemplateServiceTest(WorkerCollectionCluster collectionCluster)
        {
            _collectionCluster = collectionCluster;
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateExists_ShouldReturnTemplate()
        {
            // Arrange
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                false
            );

            using var templateDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<TemplateDbContext>(WorkerCollectionCluster.TemplateDbName);
            await templateDbContext.EmailTemplates.AddAsync(emailTemplate);
            await templateDbContext.SaveChangesAsync();

            var templateService = (TemplateService)_collectionCluster.NotificationWebApp.GetService<ITemplateService>();
            await templateService.RecacheTemplatesAsync();

            // Act
            var template = templateService.GetEmailTemplate(emailTemplate.TemplateId, emailTemplate.Language);

            // Assert
            Assert.NotNull(template);
            Assert.Equal(emailTemplate.Subject, template.Subject);
            Assert.Equal(emailTemplate.Body, template.Body);
            Assert.Equal(emailTemplate.IsBodyHtml, template.IsBodyHtml);
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateWithDefaultLangExistsButGivenLangIsNotGiven_ShouldReturnTemplateWithDefaultLang()
        {
            // Arrange
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlphanumeric(),
                SupportedLanguages.DefaultLanguage,
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                false);

            using var templateDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<TemplateDbContext>(WorkerCollectionCluster.TemplateDbName);
            await templateDbContext.EmailTemplates.AddAsync(emailTemplate);
            await templateDbContext.SaveChangesAsync();

            var templateService = (TemplateService)_collectionCluster.NotificationWebApp.GetService<ITemplateService>();
            await templateService.RecacheTemplatesAsync();

            // Act
            var template = templateService.GetEmailTemplate(emailTemplate.TemplateId, "test");

            // Assert
            Assert.NotNull(template);
            Assert.Equal(emailTemplate.Subject, template.Subject);
            Assert.Equal(emailTemplate.Body, template.Body);
            Assert.Equal(emailTemplate.IsBodyHtml, template.IsBodyHtml);
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var templateService = (TemplateService)_collectionCluster.NotificationWebApp.GetService<ITemplateService>();

            // Act
            var template = templateService.GetEmailTemplate(StringGenerator.GenerateNumeric(), StringGenerator.GenerateNumeric());

            // Assert
            Assert.Null(template);
        }

        [Fact]
        public async Task RecacheAllTemplatesAsync_WhenItIsCalled_ShouldRecacheTemplates()
        {
            // Arrange
            var templateService = (TemplateService)_collectionCluster.NotificationWebApp.GetService<ITemplateService>();
            await templateService.RecacheTemplatesAsync();
            
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                false);

            using var templateDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<TemplateDbContext>(WorkerCollectionCluster.TemplateDbName);
            await templateDbContext.EmailTemplates.AddAsync(emailTemplate);
            await templateDbContext.SaveChangesAsync();

            var numberOfEmailTemplates = ((IDictionary)(typeof(TemplateService)
                .GetField("_emailTemplates", BindingFlags.NonPublic | BindingFlags.Instance)!)
                .GetValue(templateService)!).Count;

            // Act
            await templateService.RecacheTemplatesAsync();

            var newNumberOfEmailTemplates = ((IDictionary)(typeof(TemplateService)
                .GetField("_emailTemplates", BindingFlags.NonPublic | BindingFlags.Instance)!)
                .GetValue(templateService)!).Count;

            // Assert
            Assert.Equal(numberOfEmailTemplates + 1, newNumberOfEmailTemplates);
        }
    }
}
