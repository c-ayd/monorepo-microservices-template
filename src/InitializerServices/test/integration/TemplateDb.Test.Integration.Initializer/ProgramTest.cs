using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Entities;
using Shared.Test.Generators;
using Shared.Test.Helpers;
using Shared.Test.Helpers.Fixtures;
using TemplateDb.Initializer.Options;
using TemplateDb.Test.Integration.Initializer.Collections;

namespace TemplateDb.Test.Integration.Initializer
{
    [Collection(nameof(PostgreSqlCollection))]
    public class ProgramTest
    {
        private readonly PostgreSqlFixture _postgreSqlFixture;

        public ProgramTest(PostgreSqlCollectionCluster collectionCluster)
        {
            _postgreSqlFixture = collectionCluster.PostgreSqlFixture;
        }

        [Fact]
        public async Task InitializeAsync_WhenThereIsNoDataInDb_ShouldSeedDataToDb()
        {
            // Arrange
            await _postgreSqlFixture.DropDatabaseAsync<TemplateDbContext>();

            var configuration = ConfigurationHelper.CreateConfigurationFromTestSettings();
            var seedDataOptions = configuration.GetSection("Templates").Get<TemplateDbSeedDataOptions>()!;

            await using var templateDbContext = _postgreSqlFixture.CreateDbContext<TemplateDbContext>();

            // Act
            await ProgramInitializeAsync(seedDataOptions, templateDbContext);

            // Assert
            templateDbContext.ChangeTracker.Clear();

            var emailTemplatesFromDb = await templateDbContext.EmailTemplates.ToListAsync();
            Assert.Equal(seedDataOptions.Email.Count, emailTemplatesFromDb.Count);
            
            foreach (var emailTemplate in seedDataOptions.Email)
            {
                var template = emailTemplatesFromDb
                    .FirstOrDefault(t => t.TemplateId == emailTemplate.TemplateId &&
                        t.Language == emailTemplate.Language);

                if (template == null)
                    Assert.Fail($"Template with {emailTemplate.TemplateId} id and {emailTemplate.Language} language is not saved to the DB.");

                var body = await File.ReadAllTextAsync(AppContext.BaseDirectory + emailTemplate.Body);
                Assert.Equal(emailTemplate.Subject, template.Subject);
                Assert.Equal(body, template.Body);
                Assert.Equal(emailTemplate.IsBodyHtml, template.IsBodyHtml);
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenThereIsSomeSameAndSomeNewDataInSeedData_ShouldUpdateSameOnesAndAddNewOnes()
        {
            // Arrange
            await _postgreSqlFixture.ClearDatabaseAsync<TemplateDbContext>();

            var configuration = ConfigurationHelper.CreateConfigurationFromTestSettings();
            var seedDataOptions = configuration.GetSection("Templates").Get<TemplateDbSeedDataOptions>()!;

            var firstTemplateId = seedDataOptions.Email[0].TemplateId;
            var firstTemplateLanguage = seedDataOptions.Email[0].Language;

            await using var templateDbContext = _postgreSqlFixture.CreateDbContext<TemplateDbContext>();
            await templateDbContext.EmailTemplates.AddAsync(new EmailTemplate(
                firstTemplateId,
                firstTemplateLanguage,
                StringGenerator.GenerateAlphanumeric(),
                StringGenerator.GenerateAlphanumeric(),
                isBodyHtml: false));
            await templateDbContext.SaveChangesAsync();

            // Act
            await ProgramInitializeAsync(seedDataOptions, templateDbContext);

            // Assert
            templateDbContext.ChangeTracker.Clear();

            var emailTemplatesFromDb = await templateDbContext.EmailTemplates.ToListAsync();
            Assert.Equal(seedDataOptions.Email.Count, emailTemplatesFromDb.Count);

            foreach (var emailTemplate in seedDataOptions.Email)
            {
                var template = emailTemplatesFromDb
                    .FirstOrDefault(t => t.TemplateId == emailTemplate.TemplateId &&
                        t.Language == emailTemplate.Language);

                if (template == null)
                    Assert.Fail($"Template with {emailTemplate.TemplateId} id and {emailTemplate.Language} language is not saved to the DB.");

                var body = await File.ReadAllTextAsync(AppContext.BaseDirectory + emailTemplate.Body);
                Assert.Equal(emailTemplate.Subject, template.Subject);
                Assert.Equal(body, template.Body);
                Assert.Equal(emailTemplate.IsBodyHtml, template.IsBodyHtml);
            }
        }

        private Task ProgramInitializeAsync(TemplateDbSeedDataOptions seedDataOptions, TemplateDbContext templateDbContext)
        {
            var initializeMethodInfo = typeof(TemplateDb.Initializer.Program).GetMethod("InitializeAsync", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
            return (Task)initializeMethodInfo.Invoke(null, [seedDataOptions, templateDbContext])!;
        }
    }
}
