using System.Reflection;
using AuthDb.Initializer.Options;
using AuthDb.Test.Integration.Initializer.Collections;
using AuthService.Domain.Entities;
using AuthService.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Test.Generators;
using Shared.Test.Helpers.Fixtures;

namespace AuthDb.Test.Integration.Initializer
{
    [Collection(nameof(PostgreSqlCollection))]
    public class ProgramTest
    {
        private readonly PostgreSqlFixture _postgreSqlFixture;

        public ProgramTest(PostgreSqlCollectionCluster collectionCluster)
        {
            _postgreSqlFixture = collectionCluster.PostgreSqlFixture;
        }

        [Theory]
        [InlineData(true, true, SupportedLanguages.German)]
        [InlineData(true, true, null)]
        [InlineData(true, true, "abc")]
        [InlineData(true, false, null)]
        [InlineData(false, true, SupportedLanguages.German)]
        [InlineData(false, true, null)]
        [InlineData(false, true, "abc")]
        [InlineData(false, false, null)]
        public async Task InitializeAsync_WhenThereIsNoDataInDb_ShouldAddSeedDataToDb(bool force, bool hasRole, string? language)
        {
            // Arrange
            await _postgreSqlFixture.DropDatabaseAsync<AuthDbContext>();

            var seedRoleName = StringGenerator.GenerateAlphanumeric();
            var seedAccountEmail = EmailGenerator.Generate();
            var seedDataOptions = new AuthDbSeedDataOptions()
            {
                Roles = new List<string>() { seedRoleName },
                Accounts = new List<AuthDbSeedDataOptions.AccountDetails>()
                {
                    new AuthDbSeedDataOptions.AccountDetails()
                    {
                        Email = seedAccountEmail,
                        Role = hasRole ? seedRoleName : null,
                        PreferredLanguage = language
                    }
                }
            };

            await using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();

            // Act
            await ProgramInitializeAsync(force, seedDataOptions, authDbContext);

            // Assert
            authDbContext.ChangeTracker.Clear();

            var rolesFromDb = await authDbContext.Roles.ToListAsync();
            var accountsFromDb = await authDbContext.Accounts.Include(a => a.Roles).ToListAsync();
            Assert.Single(rolesFromDb);
            Assert.Single(accountsFromDb);

            Assert.Equal(seedRoleName, rolesFromDb[0].Name);
            Assert.Equal(seedAccountEmail, accountsFromDb[0].Email);
            
            if (hasRole)
            {
                Assert.Equal(seedRoleName, accountsFromDb[0].Roles.ElementAt(0).Name);
            }
            else
            {
                Assert.Empty(accountsFromDb[0].Roles);
            }

            if (language == SupportedLanguages.German)
            {
                Assert.Equal(language, accountsFromDb[0].PreferredLanguage);
            }
            else
            {
                Assert.Equal(SupportedLanguages.DefaultLanguage, accountsFromDb[0].PreferredLanguage);
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenThereIsDataInDbAndNoForceFlag_ShouldDoNothing()
        {
            // Arrange
            await _postgreSqlFixture.ClearDatabaseAsync<AuthDbContext>();

            await using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();
            await authDbContext.Roles.AddAsync(new Role(StringGenerator.GenerateAlphanumeric()));
            await authDbContext.Accounts.AddAsync(new Account(EmailGenerator.Generate(), StringGenerator.GenerateAlphanumeric(), "abc"));
            await authDbContext.SaveChangesAsync();

            var seedRoleName = StringGenerator.GenerateAlphanumeric();
            var seedAccountEmail = EmailGenerator.Generate();
            var seedDataOptions = new AuthDbSeedDataOptions()
            {
                Roles = new List<string>() { seedRoleName },
                Accounts = new List<AuthDbSeedDataOptions.AccountDetails>()
                {
                    new AuthDbSeedDataOptions.AccountDetails()
                    {
                        Email = seedAccountEmail
                    }
                }
            };

            // Act
            await ProgramInitializeAsync(false, seedDataOptions, authDbContext);

            // Assert
            authDbContext.ChangeTracker.Clear();

            var rolesFromDb = await authDbContext.Roles.ToListAsync();
            var accountsFromDb = await authDbContext.Accounts.Include(a => a.Roles).ToListAsync();
            Assert.Single(rolesFromDb);
            Assert.Single(accountsFromDb);
        }

        [Fact]
        public async Task InitializeAsync_WhenThereIsDataInDbAndForceFlagAndNoUniqueSeedData_ShouldDoNothing()
        {
            // Arrange
            await _postgreSqlFixture.ClearDatabaseAsync<AuthDbContext>();

            var seedRoleName = StringGenerator.GenerateAlphanumeric();
            var seedAccountEmail = EmailGenerator.Generate();
            var seedDataOptions = new AuthDbSeedDataOptions()
            {
                Roles = new List<string>() { seedRoleName },
                Accounts = new List<AuthDbSeedDataOptions.AccountDetails>()
                {
                    new AuthDbSeedDataOptions.AccountDetails()
                    {
                        Email = seedAccountEmail
                    }
                }
            };

            await using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();
            await authDbContext.Roles.AddAsync(new Role(seedRoleName));
            await authDbContext.Accounts.AddAsync(new Account(seedAccountEmail, StringGenerator.GenerateAlphanumeric(), "abc"));
            await authDbContext.SaveChangesAsync();

            // Act
            await ProgramInitializeAsync(true, seedDataOptions, authDbContext);

            // Assert
            authDbContext.ChangeTracker.Clear();

            var rolesFromDb = await authDbContext.Roles.ToListAsync();
            var accountsFromDb = await authDbContext.Accounts.Include(a => a.Roles).ToListAsync();
            Assert.Single(rolesFromDb);
            Assert.Single(accountsFromDb);
        }

        [Fact]
        public async Task InitializeAsync_WhenThereIsDataInDbAndForceFlagAndUniqueSeedData_ShouldAddUniqueDataToDb()
        {
            // Arrange
            await _postgreSqlFixture.ClearDatabaseAsync<AuthDbContext>();

            var seedRoleName = StringGenerator.GenerateAlphanumeric();
            var seedAccountEmail = EmailGenerator.Generate();
            var seedDataOptions = new AuthDbSeedDataOptions()
            {
                Roles = new List<string>()
                {
                    seedRoleName,
                    seedRoleName + "a"
                },
                Accounts = new List<AuthDbSeedDataOptions.AccountDetails>()
                {
                    new AuthDbSeedDataOptions.AccountDetails()
                    {
                        Email = seedAccountEmail
                    },
                    new AuthDbSeedDataOptions.AccountDetails()
                    {
                        Email = seedAccountEmail + "a"
                    }
                }
            };

            await using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();
            await authDbContext.Roles.AddAsync(new Role(seedRoleName));
            await authDbContext.Accounts.AddAsync(new Account(seedAccountEmail, StringGenerator.GenerateAlphanumeric(), "abc"));
            await authDbContext.SaveChangesAsync();

            // Act
            await ProgramInitializeAsync(true, seedDataOptions, authDbContext);

            // Assert
            authDbContext.ChangeTracker.Clear();

            var rolesFromDb = await authDbContext.Roles.ToListAsync();
            var accountsFromDb = await authDbContext.Accounts.Include(a => a.Roles).ToListAsync();
            Assert.Equal(2, rolesFromDb.Count);
            Assert.Equal(2, accountsFromDb.Count);

            Assert.Contains(seedRoleName + "a", rolesFromDb.Select(r => r.Name).ToList());

            var account = accountsFromDb.FirstOrDefault(a => a.Email == seedAccountEmail + "a");
            Assert.NotNull(account);
            Assert.Empty(account.Roles);
            Assert.Equal(SupportedLanguages.DefaultLanguage, account.PreferredLanguage);
        }

        private Task ProgramInitializeAsync(bool force, AuthDbSeedDataOptions seedDataOptions, AuthDbContext authDbContext)
        {
            var initializeMethodInfo = typeof(AuthDb.Initializer.Program).GetMethod("InitializeAsync", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
            return (Task)initializeMethodInfo.Invoke(null, [force, seedDataOptions, authDbContext])!;
        }
    }
}
