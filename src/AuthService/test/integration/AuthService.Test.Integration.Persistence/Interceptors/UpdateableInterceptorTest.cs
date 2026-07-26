using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.SeedWork;
using AuthService.Persistence.Exceptions;
using AuthService.Test.Integration.Persistence.Collections;
using AuthService.Test.Utility.Fixtures;
using Shared.TestGenerators;

namespace AuthService.Test.Integration.Persistence.Interceptors
{
    [Collection(nameof(AuthDbContextCollection))]
    public class UpdateableInterceptorTest
    {
        private readonly AuthDbContextFixture _authDbContextFixture;

        public UpdateableInterceptorTest(AuthDbContextFixture authDbContextFixture)
        {
            _authDbContextFixture = authDbContextFixture;
        }

        [Fact]
        public async Task UpdateableInterceptor_WhenEntityIsUpdateableAndIsUpdated_ShouldSetUpdatedDate()
        {
            // Arrange
            using var authDbContext = _authDbContextFixture.CreateAuthDbContext();

            var now = DateTimeOffset.UtcNow;
            var passwordLength = 10;
            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate(length: passwordLength));
            var accountId = account.Id;

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            if (account.UpdatedDate != null)
                Assert.Fail($"The {nameof(IUpdateable.UpdatedDate)} property was set while creating the entity in the database");

            // Act
            account.PasswordHashed = PasswordGenerator.Generate(length: passwordLength + 1);
            await authDbContext.SaveChangesAsync();

            // Assert
            authDbContext.ChangeTracker.Clear();
            var updatedAccount = await authDbContext.Accounts.FindAsync(accountId);

            Assert.NotNull(updatedAccount);
            Assert.NotNull(updatedAccount.UpdatedDate);
            Assert.True(DateTimeOffset.Compare(now, updatedAccount.UpdatedDate.Value) <= 0, $"The {nameof(IUpdateable.UpdatedDate)} property is in the past.");
        }

        [Fact]
        public async Task UpdateableInterceptor_WhenEntityIsNotUpdateableAndIsUpdated_ShouldThrowException()
        {
            // Arrange
            using var authDbContext = _authDbContextFixture.CreateAuthDbContext();

            var account = new Account(EmailGenerator.Generate(), PasswordGenerator.Generate());
            var tokenLength = 10;
            var token = new Token(account.Id, ETokenPurpose.EmailVerification, StringGenerator.GeneratePrintableAscii(length: tokenLength), DateTimeOffset.UtcNow);

            account.Tokens.Add(token);
            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            // Act
            token.ValueHashed = StringGenerator.GeneratePrintableAscii(length: tokenLength + 1);
            var exception = await Record.ExceptionAsync(async () =>
            {
                await authDbContext.SaveChangesAsync();
            });

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<NonUpdateableEntityException>(exception);
        }
    }
}
