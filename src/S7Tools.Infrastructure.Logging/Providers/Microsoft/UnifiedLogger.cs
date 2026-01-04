using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Sinks;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

public sealed class UnifiedLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IEnumerable<ILogSink> _sinks;

    public UnifiedLogger(string categoryName, IEnumerable<ILogSink> sinks)
    {
        _categoryName = categoryName;
        _sinks = sinks;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => new DisposableScope();

    public bool IsEnabled(LogLevel logLevel) => true;

    private sealed class DisposableScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var logEntry = new LogEntry(
            DateTime.UtcNow,
            logLevel,
            _categoryName,
            formatter(state, exception),
            exception?.ToString()
        );

        foreach (var sink in _sinks)
        {
            sink.Write(logEntry);
        }
    }
}
