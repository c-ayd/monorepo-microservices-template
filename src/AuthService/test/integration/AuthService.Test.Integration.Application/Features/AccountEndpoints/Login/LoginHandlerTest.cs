using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Application.Abstractions.Crypto;
using AuthService.Application.Features.AccountEndpoints.Login;
using AuthService.Application.Options;
using AuthService.Application.Validations.Constraints;
using AuthService.Domain.Entities;
using AuthService.Test.Integration.Application.Collections;
using AuthService.Test.Utility.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Http.Authentication;
using Shared.Http.Response;
using Shared.Test.Generators;

namespace AuthService.Test.Integration.Application.Features.AccountEndpoints.Login
{
    [Collection(nameof(AuthApiCollection))]
    public class LoginHandlerTest
    {
        private readonly AuthApiFixture _authApiFixture;

        public LoginHandlerTest(AuthApiFixture authApiFixture)
        {
            _authApiFixture = authApiFixture;
        }

        [Fact]
        public async Task Handle_WhenAccountDoesNotExist_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new LoginRequest(
                EmailGenerator.Generate(),
                StringGenerator.GenerateAlphanumeric());

            // Act
            var response = await _authApiFixture.Client.PostAsJsonAsync("/accounts/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Handle_WhenAccountIsBanned_ShouldReturnForbidden()
        {
            // Arrange
            var email = EmailGenerator.Generate();
            var account = new Account(email, PasswordGenerator.Generate());
            account.IsBanned = true;

            using var authDbContext = _authApiFixture.CreateAuthDbContext();

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var request = new LoginRequest(email, PasswordGenerator.Generate());

            // Act
            var response = await _authApiFixture.Client.PostAsJsonAsync("/accounts/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Handle_WhenAccountIsLocked_ShouldReturnLocked()
        {
            // Arrange
            var email = EmailGenerator.Generate();
            var password = PasswordGenerator.Generate(
                includeSpecialChars: true,
                specialChars: AccountConstraints.PasswordSpecialCharacters,
                length: AccountConstraints.PasswordMinLength
            );

            await using var scope = _authApiFixture.Factory.Services.CreateAsyncScope();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var account = new Account(email, passwordHasher.Hash(password));
            account.IsLocked = true;
            account.FailedLoginAttempts = 3;
            account.UnlockDate = DateTimeOffset.UtcNow.AddDays(1);

            using var authDbContext = _authApiFixture.CreateAuthDbContext();

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();
            
            var request = new LoginRequest(email, password);

            // Act
            var response = await _authApiFixture.Client.PostAsJsonAsync("/accounts/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.Locked, response.StatusCode);
            
            authDbContext.ChangeTracker.Clear();
            var accountFromDb = await authDbContext.Accounts.FindAsync(account.Id);
            Assert.Equal(4, accountFromDb!.FailedLoginAttempts);
        }

        [Fact]
        public async Task Handle_WhenPasswordIsWrong_ShouldReturnBadRequest()
        {
            // Arrange
            var email = EmailGenerator.Generate();
            var password = PasswordGenerator.Generate(
                includeSpecialChars: true,
                specialChars: AccountConstraints.PasswordSpecialCharacters,
                length: AccountConstraints.PasswordMinLength
            );

            await using var scope = _authApiFixture.Factory.Services.CreateAsyncScope();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var account = new Account(email, passwordHasher.Hash(password));

            using var authDbContext = _authApiFixture.CreateAuthDbContext();

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var request = new LoginRequest(email, password + "a");

            // Act
            var response = await _authApiFixture.Client.PostAsJsonAsync("/accounts/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            authDbContext.ChangeTracker.Clear();
            var accountFromDb = await authDbContext.Accounts.FindAsync(account.Id);
            Assert.Equal(1, accountFromDb!.FailedLoginAttempts);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Handle_WhenPasswordIsWrongAndAccountIsLocked_ShouldReturnLocked(int additionalFailedAttempts)
        {
            // Arrange
            var email = EmailGenerator.Generate();
            var password = PasswordGenerator.Generate(
                includeSpecialChars: true,
                specialChars: AccountConstraints.PasswordSpecialCharacters,
                length: AccountConstraints.PasswordMinLength
            );

            await using var scope = _authApiFixture.Factory.Services.CreateAsyncScope();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var accountLockOptions = _authApiFixture.GetOptions<AccountLockOptions>();
            var failedAttempts = accountLockOptions.NumberOfFailedAttempsBeforeLock - 1 + additionalFailedAttempts;

            var account = new Account(email, passwordHasher.Hash(password));
            account.FailedLoginAttempts = failedAttempts;

            using var authDbContext = _authApiFixture.CreateAuthDbContext();

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var request = new LoginRequest(email, password + "a");

            // Act
            var response = await _authApiFixture.Client.PostAsJsonAsync("/accounts/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.Locked, response.StatusCode);

            authDbContext.ChangeTracker.Clear();
            var accountFromDb = await authDbContext.Accounts.FindAsync(account.Id);
            Assert.Equal(failedAttempts + 1, accountFromDb!.FailedLoginAttempts);
            Assert.True(accountFromDb.IsLocked, "The account is not locked.");

            var now = DateTimeOffset.UtcNow;
            var multiplier = Math.Min(accountFromDb.FailedLoginAttempts - accountLockOptions.NumberOfFailedAttempsBeforeLock + 1, accountLockOptions.MaxLockTimeMultiplier);
            var lockTimeInMinutes = accountLockOptions.LockTimeInMinutes * multiplier;
            Assert.InRange((accountFromDb.UnlockDate!.Value - now).TotalMinutes, lockTimeInMinutes - 1, lockTimeInMinutes + 1);
        }

        [Fact]
        public async Task Handle_WhenCredentialsAreCorrect_ShouldCreateSessionAndSetCookiesAndReturnOk()
        {
            // Arrange
            var email = EmailGenerator.Generate();
            var password = PasswordGenerator.Generate(
                includeSpecialChars: true,
                specialChars: AccountConstraints.PasswordSpecialCharacters,
                length: AccountConstraints.PasswordMinLength
            );

            await using var scope = _authApiFixture.Factory.Services.CreateAsyncScope();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var account = new Account(email, passwordHasher.Hash(password));

            using var authDbContext = _authApiFixture.CreateAuthDbContext();

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var request = new LoginRequest(email, password);

            // Act
            var response = await _authApiFixture.Client.PostAsJsonAsync("/accounts/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty(JsonResponseBuilder.DataKey, out var dataElement), $"The {JsonResponseBuilder.DataKey} key does not exist in the response");
            Assert.True(dataElement.TryGetProperty("accessToken", out var accessTokenElement), "The accessToken key does not exists in the response");
            Assert.NotNull(accessTokenElement.GetString());
            Assert.True(dataElement.TryGetProperty("roles", out _), "The roles key does not exists in the response");

            Assert.True(root.TryGetProperty(JsonResponseBuilder.MetadataKey, out var metadataElement), $"The {JsonResponseBuilder.MetadataKey} key does not exist in the response");
            Assert.True(metadataElement.TryGetProperty("isEmailVerified", out _), "The isEmailVerified key does not exists in the response");
            Assert.True(metadataElement.TryGetProperty("preferredLanguage", out _), "The preferredLanguage key does not exists in the response");

            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookieValues) &&
                        cookieValues.Any(c => c.StartsWith(CookieKeys.RefreshToken)), "The cookie is not set for the refresh token.");

            authDbContext.ChangeTracker.Clear();
            var accountFromDb = await authDbContext.Accounts
                .Where(a => a.Id.Equals(account.Id))
                .Include(a => a.Sessions)
                .FirstOrDefaultAsync();

            Assert.Single(accountFromDb!.Sessions);
        }

        [Fact]
        public async Task Handle_WhenAccountIsLockedButLockedDateIsInPast_ShouldReturnOk()
        {
            // Arrange
            var email = EmailGenerator.Generate();
            var password = PasswordGenerator.Generate(
                includeSpecialChars: true,
                specialChars: AccountConstraints.PasswordSpecialCharacters,
                length: AccountConstraints.PasswordMinLength
            );

            await using var scope = _authApiFixture.Factory.Services.CreateAsyncScope();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var account = new Account(email, passwordHasher.Hash(password));
            account.IsLocked = true;
            account.UnlockDate = DateTimeOffset.UtcNow.AddDays(-1);

            using var authDbContext = _authApiFixture.CreateAuthDbContext();

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var request = new LoginRequest(email, password);

            // Act
            var response = await _authApiFixture.Client.PostAsJsonAsync("/accounts/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            authDbContext.ChangeTracker.Clear();
            var accountFromDb = await authDbContext.Accounts.FindAsync(account.Id);
            Assert.Equal(0, accountFromDb!.FailedLoginAttempts);
            Assert.False(accountFromDb.IsLocked, "The account is locked.");
        }
    }
}
