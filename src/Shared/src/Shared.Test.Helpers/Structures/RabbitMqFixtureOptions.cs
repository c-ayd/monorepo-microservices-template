namespace Shared.Test.Helpers.Structures
{
    public record RabbitMqFixtureOptions(
        string Username,
        string Password,
        string Host,
        int Port
    );
}
