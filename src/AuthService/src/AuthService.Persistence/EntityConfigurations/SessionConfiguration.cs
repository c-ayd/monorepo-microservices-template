using AuthService.Application.Validations.Constraints;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Persistence.EntityConfigurations
{
    public class SessionConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            builder.Property(l => l.DeviceInfo)
                .HasMaxLength(SessionConstraints.DeviceInfoMaxLength);
        }
    }
}
