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
using S7Tools.Resources;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Tasks;

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
    private readonly TaskDetailsViewModel _taskDetailsViewModel;
    private readonly TaskStatisticsViewModel _taskStatisticsViewModel;
    private readonly TaskCommandManager _taskCommandManager;
    private readonly CompositeDisposable _disposables = [];

    // State-based task collections for UI binding
    private ObservableCollection<TaskExecution> _createdTasks = [];
    private ObservableCollection<TaskExecution> _queuedTasks = [];
    private ObservableCollection<TaskExecution> _scheduledTasks = [];
    private ObservableCollection<TaskExecution> _activeTasks = [];
    private ObservableCollection<TaskExecution> _finishedTasks = [];

    // Current selection and UI state
    private TaskExecution _selectedTask = TaskExecution.Empty;
    private bool _isLoading;
    private string? _statusMessage;
    private bool _isAutoRefreshEnabled = true;
    private int _refreshIntervalSeconds = 2;

    // Throttling for UI updates
    private readonly System.Reactive.Subjects.ISubject<TaskExecution> _taskStateChangedSubject = new System.Reactive.Subjects.Subject<TaskExecution>();

    private int _selectedTabIndex;
    private bool _isTaskDetailsPanelExpanded = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskManagerViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger for this view model.</param>
    /// <param name="taskScheduler">The task scheduler service for task management operations.</param>
    /// <param name="jobManager">The job manager service for accessing job configurations.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    /// <param name="dialogService">The dialog service for user confirmations.</param>
    /// <param name="taskDetailsViewModel">The task details view model for the details panel.</param>
    /// <param name="taskStatisticsViewModel">The view model for task statistics.</param>
    /// <param name="taskCommandManager">The manager for handling task commands.</param>
    public TaskManagerViewModel(
        ILogger<TaskManagerViewModel> logger,
        ITaskScheduler taskScheduler,
        IJobManager jobManager,
        IUIThreadService uiThreadService,
        IDialogService dialogService,
        TaskDetailsViewModel taskDetailsViewModel,
        TaskStatisticsViewModel taskStatisticsViewModel,
        TaskCommandManager taskCommandManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _taskDetailsViewModel = taskDetailsViewModel ?? throw new ArgumentNullException(nameof(taskDetailsViewModel));
        _taskStatisticsViewModel = taskStatisticsViewModel ?? throw new ArgumentNullException(nameof(taskStatisticsViewModel));
        _taskCommandManager = taskCommandManager ?? throw new ArgumentNullException(nameof(taskCommandManager));

        SetupCommands();
        SetupCollections();
        SetupAutoRefresh();
        SubscribeToTaskEvents();

        // Wire up task selection to update details view
        this.WhenAnyValue(x => x.SelectedTask)
            .Subscribe(task => _taskDetailsViewModel.TaskExecution = task)
            .DisposeWith(_disposables);

        // Initialize with current tasks - with retry logic for reliability
        _ = Task.Run(async () =>
        {
            try
            {
                // Small delay to ensure scheduler is fully initialized
                await Task.Delay(100).ConfigureAwait(false);

                await LoadTasksAsync().ConfigureAwait(false);
                _logger.LogInformation("Task Manager initialized with {TaskCount} tasks", Statistics.TotalTasksCount);

                // If no tasks loaded, try one more time after a longer delay
                // This handles cases where the scheduler is still initializing
                if (Statistics.TotalTasksCount == 0)
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    await LoadTasksAsync().ConfigureAwait(false);
                    _logger.LogInformation("Task Manager retry load completed with {TaskCount} tasks", Statistics.TotalTasksCount);
                }
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
    /// Gets the task details view model for the details panel.
    /// </summary>
    public TaskDetailsViewModel TaskDetailsViewModel => _taskDetailsViewModel;

    /// <summary>
    /// Gets the statistics view model.
    /// </summary>
    public TaskStatisticsViewModel Statistics => _taskStatisticsViewModel;

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
    /// Gets a combined collection of all actionable tasks (Created, Queued, and Active).
    /// </summary>
    /// <remarks>
    /// Provides a unified view of tasks that need user attention or are in progress.
    /// This includes newly created tasks waiting to start, queued tasks, and actively running tasks.
    /// Used for the main task grid view to show all tasks that aren't finished.
    /// </remarks>
    public IEnumerable<TaskExecution> AllActionableTasks => CreatedTasks
        .Concat(QueuedTasks)
        .Concat(ScheduledTasks)
        .Concat(ActiveTasks)
        .OrderBy(t => t.CreatedAt);

    /// <summary>
    /// Gets or sets the currently selected task across all collections.
    /// </summary>
    /// <remarks>
    /// Drives the enable/disable state of task operation commands.
    /// Used for displaying detailed task information and progress.
    /// </remarks>
    /// <summary>
    /// Gets or sets the currently selected task.
    /// Never null; uses TaskExecution.Empty when no task is selected.
    /// </summary>
    public TaskExecution SelectedTask
    {
        get => _selectedTask;
        set => this.RaiseAndSetIfChanged(ref _selectedTask, value ?? TaskExecution.Empty);
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
    /// Gets or sets the selected tab index in the TaskManagerView.
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, Math.Clamp(value, 0, 2));
    }

    // Last update timestamp for UI display
    private DateTime _lastUpdated = DateTime.UtcNow.ToLocalTime();

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

    /// <summary>
    /// Gets or sets a value indicating whether the task details panel is expanded.
    /// </summary>
    /// <remarks>
    /// Controls the visibility state of the collapsible task details panel.
    /// Defaults to true (expanded) for immediate visibility of task information.
    /// </remarks>
    public bool IsTaskDetailsPanelExpanded
    {
        get => _isTaskDetailsPanelExpanded;
        set => this.RaiseAndSetIfChanged(ref _isTaskDetailsPanelExpanded, value);
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
    public ReactiveCommand<TaskExecution?, Unit> StartTaskCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to stop the selected running task.
    /// </summary>
    /// <remarks>
    /// Cancels a running or queued task, moving it to cancelled state.
    /// Enabled when a task in Running, Queued, or Paused state is selected.
    /// </remarks>
    public ReactiveCommand<TaskExecution?, Unit> StopTaskCommand { get; private set; } = null!;

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
    public ReactiveCommand<TaskExecution?, Unit> RestartTaskCommand { get; private set; } = null!;

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
            .Select(task => task.TaskId != Guid.Empty);

        IObservable<bool> canStart = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task.TaskId != Guid.Empty && task.State == TaskState.Created);

        IObservable<bool> canStop = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task.TaskId != Guid.Empty && task.CanCancel);

        IObservable<bool> canSchedule = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task.TaskId != Guid.Empty && task.State == TaskState.Created);

        IObservable<bool> canRestart = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task.TaskId != Guid.Empty && task.CanRestart);

        IObservable<bool> canPause = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task.TaskId != Guid.Empty && task.State == TaskState.Running);

        IObservable<bool> canResume = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task.TaskId != Guid.Empty && task.State == TaskState.Paused);

        IObservable<bool> canDelete = this.WhenAnyValue(x => x.SelectedTask)
            .Select(task => task.TaskId != Guid.Empty && task.IsTerminal);

        IObservable<bool> hasFinishedTasks = this.WhenAnyValue(x => x.FinishedTasks.Count)
            .Select(count => count > 0);

        // Create commands with proper async patterns
        // For commands that accept TaskExecution? parameter (used from DataGrid buttons),
        // we don't use CanExecute observables because the validation happens inside the command
        // based on the passed parameter, not SelectedTask
        StartTaskCommand = ReactiveCommand.CreateFromTask<TaskExecution?>(ExecuteStartTaskAsync);
        StopTaskCommand = ReactiveCommand.CreateFromTask<TaskExecution?>(ExecuteStopTaskAsync);
        ScheduleTaskCommand = ReactiveCommand.CreateFromTask(ExecuteScheduleTaskAsync, canSchedule);
        RestartTaskCommand = ReactiveCommand.CreateFromTask<TaskExecution?>(ExecuteRestartTaskAsync);
        PauseTaskCommand = ReactiveCommand.CreateFromTask(ExecutePauseTaskAsync, canPause);
        ResumeTaskCommand = ReactiveCommand.CreateFromTask(ExecuteResumeTaskAsync, canResume);
        DeleteTaskCommand = ReactiveCommand.CreateFromTask(ExecuteDeleteTaskAsync, canDelete);
        RefreshTasksCommand = ReactiveCommand.CreateFromTask(ExecuteRefreshTasksAsync);
        ClearFinishedTasksCommand = ReactiveCommand.CreateFromTask(ExecuteClearFinishedTasksAsync, hasFinishedTasks);
        CreateTaskCommand = ReactiveCommand.CreateFromTask(ExecuteCreateTaskAsync);

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

    // Throttling for progress updates
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, (double Percentage, string Operation, Dictionary<string, object>? ExtraData)> _pendingProgressUpdates = new();
    private IDisposable? _progressUpdateTimer;

    private void SubscribeToTaskEvents()
    {
        // Subscribe to task state changes from the scheduler for real-time updates
        _taskScheduler.TaskStateChanged += OnTaskStateChanged;
        // Subscribe to progress updates
        _taskScheduler.TaskProgressUpdated += OnTaskProgressUpdated;

        // Setup throttling for state changes to prevent UI freezing
        _taskStateChangedSubject
            .Sample(TimeSpan.FromMilliseconds(250))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(task =>
            {
                _ = _uiThreadService.InvokeOnUIThreadAsync(async () =>
                {
                    try
                    {
                        await LoadTasksAsync().ConfigureAwait(false);
                        _logger.LogDebug("UI refreshed after throttled task state change");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to refresh UI after task state change");
                    }
                });
            })
            .DisposeWith(_disposables);

        // Setup throttling timer for PROGRESS updates (update UI every 250ms max)
        _progressUpdateTimer = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ProcessPendingProgressUpdates())
            .DisposeWith(_disposables);
    }

    private void OnTaskStateChanged(TaskExecution taskExecution)
    {
        // Push to subject for throttling instead of direct update
        _taskStateChangedSubject.OnNext(taskExecution);
    }

    private void OnTaskProgressUpdated(Guid taskId, double percentage, string operation, Dictionary<string, object>? extraData = null)
    {
        // Buffer the update instead of pushing to UI immediately
        _pendingProgressUpdates[taskId] = (percentage, operation, extraData);
    }

    private void ProcessPendingProgressUpdates()
    {
        if (_pendingProgressUpdates.IsEmpty)
        {
            return;
        }

        // Atomically drain the dictionary to prevent lost updates.
        // We iterate through the keys and try to remove each item.
        // If an item is removed successfully, we process it.
        foreach (var key in _pendingProgressUpdates.Keys)
        {
            if (_pendingProgressUpdates.TryRemove(key, out var update))
            {
                (double percentage, string operation, Dictionary<string, object>? extraData) = update;

                // Find the task in ActiveTasks (most likely) or other collections
                TaskExecution? task = ActiveTasks.FirstOrDefault(t => t.TaskId == key) ??
                           AllActionableTasks.FirstOrDefault(t => t.TaskId == key);

                // Update properties directly
                task?.UpdateProgress(percentage, operation, extraData);
            }
        }
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
                MergeCollection(CreatedTasks, tasksByState[TaskDisplayState.Created].SelectMany(x => x));
                MergeCollection(QueuedTasks, tasksByState[TaskDisplayState.Queued].SelectMany(x => x));
                MergeCollection(ScheduledTasks, tasksByState[TaskDisplayState.Scheduled].SelectMany(x => x));
                MergeCollection(ActiveTasks, tasksByState[TaskDisplayState.Active].SelectMany(x => x));
                MergeCollection(FinishedTasks, tasksByState[TaskDisplayState.Finished].SelectMany(x => x));

                UpdateStatistics();
                LastUpdated = DateTime.UtcNow.ToLocalTime();
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

    /// <summary>
    /// Merges new items into an existing collection, preserving existing instances where possible.
    /// This prevents selection loss and reduces UI flickering.
    /// </summary>
    private static void MergeCollection(ObservableCollection<TaskExecution> collection, IEnumerable<TaskExecution> newItems)
    {
        var newItemsList = newItems.OrderByDescending(t => t.CreatedAt).ToList();
        var newIds = newItemsList.Select(x => x.TaskId).ToHashSet();

        // 1. Remove items that are no longer in the list
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (!newIds.Contains(collection[i].TaskId))
            {
                collection.RemoveAt(i);
            }
        }

        // 2. Add or Update items
        for (int i = 0; i < newItemsList.Count; i++)
        {
            TaskExecution newItem = newItemsList[i];

            // detailed check for matching ID at current position could be optimized, 
            // but simple lookup is safer for now
            TaskExecution? existingItem = collection.FirstOrDefault(x => x.TaskId == newItem.TaskId);

            if (existingItem != null)
            {
                // Update properties of existing item
                if (existingItem.State != newItem.State)
                {
                    existingItem.State = newItem.State;
                }
                if (existingItem.ProgressPercentage != newItem.ProgressPercentage)
                {
                    existingItem.ProgressPercentage = newItem.ProgressPercentage;
                }
                if (existingItem.CurrentOperation != newItem.CurrentOperation)
                {
                    existingItem.CurrentOperation = newItem.CurrentOperation;
                }

                // Ensure correct order (move if needed)
                int currentIdx = collection.IndexOf(existingItem);
                if (currentIdx != i)
                {
                    collection.Move(currentIdx, i);
                }
            }
            else
            {
                // New item, insert at correct position
                collection.Insert(i, newItem);
            }
        }
    }

    private void UpdateStatistics()
    {
        _taskStatisticsViewModel.Update(
            CreatedTasks.Count,
            QueuedTasks.Count,
            ScheduledTasks.Count,
            ActiveTasks,
            FinishedTasks);

        // Notify that AllActionableTasks has changed (since it's computed from multiple collections)
        this.RaisePropertyChanged(nameof(AllActionableTasks));
    }

    #endregion

    #region Command Implementations

    private async Task ExecuteStartTaskAsync(TaskExecution? task)
    {
        TaskExecution? targetTask = task ?? SelectedTask;
        if (targetTask == null || targetTask.TaskId == Guid.Empty)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = UIStrings.Status_StartingTask;

            var result = await _taskCommandManager.StartTaskAsync(targetTask);
            UpdateCommandResult(result);
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "starting task");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteStopTaskAsync(TaskExecution? task)
    {
        TaskExecution? targetTask = task ?? SelectedTask;
        if (targetTask == null)
            return;

        try
        {
            IsLoading = true;
            // No status message here as it might show dialog

            var result = await _taskCommandManager.StopTaskAsync(targetTask);
            UpdateCommandResult(result);
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "stopping task");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteScheduleTaskAsync()
    {
        if (SelectedTask == null)
            return;

        try
        {
            var result = await _taskCommandManager.ScheduleTaskAsync(SelectedTask);
            UpdateCommandResult(result);
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "scheduling task");
        }
    }

    private async Task ExecuteRestartTaskAsync(TaskExecution? task)
    {
        TaskExecution? targetTask = task ?? SelectedTask;
        if (targetTask == null)
            return;

        try
        {
            IsLoading = true;
            var result = await _taskCommandManager.RestartTaskAsync(targetTask);
            UpdateCommandResult(result);
            if (result.IsSuccess)
            {
                // Logic to select new task? Manager doesn't return the new task ID explicitly in Result message
                // But we have auto-refresh.
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "restarting task");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecutePauseTaskAsync()
    {
        if (SelectedTask == null)
            return;

        try
        {
            IsLoading = true;
            var result = await _taskCommandManager.PauseTaskAsync(SelectedTask);
            UpdateCommandResult(result);
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "pausing task");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteResumeTaskAsync()
    {
        if (SelectedTask == null)
            return;

        try
        {
            IsLoading = true;
            var result = await _taskCommandManager.ResumeTaskAsync(SelectedTask);
            UpdateCommandResult(result);
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "resuming task");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteDeleteTaskAsync()
    {
        if (SelectedTask == null)
            return;

        try
        {
            IsLoading = true;
            // StatusMessage = UIStrings.Status_DeletingTask; // Handled by Result update or inside if we passed context

            var result = await _taskCommandManager.DeleteTaskAsync(SelectedTask);
            UpdateCommandResult(result);

            if (result.IsSuccess)
            {
                SelectedTask = TaskExecution.Empty;
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "deleting task");
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
            StatusMessage = UIStrings.Status_RefreshingTasks;

            await LoadTasksAsync().ConfigureAwait(false);

            StatusMessage = $"Refreshed {Statistics.TotalTasksCount} tasks";
            _logger.LogDebug("Manually refreshed Task Manager with {TaskCount} tasks", Statistics.TotalTasksCount);
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "refreshing tasks");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteClearFinishedTasksAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _taskCommandManager.ClearFinishedTasksAsync();
            UpdateCommandResult(result);

            if (result.IsSuccess)
            {
                await LoadTasksAsync().ConfigureAwait(false);
                if (SelectedTask?.IsTerminal == true)
                {
                    SelectedTask = TaskExecution.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "clearing finished tasks");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteCreateTaskAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _taskCommandManager.CreateTaskAsync();
            UpdateCommandResult(result);

            // We might want to select the new task if possible, but Manager doesn't return it yet.
            // For now, auto-refresh and user finds it. 
            // Previous code did: SelectedTask = newTask;
            // We can improve this if needed by adding Data to CommandResult.
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "creating task");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCommandResult(CommandResult result)
    {
        if (result.IsCancelled)
            return;

        if (!string.IsNullOrEmpty(result.Message))
        {
            _ = _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = result.Message);
        }
    }

    private void HandleCommandException(Exception ex, string operation)
    {
        _logger.LogError(ex, "Error {Operation} in Task Manager", operation);
        _ = _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = $"Error {operation}: {ex.Message}");
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
            _progressUpdateTimer?.Dispose();
            _taskScheduler.TaskStateChanged -= OnTaskStateChanged;
            _taskScheduler.TaskProgressUpdated -= OnTaskProgressUpdated;

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
