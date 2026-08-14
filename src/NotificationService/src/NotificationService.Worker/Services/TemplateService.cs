using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.DbContexts;
using NotificationService.Worker.Dtos;

namespace NotificationService.Worker.Services
{
    public class TemplateService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public TemplateService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<EmailTemplateDto?> GetEmailTemplateAsync(string templateId, string language, CancellationToken cancellationToken = default)
        {
            _emailTemplates.TryGetValue((templateId, language), out var emailTemplate);
            if (emailTemplate != null)
                return emailTemplate;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var templateDbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();

            var template = await templateDbContext.EmailTemplates.FindAsync(templateId, language, cancellationToken);
            if (template != null)
            {
                emailTemplate = new EmailTemplateDto(template.Subject, template.Body, template.IsBodyHtml);
                _emailTemplates.TryAdd((templateId, language), emailTemplate);
            }

            return emailTemplate;
        }

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

        private ConcurrentDictionary<(string templateId, string language), EmailTemplateDto> _emailTemplates = 
            new ConcurrentDictionary<(string templateId, string language), EmailTemplateDto>();
    }
}
