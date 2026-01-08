using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

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
    private readonly string _tasksFilePath;

    private readonly ConcurrentDictionary<Guid, TaskExecution> _tasks = new();
    private readonly ConcurrentQueue<Guid> _taskQueue = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _scheduledTasks = new();
    private readonly SemaphoreSlim _schedulerSemaphore = new(1, 1);
    private readonly SemaphoreSlim _persistenceSemaphore = new(1, 1);
    private readonly Timer _processingTimer;
    private readonly Timer _cleanupTimer;
    private readonly Timer _persistenceTimer;

    private bool _isRunning;
    private bool _disposed;
    private int _maxConcurrentTasks = Environment.ProcessorCount;
    private readonly DateTime _startTime = DateTime.Now;

    // Statistics
    private long _totalTasksProcessed;
    private long _successfulTasks;
    private long _failedTasks;
    private long _cancelledTasks;
    private readonly List<TimeSpan> _executionTimes = [];
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
    public EnhancedTaskScheduler(
        ILogger<EnhancedTaskScheduler> logger,
        IResourceCoordinator resourceCoordinator,
        IBootloaderService bootloaderService,
        IJobManager jobManager,
        IPathService pathService,
        ITaskLoggerFactory taskLoggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceCoordinator = resourceCoordinator ?? throw new ArgumentNullException(nameof(resourceCoordinator));
        _bootloaderService = bootloaderService ?? throw new ArgumentNullException(nameof(bootloaderService));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _taskLoggerFactory = taskLoggerFactory ?? throw new ArgumentNullException(nameof(taskLoggerFactory));

        // Set up tasks file path using PathService
        _tasksFilePath = _pathService.TasksPath;
        string? tasksDirectory = Path.GetDirectoryName(_tasksFilePath);
        if (!string.IsNullOrEmpty(tasksDirectory))
        {
            Directory.CreateDirectory(tasksDirectory);
        }

        // Set up timers for periodic processing, cleanup, and persistence
        _processingTimer = new Timer(ProcessTasks, null, Timeout.Infinite, Timeout.Infinite);
        _cleanupTimer = new Timer(PerformCleanup, null, Timeout.Infinite, Timeout.Infinite);
        _persistenceTimer = new Timer(PersistTasks, null, Timeout.Infinite, Timeout.Infinite);

        // Start timers
        _processingTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        _cleanupTimer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        _persistenceTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

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
            CreatedAt = DateTime.Now
        };

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

        task.UpdateState(TaskState.Queued, "Task queued for execution");
        _taskQueue.Enqueue(taskId);

        TaskStateChanged?.Invoke(task);

        _logger.LogInformation("Enqueued task {TaskId} ({JobName})", taskId, task.JobName);
        return Task.FromResult(true);
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

        // Normalize to Local timezone per requirement
        DateTime localTime = scheduledTime.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(scheduledTime, DateTimeKind.Local),
            DateTimeKind.Utc => scheduledTime.ToLocalTime(),
            _ => scheduledTime
        };

        task.UpdateState(TaskState.Scheduled, $"Scheduled for {localTime}");
        task.ProgressData["ScheduledTime"] = localTime;
        _scheduledTasks[taskId] = localTime;

        TaskStateChanged?.Invoke(task);

        _logger.LogInformation("Scheduled task {TaskId} ({JobName}) for {ScheduledTime}",
            taskId, task.JobName, localTime);

        // If time already passed or is now, promote to queue immediately
        if (localTime <= DateTime.Now)
        {
            _scheduledTasks.TryRemove(taskId, out _);
            task.UpdateState(TaskState.Queued, "Promoted to queue from schedule");
            TaskStateChanged?.Invoke(task);
            _taskQueue.Enqueue(taskId);
        }

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
            if (!_resourceCoordinator.TryAcquire([resource]))
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
                            reasons.Add($"Serial port '{resource.Id}' is in use by task '{lockingTask.JobName}' (started {lockingTask.StartedAt:HH:mm:ss})");
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

            return DateTime.Now.Add(estimatedWaitTime);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<ResourceUsageInfo> GetResourceUsageAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TaskExecution> runningTasks = await GetRunningTasksAsync(cancellationToken).ConfigureAwait(false);
        var allResources = runningTasks.SelectMany(t => t.LockedResources).ToList();
        var resourceLocks = allResources.ToDictionary(r => r, r => runningTasks.First(t => t.LockedResources.Contains(r)).TaskId);

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
            // Wait for running tasks to complete
            IReadOnlyCollection<TaskExecution> runningTasks = await GetRunningTasksAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Waiting for {Count} running tasks to complete", runningTasks.Count);

            while (runningTasks.Count > 0)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                runningTasks = await GetRunningTasksAsync(cancellationToken).ConfigureAwait(false);
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
        await Task.Yield();
    }

    #endregion

    #region Cleanup and Maintenance

    /// <inheritdoc/>
    public async Task<int> CleanupOldTasksAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        DateTime cutoffTime = DateTime.Now - maxAge;
        var oldTasks = _tasks.Values
            .Where(t => t.IsTerminal && t.CompletedAt < cutoffTime)
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
            Uptime = DateTime.Now - _startTime
        };
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Timer callback to process queued tasks.
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
        await _schedulerSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            // Promote due scheduled tasks
            if (!_scheduledTasks.IsEmpty)
            {
                DateTime nowLocal = DateTime.Now;
                List<Guid> dueTaskIds = [.. _scheduledTasks.Where(kvp => kvp.Value <= nowLocal).Select(kvp => kvp.Key)];
                foreach (Guid dueId in dueTaskIds)
                {
                    if (_tasks.TryGetValue(dueId, out TaskExecution? scheduledTask) && scheduledTask.State == TaskState.Scheduled)
                    {
                        scheduledTask.UpdateState(TaskState.Queued, "Promoted to queue from schedule");
                        TaskStateChanged?.Invoke(scheduledTask);
                        _taskQueue.Enqueue(dueId);
                    }
                    _scheduledTasks.TryRemove(dueId, out _);
                }
            }

            int runningCount = _tasks.Values.Count(t => t.State == TaskState.Running);
            int availableSlots = _maxConcurrentTasks - runningCount;

            if (availableSlots <= 0)
            {
                return;
            }

            var tasksToStart = new List<Guid>();

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

                        // Re-queue for later
                        _taskQueue.Enqueue(taskId);
                        break; // Stop trying if resources aren't available
                    }
                }
            }

            // Start the tasks
            foreach (Guid taskId in tasksToStart)
            {
                _ = Task.Run(() => ExecuteTaskAsync(taskId));
            }
        }
        finally
        {
            _schedulerSemaphore.Release();
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

            // Create task-specific logger
            taskLogger = await _taskLoggerFactory.CreateTaskLoggerAsync(
                taskId,
                task.JobName,
                captureProtocol: true,
                captureProcessOutput: true,
                CancellationToken.None).ConfigureAwait(false);

            task.Logger = taskLogger;

            // Log task start
            taskLogger.MainLogger?.LogInformation(
                "Task execution started: {JobName} (ID: {TaskId}) at {StartTime}",
                task.JobName, taskId, DateTime.UtcNow);

            // Get the job profile
            JobProfile jobProfile = await _jobManager.GetByIdAsync(task.JobProfileId).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Job profile {task.JobProfileId} not found");

            taskLogger.MainLogger?.LogInformation("Job profile loaded: {ProfileName}", jobProfile.Name);

            // Create progress reporter
            var progress = new Progress<(string stage, double percent)>(p =>
            {
                task.UpdateProgress(p.percent, p.stage);
                TaskProgressUpdated?.Invoke(task.TaskId, p.percent, p.stage);

                // Log progress to task logger
                taskLogger.MainLogger?.LogInformation(
                    "Progress: {Stage} - {Percent:F1}%",
                    p.stage, p.percent);
            });

            // Execute the job
            taskLogger.MainLogger?.LogInformation("Starting bootloader execution");
            Job executionJob = await _jobManager.CreateExecutionJobAsync(jobProfile.Id, CancellationToken.None).ConfigureAwait(false);
            byte[] dumpData = await _bootloaderService.DumpAsync(
                executionJob.ProfileSet,
                progress,
                taskLogger.MainLogger,
                taskLogger.ProcessLogger,
                taskLogger.ProtocolLogger,
                CancellationToken.None)
                .ConfigureAwait(false);

            taskLogger.MainLogger?.LogInformation("Bootloader dump completed. Size: {Size} bytes", dumpData.Length);

            // Save the output
            string outputFile = Path.Combine(jobProfile.OutputPath, $"dump-{task.TaskId:N}.bin");
            Directory.CreateDirectory(jobProfile.OutputPath);
            await File.WriteAllBytesAsync(outputFile, dumpData, CancellationToken.None).ConfigureAwait(false);

            taskLogger.MainLogger?.LogInformation("Output saved to: {OutputFile}", outputFile);

            // Mark as completed
            task.MarkAsCompleted(outputFile, dumpData.Length);
            TaskStateChanged?.Invoke(task);

            // Update statistics
            Interlocked.Increment(ref _totalTasksProcessed);
            Interlocked.Increment(ref _successfulTasks);

            if (task.ExecutionTime.HasValue)
            {
                lock (_executionTimes)
                {
                    _executionTimes.Add(task.ExecutionTime.Value);
                    if (_executionTimes.Count > 1000) // Keep only last 1000 execution times
                    {
                        _executionTimes.RemoveAt(0);
                    }
                }
            }

            taskLogger.MainLogger?.LogInformation(
                "Task completed successfully. Execution time: {ExecutionTime}",
                task.ExecutionTime);

            _logger.LogInformation("Task {TaskId} ({JobName}) completed successfully. Output: {OutputFile}",
                taskId, task.JobName, outputFile);
        }
        catch (OperationCanceledException)
        {
            task.UpdateState(TaskState.Cancelled, "Task was cancelled");
            TaskStateChanged?.Invoke(task);
            Interlocked.Increment(ref _cancelledTasks);

            taskLogger?.MainLogger?.LogWarning("Task was cancelled");
            _logger.LogWarning("Task {TaskId} ({JobName}) was cancelled", taskId, task.JobName);
        }
        catch (Exception ex)
        {
            task.MarkAsFailed(ex.Message, ex.ToString());
            TaskStateChanged?.Invoke(task);
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
    /// Timer callback for periodic cleanup operations.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void PerformCleanup(object? state)
    {
        if (_disposed)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                // Clean up tasks older than 24 hours
                int cleanedCount = await CleanupOldTasksAsync(TimeSpan.FromHours(24)).ConfigureAwait(false);

                // Persist changes after cleanup
                if (cleanedCount > 0)
                {
                    await SaveTasksAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic cleanup");
            }
        });
    }

    /// <summary>
    /// Timer callback for periodic task persistence.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void PersistTasks(object? state)
    {
        if (_disposed)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await SaveTasksAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic task persistence");
            }
        });
    }

    /// <summary>
    /// Saves all tasks to persistent storage.
    /// </summary>
    private async Task SaveTasksAsync()
    {
        await _persistenceSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var tasksToSave = _tasks.Values.ToList();
            string json = System.Text.Json.JsonSerializer.Serialize(tasksToSave, JsonOptions);

            await File.WriteAllTextAsync(_tasksFilePath, json).ConfigureAwait(false);
            _logger.LogDebug("Saved {Count} tasks to {Path}", tasksToSave.Count, _tasksFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save tasks to {Path}", _tasksFilePath);
        }
        finally
        {
            _persistenceSemaphore.Release();
        }
    }

    /// <summary>
    /// Loads tasks from persistent storage.
    /// </summary>
    private async Task LoadTasksAsync()
    {
        await _persistenceSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(_tasksFilePath))
            {
                _logger.LogInformation("No existing tasks file found at {Path}", _tasksFilePath);
                return;
            }

            string json = await File.ReadAllTextAsync(_tasksFilePath).ConfigureAwait(false);
            List<TaskExecution>? loadedTasks = System.Text.Json.JsonSerializer.Deserialize<List<TaskExecution>>(json);

            if (loadedTasks != null && loadedTasks.Count > 0)
            {
                foreach (TaskExecution task in loadedTasks)
                {
                    _tasks[task.TaskId] = task;

                    // Restore queued and scheduled tasks to their queues
                    if (task.State == TaskState.Queued)
                    {
                        _taskQueue.Enqueue(task.TaskId);
                    }
                    else if (task.State == TaskState.Scheduled && task.ProgressData.TryGetValue("ScheduledTime", out object? scheduledObj))
                    {
                        if (scheduledObj is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            string? dateString = jsonElement.GetString();
                            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out DateTime scheduledTime))
                            {
                                _scheduledTasks[task.TaskId] = scheduledTime;
                            }
                        }
                    }
                    // Reset running tasks to queued (they were interrupted by app close)
                    else if (task.State == TaskState.Running)
                    {
                        task.UpdateState(TaskState.Queued, "Restored from interrupted session");
                        _taskQueue.Enqueue(task.TaskId);
                    }
                }

                _logger.LogInformation("Loaded {Count} tasks from {Path}", loadedTasks.Count, _tasksFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tasks from {Path}", _tasksFilePath);
        }
        finally
        {
            _persistenceSemaphore.Release();
        }
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

            _processingTimer?.Dispose();
            _cleanupTimer?.Dispose();
            _persistenceTimer?.Dispose();
            _schedulerSemaphore?.Dispose();
            _persistenceSemaphore?.Dispose();
        }
        _disposed = true;
        _logger.LogInformation("EnhancedTaskScheduler disposed");
    }

    #endregion
}
