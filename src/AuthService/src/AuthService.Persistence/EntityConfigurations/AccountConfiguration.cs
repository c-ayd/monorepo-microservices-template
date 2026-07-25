using AuthService.Application.Validations.Constraints;
using AuthService.Domain.Entities;
using AuthService.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Persistence.EntityConfigurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.HasIndex(a => a.Email);

            builder.Property(a => a.Email)
                .HasMaxLength(AccountConstraints.EmailMaxLength)
                .HasConversion<ToLowerConverter>();

            builder.Property(a => a.NewEmail)
                .HasMaxLength(AccountConstraints.EmailMaxLength)
                .HasConversion<ToLowerConverter>();

            // Relationships
            builder.HasMany(a => a.Roles)
                .WithMany(r => r.Accounts);

            builder.HasMany(a => a.Logins)
                .WithOne(l => l.Account)
                .HasForeignKey(l => l.AccountId);

            builder.HasMany(a => a.Tokens)
                .WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId);
        }
    }
}
