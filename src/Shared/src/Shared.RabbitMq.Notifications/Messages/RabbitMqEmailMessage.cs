namespace Shared.RabbitMq.Notifications.Messages
{
    public record RabbitMqEmailMessage(
        IEnumerable<string> To,
        string Subject,
        string Body,
        bool IsBodyHtml
    );
}
