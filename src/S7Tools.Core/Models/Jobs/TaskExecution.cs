using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents the execution state and progress of a job.
/// Tracks runtime information separate from the job configuration.
/// </summary>
public class TaskExecution : INotifyPropertyChanged
{
    private TaskState _state = TaskState.Created;
    private double _progressPercentage;
    private string _currentOperation = string.Empty;
    private DateTime? _queuedAt;
    private DateTime? _startedAt;
    private DateTime? _completedAt;
    private string? _errorMessage;
    private string? _errorDetails;
    private string? _failedStep;
    private string? _outputFilePath;
    private long? _outputFileSize;
    private TimeSpan? _estimatedTimeRemaining;
    private IReadOnlyList<ResourceKey> _lockedResources = [];

    /// <summary>
    /// Event triggered when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed and/or null.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the field and raises the property changed event if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The reference to the field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property (optional).</param>
    /// <returns>True if the value changed, false otherwise.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

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
    public TaskState State
    {
        get => _state;
        set
        {
            if (SetField(ref _state, value))
            {
                OnPropertyChanged(nameof(IsTerminal));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(CanRestart));
            }
        }
    }

    /// <summary>
    /// Gets or sets the time when the task was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the time when the task was queued for execution.
    /// </summary>
    public DateTime? QueuedAt
    {
        get => _queuedAt;
        set => SetField(ref _queuedAt, value);
    }

    /// <summary>
    /// Gets or sets the time when the task execution started.
    /// </summary>
    public DateTime? StartedAt
    {
        get => _startedAt;
        set
        {
            if (SetField(ref _startedAt, value))
            {
                OnPropertyChanged(nameof(ExecutionTime));
            }
        }
    }

    /// <summary>
    /// Gets or sets the time when the task execution completed.
    /// </summary>
    public DateTime? CompletedAt
    {
        get => _completedAt;
        set
        {
            if (SetField(ref _completedAt, value))
            {
                OnPropertyChanged(nameof(ExecutionTime));
            }
        }
    }

    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => SetField(ref _progressPercentage, value);
    }

    /// <summary>
    /// Gets or sets the current operation or stage description.
    /// </summary>
    public string CurrentOperation
    {
        get => _currentOperation;
        set => SetField(ref _currentOperation, value);
    }

    /// <summary>
    /// Gets or sets the error message if the task failed.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    /// <summary>
    /// Gets or sets the full error details or stack trace.
    /// </summary>
    public string? ErrorDetails
    {
        get => _errorDetails;
        set => SetField(ref _errorDetails, value);
    }

    /// <summary>
    /// Gets or sets the step name where the task failed (e.g., "Power Cycle", "Memory Dump").
    /// </summary>
    public string? FailedStep
    {
        get => _failedStep;
        set => SetField(ref _failedStep, value);
    }

    /// <summary>
    /// Gets or sets the output file path if the task completed successfully.
    /// </summary>
    public string? OutputFilePath
    {
        get => _outputFilePath;
        set => SetField(ref _outputFilePath, value);
    }

    /// <summary>
    /// Gets or sets the size of the output file in bytes.
    /// </summary>
    public long? OutputFileSize
    {
        get => _outputFileSize;
        set => SetField(ref _outputFileSize, value);
    }

    private long? _bytesRead;
    /// <summary>
    /// Gets or sets the number of bytes read so far (for memory operations).
    /// </summary>
    public long? BytesRead
    {
        get => _bytesRead;
        set => SetField(ref _bytesRead, value);
    }

    private long? _totalBytes;
    /// <summary>
    /// Gets or sets the total number of bytes expected (for memory operations).
    /// </summary>
    public long? TotalBytes
    {
        get => _totalBytes;
        set => SetField(ref _totalBytes, value);
    }

    /// <summary>
    /// Gets or sets additional progress data as key-value pairs.
    /// </summary>
    public Dictionary<string, object> ProgressData { get; set; } = [];

    /// <summary>
    /// Gets or sets the resource keys that were locked for this task.
    /// </summary>
    public IReadOnlyList<ResourceKey> LockedResources
    {
        get => _lockedResources;
        set => SetField(ref _lockedResources, value);
    }

    /// <summary>
    /// Gets or sets the estimated time remaining for task completion.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        set => SetField(ref _estimatedTimeRemaining, value);
    }

    /// <summary>
    /// Gets or sets the priority of this task execution.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    /// <summary>
    /// Gets or sets the task-specific logger information.
    /// </summary>
    public TaskLogger? Logger { get; set; }

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
    public TimeSpan TotalTime => DateTime.Now - CreatedAt;

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
                QueuedAt = DateTime.Now;
                break;
            case TaskState.Running:
                StartedAt = DateTime.Now;
                break;
            case TaskState.Completed:
            case TaskState.Failed:
            case TaskState.Cancelled:
                CompletedAt = DateTime.Now;
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
            foreach (KeyValuePair<string, object> kvp in progressData)
            {
                ProgressData[kvp.Key] = kvp.Value;
            }

            if (progressData.TryGetValue("BytesRead", out object? bytesReadObj) && bytesReadObj is long bytesRead)
            {
                BytesRead = bytesRead;
            }

            if (progressData.TryGetValue("TotalBytes", out object? totalBytesObj) && totalBytesObj is long totalBytes)
            {
                TotalBytes = totalBytes;
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
