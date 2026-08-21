using System.Net;
using Shared.Http.Response;
using Shared.Http.Response.Structures;

namespace ApiGateway.Web.Middlewares
{
    public class GlobalExceptionHandler
    {
        private static readonly ErrorItem _internalServerError = new ErrorItem(
            Code: "internal_server_error",
            Message: "Something went wrong."
        );

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            RequestDelegate next,
            ILogger<GlobalExceptionHandler> logger)
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
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await JsonResponseBuilder.Error(HttpStatusCode.InternalServerError, [_internalServerError]).ExecuteAsync(context);
                }
            }
        }
    }
}
