namespace AuthDb.Initializer.Options
{
    public class AuthDbSeedDataOptions
    {
        public required List<string> Roles { get; set; }
        public required List<AccountDetails> Accounts { get; set; }

        public class AccountDetails
        {
            public required string Email { get; set; }
            public string? Role { get; set; }
            public string? PreferredLanguage { get; set; }
        }
    }
}
