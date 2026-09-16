using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Application.Abstractions.Authentication
{
    /// <summary>
    /// Provides methods to access the RSA private and public keys.
    /// </summary>
    public interface IJwtKeyService
    {
        RsaSecurityKey PrivateKey { get; }
        RsaSecurityKey PublicKey { get; }
        RSAParameters PublicKeyParameters { get; }
    }
}
