using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Represents a log entry with all necessary information for display and processing.
/// </summary>
public sealed record LogModel
{
    /// <summary>
    /// Gets the unique identifier for this log entry.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// Gets the log level (Information, Warning, Error, etc.).
    /// </summary>
    public LogLevel Level { get; init; }

    /// <summary>
    /// Gets the category name (typically the logger name).
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets the log message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the exception information, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the event ID associated with this log entry.
    /// </summary>
    public EventId EventId { get; init; }

    /// <summary>
    /// Gets the scope information for this log entry.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Gets additional properties associated with this log entry.
    /// </summary>
    public Dictionary<string, object?> Properties { get; init; } = new();

    /// <summary>
    /// Gets the formatted message including exception details if present.
    /// </summary>
    public string FormattedMessage => Exception != null
        ? $"{Message}\n{Exception}"
        : Message;
}
