using NotificationService.Worker.Dtos;
using NotificationService.Worker.Services;

namespace NotificationService.Worker.Abstractions
{
    /// <summary>
    /// Provides methods to get email templates.
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Gets a specific email template based on a given template ID and language.
        /// </summary>
        /// <param name="templateId">ID of the template</param>
        /// <param name="language">Language of the template</param>
        /// <returns>Returns the email template.</returns>
        EmailTemplateDto? GetEmailTemplate(string templateId, string? language);
    }
}
