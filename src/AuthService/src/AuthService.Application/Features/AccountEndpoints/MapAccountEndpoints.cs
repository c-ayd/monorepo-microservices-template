using AuthService.Application.Features.AccountEndpoints.Login;
using AuthService.Application.Features.AccountEndpoints.Logout;
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

            group.MapDelete("/logout", LogoutHandler.Handle)
                .RequireAuthorization();    // Since the endpoint deletes open session, it requires authorization.
        }
    }
}
