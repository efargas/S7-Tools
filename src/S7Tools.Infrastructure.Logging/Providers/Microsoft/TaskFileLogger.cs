using System;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// A logger that writes logs to a file via a provider for a specific task.
/// </summary>
public sealed class TaskFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly UnifiedFileLoggerProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskFileLogger"/> class.
    /// </summary>
    public TaskFileLogger(string categoryName, UnifiedFileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => Microsoft.Extensions.Logging.Abstractions.NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _provider._config.LogLevel;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var logEntry = new LogEntry(
            DateTime.UtcNow,
            logLevel,
            _categoryName,
            formatter(state, exception),
            exception?.ToString()
        );

        var message = System.Text.Json.JsonSerializer.Serialize(logEntry, LogJsonContext.Default.LogEntry);

        var logType = "Main";
        var parts = _categoryName.Split('.');
        if (parts.Length > 0)
        {
            var lastPart = parts.Last();
            if (lastPart.Equals("Process", StringComparison.OrdinalIgnoreCase) ||
                lastPart.Equals("Protocol", StringComparison.OrdinalIgnoreCase))
            {
                logType = lastPart;
            }
        }
        _provider.AddLogMessage(message, logType);
    }
}
