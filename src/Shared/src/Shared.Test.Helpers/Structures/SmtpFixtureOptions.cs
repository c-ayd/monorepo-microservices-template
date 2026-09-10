namespace Shared.Test.Helpers.Structures
{
    public record SmtpFixtureOptions(
        string Email,
        string DisplayName,
        string Server,
        int Port,
        bool EnableSsl
    );
}
