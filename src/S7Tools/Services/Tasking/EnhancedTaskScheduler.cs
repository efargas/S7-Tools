using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Extensions;

namespace S7Tools.Services.Tasking;

/// <summary>
/// Enhanced task scheduler that manages task execution with comprehensive state tracking,
/// resource coordination, and parallel execution capabilities.
/// </summary>
public class EnhancedTaskScheduler : ITaskScheduler, IDisposable
{
    #region Private Fields

    private readonly ILogger<EnhancedTaskScheduler> _logger;
    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly IBootloaderService _bootloaderService;
    private readonly IJobManager _jobManager;
    private readonly IPathService _pathService;
    private readonly ITaskLoggerFactory _taskLoggerFactory;
    private readonly ITimeProvider _timeProvider;
    private readonly string _tasksFilePath;
    private readonly string _historyFilePath;

    private readonly ConcurrentDictionary<Guid, TaskExecution> _tasks = new();
    private readonly ConcurrentQueue<Guid> _taskQueue = new();
    private const int MaxQueueSize = 1000;
    private readonly ConcurrentDictionary<Guid, DateTime> _scheduledTasks = new();
    private readonly ConcurrentDictionary<Guid, Task> _activeExecutions = new(); // Track active execution tasks
    private readonly SemaphoreSlim _schedulerSemaphore = new(1, 1);
    private readonly SemaphoreSlim _persistenceSemaphore = new(1, 1);
    private readonly Timer _scheduleTimer;
    private bool _isRunning;
    private bool _disposed;
    private int _maxConcurrentTasks = Environment.ProcessorCount;
    private readonly DateTime _startTime;

    // Statistics
    private long _totalTasksProcessed;
    private long _successfulTasks;
    private long _failedTasks;
    private long _cancelledTasks;
    private readonly Queue<TimeSpan> _executionTimes = new(); // Use Queue for O(1) operations
    private const int MaxExecutionTimesCount = 1000;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    #endregion

    #region Events

    /// <inheritdoc/>
    public event TaskStateChanged? TaskStateChanged;

    /// <inheritdoc/>
    public event TaskProgressUpdated? TaskProgressUpdated;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the EnhancedTaskScheduler class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="resourceCoordinator">Resource coordinator for managing exclusive resource access.</param>
    /// <param name="bootloaderService">Bootloader service for job execution.</param>
    /// <param name="jobManager">Job manager for accessing job configurations.</param>
    /// <param name="pathService">Path service for resolving application paths.</param>
    /// <param name="taskLoggerFactory">Task logger factory for creating task-specific loggers.</param>
    /// <param name="timeProvider">Time provider for consistent task scheduling.</param>
    public EnhancedTaskScheduler(
        ILogger<EnhancedTaskScheduler> logger,
        IResourceCoordinator resourceCoordinator,
        IBootloaderService bootloaderService,
        IJobManager jobManager,
        IPathService pathService,
        ITaskLoggerFactory taskLoggerFactory,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceCoordinator = resourceCoordinator ?? throw new ArgumentNullException(nameof(resourceCoordinator));
        _bootloaderService = bootloaderService ?? throw new ArgumentNullException(nameof(bootloaderService));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _taskLoggerFactory = taskLoggerFactory ?? throw new ArgumentNullException(nameof(taskLoggerFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _startTime = _timeProvider.GetUtcNow();

        // Set up tasks file path using PathService
        _tasksFilePath = _pathService.TasksPath;
        _historyFilePath = Path.Combine(Path.GetDirectoryName(_tasksFilePath) ?? string.Empty, "Tasks_History.json");

        string? tasksDirectory = Path.GetDirectoryName(_tasksFilePath);
        if (!string.IsNullOrEmpty(tasksDirectory))
        {
            Directory.CreateDirectory(tasksDirectory);
        }

        // Initialize schedule timer (disabled initially)
        _scheduleTimer = new Timer(ProcessTasks, null, Timeout.Infinite, Timeout.Infinite);

        // Load existing tasks from persistence
        _ = LoadTasksAsync();

        _logger.LogInformation("EnhancedTaskScheduler initialized with max concurrent tasks: {MaxConcurrentTasks}, persistence path: {Path}",
            _maxConcurrentTasks, _tasksFilePath);
    }

    #endregion

    #region Properties

    /// <inheritdoc/>
    public bool IsRunning => _isRunning;

    /// <inheritdoc/>
    public int MaxConcurrentTasks => _maxConcurrentTasks;

    #endregion

    #region Task Lifecycle Management

    /// <inheritdoc/>
    public Task<TaskExecution> CreateTaskAsync(JobProfile jobProfile, TaskPriority priority = TaskPriority.Normal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobProfile);

        _logger.LogInformation("Creating task from job profile '{JobName}' (ID: {JobId}) with priority {Priority}",
            jobProfile.Name, jobProfile.Id, priority);

        Job executionJob = jobProfile.ToExecutionJob();
        var taskExecution = new TaskExecution
        {
            TaskId = Guid.NewGuid(),
            JobProfileId = jobProfile.Id,
            JobName = jobProfile.Name,
            State = TaskState.Created,
            Priority = priority,
            LockedResources = executionJob.Resources,
            CreatedAt = _timeProvider.GetLocalNow()
        };
        taskExecution.Initialize(_timeProvider);

        _tasks[taskExecution.TaskId] = taskExecution;

        TaskStateChanged?.Invoke(taskExecution);
        _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist task creation

        _logger.LogInformation("Created task {TaskId} for job '{JobName}' with {ResourceCount} resources",
            taskExecution.TaskId, jobProfile.Name, executionJob.Resources.Count);

        return Task.FromResult(taskExecution);
    }

    /// <inheritdoc/>
    public Task<bool> EnqueueTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            _logger.LogWarning("Task {TaskId} not found for enqueue", taskId);
            return Task.FromResult(false);
        }

