using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Dtos;

namespace NotificationService.Worker.Services
{
    public class TemplateService
    {
        public const string DefaultLanguage = "en";

        private readonly IServiceScopeFactory _scopeFactory;

        public TemplateService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public EmailTemplateDto? GetEmailTemplateAsync(string templateId, string? language = DefaultLanguage)
        {
            return GetTemplate(_emailTemplates, templateId, language);
        }

        private T? GetTemplate<T>(ConcurrentDictionary<(string templateId, string language), T> templates, string templateId, string? language)
        {
            if (language == null)
            {
                language = DefaultLanguage;
            }

            templates.TryGetValue((templateId, language), out var template);
            if (template != null)
                return template;

            if (language != DefaultLanguage)
            {
                templates.TryGetValue((templateId, DefaultLanguage), out template);
            }

            return template;
        }

        private ConcurrentDictionary<(string templateId, string language), EmailTemplateDto> _emailTemplates = 
            new ConcurrentDictionary<(string templateId, string language), EmailTemplateDto>();

        public async Task RecacheAllTemplatesAsync(CancellationToken cancellationToken = default)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var templateDbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            
            // Email templates
            var emailTemplates = await templateDbContext.EmailTemplates.ToListAsync(cancellationToken);
            foreach (var emailTemplate in emailTemplates)
            {
                var template = new EmailTemplateDto(emailTemplate.Subject, emailTemplate.Body, emailTemplate.IsBodyHtml);
                _emailTemplates.AddOrUpdate((emailTemplate.TemplateId, emailTemplate.Language), template, (key, oldValue) => template);
            }
        }
    }
}
