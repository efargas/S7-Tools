using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Helpers;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Tasks;

/// <summary>
/// Wrapper ViewModel for the "History" category. Exposes the shared TaskManagerViewModel as Manager.
/// </summary>
public sealed class HistoryTasksViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IJobManager _jobManager;
    private readonly ILogger<HistoryTasksViewModel> _logger;
    private TaskExecution? _selectedHistoryTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryTasksViewModel"/> class.
    /// </summary>
    public HistoryTasksViewModel(
        TaskManagerViewModel manager,
        IJobManager jobManager,
        ILogger<HistoryTasksViewModel> logger)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SetupCommands();

        // Wire up selected history task to the TaskDetailsViewModel
        this.WhenAnyValue(x => x.SelectedHistoryTask)
            .Subscribe(task => Manager.TaskDetailsViewModel.TaskExecution = task)
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Gets or sets the Manager.
    /// </summary>
    public TaskManagerViewModel Manager { get; }

    /// <summary>
    /// Gets the task details view model for displaying selected task details.
    /// </summary>
    public TaskDetailsViewModel TaskDetailsViewModel => Manager.TaskDetailsViewModel;

    /// <summary>
    /// Gets or sets the currently selected task in the history view.
    /// </summary>
    public TaskExecution? SelectedHistoryTask
    {
        get => _selectedHistoryTask;
        set => this.RaiseAndSetIfChanged(ref _selectedHistoryTask, value);
    }

    /// <summary>
    /// Gets the command to open the logs folder for the selected task.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenLogsFolderCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to open the dumps folder for the selected task.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenDumpsFolderCommand { get; private set; } = null!;

    private void SetupCommands()
    {
        IObservable<bool> hasSelectedTask = this.WhenAnyValue(x => x.SelectedHistoryTask)
            .Select(task => task != null);

        OpenLogsFolderCommand = ReactiveCommand.CreateFromTask(ExecuteOpenLogsFolderAsync, hasSelectedTask);
        OpenDumpsFolderCommand = ReactiveCommand.CreateFromTask(ExecuteOpenDumpsFolderAsync, hasSelectedTask);
    }

    private async Task ExecuteOpenLogsFolderAsync()
    {
        if (SelectedHistoryTask == null || SelectedHistoryTask.TaskId == Guid.Empty)
        {
            _logger.LogWarning("Cannot open logs folder: No task selected");
            return;
        }

        try
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Tasks", SelectedHistoryTask.TaskId.ToString());
            
            if (Directory.Exists(logDirectory))
            {
                await PlatformHelper.OpenDirectoryInExplorerAsync(logDirectory);
                _logger.LogInformation("Opened logs folder: {LogDirectory}", logDirectory);
            }
            else
            {
                _logger.LogWarning("Logs folder does not exist: {LogDirectory}", logDirectory);
                
                // Fallback to parent logs folder
                string mainLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Tasks");
                if (Directory.Exists(mainLogs))
                {
                    await PlatformHelper.OpenDirectoryInExplorerAsync(mainLogs);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open logs folder");
        }
    }

    private async Task ExecuteOpenDumpsFolderAsync()
    {
        if (SelectedHistoryTask == null)
        {
            _logger.LogWarning("Cannot open dumps folder: No task selected");
            return;
        }

        try
        {
            // Get the job profile to retrieve the dump folder path
            JobProfile? jobProfile = await _jobManager.GetByIdAsync(SelectedHistoryTask.JobProfileId);
            if (jobProfile == null)
            {
                _logger.LogWarning("Cannot open dumps folder: Job profile not found for task {TaskId}", SelectedHistoryTask.TaskId);
                return;
            }

            string dumpsDirectory = jobProfile.OutputPath;
            if (!string.IsNullOrEmpty(dumpsDirectory) && Directory.Exists(dumpsDirectory))
            {
                await PlatformHelper.OpenDirectoryInExplorerAsync(dumpsDirectory);
                _logger.LogInformation("Opened dumps folder: {DumpsDirectory}", dumpsDirectory);
            }
            else
            {
                _logger.LogWarning("Dumps folder does not exist: {DumpsDirectory}", dumpsDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open dumps folder");
        }
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
