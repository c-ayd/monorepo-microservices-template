namespace Shared.RabbitMq.Notifications.Messages
{
    public record EmailMessage(
        string[] To,
        string TemplateId,
        string? Language,
        string[]? SubjectParameters = null,
        string[]? BodyParameters = null
    );
}
