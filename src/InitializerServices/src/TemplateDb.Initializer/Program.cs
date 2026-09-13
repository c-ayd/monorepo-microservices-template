using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Entities;
using TemplateDb.Initializer.Options;

namespace TemplateDb.Initializer
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            System.Console.WriteLine("TemplateDB Initializer started.");

            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            var templateDbSeedDataOptions = configuration.GetSection("Templates").Get<TemplateDbSeedDataOptions>()!;

            await using var templateDbContext = new TemplateDbContext(new DbContextOptionsBuilder<TemplateDbContext>()
                .UseNpgsql(configuration.GetConnectionString("TemplateDb"))
                .Options);

            await InitializeAsync(templateDbSeedDataOptions, templateDbContext);

            return 0;
        }

        private static async Task InitializeAsync(TemplateDbSeedDataOptions seedDataOptions, TemplateDbContext templateDbContext)
        {
            // Migrate
            await templateDbContext.Database.MigrateAsync();

            System.Console.WriteLine("Migration completed.");

            // Seed data
            await using var transaction = await templateDbContext.Database.BeginTransactionAsync();
            try
            {
                await SeedEmailTemplatesAsync(seedDataOptions, templateDbContext);

                await transaction.CommitAsync();

                System.Console.WriteLine("Seed data is committed to the DB.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                System.Console.WriteLine($"Something went wrong. Exiting without saving any seed data...");
                throw;
            }
        }

        private static async Task SeedEmailTemplatesAsync(TemplateDbSeedDataOptions seedDataOptions, TemplateDbContext templateDbContext)
        {
            var newCounter = 0;
            var updateCounter = 0;
            foreach (var emailTemplate in seedDataOptions.Email)
            {
                var template = await templateDbContext.EmailTemplates
                    .FirstOrDefaultAsync(t => t.TemplateId == emailTemplate.TemplateId &&
                        t.Language == emailTemplate.Language);

                var body = await File.ReadAllTextAsync(AppContext.BaseDirectory + emailTemplate.Body);
                if (template != null)
                {
                    System.Console.WriteLine($"Email template with {emailTemplate.TemplateId} ID and {emailTemplate.Language} language is already in the DB. Updating...");
                    
                    template.Subject = emailTemplate.Subject;
                    template.Body = body;
                    template.IsBodyHtml = emailTemplate.IsBodyHtml;

                    ++updateCounter;
                }
                else
                {
                    await templateDbContext.EmailTemplates.AddAsync(new EmailTemplate(
                        emailTemplate.TemplateId,
                        emailTemplate.Language,
                        emailTemplate.Subject,
                        body,
                        emailTemplate.IsBodyHtml
                    ));

                    ++newCounter;
                }
            }

            await templateDbContext.SaveChangesAsync();

            System.Console.WriteLine($"{newCounter} email template(s) are added to the DB.");
            System.Console.WriteLine($"{updateCounter} email template(s) are updated in the DB.");
        }
    }
}
