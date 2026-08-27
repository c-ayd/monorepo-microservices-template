using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Test.Integration.Worker.Collections;
using NotificationService.Test.Integration.Worker.Fixtures;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Entities;
using NotificationService.Worker.Services;
using Shared.Test.Generators;

namespace NotificationService.Test.Integration.Worker.Services
{
    [Collection(nameof(WorkerCollection))]
    public class TemplateServiceTest
    {
        private readonly WorkerFixture _workerFixture;

        private readonly TemplateService _templateService;

        public TemplateServiceTest(WorkerFixture workerFixture)
        {
            _workerFixture = workerFixture;

            var scopeFactory = new ServiceCollection()
                .AddDbContext<TemplateDbContext>(_ => _.UseNpgsql(_workerFixture.GetTemplateDbConnectionString()))
                .BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();

            _templateService = new TemplateService(scopeFactory);
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateExists_ShouldReturnTemplate()
        {
            // Arrange
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(10),
                StringGenerator.GenerateAlpha(10),
                StringGenerator.GenerateAlpha(10),
                StringGenerator.GenerateAlpha(10),
                false
            );

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();

            await _templateService.RecacheAllTemplatesAsync();

            // Act
            var template = _templateService.GetEmailTemplateAsync(emailTemplate.TemplateId, emailTemplate.Language);

            // Assert
            Assert.NotNull(template);
            Assert.Equal(emailTemplate.Subject, template.Subject);
            Assert.Equal(emailTemplate.Body, template.Body);
            Assert.Equal(emailTemplate.IsBodyHtml, template.IsBodyHtml);
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateWithDefaultLangExistsAndLangIsNotGiven_ShouldReturnTemplate()
        {
            // Arrange
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(11),
                TemplateService.DefaultLanguage,
                StringGenerator.GenerateAlpha(11),
                StringGenerator.GenerateAlpha(11),
                false
            );

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();

            await _templateService.RecacheAllTemplatesAsync();

            // Act
            var template = _templateService.GetEmailTemplateAsync(emailTemplate.TemplateId);

            // Assert
            Assert.NotNull(template);
            Assert.Equal(emailTemplate.Subject, template.Subject);
            Assert.Equal(emailTemplate.Body, template.Body);
            Assert.Equal(emailTemplate.IsBodyHtml, template.IsBodyHtml);
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateDoesNotExist_ShouldReturnNull()
        {
            // Act
            var template = _templateService.GetEmailTemplateAsync(StringGenerator.GenerateNumeric(), StringGenerator.GenerateNumeric());

            // Assert
            Assert.Null(template);
        }

        [Fact]
        public async Task RecacheAllTemplatesAsync_WhenItIsCalled_ShouldRecacheTemplates()
        {
            // Arrange
            await _templateService.RecacheAllTemplatesAsync();
            
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(12),
                StringGenerator.GenerateAlpha(12),
                StringGenerator.GenerateAlpha(12),
                StringGenerator.GenerateAlpha(12),
                false
            );

            using var dbContext = _workerFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();

            var numberOfEmailTemplates = ((IDictionary)(typeof(TemplateService)
                .GetField("_emailTemplates", BindingFlags.NonPublic | BindingFlags.Instance)!)
                .GetValue(_templateService)!).Count;

            // Act
            await _templateService.RecacheAllTemplatesAsync();

            var newNumberOfEmailTemplates = ((IDictionary)(typeof(TemplateService)
                .GetField("_emailTemplates", BindingFlags.NonPublic | BindingFlags.Instance)!)
                .GetValue(_templateService)!).Count;

            // Assert
            Assert.Equal(numberOfEmailTemplates + 1, newNumberOfEmailTemplates);
        }
    }
}
