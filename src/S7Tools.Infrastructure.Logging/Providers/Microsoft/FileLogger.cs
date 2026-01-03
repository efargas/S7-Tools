using System;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// A logger that writes logs to a file via a provider.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly UnifiedFileLoggerProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <param name="provider">The provider to use for logging.</param>
    public FileLogger(string categoryName, UnifiedFileLoggerProvider provider)
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

        var message = System.Text.Json.JsonSerializer.Serialize(logEntry, LogJsonContext.Default.LogEntry);
        _provider.AddLogMessage(message, _categoryName);
    }
}
