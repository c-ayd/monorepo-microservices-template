namespace NotificationService.Worker.Dtos
{
    public record EmailTemplateDto(
        string Subject,
        string Body,
        bool IsBodyHtml
    );
}
