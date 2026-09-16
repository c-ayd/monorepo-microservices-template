using Shared.Helpers.Options;

namespace ApiGateway.Web.Options
{
    public class ConnectionStringsOptions : IOptions
    {
        public static string Key => "ConnectionStrings";

        public required string AuthTokenBlacklistRedis { get; set; }
    }
}
