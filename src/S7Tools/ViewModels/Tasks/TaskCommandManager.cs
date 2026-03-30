using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Extensions;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Tasks;

/// <summary>
/// Encapsulates the execution logic for Task Manager commands.
/// Handles interactions with the scheduler, dialogs, and logging, returning operation results to the ViewModel.
/// </summary>
public class TaskCommandManager
{
    private readonly ILogger<TaskCommandManager> _logger;
    private readonly ITaskScheduler _taskScheduler;
    private readonly IJobManager _jobManager;
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCommandManager"/> class.
    /// </summary>
    public TaskCommandManager(
        ILogger<TaskCommandManager> logger,
        ITaskScheduler taskScheduler,
        IJobManager jobManager,
        IDialogService dialogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <summary>
    /// Executes the StartTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> StartTaskAsync(TaskExecution task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.State is not (TaskState.Created or TaskState.Scheduled))
        {
            _logger.LogWarning("Cannot start task {TaskId} - current state is {State}", task.TaskId, task.State);
            return CommandResult.Failure($"Cannot start task '{task.JobName}' - task is in '{task.State}' state (must be Created or Scheduled)");
        }

        try
        {
            _logger.LogInformation("Starting task {TaskId} ({JobName})", task.TaskId, task.JobName);
            bool success = await _taskScheduler.EnqueueTaskAsync(task.TaskId).ConfigureAwait(false);

            if (success)
            {
                return CommandResult.Success($"Task '{task.JobName}' queued for execution");
            }
            else
            {
                return CommandResult.Failure($"Failed to start task '{task.JobName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting task {TaskId}", task.TaskId);
            return CommandResult.Failure($"Error starting task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the StopTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> StopTaskAsync(TaskExecution task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.CanCancel)
        {
            _logger.LogWarning("Cannot stop task {TaskId} - current state is {State}", task.TaskId, task.State);
            return CommandResult.Failure($"Cannot stop task '{task.JobName}' - task is in '{task.State}' state");
        }

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Stop Task",
            $"Are you sure you want to stop the task '{task.JobName}'?").ConfigureAwait(false);

        if (!confirmed)
        {
            return CommandResult.Cancelled();
        }

        try
        {
            _logger.LogInformation("Stopping task {TaskId} ({JobName})", task.TaskId, task.JobName);
            bool success = await _taskScheduler.CancelTaskAsync(task.TaskId).ConfigureAwait(false);

            if (success)
            {
                return CommandResult.Success($"Task '{task.JobName}' stop requested");
            }
            else
            {
                return CommandResult.Failure($"Failed to stop task '{task.JobName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping task {TaskId}", task.TaskId);
            return CommandResult.Failure($"Error stopping task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the PauseTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> PauseTaskAsync(TaskExecution task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.State != TaskState.Running)
        {
            return CommandResult.Failure($"Cannot pause task '{task.JobName}' - task is in '{task.State}' state (must be Running)");
        }

        try
        {
            _logger.LogInformation("Pausing task {TaskId} ({JobName})", task.TaskId, task.JobName);
            bool success = await _taskScheduler.PauseTaskAsync(task.TaskId).ConfigureAwait(false);

            if (success)
            {
                return CommandResult.Success($"Task '{task.JobName}' paused");
            }
            else
            {
                return CommandResult.Failure($"Failed to pause task '{task.JobName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing task {TaskId}", task.TaskId);
            return CommandResult.Failure($"Error pausing task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the ResumeTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> ResumeTaskAsync(TaskExecution task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.State != TaskState.Paused)
        {
            return CommandResult.Failure($"Cannot resume task '{task.JobName}' - task is in '{task.State}' state (must be Paused)");
        }

        try
        {
            _logger.LogInformation("Resuming task {TaskId} ({JobName})", task.TaskId, task.JobName);
            bool success = await _taskScheduler.ResumeTaskAsync(task.TaskId).ConfigureAwait(false);

            if (success)
            {
                return CommandResult.Success($"Task '{task.JobName}' resumed");
            }
            else
            {
                return CommandResult.Failure($"Failed to resume task '{task.JobName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming task {TaskId}", task.TaskId);
            return CommandResult.Failure($"Error resuming task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the RestartTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> RestartTaskAsync(TaskExecution task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.CanRestart)
        {
            return CommandResult.Failure($"Cannot restart task '{task.JobName}' - task is in '{task.State}' state");
        }

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Restart Task",
            $"Are you sure you want to restart the task '{task.JobName}'?").ConfigureAwait(false);

        if (!confirmed)
        {
            return CommandResult.Cancelled();
        }

        try
        {
            _logger.LogInformation("Restarting task {TaskId} ({JobName})", task.TaskId, task.JobName);

            // Get the job profile first
            var jobProfile = await _jobManager.GetByIdAsync(task.JobProfileId).ConfigureAwait(false);
            if (jobProfile == null)
            {
                return CommandResult.Failure($"Failed to find job profile for task '{task.JobName}'");
            }

            var newTask = await _taskScheduler.CreateTaskAsync(jobProfile).ConfigureAwait(false);

            if (newTask != null)
            {
                // Auto-start the restarted task
                await _taskScheduler.EnqueueTaskAsync(newTask.TaskId).ConfigureAwait(false);
                return CommandResult.Success($"Task '{task.JobName}' restarted", newTask);
            }
            else
            {
                return CommandResult.Failure($"Failed to recreate task '{task.JobName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting task {TaskId}", task.TaskId);
            return CommandResult.Failure($"Error restarting task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the DeleteTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> DeleteTaskAsync(TaskExecution task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.IsTerminal)
        {
            return CommandResult.Failure($"Cannot delete task '{task.JobName}' - task is running or active");
        }

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Task",
            $"Are you sure you want to delete the task '{task.JobName}'?").ConfigureAwait(false);

        if (!confirmed)
        {
            return CommandResult.Cancelled();
        }

        try
        {
            _logger.LogInformation("Deleting task {TaskId} ({JobName})", task.TaskId, task.JobName);
            bool success = await _taskScheduler.RemoveTaskAsync(task.TaskId).ConfigureAwait(false);

            if (success)
            {
                return CommandResult.Success($"Task '{task.JobName}' deleted");
            }
            else
            {
                return CommandResult.Failure($"Failed to delete task '{task.JobName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", task.TaskId);
            return CommandResult.Failure($"Error deleting task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the ScheduleTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> ScheduleTaskAsync(TaskExecution task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.State != TaskState.Created)
        {
            return CommandResult.Failure($"Cannot schedule task '{task.JobName}' - task is in '{task.State}' state");
        }

        // Show input dialog for delay
        var result = await _dialogService.ShowInputAsync(
            "Schedule Task",
            "Enter delay in minutes (or specific time like '12:30'):",
            "5").ConfigureAwait(false);

        if (result.IsCancelled || string.IsNullOrWhiteSpace(result.Value))
        {
            return CommandResult.Cancelled();
        }

        DateTime? scheduleTime = ParseScheduleTime(result.Value);

        if (scheduleTime == null)
        {
            await _dialogService.ShowErrorAsync("Invalid Time", "Please enter a valid number of minutes or a time format (HH:mm)").ConfigureAwait(false);
            return CommandResult.Cancelled(); // Or Failure, but we already showed error
        }

        try
        {
            _logger.LogInformation("Scheduling task {TaskId} for {Time}", task.TaskId, scheduleTime.Value);
            bool success = await _taskScheduler.ScheduleTaskAsync(task.TaskId, scheduleTime.Value).ConfigureAwait(false);

            if (success)
            {
                return CommandResult.Success($"Task '{task.JobName}' scheduled for {scheduleTime.Value:t}");
            }
            else
            {
                return CommandResult.Failure($"Failed to schedule task '{task.JobName}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling task {TaskId}", task.TaskId);
            return CommandResult.Failure($"Error scheduling task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the CreateTaskAsync operation.
    /// </summary>
    public async Task<CommandResult> CreateTaskAsync()
    {
        try
        {
            // Show job selection dialog
            var selectedJob = await _dialogService.ShowJobSelectionAsync().ConfigureAwait(false);

            if (selectedJob == null)
            {
                return CommandResult.Cancelled();
            }

            // Create task from job
            var newTask = await _taskScheduler.CreateTaskAsync(selectedJob).ConfigureAwait(false);

            if (newTask != null)
            {
                _logger.LogInformation("Created new task {TaskId} for job {JobName}", newTask.TaskId, selectedJob.Name);
                return CommandResult.Success($"Created new task for job '{selectedJob.Name}'");
            }
            else
            {
                return CommandResult.Failure($"Failed to create task for job '{selectedJob.Name}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return CommandResult.Failure($"Error creating task: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the ClearFinishedTasksAsync operation.
    /// </summary>
    public async Task<CommandResult> ClearFinishedTasksAsync()
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Clear Finished Tasks",
            "Are you sure you want to remove all finished tasks?").ConfigureAwait(false);

        if (!confirmed)
        {
            return CommandResult.Cancelled();
        }

        try
        {
            int count = await _taskScheduler.ClearFinishedTasksAsync().ConfigureAwait(false);
            _logger.LogInformation("Cleared {Count} finished tasks", count);
            return CommandResult.Success($"Cleared {count} finished tasks");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing finished tasks");
            return CommandResult.Failure($"Error clearing tasks: {ex.Message}");
        }
    }

    private DateTime? ParseScheduleTime(string input)
    {
        // Try parsing as minutes offset
        if (int.TryParse(input, out int minutes) && minutes > 0)
        {
            return DateTime.UtcNow.AddMinutes(minutes); // Scheduler uses UTC? TaskManagerViewModel uses Local. 
            // NOTE: The user input is likely expecting "minutes from now".
            // If the scheduler expects specific kind, we should be careful.
            // Assuming scheduler handles DateTimeOffset or UTC DateTime.
        }

        // Try parsing as specific time today
        if (TimeSpan.TryParse(input, out TimeSpan time))
        {
            DateTime now = DateTime.UtcNow.ToLocalTime(); // User's local time
            DateTime scheduled = now.Date.Add(time);

            if (scheduled < now)
            {
                scheduled = scheduled.AddDays(1);
            }

            return scheduled.ToUniversalTime();
        }

        return null;
    }
}

/// <summary>
/// Represents the struct.
/// </summary>
public readonly record struct CommandResult
{
    /// <summary>
    /// Gets or sets the IsSuccess.
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// Gets or sets the IsCancelled.
    /// </summary>
    public bool IsCancelled { get; }
    /// <summary>
    /// Gets or sets the Message.
    /// </summary>
    public string Message { get; }
    /// <summary>
    /// Gets or sets the Data.
    /// </summary>
    public object? Data { get; }

    private CommandResult(bool isSuccess, bool isCancelled, string message, object? data = null)
    {
        IsSuccess = isSuccess;
        IsCancelled = isCancelled;
        Message = message;
        Data = data;
    }

    /// <summary>
    /// Executes the Success operation.
    /// </summary>
    public static CommandResult Success(string message, object? data = null) => new(true, false, message, data);
    /// <summary>
    /// Executes the Failure operation.
    /// </summary>
    public static CommandResult Failure(string message) => new(false, false, message);
    /// <summary>
    /// Executes the Cancelled operation.
    /// </summary>
    public static CommandResult Cancelled() => new(false, true, string.Empty);
}
