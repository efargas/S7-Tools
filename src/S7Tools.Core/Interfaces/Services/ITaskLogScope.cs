namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Provides a facility to enrich log events with task-specific metadata across execution contexts.
/// </summary>
public interface ITaskLogScope
{
    /// <summary>
    /// Begins a logical operation scope for a specific task.
    /// All logs recorded within this scope will be enriched with the task details.
    /// </summary>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="taskName">The human-readable task name.</param>
    /// <param name="logScope">The sub-scope within the task (e.g., "Main", "Process").</param>
    /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
    IDisposable BeginScope(Guid taskId, string taskName, string logScope = "Main");
}
