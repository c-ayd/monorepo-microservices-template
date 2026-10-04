using Microsoft.Extensions.Logging;
using Shared.Test.Structures;

namespace Shared.Test.Helpers.Fixtures
{
    /// <summary>
    /// Is a null logger for test cases requiring a logger.
    /// </summary>
    /// <typeparam name="T">Type of the class requiring a logger</typeparam>
    public class LoggerFixture<T> : ILogger<T>
        where T : class
    {
        private List<Log> _logs = new List<Log>();
        public IReadOnlyList<Log> Logs => _logs.AsReadOnly();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logs.Add(new Log(
                logLevel,
                eventId,
                formatter(state, exception),
                exception
            ));
        }
    }
}
