using System.Net;
using Microsoft.AspNetCore.Http;
using Shared.Http.Response.Structures;

namespace Shared.Http.Response.Middlewares
{
    public class AuthErrorResponseMiddleware
    {
        private static readonly ErrorItem _unauthorizedError = new ErrorItem(
            Code: "request_unauthenticated",
            Message: "Access is denied."
        );

        private static readonly ErrorItem _forbiddenError = new ErrorItem(
            Code: "request_forbidden",
            Message: "Access is forbidden."
        );

        private readonly RequestDelegate _next;

        public AuthErrorResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            switch (context.Response.StatusCode)
            {
                case StatusCodes.Status401Unauthorized:
                    await JsonResponseBuilder.Error(HttpStatusCode.Unauthorized, [_unauthorizedError])
                        .ExecuteAsync(context);
                    break;
                case StatusCodes.Status403Forbidden:
                    await JsonResponseBuilder.Error(HttpStatusCode.Forbidden, [_forbiddenError])
                        .ExecuteAsync(context);
                    break;
                default:
                    break;
            }
        }
    }
}
