using AuthService.Application.Features.AccountEndpoints.Login;
using AuthService.Application.Features.AccountEndpoints.Register;
using Microsoft.AspNetCore.Builder;
using Shared.Http.DependencyInjection;

namespace AuthService.Application.Features.AccountEndpoints
{
    public static class MapEndpoints
    {
        public static void MapAccountEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/accounts");

            group.MapPost("/register", RegisterHandler.Handle)
                .AddValidation<RegisterRequest>();
            
            group.MapPost("/login", LoginHandler.Handle)
                .AddValidation<LoginRequest>();
        }
    }
}
