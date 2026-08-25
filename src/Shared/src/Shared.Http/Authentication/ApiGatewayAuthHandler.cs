using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.Http.Authentication
{
    public class ApiGatewayAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ApiGatewayAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if the user is authenticated
            if (!Request.Headers.TryGetValue(ApiGatewayConstants.UserId.HeaderKey, out var userId))
                return AuthenticateResult.NoResult();

            // Extract the user ID
            var claims = new List<Claim>()
            {
                new Claim(ApiGatewayConstants.UserId.ClaimType, userId.ToString())
            };

            // Extract the user roles
            if (Request.Headers.TryGetValue(ApiGatewayConstants.UserRoles.HeaderKey, out var userRoles))
            {
                foreach (var userRole in userRoles.ToArray())
                {
                    claims.Add(new Claim(ApiGatewayConstants.UserRoles.ClaimType, userRole!));
                }
            }

            // Extract the other user claims requiring value checks
            foreach (var userClaim in ApiGatewayConstants.UserClaims)
            {
                if (Request.Headers.TryGetValue(userClaim.HeaderKey, out var headerValue))
                {
                    claims.Add(new Claim(userClaim.ClaimType, headerValue.ToString()));
                }
            }

            var identity = new ClaimsIdentity(claims, ApiGatewayConstants.AuthenticationScheme,
                ApiGatewayConstants.UserId.ClaimType, ApiGatewayConstants.UserRoles.ClaimType);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ApiGatewayConstants.AuthenticationScheme);
            return AuthenticateResult.Success(ticket);
        }
    }
}
