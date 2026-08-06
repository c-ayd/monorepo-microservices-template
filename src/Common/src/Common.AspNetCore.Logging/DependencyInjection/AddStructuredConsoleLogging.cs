using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Common.AspNetCore.Logging.DependencyInjection
{
    public static partial class DependencyInjection
    {
        public static void AddStructuredConsoleLogging(this ILoggingBuilder logging, string appName, bool isDevelopment)
        {
            LoggingOptions.ApplicationName = appName;

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
