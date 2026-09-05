using Shared.Helpers.Options;

namespace AuthService.Application.Options
{
    public class ConnectionStringsOptions : IOptions
    {
        public static string Key => "ConnectionStrings";

        public required string AuthDb { get; set; }
        public required string AuthRejectedMessagesDb { get; set; }
        public required string AuthDataProtectionRedis { get; set; }
        public required string AuthTokenBlacklistRedis { get; set; }
    }
}
