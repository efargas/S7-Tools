using Microsoft.Extensions.Logging;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents logging information for a specific task execution.
/// Provides separate logging channels for different aspects of task execution.
/// </summary>
public class TaskLogger
{
    /// <summary>
    /// Gets or sets the unique identifier for the task this logger belongs to.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the main task logger instance for general task messages.
    /// </summary>
    public ILogger? MainLogger { get; set; }



    /// <summary>
    /// Gets or sets the process logger for capturing socat stdout/stderr.
    /// </summary>
    public ILogger? ProcessLogger { get; set; }

    /// <summary>
    /// Gets or sets the in-memory DataStore ID for main task logs.
    /// </summary>
    public string? MainLogDataStoreId { get; set; }



    /// <summary>
    /// Gets or sets the in-memory DataStore ID for process output logs.
    /// </summary>
    public string? ProcessLogDataStoreId { get; set; }

    /// <summary>
    /// Gets or sets the file path for the main task log file.
    /// </summary>
    public string? MainLogFilePath { get; set; }



    /// <summary>
    /// Gets or sets the file path for the process output log file.
    /// </summary>
    public string? ProcessLogFilePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled for this task.
    /// </summary>
    public bool IsEnabled { get; set; } = true;



    /// <summary>
    /// Gets or sets a value indicating whether process output logging is enabled.
    /// </summary>
    public bool CaptureProcessOutput { get; set; } = true;

    /// <summary>
    /// Gets or sets the time when logging was initialized.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the time when logging was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of log entries written.
    /// </summary>
    public long TotalLogEntries { get; set; }

    /// <summary>
    /// Gets or sets the total size of log files in bytes.
    /// </summary>
    public long TotalLogFilesSize { get; set; }

    /// <summary>
    /// Gets a value indicating whether the logger has been finalized.
    /// </summary>
    public bool IsFinalized => FinalizedAt.HasValue;

    /// <summary>
    /// Creates a summary string of the task logger.
    /// </summary>
    /// <returns>A formatted summary string.</returns>
    public string GetSummary()
    {
        var parts = new List<string>();

        if (MainLogFilePath != null)
        {
            parts.Add($"Main: {Path.GetFileName(MainLogFilePath)}");
        }



        if (ProcessLogFilePath != null && CaptureProcessOutput)
        {
            parts.Add($"Process: {Path.GetFileName(ProcessLogFilePath)}");
        }

        if (TotalLogEntries > 0)
        {
            parts.Add($"{TotalLogEntries} entries");
        }

        if (TotalLogFilesSize > 0)
        {
            parts.Add($"{TotalLogFilesSize / 1024.0:F1} KB");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "No logs";
    }
}
