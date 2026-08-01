using Shared.AspNetCore.Helpers.Options;

namespace NotificationService.Options
{
    public class SmtpOptions : IOptions
    {
        public static string Key => "Smtp";

        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string SenderEmail { get; set; }
        public required string SenderDisplayName { get; set; }
        public required string Server { get; set; }
        public required int Port { get; set; }
    }
}
