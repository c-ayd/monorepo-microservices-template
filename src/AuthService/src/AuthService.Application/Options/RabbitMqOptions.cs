using Shared.Helpers.Options;

namespace AuthService.Application.Options
{
    public class RabbitMqOptions : IOptions
    {
        public static string Key => "RabbitMq";

        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Host { get; set; }
        public required int Port { get; set; }
    }
}
