using AuthService.Domain.Entities;
using AuthService.Test.Integration.Persistence.Collections;
using AuthService.Test.Utility.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shared.TestGenerators;

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
            var login = new Login(account.Id, StringGenerator.GeneratePrintableAscii(), DateTime.UtcNow);
            var loginId = login.Id;

            // Act
            account.Logins.Add(login);
            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            // Assert
            authDbContext.ChangeTracker.Clear();
            var loginFromDb = await authDbContext.Logins.FindAsync(loginId);

            Assert.NotNull(loginFromDb);
        }
    }
}
