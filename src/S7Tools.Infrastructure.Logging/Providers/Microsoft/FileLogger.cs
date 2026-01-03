using System;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// A logger that writes logs to a file via a provider.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <param name="provider">The provider to use for logging.</param>
    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _provider._config.Value.LogLevel;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var logEntry = new
        {
            Timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
            LogLevel = logLevel.ToString(),
            Category = _categoryName,
            Message = formatter(state, exception),
            Exception = exception?.ToString()
        };

        var message = System.Text.Json.JsonSerializer.Serialize(logEntry);
        _provider.AddLogMessage(message);
    }
}
