using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Application.Dtos.Authentication
{
    public class JwtKeyDto
    {
        public required string Id { get; set; }
        public required RsaSecurityKey Key { get; set; }
        public RSAParameters? Parameters { get; set; }
    }
}
