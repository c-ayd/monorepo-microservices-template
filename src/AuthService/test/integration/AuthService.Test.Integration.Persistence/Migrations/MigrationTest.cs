using AuthService.Persistence.DbContexts;
using AuthService.Test.Utility;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AuthService.Test.Integration.Persistence.Migrations
{
    public class MigrationTests : IAsyncLifetime
    {
        private PostgreSqlContainer _container = null!;

        public async Task InitializeAsync()
        {
            _container = new PostgreSqlBuilder("postgres:18.4")
                .Build();
            await _container.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        [Fact]
        public async Task Migration_WhenMigrationsRun_ShouldThrowNoException()
        {
            // Arrange
            using var authDbContext = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options);

            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await authDbContext.Database.MigrateAsync();
            });

            // Assert
            Assert.Null(exception);
        }
    }
}
