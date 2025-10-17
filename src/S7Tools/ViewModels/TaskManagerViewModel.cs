using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Task Manager activity that provides real-time task monitoring and management.
/// Displays tasks organized by their execution state with commands for task lifecycle operations.
/// </summary>
/// <remarks>
/// This ViewModel implements the VSCode-style activity pattern for task management, providing:
/// - Real-time task collections organized by state (Created, Queued, Scheduled, Active, Finished)
/// - Task lifecycle commands (Start, Stop, Schedule, Cancel, Restart)
/// - Live progress monitoring with automatic UI updates
/// - Resource conflict visualization and resolution
/// - Performance statistics and task execution metrics
///
/// Architecture:
/// - Reactive collections automatically update when task states change
/// - Commands use proper async patterns with cancellation support
/// - UI thread marshaling for cross-thread task updates
/// - Resource coordination prevents conflicts and enables parallel execution
/// - Comprehensive error handling with user-friendly messaging
/// </remarks>
public class TaskManagerViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<TaskManagerViewModel> _logger;
    private readonly ITaskScheduler _taskScheduler;
    private readonly IJobManager _jobManager;
    private readonly IUIThreadService _uiThreadService;
    private readonly IDialogService _dialogService;
    private readonly CompositeDisposable _disposables = new();

    // State-based task collections for UI binding
    private ObservableCollection<TaskExecution> _createdTasks = new();
    private ObservableCollection<TaskExecution> _queuedTasks = new();
    private ObservableCollection<TaskExecution> _scheduledTasks = new();
    private ObservableCollection<TaskExecution> _activeTasks = new();
    private ObservableCollection<TaskExecution> _finishedTasks = new();

    // Current selection and UI state
    private TaskExecution? _selectedTask;
    private bool _isLoading;
    private string? _statusMessage;
    private bool _isAutoRefreshEnabled = true;
    private int _refreshIntervalSeconds = 2;

    // Task statistics for dashboard
    private int _totalTasksCount;
    private int _runningTasksCount;
    private int _failedTasksCount;
    private TimeSpan _averageExecutionTime;
    private string _resourceUtilization = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskManagerViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger for this view model.</param>
    /// <param name="taskScheduler">The task scheduler service for task management operations.</param>
    /// <param name="jobManager">The job manager service for accessing job configurations.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    /// <param name="dialogService">The dialog service for user confirmations.</param>
    public TaskManagerViewModel(
        ILogger<TaskManagerViewModel> logger,
        ITaskScheduler taskScheduler,
        IJobManager jobManager,
        IUIThreadService uiThreadService,
        IDialogService dialogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        SetupCommands();
        SetupCollections();
        SetupAutoRefresh();
        SubscribeToTaskEvents();

        // Initialize with current tasks
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadTasksAsync().ConfigureAwait(false);
                _logger.LogInformation("Task Manager initialized with {TaskCount} tasks", TotalTasksCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Task Manager");
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = $"Failed to load tasks: {ex.Message}");
            }
        });
    }

    #region Properties

    /// <summary>
    /// Gets the collection of tasks in the Created state.
    /// </summary>
    /// <remarks>
    /// Tasks that have been created but not yet queued for execution.
    /// These tasks can be started immediately or scheduled for later execution.
    /// </remarks>
    public ObservableCollection<TaskExecution> CreatedTasks
    {
        get => _createdTasks;
        private set => this.RaiseAndSetIfChanged(ref _createdTasks, value);
    }

    /// <summary>
    /// Gets the collection of tasks in the Queued state.
    /// </summary>
    /// <remarks>
    /// Tasks that are waiting in the execution queue.
    /// These tasks will start automatically when resources become available.
    /// </remarks>
    public ObservableCollection<TaskExecution> QueuedTasks
    {
        get => _queuedTasks;
        private set => this.RaiseAndSetIfChanged(ref _queuedTasks, value);
    }

    /// <summary>
    /// Gets the collection of tasks in the Scheduled state.
    /// </summary>
    /// <remarks>
    /// Tasks that are scheduled to run at a specific future time.
    /// These tasks will automatically move to queued state when their scheduled time arrives.
    /// </remarks>
    public ObservableCollection<TaskExecution> ScheduledTasks
    {
        get => _scheduledTasks;
        private set => this.RaiseAndSetIfChanged(ref _scheduledTasks, value);
    }

    /// <summary>
    /// Gets the collection of tasks that are currently active (running or paused).
    /// </summary>
    /// <remarks>
    /// Tasks that are currently executing or temporarily paused.
    /// Shows real-time progress updates and current operation status.
    /// </remarks>
    public ObservableCollection<TaskExecution> ActiveTasks
    {
        get => _activeTasks;
        private set => this.RaiseAndSetIfChanged(ref _activeTasks, value);
    }

    /// <summary>
    /// Gets the collection of tasks that have finished execution (completed, failed, or cancelled).
    /// </summary>
    /// <remarks>
    /// Tasks in terminal states that provide execution history and results.
    /// Failed tasks can be restarted; completed tasks show output file information.
    /// </remarks>
    public ObservableCollection<TaskExecution> FinishedTasks
    {
        get => _finishedTasks;
        private set => this.RaiseAndSetIfChanged(ref _finishedTasks, value);
    }

    /// <summary>
    /// Gets or sets the currently selected task across all collections.
    /// </summary>
    /// <remarks>
    /// Drives the enable/disable state of task operation commands.
    /// Used for displaying detailed task information and progress.
    /// </remarks>
    public TaskExecution? SelectedTask
    {
        get => _selectedTask;
        set => this.RaiseAndSetIfChanged(ref _selectedTask, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether task operations are in progress.
    /// </summary>
    /// <remarks>
    /// Used to show loading indicators and disable UI during async operations.
    /// Prevents concurrent task modifications during state transitions.
    /// </remarks>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Gets or sets the current status message for user feedback.
    /// </summary>
    /// <remarks>
    /// Displays operation results, task execution status, and general information.
    /// Automatically cleared after successful operations or when new status is set.
    /// </remarks>
    public string? StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether automatic task refresh is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, task collections are automatically updated at regular intervals.
    /// Useful for real-time monitoring but can be disabled to reduce system load.
    /// </remarks>
    public bool IsAutoRefreshEnabled
    {
        get => _isAutoRefreshEnabled;
        set => this.RaiseAndSetIfChanged(ref _isAutoRefreshEnabled, value);
    }

    /// <summary>
    /// Gets or sets the auto-refresh interval in seconds.
    /// </summary>
    /// <remarks>
    /// Controls how frequently task data is refreshed from the scheduler.
    /// Valid range is 1-60 seconds. Default is 2 seconds for responsive UI.
    /// </remarks>
    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set => this.RaiseAndSetIfChanged(ref _refreshIntervalSeconds, Math.Clamp(value, 1, 60));
    }

    /// <summary>
    /// Gets the total number of tasks across all states.
    /// </summary>
    public int TotalTasksCount
    {
        get => _totalTasksCount;
        private set => this.RaiseAndSetIfChanged(ref _totalTasksCount, value);
    }

    /// <summary>
    /// Gets the number of currently running tasks.
    /// </summary>
    public int RunningTasksCount
    {
        get => _runningTasksCount;
        private set => this.RaiseAndSetIfChanged(ref _runningTasksCount, value);
    }

    /// <summary>
    /// Gets the number of failed tasks.
    /// </summary>
    public int FailedTasksCount
    {
        get => _failedTasksCount;
        private set => this.RaiseAndSetIfChanged(ref _failedTasksCount, value);
    }

    /// <summary>
    /// Gets the average execution time for completed tasks.
    /// </summary>
    public TimeSpan AverageExecutionTime
    {
        get => _averageExecutionTime;
        private set => this.RaiseAndSetIfChanged(ref _averageExecutionTime, value);
    }

    /// <summary>
    /// Gets the current resource utilization summary.
    /// </summary>
    public string ResourceUtilization
    {
        get => _resourceUtilization;
        private set => this.RaiseAndSetIfChanged(ref _resourceUtilization, value);
    }

    // Last update timestamp for UI display
    private DateTime _lastUpdated = DateTime.Now;

    /// <summary>
    /// Gets the timestamp of the last task list update.
    /// </summary>
    /// <remarks>
    /// Shows when the task collections were last refreshed, useful for real-time monitoring.
    /// Updated automatically during refresh operations and task state changes.
    /// </remarks>
    public DateTime LastUpdated
    {
        get => _lastUpdated;
        private set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to start the selected task.
    /// </summary>
    /// <remarks>
    /// Moves a created task to the queued state for immediate execution.
    /// Enabled when a task in Created state is selected.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> StartTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to stop the selected running task.
    /// </summary>
    /// <remarks>
    /// Cancels a running or queued task, moving it to cancelled state.
    /// Enabled when a task in Running, Queued, or Paused state is selected.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> StopTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to schedule the selected task for future execution.
    /// </summary>
    /// <remarks>
    /// Allows scheduling a created task to run at a specific date and time.
    /// Enabled when a task in Created state is selected.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> ScheduleTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to restart a failed or cancelled task.
    /// </summary>
    /// <remarks>
    /// Creates a new task execution from a failed or cancelled task configuration.
    /// Enabled when a task in Failed or Cancelled state is selected.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> RestartTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to pause the selected running task.
    /// </summary>
    /// <remarks>
    /// Temporarily pauses a running task, allowing it to be resumed later.
    /// Enabled when a task in Running state is selected and supports pausing.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> PauseTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to resume a paused task.
    /// </summary>
    /// <remarks>
    /// Resumes execution of a previously paused task.
    /// Enabled when a task in Paused state is selected.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> ResumeTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to delete the selected finished task.
    /// </summary>
    /// <remarks>
    /// Removes a completed, failed, or cancelled task from the task history.
    /// Enabled when a task in a terminal state is selected.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> DeleteTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to refresh all task collections from the scheduler.
    /// </summary>
    /// <remarks>
    /// Manually refreshes task data to ensure UI is synchronized with scheduler state.
    /// Always enabled and useful when auto-refresh is disabled.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> RefreshTasksCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to clear all finished tasks from the collections.
    /// </summary>
    /// <remarks>
    /// Removes all tasks in terminal states (completed, failed, cancelled) after confirmation.
    /// Helps manage task history size and UI performance.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> ClearFinishedTasksCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to create a new task from a job profile.
    /// </summary>
    /// <remarks>
    /// Opens job selection dialog and creates a new task from the selected job.
    /// Always enabled and provides the primary entry point for task creation.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> CreateTaskCommand { get; private set; } = null!;

    #endregion

    #region Private Implementation

    private void SetupCommands()
    {
        // Selection-dependent commands with proper validation
        IObservable<bool> hasSelectedTask = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task != null);

        IObservable<bool> canStart = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task?.State == TaskState.Created);

        IObservable<bool> canStop = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task?.CanCancel == true);

        IObservable<bool> canSchedule = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task?.State == TaskState.Created);

        IObservable<bool> canRestart = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task?.CanRestart == true);

        IObservable<bool> canPause = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task?.State == TaskState.Running);

        IObservable<bool> canResume = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task?.State == TaskState.Paused);

        IObservable<bool> canDelete = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task?.IsTerminal == true);

        IObservable<bool> hasFinishedTasks = this.WhenAnyValue(x => x.FinishedTasks.Count)
            .Select(count => count > 0);

        // Create commands with proper async patterns
        StartTaskCommand = ReactiveCommand.CreateFromTask(ExecuteStartTaskAsync, canStart);
        StopTaskCommand = ReactiveCommand.CreateFromTask(ExecuteStopTaskAsync, canStop);
        ScheduleTaskCommand = ReactiveCommand.CreateFromTask(ExecuteScheduleTaskAsync, canSchedule);
        RestartTaskCommand = ReactiveCommand.CreateFromTask(ExecuteRestartTaskAsync, canRestart);
        PauseTaskCommand = ReactiveCommand.CreateFromTask(ExecutePauseTaskAsync, canPause);
        ResumeTaskCommand = ReactiveCommand.CreateFromTask(ExecuteResumeTaskAsync, canResume);
        DeleteTaskCommand = ReactiveCommand.CreateFromTask(ExecuteDeleteTaskAsync, canDelete);
        RefreshTasksCommand = ReactiveCommand.CreateFromTask(ExecuteRefreshTasksAsync);
        ClearFinishedTasksCommand = ReactiveCommand.CreateFromTask(ExecuteClearFinishedTasksAsync, hasFinishedTasks);
        CreateTaskCommand = ReactiveCommand.CreateFromTask(ExecuteCreateTaskAsync);

        // Subscribe to command execution for logging
        StartTaskCommand.Subscribe(_ => _logger.LogDebug("Start task command executed for task {TaskId}", SelectedTask?.TaskId)).DisposeWith(_disposables);
        StopTaskCommand.Subscribe(_ => _logger.LogDebug("Stop task command executed for task {TaskId}", SelectedTask?.TaskId)).DisposeWith(_disposables);
        ScheduleTaskCommand.Subscribe(_ => _logger.LogDebug("Schedule task command executed for task {TaskId}", SelectedTask?.TaskId)).DisposeWith(_disposables);
        RestartTaskCommand.Subscribe(_ => _logger.LogDebug("Restart task command executed for task {TaskId}", SelectedTask?.TaskId)).DisposeWith(_disposables);
    }

    private void SetupCollections()
    {
        // Track collection changes for statistics updates
        CreatedTasks.CollectionChanged += OnTaskCollectionChanged;
        QueuedTasks.CollectionChanged += OnTaskCollectionChanged;
        ScheduledTasks.CollectionChanged += OnTaskCollectionChanged;
        ActiveTasks.CollectionChanged += OnTaskCollectionChanged;
        FinishedTasks.CollectionChanged += OnTaskCollectionChanged;
    }

    private void SetupAutoRefresh()
    {
        // Create auto-refresh timer that respects the enabled flag and interval
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
            .Where(_ => IsAutoRefreshEnabled)
            .Sample(this.WhenAnyValue(x => x.RefreshIntervalSeconds).Select(interval => TimeSpan.FromSeconds(interval)))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(async _ =>
            {
                try
                {
                    await LoadTasksAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Auto-refresh failed for Task Manager");
                }
            })
            .DisposeWith(_disposables);
    }

    private void SubscribeToTaskEvents()
    {
        // Subscribe to task state changes from the scheduler for real-time updates
        _taskScheduler.TaskStateChanged += OnTaskStateChanged;
    }

    private void OnTaskStateChanged(TaskExecution taskExecution)
    {
        // Update UI on the UI thread when task state changes
        _ = _uiThreadService.InvokeOnUIThreadAsync(async () =>
        {
            try
            {
                // Refresh tasks to ensure UI is synchronized
                await LoadTasksAsync().ConfigureAwait(false);

                _logger.LogDebug("Task state changed: {TaskId} -> {State}", taskExecution.TaskId, taskExecution.State);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle task state change for task {TaskId}", taskExecution.TaskId);
            }
        });
    }

    private void OnTaskCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update statistics when collections change
        UpdateStatistics();
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            // Get all tasks from the scheduler
            IReadOnlyCollection<TaskExecution> allTasks = await _taskScheduler.GetAllTasksAsync().ConfigureAwait(false);

            // Group tasks by state
            ILookup<TaskDisplayState, IEnumerable<TaskExecution>> tasksByState = allTasks.GroupBy(t => GetTaskDisplayState(t.State)).ToLookup(g => g.Key, g => g.AsEnumerable());

            // Update collections on UI thread
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                UpdateCollection(CreatedTasks, tasksByState[TaskDisplayState.Created].SelectMany(x => x));
                UpdateCollection(QueuedTasks, tasksByState[TaskDisplayState.Queued].SelectMany(x => x));
                UpdateCollection(ScheduledTasks, tasksByState[TaskDisplayState.Scheduled].SelectMany(x => x));
                UpdateCollection(ActiveTasks, tasksByState[TaskDisplayState.Active].SelectMany(x => x));
                UpdateCollection(FinishedTasks, tasksByState[TaskDisplayState.Finished].SelectMany(x => x));

                UpdateStatistics();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tasks for Task Manager");
            throw;
        }
    }

    private static TaskDisplayState GetTaskDisplayState(TaskState state)
    {
        return state switch
        {
            TaskState.Created => TaskDisplayState.Created,
            TaskState.Queued => TaskDisplayState.Queued,
            TaskState.Scheduled => TaskDisplayState.Scheduled,
            TaskState.Running or TaskState.Paused => TaskDisplayState.Active,
            TaskState.Completed or TaskState.Failed or TaskState.Cancelled => TaskDisplayState.Finished,
            _ => TaskDisplayState.Finished
        };
    }

    private static void UpdateCollection(ObservableCollection<TaskExecution> collection, IEnumerable<TaskExecution> newItems)
    {
        // Clear and repopulate collection to ensure proper synchronization
        collection.Clear();
        foreach (TaskExecution? item in newItems.OrderByDescending(t => t.CreatedAt))
        {
            collection.Add(item);
        }
    }

    private void UpdateStatistics()
    {
        int allTasksCount = CreatedTasks.Count + QueuedTasks.Count + ScheduledTasks.Count + ActiveTasks.Count + FinishedTasks.Count;
        TotalTasksCount = allTasksCount;
        RunningTasksCount = ActiveTasks.Count(t => t.State == TaskState.Running);
        FailedTasksCount = FinishedTasks.Count(t => t.State == TaskState.Failed);

        // Calculate average execution time for completed tasks
        var completedTasks = FinishedTasks.Where(t => t.State == TaskState.Completed && t.ExecutionTime.HasValue).ToList();
        if (completedTasks.Count > 0)
        {
            double totalTime = completedTasks.Sum(t => t.ExecutionTime!.Value.TotalMilliseconds);
            AverageExecutionTime = TimeSpan.FromMilliseconds(totalTime / completedTasks.Count);
        }
        else
        {
            AverageExecutionTime = TimeSpan.Zero;
        }

        // Update resource utilization summary
        int activeResourceCount = ActiveTasks.SelectMany(t => t.LockedResources).Distinct().Count();
        ResourceUtilization = $"{activeResourceCount} resources in use";
    }

    #endregion

    #region Command Implementations

    private async Task ExecuteStartTaskAsync()
    {
        if (SelectedTask == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Starting task...";

            bool success = await _taskScheduler.EnqueueTaskAsync(SelectedTask.TaskId).ConfigureAwait(false);

            if (success)
            {
                StatusMessage = $"Task '{SelectedTask.JobName}' started successfully";
                _logger.LogInformation("Started task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
            else
            {
                StatusMessage = $"Failed to start task '{SelectedTask.JobName}'";
                _logger.LogWarning("Failed to start task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error starting task: {ex.Message}";
            _logger.LogError(ex, "Error starting task {TaskId}", SelectedTask?.TaskId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteStopTaskAsync()
    {
        if (SelectedTask == null)
        {
            return;
        }

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Stop Task",
            $"Are you sure you want to stop the task '{SelectedTask.JobName}'?").ConfigureAwait(false);

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Stopping task...";

            bool success = await _taskScheduler.CancelTaskAsync(SelectedTask.TaskId).ConfigureAwait(false);

            if (success)
            {
                StatusMessage = $"Task '{SelectedTask.JobName}' stopped successfully";
                _logger.LogInformation("Stopped task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
            else
            {
                StatusMessage = $"Failed to stop task '{SelectedTask.JobName}'";
                _logger.LogWarning("Failed to stop task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error stopping task: {ex.Message}";
            _logger.LogError(ex, "Error stopping task {TaskId}", SelectedTask?.TaskId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteScheduleTaskAsync()
    {
        if (SelectedTask == null)
        {
            return;
        }

        try
        {
            // TODO: Show schedule dialog to get date/time
            // For now, schedule for 5 minutes from now as a placeholder
            DateTime scheduledTime = DateTime.Now.AddMinutes(5);

            IsLoading = true;
            StatusMessage = "Scheduling task...";

            bool success = await _taskScheduler.ScheduleTaskAsync(SelectedTask.TaskId, scheduledTime).ConfigureAwait(false);

            if (success)
            {
                StatusMessage = $"Task '{SelectedTask.JobName}' scheduled for {scheduledTime:HH:mm}";
                _logger.LogInformation("Scheduled task {TaskId} ({JobName}) for {ScheduledTime}",
                    SelectedTask.TaskId, SelectedTask.JobName, scheduledTime);
            }
            else
            {
                StatusMessage = $"Failed to schedule task '{SelectedTask.JobName}'";
                _logger.LogWarning("Failed to schedule task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scheduling task: {ex.Message}";
            _logger.LogError(ex, "Error scheduling task {TaskId}", SelectedTask?.TaskId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteRestartTaskAsync()
    {
        if (SelectedTask == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Restarting task...";

            TaskExecution? restartedTask = await _taskScheduler.RestartTaskAsync(SelectedTask.TaskId).ConfigureAwait(false);

            if (restartedTask != null)
            {
                StatusMessage = $"Task '{SelectedTask.JobName}' restarted successfully";
                _logger.LogInformation("Restarted task {TaskId} ({JobName}) as {NewTaskId}",
                    SelectedTask.TaskId, SelectedTask.JobName, restartedTask.TaskId);

                // Select the new task
                SelectedTask = restartedTask;
            }
            else
            {
                StatusMessage = $"Failed to restart task '{SelectedTask.JobName}'";
                _logger.LogWarning("Failed to restart task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error restarting task: {ex.Message}";
            _logger.LogError(ex, "Error restarting task {TaskId}", SelectedTask?.TaskId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecutePauseTaskAsync()
    {
        if (SelectedTask == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Pausing task...";

            bool success = await _taskScheduler.PauseTaskAsync(SelectedTask.TaskId).ConfigureAwait(false);

            if (success)
            {
                StatusMessage = $"Task '{SelectedTask.JobName}' paused successfully";
                _logger.LogInformation("Paused task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
            else
            {
                StatusMessage = $"Failed to pause task '{SelectedTask.JobName}'";
                _logger.LogWarning("Failed to pause task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pausing task: {ex.Message}";
            _logger.LogError(ex, "Error pausing task {TaskId}", SelectedTask?.TaskId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteResumeTaskAsync()
    {
        if (SelectedTask == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Resuming task...";

            bool success = await _taskScheduler.ResumeTaskAsync(SelectedTask.TaskId).ConfigureAwait(false);

            if (success)
            {
                StatusMessage = $"Task '{SelectedTask.JobName}' resumed successfully";
                _logger.LogInformation("Resumed task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
            else
            {
                StatusMessage = $"Failed to resume task '{SelectedTask.JobName}'";
                _logger.LogWarning("Failed to resume task {TaskId} ({JobName})", SelectedTask.TaskId, SelectedTask.JobName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resuming task: {ex.Message}";
            _logger.LogError(ex, "Error resuming task {TaskId}", SelectedTask?.TaskId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteDeleteTaskAsync()
    {
        if (SelectedTask == null)
        {
            return;
        }

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Task",
            $"Are you sure you want to delete the task '{SelectedTask.JobName}'? This will remove it from the task history.").ConfigureAwait(false);

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting task...";

            // Use cleanup method to remove old finished tasks
            // Since there's no direct delete method, we'll just mark it and let cleanup handle it
            StatusMessage = $"Task '{SelectedTask.JobName}' will be removed during next cleanup";
            _logger.LogInformation("Marked task {TaskId} ({JobName}) for cleanup", SelectedTask.TaskId, SelectedTask.JobName);
            SelectedTask = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting task: {ex.Message}";
            _logger.LogError(ex, "Error deleting task {TaskId}", SelectedTask?.TaskId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteRefreshTasksAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing tasks...";

            await LoadTasksAsync().ConfigureAwait(false);

            StatusMessage = $"Refreshed {TotalTasksCount} tasks";
            _logger.LogDebug("Manually refreshed Task Manager with {TaskCount} tasks", TotalTasksCount);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing tasks: {ex.Message}";
            _logger.LogError(ex, "Error refreshing tasks in Task Manager");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteClearFinishedTasksAsync()
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Clear Finished Tasks",
            $"Are you sure you want to clear all {FinishedTasks.Count} finished tasks? This will remove them from the task history.").ConfigureAwait(false);

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Clearing finished tasks...";

            // Use cleanup method to remove old tasks
            int clearedCount = await _taskScheduler.CleanupOldTasksAsync(TimeSpan.Zero).ConfigureAwait(false);

            StatusMessage = $"Cleared {clearedCount} finished tasks";
            _logger.LogInformation("Cleared {ClearedCount} finished tasks from Task Manager", clearedCount);

            if (SelectedTask?.IsTerminal == true)
            {
                SelectedTask = null;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error clearing finished tasks: {ex.Message}";
            _logger.LogError(ex, "Error clearing finished tasks in Task Manager");
        }
        finally
        {
            IsLoading = false;
        }
    }    private async Task ExecuteCreateTaskAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating new task...";

            // TODO: Show job selection dialog
            // For now, create from the first available job as a placeholder
            IEnumerable<JobProfile> jobs = await _jobManager.GetAllAsync().ConfigureAwait(false);
            JobProfile? firstJob = jobs.FirstOrDefault();

            if (firstJob == null)
            {
                StatusMessage = "No job profiles available. Please create a job profile first.";
                return;
            }

            TaskExecution newTask = await _taskScheduler.CreateTaskAsync(firstJob).ConfigureAwait(false);

            StatusMessage = $"Created new task '{newTask.JobName}' from job '{firstJob.Name}'";
            _logger.LogInformation("Created new task {TaskId} from job {JobId} ({JobName})",
                newTask.TaskId, firstJob.Id, firstJob.Name);

            // Select the new task
            SelectedTask = newTask;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating task: {ex.Message}";
            _logger.LogError(ex, "Error creating new task in Task Manager");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="TaskManagerViewModel"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _taskScheduler.TaskStateChanged -= OnTaskStateChanged;

            CreatedTasks.CollectionChanged -= OnTaskCollectionChanged;
            QueuedTasks.CollectionChanged -= OnTaskCollectionChanged;
            ScheduledTasks.CollectionChanged -= OnTaskCollectionChanged;
            ActiveTasks.CollectionChanged -= OnTaskCollectionChanged;
            FinishedTasks.CollectionChanged -= OnTaskCollectionChanged;

            _disposables?.Dispose();
        }
    }

    #endregion
}

/// <summary>
/// Internal enumeration for organizing tasks by display state in the UI.
/// </summary>
internal enum TaskDisplayState
{
    /// <summary>
    /// Tasks that have been created but not yet queued.
    /// </summary>
    Created,

    /// <summary>
    /// Tasks that are waiting in the execution queue.
    /// </summary>
    Queued,

    /// <summary>
    /// Tasks that are scheduled to run at a future time.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Tasks that are currently running or paused.
    /// </summary>
    Active,

    /// <summary>
    /// Tasks that have completed, failed, or been cancelled.
    /// </summary>
    Finished
}
