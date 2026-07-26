using Shared.AspNetCore.Helpers.Options;

namespace AuthService.Infrastructure.Options
{
    public class JwtOptions : IOptions
    {
        public static string Key => "Jwt";

        public required string KeyId { get; set; }
        public required string PrivateKeyPath { get; set; }
        public required string PublicKeyPath { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public required int AccessTokenLifespanInMinutes { get; set; }
        public required int RefreshTokenLifespanInDays { get; set; }
    }
}
