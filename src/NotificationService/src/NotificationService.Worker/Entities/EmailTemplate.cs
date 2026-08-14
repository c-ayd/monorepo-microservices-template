#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace NotificationService.Worker.Entities
{
    public class EmailTemplate
    {
        public string TemplateId { get; set; }
        public string Language { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; }

        // Reserved for EF Core
        private EmailTemplate()
        {
        }

        public EmailTemplate(string templateId,
            string language,
            string subject,
            string body,
            bool isBodyHtml)
        {
            TemplateId = templateId;
            Language = language;
            Subject = subject;
            Body = body;
            IsBodyHtml = isBodyHtml;
        }
    }
}
