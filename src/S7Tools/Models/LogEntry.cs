namespace S7Tools.Models;

/// <summary>
/// Represents a single log entry with timestamp, level, and message.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp (Local Time) when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the log level (Trace, Debug, Information, Warning, Error, Critical).
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Gets or sets the log category or source.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the formatted message for display.
    /// </summary>
    public string FormattedMessage => Message;
}
