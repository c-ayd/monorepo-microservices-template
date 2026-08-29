using System.Security.Claims;
using Shared.Http.Authentication;

namespace AuthService.Api.Middlewares
{
    public class AccountPreferenceMiddleware
    {
        private readonly RequestDelegate _next;

        public AccountPreferenceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                context.Items["PreferredLanguage"] = context.User.FindFirstValue(ApiGatewayAuthKeys.Claims.PreferredLanguage.ClaimType);
            }
            else
            {
                context.Items["PreferredLanguage"] = context.Request.GetTypedHeaders().AcceptLanguage
                    .OrderByDescending(h => h.Quality ?? 1.0)
                    .Select(h => h.Value.ToString().Split('-')[0].ToLower())
                    .FirstOrDefault() ?? "en";
            }

            await _next(context);
        }
    }
}
