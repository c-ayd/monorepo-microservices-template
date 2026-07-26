using Shared.AspNetCore.Helpers.Options;

namespace AuthService.Persistence.Options
{
    public class SeedDataOptions : IOptions
    {
        public static string Key => "SeedData";

        public required AuthDbData AuthDb { get; set; }

        public class AuthDbData
        {
            public required List<string> Roles { get; set; }
            public required List<AccountRolePair> Accounts { get; set; }

            public class AccountRolePair
            {
                public required string Email { get; set; }
                public required string Role { get; set; }
            }
        }
    }
}
