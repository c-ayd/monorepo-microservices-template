namespace Shared.RabbitMq.Notification.Messages
{
    public record EmailMessage(
        IEnumerable<string> To,
        string Subject,
        string Body,
        bool IsBodyHtml
    );
}
