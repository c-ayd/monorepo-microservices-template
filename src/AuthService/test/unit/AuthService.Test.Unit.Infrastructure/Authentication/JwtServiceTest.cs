using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Infrastructure.Authentication;
using AuthService.Infrastructure.Settings;
using AuthService.Test.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.TestGenerators;

namespace AuthService.Test.Unit.Infrastructure.Authentication
{
    public class JwtServiceTest
    {
        private readonly JwtSettings _jwtSettings;
        private readonly JwtKeyService _jwtKeyService;
        private readonly JwtService _jwtService;

        public JwtServiceTest()
        {
            _jwtSettings = ConfigurationHelper.CreateConfiguration().GetSection(JwtSettings.SettingsKey).Get<JwtSettings>()!;
            var jwtOptions = Options.Create(_jwtSettings);

            _jwtKeyService = new JwtKeyService(jwtOptions);
            _jwtService = new JwtService(jwtOptions, _jwtKeyService);
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

                ValidAudience = _jwtSettings.Audience,
                ValidIssuer = _jwtSettings.Issuer,
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
            var accessTokenLifespan = _jwtSettings.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtSettings.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTime.UtcNow;

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
            var accessTokenLifespan = _jwtSettings.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtSettings.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTime.UtcNow;

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

            var accessTokenLifespan = _jwtSettings.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtSettings.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTime.UtcNow;

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

            var accessTokenLifespan = _jwtSettings.AccessTokenLifespanInMinutes;
            var refreshTokenLifespan = _jwtSettings.RefreshTokenLifespanInDays * 24 * 60;
            var now = DateTime.UtcNow;

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
