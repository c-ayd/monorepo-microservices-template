using System.Net;
using Shared.Http.Response;
using Shared.Http.Response.Structures;

namespace ApiGateway.Web.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Something went wrong. Message: {Message}",
                    exception.Message);

                if (!context.Response.HasStarted)
                {
                    await JsonResponseBuilder.Error(HttpStatusCode.InternalServerError, [new ErrorItem(
                        Code: "internal_server_error",
                        Message: "Something went wrong."
                    )]).ExecuteAsync(context);
                }
            }
        }
    }
}
