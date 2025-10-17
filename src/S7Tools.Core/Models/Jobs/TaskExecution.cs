using System;
using System.Collections.Generic;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents the execution state and progress of a job.
/// Tracks runtime information separate from the job configuration.
/// </summary>
public class TaskExecution
{
    /// <summary>
    /// Gets or sets the unique identifier for this task execution.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the job profile being executed.
    /// </summary>
    public int JobProfileId { get; set; }

    /// <summary>
    /// Gets or sets the name of the job being executed.
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current state of the task execution.
    /// </summary>
    public TaskState State { get; set; } = TaskState.Created;

    /// <summary>
    /// Gets or sets the time when the task was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the time when the task was queued for execution.
    /// </summary>
    public DateTime? QueuedAt { get; set; }

    /// <summary>
    /// Gets or sets the time when the task execution started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the time when the task execution completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the current operation or stage description.
    /// </summary>
    public string CurrentOperation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if the task failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the full error details or stack trace.
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Gets or sets the output file path if the task completed successfully.
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    /// Gets or sets the size of the output file in bytes.
    /// </summary>
    public long? OutputFileSize { get; set; }

    /// <summary>
    /// Gets or sets additional progress data as key-value pairs.
    /// </summary>
    public Dictionary<string, object> ProgressData { get; set; } = new();

    /// <summary>
    /// Gets or sets the resource keys that were locked for this task.
    /// </summary>
    public IReadOnlyList<ResourceKey> LockedResources { get; set; } = new List<ResourceKey>();

    /// <summary>
    /// Gets or sets the estimated time remaining for task completion.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Gets or sets the priority of this task execution.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    /// <summary>
    /// Gets the total execution time if the task has completed.
    /// </summary>
    public TimeSpan? ExecutionTime
    {
        get
        {
            if (StartedAt.HasValue && CompletedAt.HasValue)
            {
                return CompletedAt.Value - StartedAt.Value;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets the total time since the task was created.
    /// </summary>
    public TimeSpan TotalTime => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Gets a value indicating whether the task is in a terminal state.
    /// </summary>
    public bool IsTerminal => State is TaskState.Completed or TaskState.Failed or TaskState.Cancelled;

    /// <summary>
    /// Gets a value indicating whether the task is currently running.
    /// </summary>
    public bool IsRunning => State == TaskState.Running;

    /// <summary>
    /// Gets a value indicating whether the task can be cancelled.
    /// </summary>
    public bool CanCancel => State is TaskState.Created or TaskState.Queued or TaskState.Running or TaskState.Paused;

    /// <summary>
    /// Gets a value indicating whether the task can be restarted.
    /// </summary>
    public bool CanRestart => State is TaskState.Failed or TaskState.Cancelled;

    /// <summary>
    /// Updates the task state and sets appropriate timestamps.
    /// </summary>
    /// <param name="newState">The new state to set.</param>
    /// <param name="message">Optional message describing the state change.</param>
    public void UpdateState(TaskState newState, string? message = null)
    {
        State = newState;

        switch (newState)
        {
            case TaskState.Queued:
                QueuedAt = DateTime.UtcNow;
                break;
            case TaskState.Running:
                StartedAt = DateTime.UtcNow;
                break;
            case TaskState.Completed:
            case TaskState.Failed:
            case TaskState.Cancelled:
                CompletedAt = DateTime.UtcNow;
                break;
        }

        if (!string.IsNullOrEmpty(message))
        {
            CurrentOperation = message;
        }
    }

    /// <summary>
    /// Updates the progress of the task execution.
    /// </summary>
    /// <param name="percentage">The progress percentage (0-100).</param>
    /// <param name="operation">Description of the current operation.</param>
    /// <param name="progressData">Additional progress data.</param>
    public void UpdateProgress(double percentage, string operation, Dictionary<string, object>? progressData = null)
    {
        ProgressPercentage = Math.Clamp(percentage, 0.0, 100.0);
        CurrentOperation = operation;

        if (progressData != null)
        {
            foreach (var kvp in progressData)
            {
                ProgressData[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Marks the task as failed with error information.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errorDetails">Detailed error information.</param>
    public void MarkAsFailed(string errorMessage, string? errorDetails = null)
    {
        UpdateState(TaskState.Failed, errorMessage);
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    /// <summary>
    /// Marks the task as completed successfully.
    /// </summary>
    /// <param name="outputFilePath">Path to the output file.</param>
    /// <param name="outputFileSize">Size of the output file in bytes.</param>
    public void MarkAsCompleted(string outputFilePath, long? outputFileSize = null)
    {
        UpdateState(TaskState.Completed, "Task completed successfully");
        OutputFilePath = outputFilePath;
        OutputFileSize = outputFileSize;
        ProgressPercentage = 100.0;
    }

    /// <summary>
    /// Creates a summary string of the task execution.
    /// </summary>
    /// <returns>A formatted summary string.</returns>
    public string GetSummary()
    {
        string summary = $"{JobName} ({TaskId:D})";

        if (State == TaskState.Running && ProgressPercentage > 0)
        {
            summary += $" - {ProgressPercentage:F1}%";
        }

        if (!string.IsNullOrEmpty(CurrentOperation))
        {
            summary += $" - {CurrentOperation}";
        }

        return summary;
    }
}

/// <summary>
/// Represents the different states a task can be in during its lifecycle.
/// </summary>
public enum TaskState
{
    /// <summary>
    /// Task has been created but not yet queued.
    /// </summary>
    Created,

    /// <summary>
    /// Task is queued and waiting for execution.
    /// </summary>
    Queued,

    /// <summary>
    /// Task has been scheduled to run at a specific time.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Task is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Task has been paused and can be resumed.
    /// </summary>
    Paused,

    /// <summary>
    /// Task completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Task failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Task was cancelled before completion.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents the priority levels for task execution.
/// </summary>
public enum TaskPriority
{
    /// <summary>
    /// Low priority - executed after normal and high priority tasks.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - default priority level.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - executed before normal and low priority tasks.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - executed immediately.
    /// </summary>
    Critical = 3
}
