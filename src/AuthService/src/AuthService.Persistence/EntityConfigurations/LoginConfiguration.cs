using AuthService.Application.Validations.Constraints;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Persistence.EntityConfigurations
{
    public class LoginConfiguration : IEntityTypeConfiguration<Login>
    {
        public void Configure(EntityTypeBuilder<Login> builder)
        {
            builder.Property(l => l.DeviceInfo)
                .HasMaxLength(LoginConstraints.DeviceInfoMaxLength);
        }
    }
}
