using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Validation;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Tasks;

/// <summary>
/// ViewModel for displaying and interacting with a selected task's details,
/// including logs, validation results, and manual controls.
/// </summary>
public class TaskDetailsViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<TaskDetailsViewModel> _logger;
    private readonly ISocatService _socatService;
    private readonly IPowerSupplyService _powerSupplyService;
    private readonly IEnhancedBootloaderService _bootloaderService;
    private readonly IUIThreadService _uiThreadService;
    private readonly CompositeDisposable _disposables = new();

    private TaskExecution? _taskExecution;
    private string _mainLogContent = "No main log data";
    private string _processLogContent = "No socat process log data";
    private string _protocolLogContent = "No protocol log data";
    private string _validationResultText = "No validation data";
    private int _selectedTabIndex;
    private bool _canStartSocat = true;
    private bool _canStopSocat;
    private bool _canControlPower = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDetailsViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="socatService">Socat service for manual server control.</param>
    /// <param name="powerSupplyService">Power supply service for manual power control.</param>
    /// <param name="bootloaderService">Bootloader service for validation.</param>
    /// <param name="uiThreadService">UI thread service for cross-thread updates.</param>
    public TaskDetailsViewModel(
        ILogger<TaskDetailsViewModel> logger,
        ISocatService socatService,
        IPowerSupplyService powerSupplyService,
        IEnhancedBootloaderService bootloaderService,
        IUIThreadService uiThreadService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _powerSupplyService = powerSupplyService ?? throw new ArgumentNullException(nameof(powerSupplyService));
        _bootloaderService = bootloaderService ?? throw new ArgumentNullException(nameof(bootloaderService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));

        SetupCommands();
        SetupLogRefresh();
    }

    #region Properties

    /// <summary>
    /// Gets or sets the task execution being displayed.
    /// </summary>
    public TaskExecution? TaskExecution
    {
        get => _taskExecution;
        set
        {
            this.RaiseAndSetIfChanged(ref _taskExecution, value);
            if (value != null)
            {
                _ = RefreshLogsAsync();
            }
        }
    }

    /// <summary>
    /// Gets or sets the main log content.
    /// </summary>
    public string MainLogContent
    {
        get => _mainLogContent;
        private set => this.RaiseAndSetIfChanged(ref _mainLogContent, value);
    }

    /// <summary>
    /// Gets or sets the process/socat log content.
    /// </summary>
    public string ProcessLogContent
    {
        get => _processLogContent;
        private set => this.RaiseAndSetIfChanged(ref _processLogContent, value);
    }

    /// <summary>
    /// Gets or sets the protocol log content.
    /// </summary>
    public string ProtocolLogContent
    {
        get => _protocolLogContent;
        private set => this.RaiseAndSetIfChanged(ref _protocolLogContent, value);
    }

    /// <summary>
    /// Gets or sets the validation result text.
    /// </summary>
    public string ValidationResultText
    {
        get => _validationResultText;
        private set => this.RaiseAndSetIfChanged(ref _validationResultText, value);
    }

    /// <summary>
    /// Gets or sets the selected tab index.
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    /// <summary>
    /// Gets or sets whether socat can be started.
    /// </summary>
    public bool CanStartSocat
    {
        get => _canStartSocat;
        private set => this.RaiseAndSetIfChanged(ref _canStartSocat, value);
    }

    /// <summary>
    /// Gets or sets whether socat can be stopped.
    /// </summary>
    public bool CanStopSocat
    {
        get => _canStopSocat;
        private set => this.RaiseAndSetIfChanged(ref _canStopSocat, value);
    }

    /// <summary>
    /// Gets or sets whether power control is available.
    /// </summary>
    public bool CanControlPower
    {
        get => _canControlPower;
        private set => this.RaiseAndSetIfChanged(ref _canControlPower, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to manually start socat server.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSocatCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to manually stop socat server.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopSocatCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to turn power ON.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PowerOnCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to turn power OFF.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PowerOffCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to power cycle the PLC.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PowerCycleCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to run validation.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RunValidationCommand { get; private set; } = null!;

    #endregion

    #region Private Implementation

    private void SetupCommands()
    {
        StartSocatCommand = ReactiveCommand.CreateFromTask(ExecuteStartSocatAsync);
        StopSocatCommand = ReactiveCommand.CreateFromTask(ExecuteStopSocatAsync);
        PowerOnCommand = ReactiveCommand.CreateFromTask(ExecutePowerOnAsync);
        PowerOffCommand = ReactiveCommand.CreateFromTask(ExecutePowerOffAsync);
        PowerCycleCommand = ReactiveCommand.CreateFromTask(ExecutePowerCycleAsync);
        RunValidationCommand = ReactiveCommand.CreateFromTask(ExecuteRunValidationAsync);
    }

    private void SetupLogRefresh()
    {
        // Auto-refresh logs every 2 seconds when task is running
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(2))
            .Where(_ => TaskExecution != null && TaskExecution.IsRunning)
            .Select(_ => Observable.FromAsync(() => RefreshLogsAsync()))
            .Switch() // Ensures only one refresh operation runs at a time
            .ObserveOn(RxApp.MainThreadScheduler) // Logging should be on a background thread, but this is an example
            .Subscribe(
                _ => { }, // Operation completed
                ex => _logger.LogWarning(ex, "Failed to auto-refresh logs for task {TaskId}", TaskExecution?.TaskId)
            )
            .DisposeWith(_disposables);
    }

    private async Task RefreshLogsAsync()
    {
        if (TaskExecution?.Logger == null)
        {
            return;
        }

        try
        {
            // TODO: Implement log retrieval from DataStore
            // For now, show placeholder with logger info
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                MainLogContent = $"Main log for task {TaskExecution.TaskId}\n" +
                                $"Log file: {TaskExecution.Logger.MainLogFilePath ?? "Not configured"}\n" +
                                $"DataStore ID: {TaskExecution.Logger.MainLogDataStoreId ?? "Not configured"}";

                ProcessLogContent = $"Process/Socat log for task {TaskExecution.TaskId}\n" +
                                   $"Log file: {TaskExecution.Logger.ProcessLogFilePath ?? "Not configured"}\n" +
                                   $"DataStore ID: {TaskExecution.Logger.ProcessLogDataStoreId ?? "Not configured"}";

                ProtocolLogContent = $"Protocol log for task {TaskExecution.TaskId}\n" +
                                    $"Log file: {TaskExecution.Logger.ProtocolLogFilePath ?? "Not configured"}\n" +
                                    $"DataStore ID: {TaskExecution.Logger.ProtocolLogDataStoreId ?? "Not configured"}";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh logs for task {TaskId}", TaskExecution.TaskId);
        }
    }

    private Task ExecuteStartSocatAsync()
    {
        _logger.LogInformation("Manual start socat command executed");
        // TODO: Implement manual socat start
        return Task.CompletedTask;
    }

    private Task ExecuteStopSocatAsync()
    {
        _logger.LogInformation("Manual stop socat command executed");
        // TODO: Implement manual socat stop
        return Task.CompletedTask;
    }

    private Task ExecutePowerOnAsync()
    {
        _logger.LogInformation("Manual power ON command executed");
        // TODO: Implement manual power ON
        return Task.CompletedTask;
    }

    private Task ExecutePowerOffAsync()
    {
        _logger.LogInformation("Manual power OFF command executed");
        // TODO: Implement manual power OFF
        return Task.CompletedTask;
    }

    private Task ExecutePowerCycleAsync()
    {
        _logger.LogInformation("Manual power cycle command executed");
        // TODO: Implement manual power cycle
        return Task.CompletedTask;
    }

    private Task ExecuteRunValidationAsync()
    {
        _logger.LogInformation("Run validation command executed");
        // TODO: Implement validation check
        ValidationResultText = "Validation not yet implemented";
        return Task.CompletedTask;
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the ViewModel.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables?.Dispose();
        }
    }

    #endregion
}
