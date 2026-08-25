using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Infrastructure.Authentication;
using AuthService.Infrastructure.Options;
using AuthService.Test.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Test.Generators;
using Shared.Test.Helpers;

namespace AuthService.Test.Unit.Infrastructure.Authentication
{
    public class JwtServiceTest
    {
        private readonly JwtOptions _jwtOptions;
        private readonly JwtKeyService _jwtKeyService;
        private readonly JwtService _jwtService;

        public JwtServiceTest()
        {
            _jwtOptions = ConfigurationHelper.CreateConfigurationFromTestSettings()
                .GetSection(JwtOptions.Key).Get<JwtOptions>()!;
            var jwtOptionsPattern = Options.Create(_jwtOptions);

            _jwtKeyService = new JwtKeyService(jwtOptionsPattern);
            _jwtService = new JwtService(jwtOptionsPattern, _jwtKeyService);
        }

        private (List<Claim>?, DateTime?, DateTime?) DecodeAccessToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters()
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,

                ValidAudience = _jwtOptions.Audience,
                ValidIssuer = _jwtOptions.Issuer,
                IssuerSigningKey = _jwtKeyService.PublicKey
            };

            try
            {
                var claimsPrincipal = handler.ValidateToken(accessToken, validationParams, out var token);
                if (token is not JwtSecurityToken jwtToken)
                {
                    Assert.Fail("Validated token is not a JWT security token.");
                    return (null, null, null);
                }

                return (claimsPrincipal.Claims.ToList(), jwtToken.ValidFrom, jwtToken.ValidTo);
            }
            catch (Exception exception)
            {
                Assert.Fail($"Validation Failed: {exception.Message}");
                return (null, null, null);
            }
        }

        [Fact]
        public void GenerateToken_WhenClaimsAndNotBeforeDateTimeAreNotGiven_ShouldGenerateToken()
        {
            // Arrange
            var accessTokenLifespan = _jwtOptions.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtOptions.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTimeOffset.UtcNow;

            // Act
            var result = _jwtService.GenerateTokens();

            // Assert
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);

            var accessTokenLifespanInMinutes = (result.AccessTokenExpirationDate - now).TotalMinutes;
            var refreshTokenLifespanInMinutes = (result.RefreshTokenExpirationDate - now).TotalMinutes;
            Assert.InRange(accessTokenLifespanInMinutes, accessTokenLifespan - 1, accessTokenLifespan + 1);
            Assert.InRange(refreshTokenLifespanInMinutes, refreshTokenLifespan - 1, refreshTokenLifespan + 1);
        }

        [Fact]
        public void GenerateJwtToken_WhenClaimsAreNotGivenButNotBeforeDateTimeIsGiven_ShouldGenerateToken()
        {
            // Arrange
            var accessTokenLifespan = _jwtOptions.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtOptions.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTimeOffset.UtcNow;

            var notBefore = now.AddMinutes(1);

            // Act
            var result = _jwtService.GenerateTokens(notBefore: notBefore);

            // Assert
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);

            var accessTokenLifespanInMinutes = (result.AccessTokenExpirationDate - now).TotalMinutes;
            var refreshTokenLifespanInMinutes = (result.RefreshTokenExpirationDate - now).TotalMinutes;
            Assert.InRange(accessTokenLifespanInMinutes, accessTokenLifespan - 1, accessTokenLifespan + 1);
            Assert.InRange(refreshTokenLifespanInMinutes, refreshTokenLifespan - 1, refreshTokenLifespan + 1);

            var (_, decodedNotBefore, _) = DecodeAccessToken(result.AccessToken);
            Assert.Equal(notBefore.ToString("dd-MM-yyyy HH:mm:ss"), decodedNotBefore!.Value.ToString("dd-MM-yyyy HH:mm:ss"));
        }

        [Fact]
        public void GenerateJwtToken_WhenClaimsAreGivenButNotBeforeDateTimeIsNotGiven_ShouldGenerateToken()
        {
            // Arrange
            var nameIdentifier = StringGenerator.GeneratePrintableAscii();
            var email = EmailGenerator.Generate();

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, nameIdentifier),
                new Claim(ClaimTypes.Email, email)
            };

            var accessTokenLifespan = _jwtOptions.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtOptions.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTimeOffset.UtcNow;

            // Act
            var result = _jwtService.GenerateTokens(claims);

            // Assert
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);

            var accessTokenLifespanInMinutes = (result.AccessTokenExpirationDate - now).TotalMinutes;
            var refreshTokenLifespanInMinutes = (result.RefreshTokenExpirationDate - now).TotalMinutes;
            Assert.InRange(accessTokenLifespanInMinutes, accessTokenLifespan - 1, accessTokenLifespan + 1);
            Assert.InRange(refreshTokenLifespanInMinutes, refreshTokenLifespan - 1, refreshTokenLifespan + 1);

            var (decodedClaims, _, _) = DecodeAccessToken(result.AccessToken);
            var decodedNameIdentifier = decodedClaims!.Find(c => c.Type == ClaimTypes.NameIdentifier)!.Value;
            var decodedEmail = decodedClaims!.Find(c => c.Type == ClaimTypes.Email)!.Value;
            Assert.Equal(nameIdentifier, decodedNameIdentifier);
            Assert.Equal(email, decodedEmail);
        }

        [Fact]
        public void GenerateJwtToken_WhenClaimsAndNotBeforeDateTimeAreGiven_ShouldGenerateToken()
        {
            // Arrange
            var nameIdentifier = StringGenerator.GeneratePrintableAscii();
            var email = EmailGenerator.Generate();

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, nameIdentifier),
                new Claim(ClaimTypes.Email, email)
            };

            var accessTokenLifespan = _jwtOptions.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtOptions.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTimeOffset.UtcNow;

            var notBefore = now.AddMinutes(1);

            // Act
            var result = _jwtService.GenerateTokens(claims, notBefore);

            // Assert
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);

            var accessTokenLifespanInMinutes = (result.AccessTokenExpirationDate - now).TotalMinutes;
            var refreshTokenLifespanInMinutes = (result.RefreshTokenExpirationDate - now).TotalMinutes;
            Assert.InRange(accessTokenLifespanInMinutes, accessTokenLifespan - 1, accessTokenLifespan + 1);
            Assert.InRange(refreshTokenLifespanInMinutes, refreshTokenLifespan - 1, refreshTokenLifespan + 1);

            var (decodedClaims, decodedNotBefore, _) = DecodeAccessToken(result.AccessToken);
            var decodedNameIdentifier = decodedClaims!.Find(c => c.Type == ClaimTypes.NameIdentifier)!.Value;
            var decodedEmail = decodedClaims!.Find(c => c.Type == ClaimTypes.Email)!.Value;
            Assert.Equal(nameIdentifier, decodedNameIdentifier);
            Assert.Equal(email, decodedEmail);
            Assert.Equal(notBefore.ToString("dd-MM-yyyy HH:mm:ss"), decodedNotBefore!.Value.ToString("dd-MM-yyyy HH:mm:ss"));
        }
    }
}
