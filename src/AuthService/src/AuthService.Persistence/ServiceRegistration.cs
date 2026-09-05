using AuthService.Application.Abstractions.DbContexts;
using AuthService.Application.Abstractions.DistributedCaches;
using AuthService.Persistence.DbContexts;
using AuthService.Persistence.DistributedCaches;
using AuthService.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Persistence
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AuthDbContext>(_ => 
                _.UseNpgsql(configuration.GetConnectionString(nameof(ConnectionStringsOptions.AuthDb))));
            services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());

            services.AddDbContext<AuthRejectedMessagesDbContext>(_ =>
                _.UseNpgsql(configuration.GetConnectionString(nameof(ConnectionStringsOptions.AuthRejectedMessagesDb))));

            services.AddSingleton<ITokenBlacklist, TokenBlacklist>();
        }
    }
}
