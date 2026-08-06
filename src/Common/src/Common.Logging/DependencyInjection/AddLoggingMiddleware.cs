using Common.Logging.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Common.Logging.DependencyInjection
{
    public static partial class DependencyInjection
    {
        public static void UseLoggingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<LoggingMiddleware>();
        }
    }
}