        if (task.State != TaskState.Created)
        {
            _logger.LogWarning("Task {TaskId} is in state {State}, cannot enqueue", taskId, task.State);
            return Task.FromResult(false);
        }

        if (!TryEnqueueInternal(taskId, task))
        {
            return Task.FromResult(false);
        }

        TaskStateChanged?.Invoke(task);

        _logger.LogInformation("Enqueued task {TaskId} ({JobName}) - Queue size: {QueueSize}/{MaxSize}",
            taskId, task.JobName, _taskQueue.Count, MaxQueueSize);

        _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist task enqueue

        TriggerScheduler(); // Critical fix: Wake up scheduler to process the new task

        return Task.FromResult(true);
    }

    /// <summary>
    /// Attempts to enqueue a task while respecting the maximum queue size.
    /// Updates task state to Failed if the queue is full.
    /// </summary>
    private bool TryEnqueueInternal(Guid taskId, TaskExecution task, string? stateMessage = null)
    {
        if (_taskQueue.Count >= MaxQueueSize)
        {
            _logger.LogError("Task queue is full ({Count}/{Max}). Cannot enqueue task {TaskId}",
                _taskQueue.Count, MaxQueueSize, taskId);
            task.UpdateState(TaskState.Failed, $"Task queue is full ({MaxQueueSize})");
            TaskStateChanged?.Invoke(task);
            return false;
        }

        task.UpdateState(TaskState.Queued, stateMessage ?? "Task queued for execution");
        _taskQueue.Enqueue(taskId);
        return true;
    }

    /// <inheritdoc/>
    public Task<bool> ScheduleTaskAsync(Guid taskId, DateTime scheduledTime, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            _logger.LogWarning("Task {TaskId} not found for scheduling", taskId);
            return Task.FromResult(false);
        }

        if (task.State is not (TaskState.Created or TaskState.Queued))
        {
            _logger.LogWarning("Task {TaskId} is in state {State}, cannot schedule", taskId, task.State);
            return Task.FromResult(false);
        }

        // Normalize to UTC for internal storage and comparison
        DateTime utcTime = scheduledTime.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(scheduledTime, DateTimeKind.Local).ToUniversalTime(),
            DateTimeKind.Local => scheduledTime.ToUniversalTime(),
            _ => scheduledTime
        };
        DateTime localTime = utcTime.ToLocalTime();

        task.UpdateState(TaskState.Scheduled, $"Scheduled for {localTime}");
        task.ProgressData["ScheduledTime"] = utcTime;
        _scheduledTasks[taskId] = utcTime;

        TaskStateChanged?.Invoke(task);

        _logger.LogInformation("Scheduled task {TaskId} ({JobName}) for {ScheduledTime}",
            taskId, task.JobName, localTime);

        // If time already passed or is now, promote to queue immediately
        if (utcTime <= _timeProvider.GetUtcNow())
        {
            _scheduledTasks.TryRemove(taskId, out _);
            if (!TryEnqueueInternal(taskId, task, "Promoted to queue from schedule"))
            {
                _logger.LogWarning("Failed to promote scheduled task {TaskId} to queue because it is full", taskId);
            }
            else
            {
                TaskStateChanged?.Invoke(task);
            }
        }

        _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist task schedule or queue promotion
        TriggerScheduler();
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> CancelTaskAsync(Guid taskId, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            _logger.LogWarning("Task {TaskId} not found for cancellation", taskId);
            return Task.FromResult(false);
        }

        if (!task.CanCancel)
        {
            _logger.LogWarning("Task {TaskId} is in state {State}, cannot cancel", taskId, task.State);
            return Task.FromResult(false);
        }

        task.UpdateState(TaskState.Cancelled, reason ?? "Task cancelled by user");

        // Remove from schedule if present
        if (_scheduledTasks.TryRemove(taskId, out _))
        {
            _logger.LogDebug("Removed task {TaskId} from scheduled tasks on cancel", taskId);
        }

        // Release resources if they were locked
        if (task.LockedResources.Count > 0)
        {
            _resourceCoordinator.Release(task.LockedResources);
            _logger.LogDebug("Released {Count} resources for cancelled task {TaskId}",
                task.LockedResources.Count, taskId);
        }

        TaskStateChanged?.Invoke(task);
        Interlocked.Increment(ref _cancelledTasks);

        _logger.LogInformation("Cancelled task {TaskId} ({JobName}). Reason: {Reason}",
            taskId, task.JobName, reason ?? "No reason provided");

        _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist cancelled state
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> PauseTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            _logger.LogWarning("Task {TaskId} not found for pausing", taskId);
            return Task.FromResult(false);
        }

        if (task.State != TaskState.Running)
        {
            _logger.LogWarning("Task {TaskId} is not running, cannot pause", taskId);
            return Task.FromResult(false);
        }

        task.UpdateState(TaskState.Paused, "Task paused by user");
        TaskStateChanged?.Invoke(task);

        _logger.LogInformation("Paused task {TaskId} ({JobName})", taskId, task.JobName);
        _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist paused state
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> ResumeTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            _logger.LogWarning("Task {TaskId} not found for resuming", taskId);
            return Task.FromResult(false);
        }

        if (task.State != TaskState.Paused)
        {
            _logger.LogWarning("Task {TaskId} is not paused, cannot resume", taskId);
            return Task.FromResult(false);
        }

        task.UpdateState(TaskState.Running, "Task resumed");
        TaskStateChanged?.Invoke(task);

        _logger.LogInformation("Resumed task {TaskId} ({JobName})", taskId, task.JobName);
        _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist resumed state
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public async Task<TaskExecution?> RestartTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? originalTask))
        {
            _logger.LogWarning("Task {TaskId} not found for restart", taskId);
            return null;
        }

        if (!originalTask.CanRestart)
        {
            _logger.LogWarning("Task {TaskId} cannot be restarted from state {State}", taskId, originalTask.State);
            return null;
        }

        // Get the original job profile
        JobProfile? jobProfile = await _jobManager.GetByIdAsync(originalTask.JobProfileId, cancellationToken).ConfigureAwait(false);
        if (jobProfile == null)
        {
            _logger.LogError("Job profile {JobProfileId} not found for task restart", originalTask.JobProfileId);
            return null;
        }

        // Create a new task
        TaskExecution newTask = await CreateTaskAsync(jobProfile, originalTask.Priority, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Restarted task {OriginalTaskId} as new task {NewTaskId} for job '{JobName}'",
            taskId, newTask.TaskId, jobProfile.Name);

        return newTask;
    }

    #endregion

    #region Task Query Operations

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<TaskExecution>> GetAllTasksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<TaskExecution>>([.. _tasks.Values]);
    }

    /// <inheritdoc/>
    public Task<TaskExecution?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(taskId, out TaskExecution? task);
        return Task.FromResult(task);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<TaskExecution>> GetTasksByStateAsync(TaskState state, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<TaskExecution>>([.. _tasks.Values.Where(t => t.State == state)]);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<TaskExecution>> GetTasksByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<TaskExecution>>([.. _tasks.Values.Where(t => t.Priority == priority)]);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<TaskExecution>> GetTasksByJobProfileAsync(int jobProfileId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<TaskExecution>>([.. _tasks.Values.Where(t => t.JobProfileId == jobProfileId)]);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TaskExecution>> GetQueuedTasksAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return
        [
            .. _tasks.Values
                .Where(t => t.State == TaskState.Queued)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
        ];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TaskExecution>> GetRunningTasksAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return [.. _tasks.Values.Where(t => t.State == TaskState.Running)];
    }

    #endregion

    #region Resource and Scheduling Information

    /// <inheritdoc/>
    public async Task<bool> CanExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            return false;
        }

        // Check if job profile can be executed
        bool canExecuteJob = await _jobManager.CanExecuteJobAsync(task.JobProfileId, cancellationToken).ConfigureAwait(false);
        if (!canExecuteJob)
        {
            return false;
        }

        // Check resource availability
        return _resourceCoordinator.TryAcquire(task.LockedResources);
    }

    /// <summary>
    /// Gets detailed reasons why a task cannot execute currently.
    /// </summary>
    /// <param name="taskId">The ID of the task to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of blocking reasons, or empty list if task can execute.</returns>
    public async Task<List<string>> GetTaskBlockingReasonsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var reasons = new List<string>();

        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            reasons.Add("Task not found");
            return reasons;
        }

        if (task.State != TaskState.Queued)
        {
            reasons.Add($"Task is in '{task.State}' state (must be Queued to execute)");
            return reasons;
        }

        // Check max concurrent tasks limit
        int runningCount = _tasks.Values.Count(t => t.State == TaskState.Running);
        if (runningCount >= _maxConcurrentTasks)
        {
            reasons.Add($"Max concurrent tasks limit reached ({runningCount}/{_maxConcurrentTasks})");
        }

        // Check job profile validity
        JobProfile? jobProfile = await _jobManager.GetByIdAsync(task.JobProfileId, cancellationToken).ConfigureAwait(false);
        if (jobProfile == null)
        {
            reasons.Add($"Job profile {task.JobProfileId} not found");
            return reasons;
        }

        bool canExecuteJob = await _jobManager.CanExecuteJobAsync(task.JobProfileId, cancellationToken).ConfigureAwait(false);
        if (!canExecuteJob)
        {
            reasons.Add($"Job profile '{jobProfile.Name}' validation failed");
        }

        // Check resource locks - detailed per resource type
        Job executionJob = jobProfile.ToExecutionJob();
        foreach (ResourceKey resource in executionJob.Resources)
        {
            if (!_resourceCoordinator.AreAvailable([resource]))
            {
                // Resource is locked - find which task is using it
                TaskExecution? lockingTask = _tasks.Values.FirstOrDefault(t =>
                    t.State == TaskState.Running &&
                    t.LockedResources.Any(r => r.Kind == resource.Kind && r.Id == resource.Id));

                if (lockingTask != null)
                {
                    switch (resource.Kind.ToLowerInvariant())
                    {
                        case "serial":
                            reasons.Add($"Serial port '{resource.Id}' is in use by task '{lockingTask.JobName}' (started {lockingTask.StartedAt?.ToLocalTime():HH:mm:ss})");
                            break;
                        case "tcp":
                            reasons.Add($"TCP port {resource.Id} is in use by task '{lockingTask.JobName}' (socat server running)");
                            break;
                        case "modbus":
                            reasons.Add($"Modbus connection '{resource.Id}' is in use by task '{lockingTask.JobName}' (power supply control active)");
                            break;
                        default:
                            reasons.Add($"Resource '{resource.Kind}:{resource.Id}' is in use by task '{lockingTask.JobName}'");
                            break;
                    }
                }
                else
                {
                    // Resource locked but no running task found - might be system lock
                    switch (resource.Kind.ToLowerInvariant())
                    {
                        case "serial":
                            reasons.Add($"Serial port '{resource.Id}' is not available (may be in use by another application)");
                            break;
                        case "tcp":
                            reasons.Add($"TCP port {resource.Id} is not available (may be in use or socat not configured)");
                            break;
                        case "modbus":
                            reasons.Add($"Modbus connection '{resource.Id}' is not available (power supply not reachable)");
                            break;
                        default:
                            reasons.Add($"Resource '{resource.Kind}:{resource.Id}' is not available");
                            break;
                    }
                }
            }
            else
            {
                // Release immediately - we were just testing
                _resourceCoordinator.Release([resource]);
            }
        }

        return reasons;
    }

    /// <inheritdoc/>
    public async Task<DateTime?> GetEstimatedStartTimeAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            return null;
        }

        if (task.State == TaskState.Running)
        {
            return task.StartedAt;
        }

        if (task.State == TaskState.Scheduled && task.ProgressData.TryGetValue("ScheduledTime", out object? scheduledObj))
        {
            return scheduledObj as DateTime?;
        }

        // For queued tasks, estimate based on running tasks and queue position
        if (task.State == TaskState.Queued)
        {
            IReadOnlyCollection<TaskExecution> runningTasks = await GetRunningTasksAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyCollection<TaskExecution> queuedTasks = await GetQueuedTasksAsync(cancellationToken).ConfigureAwait(false);

            int queuePosition = queuedTasks.TakeWhile(t => t.TaskId != taskId).Count();
            var estimatedWaitTime = TimeSpan.FromMinutes(queuePosition * 5); // Rough estimate

            return DateTime.UtcNow.Add(estimatedWaitTime);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<ResourceUsageInfo> GetResourceUsageAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TaskExecution> runningTasks = await GetRunningTasksAsync(cancellationToken).ConfigureAwait(false);
        var allResources = runningTasks.SelectMany(t => t.LockedResources).ToList();
        var resourceLocks = allResources.ToDictionary(r => r, r =>
            runningTasks.FirstOrDefault(t => t.LockedResources.Contains(r))?.TaskId ?? Guid.Empty);

        return new ResourceUsageInfo
        {
            TotalResources = 100, // This would need to be calculated based on available resources
            LockedResources = allResources.Count,
            ResourceLocks = resourceLocks
        };
    }

    #endregion

    #region Scheduler Control

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Scheduler is already running");
            return;
        }

        _isRunning = true;
        _logger.LogInformation("Task scheduler started");
        await Task.Yield();
    }

    /// <inheritdoc/>
    public async Task StopAsync(bool graceful = true, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Scheduler is not running");
            return;
        }

        _isRunning = false;

        if (graceful)
        {
            // Wait for running tasks to complete with a timeout
            var activeExecutionTasks = _activeExecutions.Values.ToList();
            _logger.LogInformation("Waiting for {Count} running tasks to complete (max 30 seconds)", activeExecutionTasks.Count);

            if (activeExecutionTasks.Count > 0)
            {
                var allTasks = Task.WhenAll(activeExecutionTasks);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                var completedTask = await Task.WhenAny(allTasks, timeoutTask).ConfigureAwait(false);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Graceful shutdown timeout reached after 30 seconds with {Count} tasks still running",
                        _activeExecutions.Count);
                }
                else
                {
                    // Await the WhenAll task to propagate any exceptions from the tasks.
                    await allTasks.ConfigureAwait(false);
                    _logger.LogInformation("All running tasks completed before shutdown");
                }
            }
        }

        _logger.LogInformation("Task scheduler stopped");
        await Task.Yield();
    }

    /// <inheritdoc/>
    public async Task SetMaxConcurrentTasksAsync(int maxConcurrentTasks, CancellationToken cancellationToken = default)
    {
        if (maxConcurrentTasks <= 0)
        {
            throw new ArgumentException("Max concurrent tasks must be greater than 0", nameof(maxConcurrentTasks));
        }

        _maxConcurrentTasks = maxConcurrentTasks;
        _logger.LogInformation("Set max concurrent tasks to {MaxConcurrentTasks}", maxConcurrentTasks);
        TriggerScheduler();
        await Task.Yield();
    }

    #endregion

    #region Cleanup and Maintenance

    /// <inheritdoc/>
    public async Task<int> CleanupOldTasksAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        // Use Local time because TaskExecution.CompletedAt uses Local time
        DateTime cutoffTime = _timeProvider.GetLocalNow() - maxAge;
        var oldTasks = _tasks.Values
            .Where(t => t.IsTerminal && t.CompletedAt.HasValue && t.CompletedAt.Value < cutoffTime)
            .ToList();

        foreach (TaskExecution? task in oldTasks)
        {
            _tasks.TryRemove(task.TaskId, out _);
        }

        _logger.LogInformation("Cleaned up {Count} old tasks older than {MaxAge}", oldTasks.Count, maxAge);
        await Task.Yield();
        return oldTasks.Count;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            return false;
        }

        if (task.State is TaskState.Running or TaskState.Paused or TaskState.Queued or TaskState.Scheduled)
        {
            _logger.LogWarning("Cannot remove task {TaskId} because it is in state {State}. Cancel it first.", taskId, task.State);
            return false;
        }

        if (_tasks.TryRemove(taskId, out _))
        {
            _logger.LogInformation("Removed task {TaskId} ({JobName})", taskId, task.JobName);
            await Task.Run(() => SaveTasksAsync(), CancellationToken.None);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public Task<int> ClearFinishedTasksAsync(CancellationToken cancellationToken = default)
    {
        return CleanupOldTasksAsync(TimeSpan.Zero, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SchedulerStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var tasksByState = _tasks.Values
            .GroupBy(t => t.State)
            .ToDictionary(g => g.Key, g => g.Count());

        TimeSpan averageExecutionTime = _executionTimes.Count > 0
            ? TimeSpan.FromTicks((long)_executionTimes.Average(t => t.Ticks))
            : TimeSpan.Zero;

        await Task.Yield();
        return new SchedulerStatistics
        {
            TotalTasksProcessed = _totalTasksProcessed,
            SuccessfulTasks = _successfulTasks,
            FailedTasks = _failedTasks,
            CancelledTasks = _cancelledTasks,
            AverageExecutionTime = averageExecutionTime,
            TasksByState = tasksByState,
            Uptime = _timeProvider.GetUtcNow() - _startTime
        };
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Triggers the scheduler to process tasks immediately.
    /// Thread-safe and fire-and-forget.
    /// </summary>
    private void TriggerScheduler()
    {
        if (_disposed)
        {
            return;
        }

        // Coalesce bursts of triggers into a single scheduled run
        try
        {
            _scheduleTimer.Change(0, Timeout.Infinite);
        }
        catch (ObjectDisposedException)
        {
            // Scheduler is shutting down; ignore
        }
    }

    /// <summary>
    /// Timer callback to process tasks.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void ProcessTasks(object? state)
    {
        if (!_isRunning || _disposed)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessTasksAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tasks");
            }
        });
    }

    /// <summary>
    /// Processes queued tasks and starts execution if resources are available.
    /// </summary>
    private async Task ProcessTasksAsync()
    {
        await _schedulerSemaphore.ExecuteAsync(async () =>
        {
            // Promote due scheduled tasks
            DateTime nowUtc = _timeProvider.GetUtcNow();
            if (!_scheduledTasks.IsEmpty)
            {
                List<Guid> dueTaskIds = [.. _scheduledTasks.Where(kvp => kvp.Value <= nowUtc).Select(kvp => kvp.Key)];
                foreach (Guid dueId in dueTaskIds)
                {
                    if (_tasks.TryGetValue(dueId, out TaskExecution? scheduledTask) && scheduledTask.State == TaskState.Scheduled)
                    {
                        if (!TryEnqueueInternal(dueId, scheduledTask, "Promoted to queue from schedule"))
                        {
                            _logger.LogWarning("Failed to promote due scheduled task {TaskId} to queue because it is full", dueId);
                        }
                        else
                        {
                            TaskStateChanged?.Invoke(scheduledTask);
                            // Persist changes as we modified state
                            _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None);
                        }
                    }
                    _scheduledTasks.TryRemove(dueId, out _);
                }
            }

            // Schedule next wake-up for remaining scheduled tasks
            if (!_scheduledTasks.IsEmpty)
            {
                DateTime nextSchedule = _scheduledTasks.Values.Min();
                if (nextSchedule > nowUtc)
                {
                    TimeSpan delay = nextSchedule - nowUtc;
                    // Add small buffer to ensure we wake up after the time
                    delay = delay.Add(TimeSpan.FromMilliseconds(100));
                    _scheduleTimer.Change(delay, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    // Should have been processed, but just in case
                    _scheduleTimer.Change(TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan);
                }
            }
            else
            {
                _scheduleTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            int runningCount = _tasks.Values.Count(t => t.State == TaskState.Running);
            int availableSlots = _maxConcurrentTasks - runningCount;

            if (availableSlots <= 0)
            {
                return;
            }

            var tasksToStart = new List<Guid>();
            var tasksToRequeue = new List<Guid>();

            // Iterate through the queue to find executable tasks
            // We continue until we fill available slots or drain the queue
            // Skipped tasks (due to locks) are collected to be re-enqueued
            while (tasksToStart.Count < availableSlots && _taskQueue.TryDequeue(out Guid taskId))
            {
                if (_tasks.TryGetValue(taskId, out TaskExecution? task) && task.State == TaskState.Queued)
                {
                    // Try to acquire resources
                    if (_resourceCoordinator.TryAcquire(task.LockedResources))
                    {
                        tasksToStart.Add(taskId);
                    }
                    else
                    {
                        // Get detailed blocking reasons and update task
                        List<string> blockingReasons = await GetTaskBlockingReasonsAsync(taskId, CancellationToken.None).ConfigureAwait(false);
                        string reasonsText = blockingReasons.Count > 0
                            ? string.Join("; ", blockingReasons)
                            : "Resources not available";

                        task.UpdateProgress(task.ProgressPercentage, $"Waiting: {reasonsText}");
                        TaskProgressUpdated?.Invoke(task.TaskId, task.ProgressPercentage, task.CurrentOperation ?? string.Empty);

                        // Collect for re-queueing
                        tasksToRequeue.Add(taskId);
                    }
                }
            }

            // Re-enqueue skipped tasks
            foreach (var skippedId in tasksToRequeue)
            {
                _taskQueue.Enqueue(skippedId);
            }

            // Start the tasks with tracking to prevent race conditions
            foreach (Guid taskId in tasksToStart)
            {
                Task executionTask = ExecuteAndTrackTaskAsync(taskId);
                _activeExecutions.TryAdd(taskId, executionTask);
            }
        });
    }

    private async Task ExecuteAndTrackTaskAsync(Guid taskId)
    {
        try
        {
            await ExecuteTaskAsync(taskId).ConfigureAwait(false);
        }
        finally
        {
            _activeExecutions.TryRemove(taskId, out _);
            TriggerScheduler(); // Wake up scheduler to process next task in queue
        }
    }

    /// <summary>
    /// Executes a single task.
    /// </summary>
    /// <param name="taskId">The ID of the task to execute.</param>
    private async Task ExecuteTaskAsync(Guid taskId)
    {
        if (!_tasks.TryGetValue(taskId, out TaskExecution? task))
        {
            _logger.LogError("Task {TaskId} not found for execution", taskId);
            return;
        }

        TaskLogger? taskLogger = null;

        try
        {
            task.UpdateState(TaskState.Running, "Starting task execution");
            TaskStateChanged?.Invoke(task);
            _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist running state

            // Create task-specific logger
            taskLogger = await _taskLoggerFactory.CreateTaskLoggerAsync(
                taskId,
                task.JobName,
                captureProcessOutput: true,
                CancellationToken.None).ConfigureAwait(false);

            task.Logger = taskLogger;

            // Log task start
            taskLogger.MainLogger?.LogInformation(
                "Task execution started: {JobName} (ID: {TaskId}) at {StartTime}",
                task.JobName, taskId, DateTime.UtcNow.ToLocalTime());

            // Get the job profile
            JobProfile jobProfile = await _jobManager.GetByIdAsync(task.JobProfileId).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Job profile {task.JobProfileId} not found");

            taskLogger.MainLogger?.LogInformation("Job profile loaded: {ProfileName}", jobProfile.Name);

            // Create progress reporter
            // Create progress reporter with throttling
            double lastLoggedPercent = -1;
            string lastLoggedStage = string.Empty;

            var progress = new Progress<(string stage, double percent, long? bytesRead, long? totalBytes)>(p =>
            {
                var extraData = new Dictionary<string, object>();
                if (p.bytesRead.HasValue)
                {
                    extraData["BytesRead"] = p.bytesRead.Value;
                }
                if (p.totalBytes.HasValue)
                {
                    extraData["TotalBytes"] = p.totalBytes.Value;
                }

                task.UpdateProgress(p.percent, p.stage, extraData);
                TaskProgressUpdated?.Invoke(task.TaskId, p.percent, p.stage, extraData);

                // Throttle logging to task logger (Info level)
                // Log if:
                // 1. Stage changed
                // 2. Percent changed by >= 5%
                // 3. Percent is 0% or 100%
                bool shouldLog = false;

                if (!string.Equals(lastLoggedStage, p.stage, StringComparison.Ordinal))
                {
                    shouldLog = true;
                    lastLoggedStage = p.stage;
                }
                else if (Math.Abs(p.percent - lastLoggedPercent) >= 5.0 || p.percent <= 0.001 || p.percent >= 99.99)
                {
                    shouldLog = true;
                    lastLoggedPercent = p.percent;
                }

                if (shouldLog)
                {
                    if (p.bytesRead.HasValue && p.totalBytes.HasValue)
                    {
                        taskLogger.MainLogger?.LogInformation(
                            "Progress: {Stage} - {Percent:F1}% ({BytesRead}/{TotalBytes} bytes)",
                            p.stage, p.percent, p.bytesRead, p.totalBytes);
                    }
                    else
                    {
                        taskLogger.MainLogger?.LogInformation(
                            "Progress: {Stage} - {Percent:F1}%",
                            p.stage, p.percent);
                    }
                }
            });

            // Execute the job
            taskLogger.MainLogger?.LogInformation("Starting bootloader execution");
            Job executionJob = await _jobManager.CreateExecutionJobAsync(jobProfile.Id, CancellationToken.None).ConfigureAwait(false);

            BootloaderResult result = await _bootloaderService.DumpAsync(
                executionJob.ProfileSet,
                progress,
                taskLogger.MainLogger,
                taskLogger.ProcessLogger,
                CancellationToken.None)
                .ConfigureAwait(false);

            long totalSize = result.SavedFiles.Sum(x => (long)x.Length);
            taskLogger.MainLogger?.LogInformation("Bootloader dump completed. Total size: {Size} bytes. Files: {Count}",
                totalSize, result.SavedFiles.Count);

            string primaryOutputFile = result.SavedFiles.FirstOrDefault() ?? string.Empty;

            if (result.SavedFiles.Count > 0)
            {
                taskLogger.MainLogger?.LogInformation("Outputs saved to: {OutputPath} ({Count} files)",
                    Path.GetDirectoryName(primaryOutputFile), result.SavedFiles.Count);
            }

            // Mark as completed
            task.MarkAsCompleted(primaryOutputFile, totalSize);
            TaskStateChanged?.Invoke(task);
            _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist completed state

            // Update statistics
            Interlocked.Increment(ref _totalTasksProcessed);
            Interlocked.Increment(ref _successfulTasks);

            if (task.ExecutionTime.HasValue)
            {
                lock (_executionTimes)
                {
                    _executionTimes.Enqueue(task.ExecutionTime.Value);
                    // M-3: Enforce max count to prevent unbounded growth
                    while (_executionTimes.Count > MaxExecutionTimesCount)
                    {
                        _executionTimes.Dequeue(); // O(1) operation
                    }
                }
            }

            taskLogger.MainLogger?.LogInformation(
                "Task completed successfully. Execution time: {ExecutionTime}",
                task.ExecutionTime);

            _logger.LogInformation("Task {TaskId} ({JobName}) completed successfully. Output: {OutputFile}",
                taskId, task.JobName, primaryOutputFile);
        }
        catch (OperationCanceledException)
        {
            task.UpdateState(TaskState.Cancelled, "Task was cancelled");
            TaskStateChanged?.Invoke(task);
            _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist cancelled state
            Interlocked.Increment(ref _cancelledTasks);

            taskLogger?.MainLogger?.LogWarning("Task was cancelled");
            _logger.LogWarning("Task {TaskId} ({JobName}) was cancelled", taskId, task.JobName);
        }
        catch (Exception ex)
        {
            task.MarkAsFailed(ex.Message, ex.ToString());
            TaskStateChanged?.Invoke(task);
            _ = Task.Run(() => SaveTasksAsync(), CancellationToken.None); // Persist failed state
            Interlocked.Increment(ref _totalTasksProcessed);
            Interlocked.Increment(ref _failedTasks);

            taskLogger?.MainLogger?.LogError(ex, "Task failed: {ErrorMessage}", ex.Message);
            _logger.LogError(ex, "Task {TaskId} ({JobName}) failed: {ErrorMessage}", taskId, task.JobName, ex.Message);
        }
        finally
        {
            // Finalize task logger
            if (taskLogger != null)
            {
                try
                {
                    await _taskLoggerFactory.FinalizeTaskLoggerAsync(taskId, CancellationToken.None).ConfigureAwait(false);
                    _logger.LogInformation("Task logger finalized. Logs saved to: {LogPath}", taskLogger.MainLogFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to finalize task logger for task {TaskId}", taskId);
                }
            }

            // Release resources
            _resourceCoordinator.Release(task.LockedResources);
            _logger.LogDebug("Released {Count} resources for task {TaskId}", task.LockedResources.Count, taskId);
        }
    }

    /// <summary>
    /// Saves all tasks to persistent storage.
    /// </summary>
    /// <summary>
    /// Saves all tasks to persistent storage, separating active and finished tasks.
    /// </summary>
    private async Task SaveTasksAsync()
    {
        await _persistenceSemaphore.ExecuteAsync(async () =>
        {
            try
            {
                var allTasks = _tasks.Values.ToList();

                // Split tasks into active and finished
                var activeTasks = allTasks.Where(t => !t.IsTerminal).ToList();
                var finishedTasks = allTasks.Where(t => t.IsTerminal).ToList();

                // Save active tasks to main file
                string activeJson = System.Text.Json.JsonSerializer.Serialize(activeTasks, JsonOptions);
                await File.WriteAllTextAsync(_tasksFilePath, activeJson).ConfigureAwait(false);

                // Save finished tasks to history file
                string historyJson = System.Text.Json.JsonSerializer.Serialize(finishedTasks, JsonOptions);
                await File.WriteAllTextAsync(_historyFilePath, historyJson).ConfigureAwait(false);

                _logger.LogDebug("Saved {ActiveCount} active tasks to {Path} and {HistoryCount} finished tasks to {HistoryPath}",
                    activeTasks.Count, _tasksFilePath, finishedTasks.Count, _historyFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save tasks to persistence files");
            }
        });
    }

    /// <summary>
    /// Loads tasks from persistent storage.
    /// </summary>
    private async Task LoadTasksAsync()
    {
        await _persistenceSemaphore.ExecuteAsync(async () =>
        {
            try
            {
                var loadedTasks = new List<TaskExecution>();

                // Load active tasks
                if (File.Exists(_tasksFilePath))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(_tasksFilePath).ConfigureAwait(false);
                        var activeTasks = System.Text.Json.JsonSerializer.Deserialize<List<TaskExecution>>(json);
                        if (activeTasks != null)
                        {
                            loadedTasks.AddRange(activeTasks);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load active tasks from {Path}", _tasksFilePath);
                    }
                }

                // Load history tasks
                if (File.Exists(_historyFilePath))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(_historyFilePath).ConfigureAwait(false);
                        var historyTasks = System.Text.Json.JsonSerializer.Deserialize<List<TaskExecution>>(json);
                        if (historyTasks != null)
                        {
                            loadedTasks.AddRange(historyTasks);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load history tasks from {Path}", _historyFilePath);
                    }
                }

                if (loadedTasks != null && loadedTasks.Count > 0)
                {
                    foreach (TaskExecution task in loadedTasks)
                    {
                        _tasks[task.TaskId] = task;

                        // Restore queued and scheduled tasks to their queues
                        if (task.State == TaskState.Queued)
                        {
                            if (!TryEnqueueInternal(task.TaskId, task, "Restored queued task"))
                            {
                                _logger.LogWarning("Failed to restore queued task {TaskId} because queue is full", task.TaskId);
                            }
                        }
                        else if (task.State == TaskState.Scheduled && task.ProgressData.TryGetValue("ScheduledTime", out object? scheduledObj))
                        {
                            if (scheduledObj is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                string? dateString = jsonElement.GetString();
                                if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out DateTime scheduledTime))
                                {
                                    _scheduledTasks[task.TaskId] = scheduledTime.Kind == DateTimeKind.Unspecified
                                        ? DateTime.SpecifyKind(scheduledTime, DateTimeKind.Utc)
                                        : scheduledTime.ToUniversalTime();
                                }
                            }
                        }
                        // Reset running tasks to queued (they were interrupted by app close)
                        else if (task.State == TaskState.Running)
                        {
                            if (!TryEnqueueInternal(task.TaskId, task, "Restored from interrupted session"))
                            {
                                _logger.LogWarning("Failed to restore interrupted running task {TaskId} because queue is full", task.TaskId);
                            }
                        }
                    }

                    _logger.LogInformation("Loaded {Count} tasks from persistence", loadedTasks.Count);

                    // Trigger scheduler to pick up any queued tasks that were restored
                    TriggerScheduler();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load tasks from persistence");
            }
        });
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the EnhancedTaskScheduler, releasing managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True to release managed resources; false for native only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            // Save tasks before disposing
            try
            {
                SaveTasksAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tasks on dispose");
            }

            _scheduleTimer?.Dispose();
            _schedulerSemaphore?.Dispose();
            _persistenceSemaphore?.Dispose();
        }
        _disposed = true;
        _logger.LogInformation("EnhancedTaskScheduler disposed");
    }

    #endregion
}
