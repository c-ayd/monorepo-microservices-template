using System.Reflection;
using AuthService.Domain.Entities;
using AuthService.Persistence.Filters;
using AuthService.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Persistence.DbContexts
{
    public class AuthDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Token> Tokens { get; set; }

        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.ApplySoftDeleteFilter();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors([
                new UpdateableInterceptor(),
                new SoftDeleteInterceptor()
            ]);
        }
    }
}
