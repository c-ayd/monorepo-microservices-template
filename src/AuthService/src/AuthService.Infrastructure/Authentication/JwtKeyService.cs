using System.Security.Cryptography;
using AuthService.Application.Abstractions.Authentication;
using AuthService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Authentication
{
    public class JwtKeyService : IJwtKeyService
    {
        public RsaSecurityKey PrivateKey { get; private set; }
        public RsaSecurityKey PublicKey { get; private set; }
        
        public JwtKeyService(IOptions<JwtOptions> jwtOptions)
        {
            var privatePem = File.ReadAllText(jwtOptions.Value.PrivateKeyPath);
            PrivateKey = LoadKey(jwtOptions.Value.KeyId, privatePem, isPrivate: true);

            var publicPem = File.ReadAllText(jwtOptions.Value.PublicKeyPath);
            PublicKey = LoadKey(jwtOptions.Value.KeyId, publicPem, isPrivate: false);
        }

        private RsaSecurityKey LoadKey(string keyId, string pem, bool isPrivate)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            return new RsaSecurityKey(rsa)
            {
                KeyId = keyId
            };
        }
    }
}
