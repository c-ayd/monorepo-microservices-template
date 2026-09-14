using System.Net;
using System.Runtime.InteropServices;
using AuthService.Application.Abstractions.Crypto;
using AuthService.Domain.Entities;
using AuthService.Persistence.DbContexts;
using AuthService.Test.Integration.Application.Collections;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Crypto;
using Shared.Http.Authentication;
using Shared.Test.Generators;

namespace AuthService.Test.Integration.Application.Features.AccountEndpoints.Logout
{
    [Collection(nameof(AuthApiCollection))]
    public class LogoutHandlerTest
    {
        private readonly AuthApiCollectionCluster _collectionCluster;

        public LogoutHandlerTest(AuthApiCollectionCluster collectionCluster)
        {
            _collectionCluster = collectionCluster;
        }

        [Fact]
        public async Task Handle_WhenNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _collectionCluster.AuthApiWebApp.CreateHttpClient();

            // Act
            var response = await client.DeleteAsync("/accounts/logout");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Handle_WhenAuthenticatedAndThereIsNoCookies_ShouldNotDeleteSessionAndReturnOk()
        {
            // Arrange
            await using var authDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<AuthDbContext>();

            var account = new Account(EmailGenerator.Generate(), StringGenerator.GenerateAlphanumeric(), SupportedLanguages.DefaultLanguage);
            account.Sessions.Add(new Session(account.Id, StringGenerator.GenerateAlphanumeric(), DateTimeOffset.UtcNow));
            
            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var client = _collectionCluster.AuthApiWebApp.CreateHttpClient();
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, account.Id.ToString());
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.PreferredLanguage.HeaderKey, SupportedLanguages.DefaultLanguage);
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.IssuedAt.HeaderKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            // Act
            var response = await client.DeleteAsync("/accounts/logout");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            authDbContext.ChangeTracker.Clear();

            var sessionsFromDb = await authDbContext.Sessions
                .Where(s => s.AccountId == account.Id)
                .ToListAsync();
            Assert.Single(sessionsFromDb);
        }

        [Fact]
        public async Task Handle_WhenAuthenticatedAndCookieValuesAreAltered_ShouldNotDeleteSessionAndReturnOk()
        {
            // Arrange
            await using var authDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<AuthDbContext>();

            var account = new Account(EmailGenerator.Generate(), StringGenerator.GenerateAlphanumeric(), SupportedLanguages.DefaultLanguage);
            account.Sessions.Add(new Session(account.Id, StringGenerator.GenerateAlphanumeric(), DateTimeOffset.UtcNow));

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var client = _collectionCluster.AuthApiWebApp.CreateHttpClient();
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, account.Id.ToString());
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.PreferredLanguage.HeaderKey, SupportedLanguages.DefaultLanguage);
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.IssuedAt.HeaderKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            
            client.DefaultRequestHeaders.Add("Cookie", $"{CookieKeys.SessionId}={Guid.NewGuid().ToString()}; {CookieKeys.RefreshToken}={StringGenerator.GenerateAlphanumeric()}");

            // Act
            var response = await client.DeleteAsync("/accounts/logout");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            authDbContext.ChangeTracker.Clear();

            var sessionsFromDb = await authDbContext.Sessions
                .Where(s => s.AccountId == account.Id)
                .ToListAsync();
            Assert.Single(sessionsFromDb);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task Handle_WhenAuthenticatedAndCookieValuesDoNotMatch_ShouldNotDeleteSessionAndReturnOk(bool matchSessionId, bool matchRefreshToken)
        {
            // Arrange
            await using var authDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<AuthDbContext>();
            var hashVersions = _collectionCluster.AuthApiWebApp.GetService<IHashVersions>();
            var dataProtectionService = _collectionCluster.AuthApiWebApp.GetService<IDataProtectionService>();

            var refreshToken = StringGenerator.GenerateAlphanumeric();
            var account = new Account(EmailGenerator.Generate(), StringGenerator.GenerateAlphanumeric(), SupportedLanguages.DefaultLanguage);
            account.Sessions.Add(new Session(account.Id, StringGenerator.GenerateAlphanumeric(), DateTimeOffset.UtcNow));

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var client = _collectionCluster.AuthApiWebApp.CreateHttpClient();
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, account.Id.ToString());
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.PreferredLanguage.HeaderKey, SupportedLanguages.DefaultLanguage);
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.IssuedAt.HeaderKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            client.DefaultRequestHeaders.Add("Cookie", $"{CookieKeys.SessionId}={dataProtectionService.Protect(dataProtectionService.CookieProtector, matchSessionId ? account.Sessions.ElementAt(0).Id.ToString() : Guid.NewGuid().ToString())}; {CookieKeys.RefreshToken}={dataProtectionService.Protect(dataProtectionService.CookieProtector, matchRefreshToken ? refreshToken : StringGenerator.GenerateAlphanumeric())}");

            // Act
            var response = await client.DeleteAsync("/accounts/logout");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            authDbContext.ChangeTracker.Clear();

            var sessionsFromDb = await authDbContext.Sessions
                .Where(s => s.AccountId == account.Id)
                .ToListAsync();
            Assert.Single(sessionsFromDb);
        }

        [Fact]
        public async Task Handle_WhenAuthenticatedAndCookieValuesMatch_ShouldDeleteSessionAndReturnOk()
        {
            // Arrange
            await using var authDbContext = _collectionCluster.PostgreSqlFixture.CreateDbContext<AuthDbContext>();
            var hashVersions = _collectionCluster.AuthApiWebApp.GetService<IHashVersions>();
            var dataProtectionService = _collectionCluster.AuthApiWebApp.GetService<IDataProtectionService>();

            var refreshToken = StringGenerator.GenerateAlphanumeric();
            var account = new Account(EmailGenerator.Generate(), StringGenerator.GenerateAlphanumeric(), SupportedLanguages.DefaultLanguage);
            account.Sessions.Add(new Session(account.Id, ValueHasher.Hash(refreshToken, hashVersions.CurrentVersion, hashVersions.GetHashOptions), DateTimeOffset.UtcNow));

            await authDbContext.Accounts.AddAsync(account);
            await authDbContext.SaveChangesAsync();

            var client = _collectionCluster.AuthApiWebApp.CreateHttpClient();
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.Id.HeaderKey, account.Id.ToString());
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.PreferredLanguage.HeaderKey, SupportedLanguages.DefaultLanguage);
            client.DefaultRequestHeaders.Add(ApiGatewayAuthKeys.Claims.IssuedAt.HeaderKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            client.DefaultRequestHeaders.Add("Cookie", $"{CookieKeys.SessionId}={dataProtectionService.Protect(dataProtectionService.CookieProtector, account.Sessions.ElementAt(0).Id.ToString())}; {CookieKeys.RefreshToken}={dataProtectionService.Protect(dataProtectionService.CookieProtector, refreshToken)}");

            // Act
            var response = await client.DeleteAsync("/accounts/logout");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            authDbContext.ChangeTracker.Clear();

            var sessionsFromDb = await authDbContext.Sessions
                .Where(s => s.AccountId == account.Id)
                .ToListAsync();
            Assert.Empty(sessionsFromDb);
        }
    }
}
