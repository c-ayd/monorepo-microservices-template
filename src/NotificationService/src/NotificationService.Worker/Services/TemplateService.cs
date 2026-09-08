using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Abstractions;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Dtos;

namespace NotificationService.Worker.Services
{
    public class TemplateService : ITemplateService
    {
        public const string DefaultLanguage = "en";

        private readonly IServiceScopeFactory _scopeFactory;

        public TemplateService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public EmailTemplateDto? GetEmailTemplate(string templateId, string? language)
            => GetTemplate(_emailTemplates, templateId, language);

        private T? GetTemplate<T>(
            ConcurrentDictionary<(string templateId, string language), T> templates,
            string templateId,
            string? language)
        {
            if (language == null)
            {
                language = DefaultLanguage;
            }

            // Try to get the requested template
            templates.TryGetValue((templateId, language), out var template);
            if (template != null)
                return template;

            // If the requested template is not found, try to get the template in the default language
            if (language != DefaultLanguage)
            {
                templates.TryGetValue((templateId, DefaultLanguage), out template);
                if (template != null)
                    return template;
            }
            
            return template;
        }

        private ConcurrentDictionary<(string templateId, string language), EmailTemplateDto> _emailTemplates =
            new ConcurrentDictionary<(string templateId, string language), EmailTemplateDto>();

        public async Task RecacheTemplatesAsync(CancellationToken cancellationToken = default)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var templateDbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();

            // Email templates
            var emailTemplates = await templateDbContext.EmailTemplates.ToListAsync(cancellationToken);
            _emailTemplates.Clear();
            foreach (var emailTemplate in emailTemplates)
            {
                _emailTemplates.TryAdd((emailTemplate.TemplateId, emailTemplate.Language),
                    new EmailTemplateDto(emailTemplate.Subject, emailTemplate.Body, emailTemplate.IsBodyHtml));
            }
        }
    }
}
