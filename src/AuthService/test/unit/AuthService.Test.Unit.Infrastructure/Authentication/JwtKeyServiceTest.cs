using AuthService.Infrastructure.Authentication;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;

namespace AuthService.Test.Unit.Infrastructure.Authentication
{
    public class JwtKeyServiceTest
    {
        private readonly JwtOptions _jwtOptions = new JwtOptions()
        {
            KeyId = "v1",
            PrivateKeyPath = "./test_jwt_private.pem",
            PublicKeyPath = "./test_jwt_public.pem",
            Issuer = "https://localhost:7000",
            Audience = "https://localhost:6000",
            AccessTokenLifespanInMinutes = 5,
            RefreshTokenLifespanInDays = 2
        };

        [Fact]
        public void Constructor_WhenServiceIsInstantiated_ShouldLoadKeys()
        {
            // Act
            var jwtKeyService = new JwtKeyService(Options.Create(_jwtOptions));

            // Assert
            Assert.NotNull(jwtKeyService.PrivateKey);
            Assert.NotNull(jwtKeyService.PublicKey);
            Assert.Equal(_jwtOptions.KeyId, jwtKeyService.PrivateKey.KeyId);
            Assert.Equal(_jwtOptions.KeyId, jwtKeyService.PublicKey.KeyId);
        }
    }
}
