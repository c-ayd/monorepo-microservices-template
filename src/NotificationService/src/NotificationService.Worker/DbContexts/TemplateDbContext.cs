using Microsoft.EntityFrameworkCore;
using NotificationService.Worker.Entities;

namespace NotificationService.Worker.DbContexts
{
    public class TemplateDbContext : DbContext
    {
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmailTemplate>(builder =>
            {
                builder.HasKey(et => new { et.TemplateId, et.Language });

                builder.Property(et => et.TemplateId)
                    .HasMaxLength(50);
                builder.Property(et => et.Language)
                    .HasMaxLength(50);
            });
        }
    }
}
