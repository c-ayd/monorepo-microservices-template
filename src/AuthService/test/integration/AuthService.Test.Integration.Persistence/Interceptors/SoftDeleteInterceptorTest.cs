using AuthService.Domain.Entities;
using AuthService.Domain.SeedWork;
using AuthService.Persistence.DbContexts;
using AuthService.Test.Integration.Persistence.Collections;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Test.Generators;
using Shared.Test.Helpers.Fixtures;

namespace AuthService.Test.Integration.Persistence.Interceptors
{
    [Collection(nameof(AuthDbContextCollection))]
    public class SoftDeleteInterceptorTest
    {
        private readonly PostgreSqlFixture _postgreSqlFixture;

        public SoftDeleteInterceptorTest(AuthDbContextCollectionCluster collectionCluster)
        {
            _postgreSqlFixture = collectionCluster.PostgreSqlFixture;
        }

        [Fact]
        public async Task SoftDeleteInterceptor_WhenEntityIsSoftDeleteableAndDeleted_ShouldNotDeleteEntityAndUpdateSoftDeleteProperties()
        {
            // Arrange
            using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();

            var now = DateTimeOffset.UtcNow;
            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate(), SupportedLanguages.DefaultLanguage);
            var accountId = account.Id;

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            // Act
            authDbContext.Accounts.Remove(account);
            await authDbContext.SaveChangesAsync();

            // Assert
            authDbContext.ChangeTracker.Clear();
            var softDeletedAccount = await authDbContext.Accounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id.Equals(accountId));

            Assert.NotNull(softDeletedAccount);
            Assert.True(softDeletedAccount.IsDeleted, $"The {nameof(ISoftDelete.IsDeleted)} property is not true.");
            Assert.NotNull(softDeletedAccount.DeletedDate);
            Assert.True(DateTimeOffset.Compare(now, softDeletedAccount.DeletedDate.Value) <= 0, $"The {nameof(ISoftDelete.DeletedDate)} property is in the past.");
        }

        [Fact]
        public async Task SoftDeleteInterceptor_WhenEntityIsNotSoftDeleteableAndDeleted_ShouldDeleteEntity()
        {
            // Arrange
            using var authDbContext = _postgreSqlFixture.CreateDbContext<AuthDbContext>();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate(), SupportedLanguages.DefaultLanguage);
            var session = new Session(account.Id, StringGenerator.GeneratePrintableAscii(), DateTimeOffset.UtcNow);
            var sessionId = session.Id;

            account.Sessions.Add(session);
            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            // Act
            authDbContext.Sessions.Remove(session);
            await authDbContext.SaveChangesAsync();

            // Assert
            authDbContext.ChangeTracker.Clear();
            var deletedSession = await authDbContext.Sessions.FindAsync(sessionId);

            Assert.Null(deletedSession);
        }
    }
}
