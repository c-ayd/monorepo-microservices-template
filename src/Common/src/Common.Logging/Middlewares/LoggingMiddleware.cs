using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Logging.Middlewares
{
    internal class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(
            RequestDelegate next,
            ILogger<LoggingMiddleware> logger)
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
