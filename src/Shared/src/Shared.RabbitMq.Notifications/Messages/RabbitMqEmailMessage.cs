namespace Shared.RabbitMq.Notifications.Messages
{
    public record RabbitMqEmailMessage(
        string[] To,
        string TemplateId,
        string? Language,
        string[]? SubjectParameters = null,
        string[]? BodyParameters = null
    );
}
