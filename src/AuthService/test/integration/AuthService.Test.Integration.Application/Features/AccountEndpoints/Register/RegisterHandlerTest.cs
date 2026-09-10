using System.Net;
using System.Net.Http.Json;
using AuthService.Application.Features.AccountEndpoints.Register;
using AuthService.Application.Options;
using AuthService.Application.Validations.Constraints;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Persistence.DbContexts;
using AuthService.Test.Integration.Application.Collections;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Test.Generators;

namespace AuthService.Test.Integration.Application.Features.AccountEndpoints.Register
{
    [Collection(nameof(AuthApiCollection))]
    public class RegisterHandlerTest
    {
        private readonly AuthApiCollectionCluster _collectionCluster;

        public RegisterHandlerTest(AuthApiCollectionCluster collectionCluster)
        {
            _collectionCluster = collectionCluster;
        }

        [Fact]
        public async Task Handle_WhenAccountWithEmailExists_ShouldReturnConflict()
        {
            // Arrange
            using var authDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<AuthDbContext>(AuthApiCollectionCluster.AuthDbName);

            var email = EmailGenerator.Generate();
            authDbContext.Accounts.Add(new Account(email, StringGenerator.GenerateAlpha(), SupportedLanguages.DefaultLanguage));
            await authDbContext.SaveChangesAsync();

            var request = new RegisterRequest(email, PasswordGenerator.Generate(
                includeSpecialChars: true,
                specialChars: AccountConstraints.PasswordSpecialCharacters,
                length: AccountConstraints.PasswordMinLength
            ));

            // Act
            var response = await _collectionCluster.AuthApiClient.PostAsJsonAsync("/accounts/register", request);
            
            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Handle_WhenAccountDoesNotExist_ShouldCreateAccountAndTokenAndReturnOk()
        {
            // Arrange
            var tokenLifespan = TimeSpan.FromHours(_collectionCluster.AuthApiWebApp.GetOptions<TokenLifespansOptions>().EmailVerificationLifespanInHours).TotalMinutes;

            var email = EmailGenerator.Generate();
            var request = new RegisterRequest(email, PasswordGenerator.Generate(
                includeSpecialChars: true,
                specialChars: AccountConstraints.PasswordSpecialCharacters,
                length: AccountConstraints.PasswordMinLength
            ));

            // Act
            var response = await _collectionCluster.AuthApiClient.PostAsJsonAsync("/accounts/register", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var authDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<AuthDbContext>(AuthApiCollectionCluster.AuthDbName);

            var account = authDbContext.Accounts
                .Where(a => a.Email == email)
                .Include(a => a.Tokens)
                .FirstOrDefault();
            Assert.NotNull(account);
            Assert.Single(account.Tokens);

            var token = account.Tokens.FirstOrDefault(t => t.Purpose == ETokenPurpose.EmailVerification);
            Assert.NotNull(token);

            var tokenLifespanInMinutes = (token.ExpirationDate - DateTimeOffset.UtcNow).TotalMinutes;
            Assert.InRange(tokenLifespanInMinutes, tokenLifespan - 1, tokenLifespan + 1);
        }
    }
}
