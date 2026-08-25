using AuthService.Domain.Entities;
using AuthService.Test.Integration.Persistence.Collections;
using AuthService.Test.Utility.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shared.Test.Generators;

namespace AuthService.Test.Integration.Persistence.Filters
{
    [Collection(nameof(AuthDbContextCollection))]
    public class SoftDeleteFilterTest
    {
        private readonly AuthDbContextFixture _authDbContextFixture;

        public SoftDeleteFilterTest(AuthDbContextFixture authDbContextFixture)
        {
            _authDbContextFixture = authDbContextFixture;
        }

        [Fact]
        public async Task SoftDeleteFilter_WhenEntityIsSoftDeleteableAndIsDeleted_ShouldNotAppearInResult()
        {
            // Arrange
            using var authDbContext = _authDbContextFixture.CreateAuthDbContext();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate());
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
            using var authDbContext = _authDbContextFixture.CreateAuthDbContext();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate());
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
            using var authDbContext = _authDbContextFixture.CreateAuthDbContext();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate());
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
