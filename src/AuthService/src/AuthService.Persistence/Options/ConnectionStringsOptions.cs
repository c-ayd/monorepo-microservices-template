using Shared.AspNetCore.Helpers.Options;

namespace AuthService.Persistence.Options
{
    public class ConnectionStringsOptions : IOptions
    {
        public static string Key => "ConnectionStrings";

        public required string AuthDb { get; set; }
    }
}
