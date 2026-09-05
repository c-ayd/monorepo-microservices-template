using Microsoft.EntityFrameworkCore;
using Shared.RabbitMq.Helpers.EntityFramework;

namespace AuthService.Persistence.DbContexts
{
    public class AuthRejectedMessagesDbContext : DbContext
    {
        public DbSet<RejectedMessage> RejectedMessages { get; set; }

        public AuthRejectedMessagesDbContext(DbContextOptions<AuthRejectedMessagesDbContext> options) : base(options)
        {
        }
    }
}
