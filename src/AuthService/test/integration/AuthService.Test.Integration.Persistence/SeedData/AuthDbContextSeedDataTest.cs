using AuthService.Domain.Entities;
using AuthService.Persistence.DbContexts;
using AuthService.Persistence.SeedData;
using AuthService.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Test.Generators;
using Testcontainers.PostgreSql;
using AuthService.Application.Abstractions.DbContexts;
using Shared.Test.Helpers;
using Shared.Constants;
using System.Text.Json;

namespace AuthService.Test.Integration.Persistence.SeedData
{
    public class AuthDbContextSeedDataTest : IAsyncLifetime
    {
        private readonly SeedDataOptions _seedDataOptions = new SeedDataOptions()
        {
            AuthDb = new SeedDataOptions.AuthDbData()
            {
                Roles = new List<string>() { "Admin" },
                Accounts = new List<SeedDataOptions.AuthDbData.AccountRolePair>()
                {
                    new SeedDataOptions.AuthDbData.AccountRolePair()
                    {
                        Email = "test@test.com",
                        Role = "Admin",
                        PreferredLanguage = "en"
                    }
                }
            }
        };

        private PostgreSqlContainer _container = null!;
        private IServiceProvider _services = null!;

        public async Task InitializeAsync()
        {
            _container = new PostgreSqlBuilder("postgres:18.4")
                .Build();
            await _container.StartAsync();

            _services = new ServiceCollection()
                .AddDbContext<AuthDbContext>(_ => _.UseNpgsql(_container.GetConnectionString()))
                .AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>())
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
            var configuration = ConfigurationHelper.CreateConfigurationFromTestSettings();

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

            foreach (var role in _seedDataOptions.AuthDb.Roles)
            {
                if (!roles.Contains(role))
                    Assert.Fail($"The Roles table does not contain a role called {role}.");
            }

            foreach (var accountRolePair in _seedDataOptions.AuthDb.Accounts)
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
            var configuration = ConfigurationHelper.CreateConfigurationFromTestSettings();

            using var authDbContext = CreateAuthDbContext();
            await authDbContext.Database.MigrateAsync();

            await authDbContext.Roles.AddAsync(new Role(_seedDataOptions.AuthDb.Roles[0] + "a"));
            await authDbContext.SaveChangesAsync();

            await authDbContext.Accounts.AddAsync(new Account(_seedDataOptions.AuthDb.Accounts[0] + "a", PasswordGenerator.Generate(), SupportedLanguages.DefaultLanguage));
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

            foreach (var role in _seedDataOptions.AuthDb.Roles)
            {
                if (roles.Contains(role))
                    Assert.Fail($"The Roles table contains a role called {role}.");
            }

            foreach (var accountRolePair in _seedDataOptions.AuthDb.Accounts)
            {
                var account = accountEmails.FirstOrDefault(e => e == accountRolePair.Email);
                if (account != null)
                    Assert.Fail($"The Accounts table has an account with the email {accountRolePair.Email}.");
            }
        }
    }
}
