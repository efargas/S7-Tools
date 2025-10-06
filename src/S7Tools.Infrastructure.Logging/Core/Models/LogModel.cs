using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Represents a log entry with all necessary information for display and processing.
/// </summary>
public sealed class LogModel
{
    /// <summary>
    /// Gets or sets the unique identifier for this log entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the log level (Information, Warning, Error, etc.).
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the category name (typically the logger name).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception information, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the event ID associated with this log entry.
    /// </summary>
    public EventId EventId { get; set; }

    /// <summary>
    /// Gets or sets the scope information for this log entry.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets additional properties associated with this log entry.
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// Gets the formatted message including exception details if present.
    /// </summary>
    public string FormattedMessage => Exception != null 
        ? $"{Message}\n{Exception}" 
        : Message;

    /// <summary>
    /// Creates a copy of this log model.
    /// </summary>
    /// <returns>A new LogModel instance with the same values.</returns>
    public LogModel Clone()
    {
        return new LogModel
        {
            Id = Id,
            Timestamp = Timestamp,
            Level = Level,
            Category = Category,
            Message = Message,
            Exception = Exception,
            EventId = EventId,
            Scope = Scope,
            Properties = new Dictionary<string, object?>(Properties)
        };
    }
}