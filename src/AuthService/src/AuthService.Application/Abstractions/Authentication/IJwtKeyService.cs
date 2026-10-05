using AuthService.Application.Dtos.Authentication;

namespace AuthService.Application.Abstractions.Authentication
{
    /// <summary>
    /// Provides methods to access the RSA private and public keys.
    /// </summary>
    public interface IJwtKeyService
    {
        string CurrentKeyId { get; }

        JwtKeyDto? GetPublicKey(string keyId);
        JwtKeyDto? GetPrivateKey(string keyId);
        IReadOnlyCollection<JwtKeyDto> GetAllPublicKeys();
        IReadOnlyCollection<JwtKeyDto> GetAllPrivateKeys();
    }
}
