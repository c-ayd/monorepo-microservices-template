using AuthService.Infrastructure.Authentication;
using AuthService.Infrastructure.Settings;
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
            var jwtSettings = ConfigurationHelper.CreateConfiguration().GetSection(JwtSettings.SettingsKey).Get<JwtSettings>()!;

            // Act
            var jwtKeyService = new JwtKeyService(Options.Create(jwtSettings));

            // Assert
            Assert.NotNull(jwtKeyService.PrivateKey);
            Assert.NotNull(jwtKeyService.PublicKey);
            Assert.Equal(jwtSettings.KeyId, jwtKeyService.PrivateKey.KeyId);
            Assert.Equal(jwtSettings.KeyId, jwtKeyService.PublicKey.KeyId);
        }
    }
}
