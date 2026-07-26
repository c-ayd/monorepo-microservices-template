using AuthService.Infrastructure.Authentication;
using AuthService.Infrastructure.Options;
using AuthService.Test.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AuthService.Test.Unit.Infrastructure.Authentication
{
    public class JwtKeyServiceTest
    {
        [Fact]
        public void Constructor_WhenServiceIsInstantiated_ShouldLoadKeys()
        {
            // Arrange
            var jwtOptions = ConfigurationHelper.CreateConfiguration().GetSection(JwtOptions.Key).Get<JwtOptions>()!;

            // Act
            var jwtKeyService = new JwtKeyService(Options.Create(jwtOptions));

            // Assert
            Assert.NotNull(jwtKeyService.PrivateKey);
            Assert.NotNull(jwtKeyService.PublicKey);
            Assert.Equal(jwtOptions.KeyId, jwtKeyService.PrivateKey.KeyId);
            Assert.Equal(jwtOptions.KeyId, jwtKeyService.PublicKey.KeyId);
        }
    }
}
