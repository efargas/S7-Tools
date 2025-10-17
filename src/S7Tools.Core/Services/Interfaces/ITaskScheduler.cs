using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Validation;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Delegate for task state change notifications.
/// </summary>
/// <param name="taskExecution">The task execution that changed state.</param>
public delegate void TaskStateChanged(TaskExecution taskExecution);

/// <summary>
/// Delegate for task progress update notifications.
/// </summary>
/// <param name="taskId">The unique identifier of the task.</param>
/// <param name="percentage">The progress percentage (0-100).</param>
/// <param name="operation">Description of the current operation.</param>
public delegate void TaskProgressUpdated(Guid taskId, double percentage, string operation);

/// <summary>
/// Defines the contract for enhanced task scheduling and execution management.
/// Manages task queuing, resource coordination, parallel execution, and progress tracking.
/// </summary>
public interface ITaskScheduler
{
    /// <summary>
    /// Event raised when a task's state changes.
    /// </summary>
    event TaskStateChanged? TaskStateChanged;

    /// <summary>
    /// Event raised when a task's progress is updated.
    /// </summary>
    event TaskProgressUpdated? TaskProgressUpdated;

    #region Task Lifecycle Management

    /// <summary>
    /// Creates a new task from a job profile and adds it to the scheduler.
    /// </summary>
    /// <param name="jobProfile">The job profile to create a task from.</param>
    /// <param name="priority">The priority level for the task.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created task execution.</returns>
    Task<TaskExecution> CreateTaskAsync(JobProfile jobProfile, TaskPriority priority = TaskPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a task for immediate execution.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task was successfully enqueued, false otherwise.</returns>
    Task<bool> EnqueueTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a task for execution at a specific time.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task to schedule.</param>
    /// <param name="scheduledTime">The time when the task should be executed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task was successfully scheduled, false otherwise.</returns>
    Task<bool> ScheduleTaskAsync(Guid taskId, DateTime scheduledTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a task that is created, queued, scheduled, or running.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task to cancel.</param>
    /// <param name="reason">Optional reason for cancellation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task was successfully cancelled, false otherwise.</returns>
    Task<bool> CancelTaskAsync(Guid taskId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a running task.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task to pause.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task was successfully paused, false otherwise.</returns>
    Task<bool> PauseTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused task.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task to resume.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task was successfully resumed, false otherwise.</returns>
    Task<bool> ResumeTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts a failed or cancelled task.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task to restart.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The new task execution created for the restart.</returns>
    Task<TaskExecution?> RestartTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    #endregion

    #region Task Query Operations

    /// <summary>
    /// Gets all tasks in the scheduler.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A read-only collection of all task executions.</returns>
    Task<IReadOnlyCollection<TaskExecution>> GetAllTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific task by its ID.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The task execution if found, null otherwise.</returns>
    Task<TaskExecution?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks by their current state.
    /// </summary>
    /// <param name="state">The task state to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Tasks in the specified state.</returns>
    Task<IReadOnlyCollection<TaskExecution>> GetTasksByStateAsync(TaskState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks by their priority level.
    /// </summary>
    /// <param name="priority">The priority level to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Tasks with the specified priority level.</returns>
    Task<IReadOnlyCollection<TaskExecution>> GetTasksByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks created from a specific job profile.
    /// </summary>
    /// <param name="jobProfileId">The ID of the job profile.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Tasks created from the specified job profile.</returns>
    Task<IReadOnlyCollection<TaskExecution>> GetTasksByJobProfileAsync(int jobProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the queue of tasks waiting for execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Tasks in the queue, ordered by priority and creation time.</returns>
    Task<IReadOnlyCollection<TaskExecution>> GetQueuedTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets currently running tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Tasks that are currently running.</returns>
    Task<IReadOnlyCollection<TaskExecution>> GetRunningTasksAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Resource and Scheduling Information

    /// <summary>
    /// Checks if a task can be executed (resources available, valid configuration).
    /// </summary>
    /// <param name="taskId">The unique identifier of the task to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the task can be executed, false otherwise.</returns>
    Task<bool> CanExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the estimated start time for a queued or scheduled task.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The estimated start time, or null if it cannot be determined.</returns>
    Task<DateTime?> GetEstimatedStartTimeAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about resource usage and availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Resource usage statistics.</returns>
    Task<ResourceUsageInfo> GetResourceUsageAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Scheduler Control

    /// <summary>
    /// Starts the scheduler to begin processing tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the scheduler, completing running tasks and stopping new task execution.
    /// </summary>
    /// <param name="graceful">Whether to wait for running tasks to complete before stopping.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the stop operation.</returns>
    Task StopAsync(bool graceful = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the scheduler is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the maximum number of tasks that can run concurrently.
    /// </summary>
    int MaxConcurrentTasks { get; }

    /// <summary>
    /// Sets the maximum number of tasks that can run concurrently.
    /// </summary>
    /// <param name="maxConcurrentTasks">The maximum number of concurrent tasks.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the configuration change.</returns>
    Task SetMaxConcurrentTasksAsync(int maxConcurrentTasks, CancellationToken cancellationToken = default);

    #endregion

    #region Cleanup and Maintenance

    /// <summary>
    /// Removes completed or failed tasks older than the specified age.
    /// </summary>
    /// <param name="maxAge">The maximum age of tasks to keep.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of tasks that were removed.</returns>
    Task<int> CleanupOldTasksAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduler statistics and performance metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Scheduler statistics.</returns>
    Task<SchedulerStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Provides information about resource usage in the scheduler.
/// </summary>
public class ResourceUsageInfo
{
    /// <summary>
    /// Gets or sets the total number of available resources.
    /// </summary>
    public int TotalResources { get; set; }

    /// <summary>
    /// Gets or sets the number of currently locked resources.
    /// </summary>
    public int LockedResources { get; set; }

    /// <summary>
    /// Gets or sets information about individual resource locks.
    /// </summary>
    public IReadOnlyDictionary<ResourceKey, Guid> ResourceLocks { get; set; } = new Dictionary<ResourceKey, Guid>();

    /// <summary>
    /// Gets the percentage of resources currently in use.
    /// </summary>
    public double UsagePercentage => TotalResources > 0 ? (double)LockedResources / TotalResources * 100.0 : 0.0;
}

/// <summary>
/// Provides statistics about scheduler performance and usage.
/// </summary>
public class SchedulerStatistics
{
    /// <summary>
    /// Gets or sets the total number of tasks processed.
    /// </summary>
    public long TotalTasksProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of tasks that completed successfully.
    /// </summary>
    public long SuccessfulTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of tasks that failed.
    /// </summary>
    public long FailedTasks { get; set; }

    /// <summary>
    /// Gets or sets the number of tasks that were cancelled.
    /// </summary>
    public long CancelledTasks { get; set; }

    /// <summary>
    /// Gets or sets the average execution time for completed tasks.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the current number of tasks in each state.
    /// </summary>
    public IReadOnlyDictionary<TaskState, int> TasksByState { get; set; } = new Dictionary<TaskState, int>();

    /// <summary>
    /// Gets or sets the scheduler uptime.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalTasksProcessed > 0 ? (double)SuccessfulTasks / TotalTasksProcessed * 100.0 : 0.0;
}
