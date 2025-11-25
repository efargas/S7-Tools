using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Validation;
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
    private readonly IJobManager _jobManager;
    private readonly IPowerSupplyProfileService _powerSupplyProfileService;
    private readonly CompositeDisposable _disposables = new();

    private TaskExecution? _taskExecution;
    private string _mainLogContent = "No main log data";
    private string _processLogContent = "No socat process log data";
    private string _protocolLogContent = "No protocol log data";
    private string _validationResultText = "No validation data";
    private string _statusMessage = string.Empty;
    private int _selectedTabIndex;
    private bool _canStartSocat = true;
    private bool _canStopSocat;
    private bool _canControlPower = true;
    private bool _isPowerConnected;
    private bool _isBusy;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskDetailsViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="socatService">Socat service for manual server control.</param>
    /// <param name="powerSupplyService">Power supply service for manual power control.</param>
    /// <param name="bootloaderService">Bootloader service for validation.</param>
    /// <param name="uiThreadService">UI thread service for cross-thread updates.</param>
    /// <param name="jobManager">Job manager for accessing job profiles.</param>
    /// <param name="powerSupplyProfileService">Power supply profile service for accessing power supply configurations.</param>
    public TaskDetailsViewModel(
        ILogger<TaskDetailsViewModel> logger,
        ISocatService socatService,
        IPowerSupplyService powerSupplyService,
        IEnhancedBootloaderService bootloaderService,
        IUIThreadService uiThreadService,
        IJobManager jobManager,
        IPowerSupplyProfileService powerSupplyProfileService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _powerSupplyService = powerSupplyService ?? throw new ArgumentNullException(nameof(powerSupplyService));
        _bootloaderService = bootloaderService ?? throw new ArgumentNullException(nameof(bootloaderService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _powerSupplyProfileService = powerSupplyProfileService ?? throw new ArgumentNullException(nameof(powerSupplyProfileService));

        SetupCommands();
        SetupLogRefresh();
        UpdatePowerConnectionState();
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

    /// <summary>
    /// Gets or sets whether the power supply is connected.
    /// </summary>
    public bool IsPowerConnected
    {
        get => _isPowerConnected;
        private set => this.RaiseAndSetIfChanged(ref _isPowerConnected, value);
    }

    /// <summary>
    /// Gets or sets the status message for operations.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets whether an operation is in progress.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
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
        // Auto-refresh logs every 2 seconds, but only when a task is selected and running.
        this.WhenAnyValue(x => x.TaskExecution)
            .Select(task => task != null && task.IsRunning
                ? Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(2))
                : Observable.Empty<long>())
            .Switch()
            .Select(_ => Observable.FromAsync(() => RefreshLogsAsync()))
            .Switch() // Ensures only one refresh operation runs at a time
            .ObserveOn(RxApp.MainThreadScheduler)
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

    private void UpdatePowerConnectionState()
    {
        IsPowerConnected = _powerSupplyService.IsConnected;
        CanControlPower = IsPowerConnected;
    }

    private async Task<JobProfile?> GetCurrentJobProfileAsync()
    {
        if (TaskExecution == null)
        {
            return null;
        }

        try
        {
            return await _jobManager.GetByIdAsync(TaskExecution.JobProfileId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job profile for task {TaskId}", TaskExecution.TaskId);
            return null;
        }
    }

    private async Task EnsurePowerSupplyConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_powerSupplyService.IsConnected)
        {
            return;
        }

        JobProfile? jobProfile = await GetCurrentJobProfileAsync().ConfigureAwait(false);
        if (jobProfile == null)
        {
            throw new InvalidOperationException("No job profile available for this task");
        }

        // Get power supply profile from the job's PowerSupplyProfileId
        var powerSupplyProfile = await _powerSupplyProfileService.GetByIdAsync(jobProfile.PowerSupplyProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (powerSupplyProfile?.Configuration == null)
        {
            throw new InvalidOperationException("No power supply configuration available for this task");
        }

        bool connected = await _powerSupplyService.ConnectAsync(powerSupplyProfile.Configuration, cancellationToken)
            .ConfigureAwait(false);

        if (!connected)
        {
            throw new InvalidOperationException("Failed to connect to power supply");
        }

        await _uiThreadService.InvokeOnUIThreadAsync(UpdatePowerConnectionState);
    }

    private Task ExecuteStartSocatAsync()
    {
        _logger.LogInformation("Manual start socat command executed");
        StatusMessage = "Socat start not yet implemented - use automated task execution";
        return Task.CompletedTask;
    }

    private Task ExecuteStopSocatAsync()
    {
        _logger.LogInformation("Manual stop socat command executed");
        StatusMessage = "Socat stop not yet implemented - use automated task execution";
        return Task.CompletedTask;
    }

    private async Task ExecutePowerOnAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = "Connecting to power supply...";
            });
            _logger.LogInformation("Manual power ON command executed");

            await EnsurePowerSupplyConnectedAsync().ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = "Turning power ON...";
            });
            bool success = await _powerSupplyService.TurnOnAsync().ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = success ? "Power ON - Success" : "Power ON - Failed";
            });

            _logger.LogInformation("Power ON result: {Success}", success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to turn power ON");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = $"Power ON failed: {ex.Message}";
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecutePowerOffAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Connecting to power supply...";
            _logger.LogInformation("Manual power OFF command executed");

            await EnsurePowerSupplyConnectedAsync().ConfigureAwait(false);

            StatusMessage = "Turning power OFF...";
            bool success = await _powerSupplyService.TurnOffAsync().ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = success ? "Power OFF - Success" : "Power OFF - Failed";
            });

            _logger.LogInformation("Power OFF result: {Success}", success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to turn power OFF");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = $"Power OFF failed: {ex.Message}";
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecutePowerCycleAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Connecting to power supply...";
            _logger.LogInformation("Manual power cycle command executed");

            await EnsurePowerSupplyConnectedAsync().ConfigureAwait(false);

            StatusMessage = "Power cycling (OFF → delay → ON)...";
            bool success = await _powerSupplyService.PowerCycleAsync(5000).ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = success ? "Power cycle - Success" : "Power cycle - Failed";
            });

            _logger.LogInformation("Power cycle result: {Success}", success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to power cycle");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = $"Power cycle failed: {ex.Message}";
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteRunValidationAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Running validation...";
            _logger.LogInformation("Run validation command executed");

            JobProfile? jobProfile = await GetCurrentJobProfileAsync().ConfigureAwait(false);
            if (jobProfile == null)
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    ValidationResultText = "Error: Could not load job profile for validation";
                    StatusMessage = "Validation failed - no job profile";
                });
                return;
            }

            ValidationResult result = await _jobManager.ValidateJobAsync(jobProfile).ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                if (result.IsValid)
                {
                    ValidationResultText = "✓ Validation passed - all checks successful";
                    StatusMessage = "Validation passed";
                }
                else
                {
                    string errors = string.Join("\n", result.Errors.Select(e => $"• {e.ErrorMessage}"));
                    ValidationResultText = $"✗ Validation failed:\n{errors}";
                    StatusMessage = $"Validation failed with {result.Errors.Count()} error(s)";
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                ValidationResultText = $"Validation error: {ex.Message}";
                StatusMessage = "Validation error";
            });
        }
        finally
        {
            IsBusy = false;
        }
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
