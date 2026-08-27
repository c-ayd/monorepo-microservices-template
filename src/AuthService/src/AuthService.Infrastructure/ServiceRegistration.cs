using AuthService.Application.Abstractions.Authentication;
using AuthService.Application.Abstractions.Crypto;
using AuthService.Application.Abstractions.Notifications;
using AuthService.Infrastructure.Authentication;
using AuthService.Infrastructure.Crypto;
using AuthService.Infrastructure.MessageBrokers;
using AuthService.Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IJwtKeyService, JwtKeyService>();
            services.AddSingleton<IJwtService, JwtService>();

            services.AddSingleton<IPasswordHasher, Pbkdf2>();
            services.AddSingleton<IHashVersions, HashVersions>();

            services.AddSingleton<RabbitMqConnectionService>();
            services.AddSingleton<IEmailService, EmailService>();
        }
    }
}
