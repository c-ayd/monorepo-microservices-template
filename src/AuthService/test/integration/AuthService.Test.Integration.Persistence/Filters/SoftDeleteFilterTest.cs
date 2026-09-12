using AuthService.Domain.Entities;
using AuthService.Persistence.DbContexts;
using AuthService.Test.Integration.Persistence.Collections;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Test.Generators;
using Shared.Test.Helpers.Fixtures;

namespace AuthService.Test.Integration.Persistence.Filters
{
    [Collection(nameof(AuthDbContextCollection))]
    public class SoftDeleteFilterTest
    {
        private readonly PostgreSqlFixture _postgreSqlFixture;

        public SoftDeleteFilterTest(AuthDbContextCollectionCluster collectionCluster)
        {
            _postgreSqlFixture = collectionCluster.PostgreSqlFixture;
        }

        [Fact]
        public async Task SoftDeleteFilter_WhenEntityIsSoftDeleteableAndIsDeleted_ShouldNotAppearInResult()
        {
            // Arrange
            using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate(), SupportedLanguages.DefaultLanguage);
            var accountId = account.Id;

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            // Act
            authDbContext.Accounts.Remove(account);
            await authDbContext.SaveChangesAsync();

            // Assert
            authDbContext.ChangeTracker.Clear();
            var accountFromDb = await authDbContext.Accounts.FindAsync(accountId);
            var deletedAccount = await authDbContext.Accounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id.Equals(accountId));

            Assert.NotNull(deletedAccount);
            Assert.Null(accountFromDb);
        }

        [Fact]
        public async Task SoftDeleteFilter_WhenEntityIsSoftDeleteableAndIsNotDeleted_ShouldAppearInResult()
        {
            // Arrange
            using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate(), SupportedLanguages.DefaultLanguage);
            var accountId = account.Id;

            // Act
            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            // Assert
            authDbContext.ChangeTracker.Clear();
            var accountFromDb = await authDbContext.Accounts.FindAsync(accountId);

            Assert.NotNull(accountFromDb);
        }

        [Fact]
        public async Task SoftDeleteFilter_WhenEntityIsNotSoftDeleteableAndIsNotDeleted_ShouldAppearInResult()
        {
            // Arrange
            using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate(), SupportedLanguages.DefaultLanguage);
            var session = new Session(account.Id, StringGenerator.GeneratePrintableAscii(), DateTimeOffset.UtcNow);
            var sessionId = session.Id;

            // Act
            account.Sessions.Add(session);
            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            // Assert
            authDbContext.ChangeTracker.Clear();
            var sessionFromDb = await authDbContext.Sessions.FindAsync(sessionId);

            Assert.NotNull(sessionFromDb);
        }
    }
}
