using AuthService.Domain.Entities;
using AuthService.Persistence.DbContexts;
using AuthService.Persistence.SeedData;
using AuthService.Persistence.Settings;
using AuthService.Test.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.TestGenerators;
using Testcontainers.PostgreSql;

namespace AuthService.Test.Integration.Persistence.SeedData
{
    public class AuthDbContextSeedDataTest : IAsyncLifetime
    {
        private PostgreSqlContainer _container = null!;
        private IServiceProvider _services = null!;

        public async Task InitializeAsync()
        {
            // NOTE: Change the version tag to match your PostgreSQL version.
            _container = new PostgreSqlBuilder("postgres:16.4")
                .Build();
            await _container.StartAsync();

            _services = new ServiceCollection()
                .AddDbContext<AuthDbContext>(_ => _.UseNpgsql(_container.GetConnectionString()))
                .BuildServiceProvider();
        }

        public AuthDbContext CreateAuthDbContext()
        {
            return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options);
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        [Fact]
        public async Task SeedDataAuthDbContextAsync_WhenDatabaseIsEmpty_ShouldCreateDefaultRolesAndAccounts()
        {
            // Arrange
            var configuration = ConfigurationHelper.CreateConfiguration();
            var seedDataSettings = configuration.GetSection(SeedDataSettings.SettingsKey).Get<SeedDataSettings>()!;

            // Act
            await _services.SeedDataAuthDbContextAsync(configuration);

            // Assert
            using var authDbContext = CreateAuthDbContext();

            var roles = await authDbContext.Roles
                .Select(r => r.Name)
                .ToListAsync();
            var accounts = await authDbContext.Accounts
                .Select(a => new
                {
                    a.Email,
                    a.IsEmailVerified,
                    RoleNames = a.Roles.Select(r => r.Name).ToList()
                })
                .ToListAsync();

            foreach (var role in seedDataSettings.AuthDb.Roles)
            {
                if (!roles.Contains(role))
                    Assert.Fail($"The Roles table does not contain a role called {role}.");
            }

            foreach (var accountRolePair in seedDataSettings.AuthDb.Accounts)
            {
                var account = accounts.FirstOrDefault(a => a.Email == accountRolePair.Email);
                if (account == null)
                    Assert.Fail($"The Accounts table does not have an account with the email {accountRolePair.Email}.");

                if (!account.IsEmailVerified)
                    Assert.Fail($"The account with the email {accountRolePair.Email} does not have email verification property set to true.");

                if (!account.RoleNames.Contains(accountRolePair.Role))
                    Assert.Fail($"The account with the email {accountRolePair.Email} does not have the {accountRolePair.Role} role.");
            }
        }

        [Fact]
        public async Task SeedDataAuthDbContextAsync_WhenDatabaseHasData_ShouldSkipSeedData()
        {
            // Arrange
            var configuration = ConfigurationHelper.CreateConfiguration();
            var seedDataSettings = configuration.GetSection(SeedDataSettings.SettingsKey).Get<SeedDataSettings>()!;

            using var authDbContext = CreateAuthDbContext();
            await authDbContext.Database.MigrateAsync();

            await authDbContext.Roles.AddAsync(new Role(seedDataSettings.AuthDb.Roles[0] + "a"));
            await authDbContext.SaveChangesAsync();

            await authDbContext.Accounts.AddAsync(new Account(seedDataSettings.AuthDb.Accounts[0] + "a", PasswordGenerator.Generate()));
            await authDbContext.SaveChangesAsync();

            // Act
            await _services.SeedDataAuthDbContextAsync(configuration);

            // Assert
            authDbContext.ChangeTracker.Clear();

            var roles = await authDbContext.Roles
                .Select(r => r.Name)
                .ToListAsync();
            var accountEmails = await authDbContext.Accounts
                .Select(a => a.Email)
                .ToListAsync();

            foreach (var role in seedDataSettings.AuthDb.Roles)
            {
                if (roles.Contains(role))
                    Assert.Fail($"The Roles table contains a role called {role}.");
            }

            foreach (var accountRolePair in seedDataSettings.AuthDb.Accounts)
            {
                var account = accountEmails.FirstOrDefault(e => e == accountRolePair.Email);
                if (account != null)
                    Assert.Fail($"The Accounts table has an account with the email {accountRolePair.Email}.");
            }
        }
    }
}
