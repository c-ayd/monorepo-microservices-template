using Shared.Helpers.Options;

namespace NotificationService.Worker.Options
{
    public class ConnectionStringsOptions : IOptions
    {
        public static string Key => "ConnectionStrings";

        public required string TemplateDb { get; set; }
    }
}
