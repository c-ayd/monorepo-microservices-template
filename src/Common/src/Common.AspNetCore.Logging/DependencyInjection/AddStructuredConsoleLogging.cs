using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Common.AspNetCore.Logging.DependencyInjection
{
    public static class DependencyInjection
    {
        public static void AddStructuredConsoleLogging(this ILoggingBuilder logging, bool isDevelopment)
        {
            logging.ClearProviders();
            logging.AddJsonConsole(config =>
            {
                config.IncludeScopes = true;
                config.UseUtcTimestamp = true;
                config.TimestampFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff'Z'";
                config.JsonWriterOptions = new JsonWriterOptions()
                {
                    Indented = isDevelopment,
                    IndentSize = 2,
                    SkipValidation = !isDevelopment,
                };
            });
        }
    }
}
