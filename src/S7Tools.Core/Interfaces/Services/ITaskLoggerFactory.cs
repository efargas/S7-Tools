using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Factory for creating and managing task-specific loggers.
/// </summary>
public interface ITaskLoggerFactory
{
    /// <summary>
    /// Creates a task-specific logger with dedicated DataStores and file outputs.
    /// </summary>
    /// <param name="taskId">The unique identifier for the task.</param>
    /// <param name="taskName">The name of the task.</param>
    /// <param name="captureProtocol">Whether to capture protocol communication logs.</param>
    /// <param name="captureProcessOutput">Whether to capture process stdout/stderr.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A TaskLogger instance with all configured loggers.</returns>
    Task<TaskLogger> CreateTaskLoggerAsync(
        Guid taskId,
        string taskName,
        bool captureProcessOutput = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes a task logger, flushing all logs and cleaning up resources.
    /// </summary>
    /// <param name="taskId">The unique identifier for the task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task FinalizeTaskLoggerAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the task logger for a specific task.
    /// </summary>
    /// <param name="taskId">The unique identifier for the task.</param>
    /// <returns>The TaskLogger instance, or null if not found.</returns>
    TaskLogger? GetTaskLogger(Guid taskId);

    /// <summary>
    /// Gets the in-memory log entries for a specific task and log type.
    /// Returns as object to avoid Infrastructure dependency in Core.
    /// </summary>
    /// <param name="taskId">The unique identifier for the task.</param>
    /// <param name="logType">The type of log (Main, Protocol, Process).</param>
    /// <returns>The log store object, or null if not found. Cast to ILogDataStore in application layer.</returns>
    object? GetTaskDataStore(Guid taskId, TaskLogType logType);
}

/// <summary>
/// Type of task log.
/// </summary>
public enum TaskLogType
{
    /// <summary>
    /// Main task log (general operations).
    /// </summary>
    Main,

    /// <summary>
    /// Protocol log (TCP/socat communication).
    /// </summary>
    Protocol,

    /// <summary>
    /// Process log (socat stdout/stderr).
    /// </summary>
    Process
}
