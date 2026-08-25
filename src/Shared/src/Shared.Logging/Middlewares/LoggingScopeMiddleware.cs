using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Logging.Middlewares
{
    public class LoggingScopeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingScopeMiddleware> _logger;

        public LoggingScopeMiddleware(
            RequestDelegate next,
            ILogger<LoggingScopeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            using (_logger.BeginScope("Application Name: {AppName}", LoggingOptions.ApplicationName))
            {
                await _next(context);
            }
        }
    }
}
