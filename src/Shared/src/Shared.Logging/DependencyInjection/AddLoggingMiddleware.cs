using Shared.Logging.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Shared.Logging.DependencyInjection
{
    public static partial class DependencyInjection
    {
        public static void UseLoggingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<LoggingMiddleware>();
        }
    }
}
