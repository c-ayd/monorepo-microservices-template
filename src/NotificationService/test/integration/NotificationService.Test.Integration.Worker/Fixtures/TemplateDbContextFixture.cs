using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.DbContexts;
using Testcontainers.PostgreSql;

namespace NotificationService.Test.Integration.Worker.Fixtures
{
    public class TemplateDbContextFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _container = null!;

        public async Task InitializeAsync()
        {
            _container = new PostgreSqlBuilder("postgres:18.4")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
                .Build();
            await _container.StartAsync();

            using var dbContext = CreateTemplateDbContext();
            await dbContext.Database.MigrateAsync();
        }

        public TemplateDbContext CreateTemplateDbContext()
        {
            return new TemplateDbContext(new DbContextOptionsBuilder<TemplateDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options);
        }

        public string GetConnectionString()
        {
            return _container.GetConnectionString();
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
