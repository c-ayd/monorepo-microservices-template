using AuthService.Persistence.DbContexts;
using AuthService.Persistence.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Persistence
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connStrings = configuration.GetSection(ConnectionStringsOptions.Key).Get<ConnectionStringsOptions>()!;

            services.AddDbContext<AuthDbContext>(_ => _.UseNpgsql(connStrings.AuthDb));
        }
    }
}
