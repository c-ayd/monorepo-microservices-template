using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.DbContexts;

namespace NotificationService.Worker.SeedData
{
    public static class TemplateDbContextSeedData
    {
        public static async Task SeedDataTemplateDbAsync(this IHost host)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var templateDbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();

            // Only create the DB
            await templateDbContext.Database.MigrateAsync();
        }
    }
}
