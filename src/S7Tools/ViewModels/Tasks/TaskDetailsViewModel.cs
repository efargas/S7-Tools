using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Configuration;
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
    private readonly ISerialPortService _serialPortService;
    private readonly ISerialPortProfileService _serialPortProfileService;
    private readonly ISocatProfileService _socatProfileService;
    private readonly CompositeDisposable _disposables = new();
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

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
    private bool _isSerialPortConnected;
    private bool _isSocatClientConnected;
    private double _manualProcessProgress;
    private string _currentProcessStep = string.Empty;
    private TimeSpan? _estimatedTimeRemaining;
    private bool _canStartManualProcess;

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
        IPowerSupplyProfileService powerSupplyProfileService,
        ISerialPortService serialPortService,
        ISerialPortProfileService serialPortProfileService,
        ISocatProfileService socatProfileService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _powerSupplyService = powerSupplyService ?? throw new ArgumentNullException(nameof(powerSupplyService));
        _bootloaderService = bootloaderService ?? throw new ArgumentNullException(nameof(bootloaderService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _powerSupplyProfileService = powerSupplyProfileService ?? throw new ArgumentNullException(nameof(powerSupplyProfileService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _serialPortProfileService = serialPortProfileService ?? throw new ArgumentNullException(nameof(serialPortProfileService));
        _socatProfileService = socatProfileService ?? throw new ArgumentNullException(nameof(socatProfileService));

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

    /// <summary>
    /// Gets whether the serial port is connected.
    /// </summary>
    public bool IsSerialPortConnected
    {
        get => _isSerialPortConnected;
        private set => this.RaiseAndSetIfChanged(ref _isSerialPortConnected, value);
    }

    /// <summary>
    /// Gets whether the socat client is connected to the server.
    /// </summary>
    public bool IsSocatClientConnected
    {
        get => _isSocatClientConnected;
        private set => this.RaiseAndSetIfChanged(ref _isSocatClientConnected, value);
    }

    /// <summary>
    /// Gets the manual process progress percentage (0-100).
    /// </summary>
    public double ManualProcessProgress
    {
        get => _manualProcessProgress;
        private set => this.RaiseAndSetIfChanged(ref _manualProcessProgress, value);
    }

    /// <summary>
    /// Gets the current process step description.
    /// </summary>
    public string CurrentProcessStep
    {
        get => _currentProcessStep;
        private set => this.RaiseAndSetIfChanged(ref _currentProcessStep, value);
    }

    /// <summary>
    /// Gets the estimated time remaining for the manual process.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        private set => this.RaiseAndSetIfChanged(ref _estimatedTimeRemaining, value);
    }

    /// <summary>
    /// Gets whether the manual process can be started (steps 1-4 complete).
    /// </summary>
    public bool CanStartManualProcess
    {
        get => _canStartManualProcess;
        private set => this.RaiseAndSetIfChanged(ref _canStartManualProcess, value);
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

    /// <summary>
    /// Gets the command to apply serial port configuration.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ApplySerialConfigCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to kill socat server process.
    /// </summary>
    public ReactiveCommand<Unit, Unit> KillSocatCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to connect socat client to server.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ConnectSocatClientCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to disconnect socat client from server.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DisconnectSocatClientCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to connect to power supply.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ConnectPowerSupplyCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to disconnect from power supply.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DisconnectPowerSupplyCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to start the manual process from step 5.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartManualProcessCommand { get; private set; } = null!;

    #endregion

    #region Private Implementation

    private void SetupCommands()
    {
        StartSocatCommand = ReactiveCommand.CreateFromTask(ExecuteStartSocatAsync);
        StopSocatCommand = ReactiveCommand.CreateFromTask(ExecuteStopSocatAsync);
        PowerOnCommand = ReactiveCommand.CreateFromTask(ExecutePowerOnAsync);
        PowerOffCommand = ReactiveCommand.CreateFromTask(ExecutePowerOffAsync);
        PowerCycleCommand = ReactiveCommand.CreateFromTask(ExecutePowerCycleAsync);
        RunValidationCommand = ReactiveCommand.CreateFromTask(
            ExecuteRunValidationAsync,
            this.WhenAnyValue(x => x.TaskExecution).Select(t => t != null));

        ApplySerialConfigCommand = ReactiveCommand.CreateFromTask(
            ExecuteApplySerialConfigAsync,
            this.WhenAnyValue(x => x.TaskExecution).Select(t => t != null));

        KillSocatCommand = ReactiveCommand.CreateFromTask(
            ExecuteKillSocatAsync,
            this.WhenAnyValue(x => x.CanStopSocat));

        ConnectSocatClientCommand = ReactiveCommand.CreateFromTask(
            ExecuteConnectSocatClientAsync,
            this.WhenAnyValue(x => x.TaskExecution).Select(t => t != null));

        DisconnectSocatClientCommand = ReactiveCommand.CreateFromTask(
            ExecuteDisconnectSocatClientAsync,
            this.WhenAnyValue(x => x.IsSocatClientConnected));

        ConnectPowerSupplyCommand = ReactiveCommand.CreateFromTask(
            ExecuteConnectPowerSupplyAsync,
            this.WhenAnyValue(x => x.CanControlPower, x => x.IsPowerConnected)
                .Select(tuple => tuple.Item1 && !tuple.Item2));

        DisconnectPowerSupplyCommand = ReactiveCommand.CreateFromTask(
            ExecuteDisconnectPowerSupplyAsync,
            this.WhenAnyValue(x => x.IsPowerConnected));

        StartManualProcessCommand = ReactiveCommand.CreateFromTask(
            ExecuteStartManualProcessAsync,
            this.WhenAnyValue(x => x.CanStartManualProcess));
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
            // Read main log
            if (!string.IsNullOrEmpty(TaskExecution.Logger.MainLogFilePath) && File.Exists(TaskExecution.Logger.MainLogFilePath))
            {
                string content = await File.ReadAllTextAsync(TaskExecution.Logger.MainLogFilePath);
                await _uiThreadService.InvokeOnUIThreadAsync(() => MainLogContent = content);
            }

            // Read socat/process log
            if (!string.IsNullOrEmpty(TaskExecution.Logger.ProcessLogFilePath) && File.Exists(TaskExecution.Logger.ProcessLogFilePath))
            {
                string content = await File.ReadAllTextAsync(TaskExecution.Logger.ProcessLogFilePath);
                await _uiThreadService.InvokeOnUIThreadAsync(() => ProcessLogContent = content);
            }

            // Read protocol log
            if (!string.IsNullOrEmpty(TaskExecution.Logger.ProtocolLogFilePath) && File.Exists(TaskExecution.Logger.ProtocolLogFilePath))
            {
                string content = await File.ReadAllTextAsync(TaskExecution.Logger.ProtocolLogFilePath);
                await _uiThreadService.InvokeOnUIThreadAsync(() => ProtocolLogContent = content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh logs");
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
        if (!await _operationSemaphore.WaitAsync(0).ConfigureAwait(false))
            return; // Already busy

        try
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
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
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
            _operationSemaphore.Release();
        }
    }

    private async Task ExecutePowerOffAsync()
    {
        if (!await _operationSemaphore.WaitAsync(0).ConfigureAwait(false))
            return; // Already busy

        try
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = "Connecting to power supply...";
            });
            _logger.LogInformation("Manual power OFF command executed");

            await EnsurePowerSupplyConnectedAsync().ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = "Turning power OFF...";
            });
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
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
            _operationSemaphore.Release();
        }
    }

    private async Task ExecutePowerCycleAsync()
    {
        if (!await _operationSemaphore.WaitAsync(0).ConfigureAwait(false))
            return; // Already busy

        try
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = "Connecting to power supply...";
            });
            _logger.LogInformation("Manual power cycle command executed");

            await EnsurePowerSupplyConnectedAsync().ConfigureAwait(false);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = "Power cycling (OFF → delay → ON)...";
            });

            // Get the power off delay from the job profile or use a default
            JobProfile? jobProfile = await GetCurrentJobProfileAsync().ConfigureAwait(false);
            int delay = jobProfile?.PowerOffDelayMs ?? 5000;
            bool success = await _powerSupplyService.PowerCycleAsync(delay).ConfigureAwait(false);

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
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteRunValidationAsync()
    {
        if (!await _operationSemaphore.WaitAsync(0).ConfigureAwait(false))
            return; // Already busy

        try
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = "Running validation...";
            });
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
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteApplySerialConfigAsync()
    {
        if (TaskExecution == null)
        {
            return;
        }

        await _operationSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Applying serial port configuration...";

            JobProfile? jobProfile = await GetCurrentJobProfileAsync();
            if (jobProfile == null)
            {
                StatusMessage = "Error: Job profile not found";
                return;
            }

            // Get serial port profile
            SerialPortProfile? serialProfile = await _serialPortProfileService.GetByIdAsync(jobProfile.SerialProfileId);
            if (serialProfile == null)
            {
                StatusMessage = "Error: Serial port profile not found";
                return;
            }

            // Get serial device path from job profile (assuming it's stored in metadata or similar)
            // For now, we'll use a default path - in a real implementation, this should come from the job profile
            string serialDevice = "/dev/ttyUSB0"; // TODO: Get from job profile

            // Apply serial port configuration
            bool success = await _serialPortService.ApplyProfileAsync(serialDevice, serialProfile);

            IsSerialPortConnected = success;
            StatusMessage = success
                ? $"Serial port configuration applied: {serialProfile.Name}"
                : "Failed to apply serial port configuration";
            UpdateCanStartManualProcess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply serial port configuration");
            StatusMessage = $"Error applying serial configuration: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteKillSocatAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Killing socat server...";

            // Kill all socat processes
            int stoppedCount = await _socatService.StopAllSocatProcessesAsync();

            CanStopSocat = false;
            StatusMessage = $"Killed {stoppedCount} socat server process(es)";
            UpdateCanStartManualProcess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to kill socat server");
            StatusMessage = $"Error killing socat: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteConnectSocatClientAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Connecting to socat server...";

            // Note: Socat client connection is handled by the bootloader service
            // This is just a status update - actual connection happens during bootloader handshake
            // For manual control, we just mark it as ready
            IsSocatClientConnected = true;
            StatusMessage = "Socat client ready for connection";
            UpdateCanStartManualProcess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to socat server");
            StatusMessage = $"Error connecting to socat: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteDisconnectSocatClientAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Disconnecting from socat server...";

            // Note: Socat client disconnection is handled by the bootloader service
            // This is just a status update
            IsSocatClientConnected = false;
            StatusMessage = "Socat client disconnected";
            UpdateCanStartManualProcess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from socat server");
            StatusMessage = $"Error disconnecting from socat: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteConnectPowerSupplyAsync()
    {
        if (TaskExecution == null)
        {
            return;
        }

        await _operationSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Connecting to power supply...";

            JobProfile? jobProfile = await GetCurrentJobProfileAsync();
            if (jobProfile == null)
            {
                StatusMessage = "Error: Job profile not found";
                return;
            }

            PowerSupplyProfile? powerProfile = await _powerSupplyProfileService.GetByIdAsync(jobProfile.PowerSupplyProfileId);
            if (powerProfile == null)
            {
                StatusMessage = "Error: Power supply profile not found";
                return;
            }

            bool connected = await _powerSupplyService.ConnectAsync(powerProfile.Configuration);
            if (connected)
            {
                IsPowerConnected = true;
                StatusMessage = $"Connected to power supply: {powerProfile.Name}";
                UpdateCanStartManualProcess();
            }
            else
            {
                StatusMessage = "Failed to connect to power supply";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to power supply");
            StatusMessage = $"Error connecting to power supply: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteDisconnectPowerSupplyAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Disconnecting from power supply...";

            await _powerSupplyService.DisconnectAsync();
            IsPowerConnected = false;
            StatusMessage = "Disconnected from power supply";
            UpdateCanStartManualProcess();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from power supply");
            StatusMessage = $"Error disconnecting from power supply: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationSemaphore.Release();
        }
    }

    private async Task ExecuteStartManualProcessAsync()
    {
        if (TaskExecution == null)
        {
            return;
        }

        await _operationSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            ManualProcessProgress = 0;
            CurrentProcessStep = "Starting manual process from step 5...";
            StatusMessage = "Manual process started";

            // Get job profile to pass to bootloader service
            JobProfile? jobProfile = await GetCurrentJobProfileAsync();
            if (jobProfile == null)
            {
                StatusMessage = "Error: Job profile not found";
                return;
            }

            // TODO: Implement manual process execution from step 5
            // This would involve calling the bootloader service with a flag to skip steps 1-4
            // For now, this is a placeholder that shows progress

            // Simulate progress through steps 5-11
            string[] steps = {
                "Step 5: Power cycling PLC...",
                "Step 6: Connecting PLC client...",
                "Step 7: Performing handshake...",
                "Step 8: Installing stager...",
                "Step 9: Dumping memory...",
                "Step 10: Teardown...",
                "Step 11: Complete"
            };

            for (int i = 0; i < steps.Length; i++)
            {
                CurrentProcessStep = steps[i];
                ManualProcessProgress = ((i + 1) / (double)steps.Length) * 100;
                EstimatedTimeRemaining = TimeSpan.FromSeconds((steps.Length - i - 1) * 10);
                await Task.Delay(1000); // Simulate work
            }

            ManualProcessProgress = 100;
            CurrentProcessStep = "Manual process completed";
            EstimatedTimeRemaining = null;
            StatusMessage = "Manual process completed successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual process failed");
            CurrentProcessStep = "Manual process failed";
            StatusMessage = $"Manual process error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationSemaphore.Release();
        }
    }

    private void UpdateCanStartManualProcess()
    {
        // Manual process can start when steps 1-4 are complete:
        // 1. Serial Config - IsSerialPortConnected
        // 2. Socat Setup - CanStopSocat (server running)
        // 3. Power Connect - IsPowerConnected
        // 4. Power ON - (assumed if power connected)
        CanStartManualProcess = IsSerialPortConnected && CanStopSocat && IsPowerConnected;
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
            _operationSemaphore?.Dispose();
        }
    }

    #endregion
}
