using Microsoft.Extensions.Logging;

namespace Shared.Test.Structures
{
    public record Log(
        LogLevel LogLevel,
        EventId EventId,
        string Message,
        Exception? Exception
    );
}
