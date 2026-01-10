using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Sinks;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// A logger implementation that delegates log entries to multiple sinks.
/// </summary>
public class UnifiedLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IEnumerable<ILogSink> _sinks;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The name of the logger category.</param>
    /// <param name="sinks">The list of sinks to write logs to.</param>
    public UnifiedLogger(string categoryName, IEnumerable<ILogSink> sinks)
    {
        _categoryName = categoryName;
        _sinks = sinks;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var logEntry = new LogEntry(
            DateTime.UtcNow,
            logLevel,
            _categoryName,
            message,
            exception);

        foreach (var sink in _sinks)
        {
            // Each sink decides if it wants to handle this entry
            // (e.g. based on category regex, loglevel, etc.)
            sink.Write(logEntry);
        }
    }
}
