using System.Security.Cryptography;
using AuthService.Application.Abstractions.Authentication;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Authentication
{
    public class JwtKeyService : IJwtKeyService
    {
        public RsaSecurityKey PrivateKey { get; private set; }
        public RsaSecurityKey PublicKey { get; private set; }
        public RSAParameters PublicKeyParameters { get; private set; }

        public JwtKeyService(IOptions<JwtOptions> jwtOptions)
        {
            var privatePem = File.ReadAllText(jwtOptions.Value.PrivateKeyPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(privatePem);
            PrivateKey = new RsaSecurityKey(rsa)
            {
                KeyId = jwtOptions.Value.KeyId
            };

            var publicPem = File.ReadAllText(jwtOptions.Value.PublicKeyPath);
            rsa = RSA.Create();
            rsa.ImportFromPem(publicPem);
            PublicKey = new RsaSecurityKey(rsa)
            {
                KeyId = jwtOptions.Value.KeyId
            };

            PublicKeyParameters = rsa.ExportParameters(false);
        }
    }
}
