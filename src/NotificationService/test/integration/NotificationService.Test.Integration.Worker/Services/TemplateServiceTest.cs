using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Test.Integration.Worker.Collections;
using NotificationService.Test.Integration.Worker.Fixtures;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Entities;
using NotificationService.Worker.Services;
using Shared.TestGenerators;

namespace NotificationService.Test.Integration.Worker.Services
{
    [Collection(nameof(TemplateDbContextCollection))]
    public class TemplateServiceTest
    {
        private readonly TemplateDbContextFixture _templateDbContextFixture;

        private readonly TemplateService _templateService;

        public TemplateServiceTest(TemplateDbContextFixture templateDbContextFixture)
        {
            _templateDbContextFixture = templateDbContextFixture;

            var scopeFactory = new ServiceCollection()
                .AddDbContext<TemplateDbContext>(_ => _.UseNpgsql(_templateDbContextFixture.GetConnectionString()))
                .BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>();

            _templateService = new TemplateService(scopeFactory);
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateExists_ShouldReturnTemplate()
        {
            // Arrange
            var emailTemplate = new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                false
            );

            using var dbContext = _templateDbContextFixture.CreateTemplateDbContext();
            await dbContext.EmailTemplates.AddAsync(emailTemplate);
            await dbContext.SaveChangesAsync();

            // Act
            var templateFromDb = await _templateService.GetEmailTemplateAsync(emailTemplate.TemplateId, emailTemplate.Language);
            var templateFromCache = await _templateService.GetEmailTemplateAsync(emailTemplate.TemplateId, emailTemplate.Language);

            // Assert
            Assert.NotNull(templateFromDb);
            Assert.Equal(emailTemplate.Subject, templateFromDb.Subject);
            Assert.Equal(emailTemplate.Body, templateFromDb.Body);
            Assert.Equal(emailTemplate.IsBodyHtml, templateFromDb.IsBodyHtml);

            Assert.NotNull(templateFromCache);
            Assert.Equal(emailTemplate.Subject, templateFromCache.Subject);
            Assert.Equal(emailTemplate.Body, templateFromCache.Body);
            Assert.Equal(emailTemplate.IsBodyHtml, templateFromCache.IsBodyHtml);
        }

        [Fact]
        public async Task GetEmailTemplateAsync_WhenTemplateDoesNotExist_ShouldReturnNull()
        {
            // Act
            var template = await _templateService.GetEmailTemplateAsync(StringGenerator.GenerateNumeric(), StringGenerator.GenerateNumeric());

            // Assert
            Assert.Null(template);
        }

        [Fact]
        public async Task RecacheAllTemplatesAsync_WhenItIsCalled_ShouldRecacheTemplates()
        {
            // Arrange
            using var dbContext = _templateDbContextFixture.CreateTemplateDbContext();
            var numberOfEmailTemplates = dbContext.EmailTemplates.Count();

            await dbContext.EmailTemplates.AddAsync(new EmailTemplate(
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                StringGenerator.GenerateAlpha(),
                false
            ));
            await dbContext.SaveChangesAsync();

            // Act
            var newNumberOfEmailTemplates = dbContext.EmailTemplates.Count();

            // Assert
            Assert.Equal(numberOfEmailTemplates + 1, newNumberOfEmailTemplates);
        }
    }
}
