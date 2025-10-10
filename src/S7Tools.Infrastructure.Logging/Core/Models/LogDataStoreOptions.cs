using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Configuration options for the log data store.
/// </summary>
public sealed class LogDataStoreOptions
{
    /// <summary>
    /// Gets or sets the maximum number of log entries to store in memory.
    /// Default is 10,000 entries.
    /// </summary>
    public int MaxEntries { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets the minimum log level to store.
    /// Default is LogLevel.Information.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether to include scopes in log entries.
    /// Default is true.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture additional properties from log entries.
    /// Default is true.
    /// </summary>
    public bool CaptureProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets the color configuration for different log levels.
    /// </summary>
    public Dictionary<LogLevel, LogEntryColor> Colors { get; set; } = LogEntryColor.VSCodeDarkColors;

    /// <summary>
    /// Gets or sets whether to automatically scroll to new log entries.
    /// Default is true.
    /// </summary>
    public bool AutoScroll { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for UI updates to improve performance.
    /// Default is 100 entries.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the interval for batched UI updates in milliseconds.
    /// Default is 100ms.
    /// </summary>
    public int BatchIntervalMs { get; set; } = 100;
}
