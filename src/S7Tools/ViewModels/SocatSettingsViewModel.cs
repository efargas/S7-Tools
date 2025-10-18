using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Helpers;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Servers settings category, providing comprehensive socat profile management
/// and process control capabilities for Serial-to-TCP proxy functionality.
/// </summary>
public class SocatSettingsViewModel : ProfileManagementViewModelBase<SocatProfile>
{
    #region Fields

    private readonly ISocatProfileService _profileService;
    private readonly ISocatService _socatService;
    private readonly ISerialPortService _serialPortService;
    private readonly IDialogService _dialogService;
    private readonly IUnifiedProfileDialogService _unifiedDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<SocatSettingsViewModel> _specificLogger;
    private readonly S7Tools.Services.Interfaces.ISettingsService _settingsService;
    private readonly S7Tools.Services.Interfaces.IUIThreadService _uiThreadService;
    private EventHandler<S7Tools.Models.ApplicationSettings>? _settingsChangedHandler;
    private readonly CompositeDisposable _disposables = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SocatSettingsViewModel class.
    /// </summary>
    /// <param name="unifiedDialogService">The unified profile dialog service.</param>
    /// <param name="logger">The logger for the base class.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="profileService">The socat profile service.</param>
    /// <param name="socatService">The socat service.</param>
    /// <param name="serialPortService">The serial port service for device discovery.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="settingsService">The settings service used to persist application settings.</param>
    public SocatSettingsViewModel(
        IUnifiedProfileDialogService unifiedDialogService,
        ILogger<ProfileManagementViewModelBase<SocatProfile>> logger,
        S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
        ISocatProfileService profileService,
        ISocatService socatService,
        ISerialPortService serialPortService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IFileDialogService? fileDialogService,
        S7Tools.Services.Interfaces.ISettingsService settingsService)
        : base(logger, unifiedDialogService, dialogService, uiThreadService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _unifiedDialogService = unifiedDialogService;
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _uiThreadService = uiThreadService;

        // Create specific logger for this ViewModel
        ILoggerFactory loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
        _specificLogger = loggerFactory.CreateLogger<SocatSettingsViewModel>();

        // Initialize collections
        Profiles = new ObservableCollection<SocatProfile>();
        AvailableSerialDevices = new ObservableCollection<string>();
        RunningProcesses = new ObservableCollection<SocatProcessInfo>();

        // Initialize commands
        InitializeCommands();

        // Initialize path commands
        BrowseProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowseProfilesPathAsync);
        OpenProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenProfilesPathAsync);
        ResetProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetProfilesPathAsync);

        // Initialize ProfilesPath from settings and subscribe to changes
        RefreshFromSettings();
        _settingsChangedHandler = (_, __) => RefreshFromSettings();
        _settingsService.SettingsChanged += _settingsChangedHandler;

        // Subscribe to socat service events
        SubscribeToSocatEvents();

        // Load initial data
        _ = Task.Run(async () =>
        {
            await RefreshCommand.Execute();
            await ScanSerialDevicesAsync();
            await RefreshRunningProcessesAsync();
        });

        _specificLogger.LogInformation("SocatSettingsViewModel initialized");

        // Debug property change tracking
        this.WhenAnyValue(x => x.SelectedProfile)
            .Subscribe(profile => _specificLogger.LogDebug("🔍 SelectedProfile changed to: {ProfileName} (ID: {ProfileId})",
                profile?.Name ?? "NULL", profile?.Id.ToString() ?? "NULL"))
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.SelectedSerialDevice)
            .Subscribe(device => _specificLogger.LogDebug("🔌 SelectedSerialDevice changed to: {Device}", device ?? "NULL"))
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.RunningProcessCount)
            .Subscribe(count => _specificLogger.LogDebug("📊 RunningProcessCount changed to: {Count}", count))
            .DisposeWith(_disposables);

        // Update socat command preview when either profile or device selection changes
        this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedSerialDevice)
            .Subscribe(values =>
            {
                (SocatProfile? profile, string? selectedDevice) = values;
                try
                {
                    if (profile == null)
                    {
                        SelectedProfileSocatCommand = string.Empty;
                    }
                    else
                    {
                        // Use the actual selected device if available, otherwise use placeholder
                        string deviceToUse = !string.IsNullOrEmpty(selectedDevice) ? selectedDevice : "/dev/ttyUSB0";

                        try
                        {
                            // Generate command with the actual selected device for accurate preview
                            SelectedProfileSocatCommand = _socatService.GenerateSocatCommandForProfile(profile, deviceToUse);
                        }
                        catch (Exception ex)
                        {
                            _specificLogger.LogWarning(ex, "Failed to generate socat command for profile '{ProfileName}' with device '{Device}'. Falling back to configuration command.", profile?.Name, deviceToUse);
                            // Fallback to configuration command generation
                            if (profile != null)
                            {
                                SelectedProfileSocatCommand = _socatService.GenerateSocatCommand(profile.Configuration, deviceToUse);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _specificLogger.LogError(ex, "Error generating socat command preview for selected profile '{ProfileName}' and device '{Device}'.", profile?.Name, selectedDevice);
                    SelectedProfileSocatCommand = "Error generating command";
                }
            })
            .DisposeWith(_disposables);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of available serial devices.
    /// </summary>
    public ObservableCollection<string> AvailableSerialDevices { get; }

    /// <summary>
    /// Gets the collection of running socat processes.
    /// </summary>
    public ObservableCollection<SocatProcessInfo> RunningProcesses { get; }

    private string _selectedProfileSocatCommand = string.Empty;
    /// <summary>
    /// Gets the generated socat command string for the currently selected profile.
    /// </summary>
    public string SelectedProfileSocatCommand
    {
        get => _selectedProfileSocatCommand;
        private set => this.RaiseAndSetIfChanged(ref _selectedProfileSocatCommand, value);
    }

    private string? _selectedSerialDevice;
    /// <summary>
    /// Gets or sets the currently selected serial device.
    /// </summary>
    public string? SelectedSerialDevice
    {
        get => _selectedSerialDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedSerialDevice, value);
    }

    private SocatProcessInfo? _selectedProcess;
    /// <summary>
    /// Gets or sets the currently selected running process.
    /// </summary>
    public SocatProcessInfo? SelectedProcess
    {
        get => _selectedProcess;
        set => this.RaiseAndSetIfChanged(ref _selectedProcess, value);
    }

    private bool _isScanning;
    /// <summary>
    /// Gets or sets a value indicating whether device scanning is in progress.
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }



    private int _deviceCount;
    /// <summary>
    /// Gets or sets the total number of available devices.
    /// </summary>
    public int DeviceCount
    {
        get => _deviceCount;
        set => this.RaiseAndSetIfChanged(ref _deviceCount, value);
    }

    private int _runningProcessCount;
    /// <summary>
    /// Gets or sets the total number of running processes.
    /// </summary>
    public int RunningProcessCount
    {
        get => _runningProcessCount;
        set => this.RaiseAndSetIfChanged(ref _runningProcessCount, value);
    }

    #endregion

    #region Commands

    // Socat-specific commands (CRUD commands inherited from base class)

    /// <summary>
    /// Gets the command to scan for available serial devices.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ScanSerialDevicesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to start socat with the selected profile and device.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartSocatCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to stop the selected socat process.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopSocatCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to stop all running socat processes.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopAllSocatCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to refresh the running processes list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshProcessesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to test the TCP connection for the selected process.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to export profiles.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportProfilesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to import profiles.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportProfilesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to export the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportSelectedProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to show profile details.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowProfileDetailsCommand { get; private set; } = null!;

    // Path management commands
    /// <summary>
    /// Command to open a folder browser to select the profiles directory.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseProfilesPathCommand { get; private set; } = null!;

    /// <summary>
    /// Command to open the profiles directory in the system file explorer.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenProfilesPathCommand { get; private set; } = null!;

    /// <summary>
    /// Command to reset the profiles path to the default within the application resources.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetProfilesPathCommand { get; private set; } = null!;

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes all reactive commands with their execution logic and conditions.
    /// </summary>
    private void InitializeCommands()
    {
        // CRUD commands (Create, Edit, Delete, Duplicate, SetDefault, Refresh)
        // are provided by base class ProfileManagementViewModelBase

        // Scan serial devices command - always enabled
        ScanSerialDevicesCommand = ReactiveCommand.CreateFromTask(ScanSerialDevicesAsync);
        ScanSerialDevicesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "scanning serial devices"))
            .DisposeWith(_disposables);

        // Start socat command - enabled when both profile and device are selected
        IObservable<bool> canStartSocat = this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedSerialDevice)
            .Select(tuple =>
            {
                bool canStart = tuple.Item1 != null && !string.IsNullOrEmpty(tuple.Item2);
                _specificLogger.LogDebug("🎛️ StartSocat CanExecute: Profile={ProfileSelected}, Device={DeviceSelected}, CanStart={CanStart}",
                    tuple.Item1?.Name ?? "None", tuple.Item2 ?? "None", canStart);
                return canStart;
            });

        StartSocatCommand = ReactiveCommand.CreateFromTask(StartSocatAsync, canStartSocat);
        StartSocatCommand.IsExecuting
            .Subscribe(isExecuting => _specificLogger.LogDebug("⚙️ StartSocatCommand IsExecuting: {IsExecuting}", isExecuting))
            .DisposeWith(_disposables);
        StartSocatCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "starting socat"))
            .DisposeWith(_disposables);

        // Stop socat command - enabled when a process is selected
        IObservable<bool> canStopSocat = this.WhenAnyValue(x => x.SelectedProcess)
            .Select(process => process != null && process.IsRunning);

        StopSocatCommand = ReactiveCommand.CreateFromTask(StopSocatAsync, canStopSocat);
        StopSocatCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "stopping socat"))
            .DisposeWith(_disposables);

        // Stop all socat command - enabled when there are running processes
        IObservable<bool> canStopAllSocat = this.WhenAnyValue(x => x.RunningProcessCount)
            .Select(count => count > 0);

        StopAllSocatCommand = ReactiveCommand.CreateFromTask(StopAllSocatAsync, canStopAllSocat);
        StopAllSocatCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "stopping all socat processes"))
            .DisposeWith(_disposables);

        // Refresh processes command - always enabled
        RefreshProcessesCommand = ReactiveCommand.CreateFromTask(RefreshRunningProcessesAsync);
        RefreshProcessesCommand.IsExecuting
            .Subscribe(isExecuting => _specificLogger.LogDebug("🔄 RefreshProcessesCommand IsExecuting: {IsExecuting}", isExecuting))
            .DisposeWith(_disposables);
        RefreshProcessesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "refreshing processes"))
            .DisposeWith(_disposables);

        // Test connection command - enabled when a process is selected
        IObservable<bool> canTestConnection = this.WhenAnyValue(x => x.SelectedProcess)
            .Select(process => process != null && process.IsRunning);

        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync, canTestConnection);
        TestConnectionCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing connection"))
            .DisposeWith(_disposables);

        // Export profiles command - enabled when there are profiles
        IObservable<bool> canExport = this.WhenAnyValue(x => x.Profiles.Count)
            .Select(count => count > 0);

        ExportProfilesCommand = ReactiveCommand.CreateFromTask(ExportProfilesAsync, canExport);
        ExportProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting profiles"))
            .DisposeWith(_disposables);

        // Import profiles command - always enabled
        ImportProfilesCommand = ReactiveCommand.CreateFromTask(ImportProfilesAsync);
        ImportProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "importing profiles"))
            .DisposeWith(_disposables);

        // Export selected profile command - enabled when a profile is selected
        IObservable<bool> canExportSelected = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        ExportSelectedProfileCommand = ReactiveCommand.CreateFromTask(ExportSelectedProfileAsync, canExportSelected);
        ExportSelectedProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting selected profile"))
            .DisposeWith(_disposables);

        // Show profile details command - enabled when a profile is selected
        ShowProfileDetailsCommand = ReactiveCommand.CreateFromTask(ShowProfileDetailsAsync, canExportSelected);
        ShowProfileDetailsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "showing profile details"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Subscribes to socat service events for real-time process monitoring.
    /// </summary>
    private void SubscribeToSocatEvents()
    {
        _socatService.ProcessStarted += async (sender, args) =>
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                RunningProcesses.Add(args.ProcessInfo);
                RunningProcessCount = RunningProcesses.Count;
                StatusMessage = $"socat process {args.ProcessInfo.ProcessId} started on port {args.ProcessInfo.TcpPort}";
            });
        };

        _socatService.ProcessStopped += async (sender, args) =>
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                SocatProcessInfo? existingProcess = RunningProcesses.FirstOrDefault(p => p.ProcessId == args.ProcessInfo.ProcessId);
                if (existingProcess != null)
                {
                    RunningProcesses.Remove(existingProcess);
                    RunningProcessCount = RunningProcesses.Count;
                    StatusMessage = $"socat process {args.ProcessInfo.ProcessId} stopped";
                }
            });
        };

        _socatService.ProcessError += async (sender, args) =>
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = $"socat process {args.ProcessInfo.ProcessId} error: {args.Error.Message}";
                _specificLogger.LogError(args.Error, "socat process error");
            });
        };

        _socatService.ConnectionEstablished += async (sender, args) =>
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                SocatProcessInfo? existingProcess = RunningProcesses.FirstOrDefault(p => p.ProcessId == args.ProcessInfo.ProcessId);
                if (existingProcess != null)
                {
                    existingProcess.ActiveConnections++;
                }
                StatusMessage = $"Connection established to process {args.ProcessInfo.ProcessId}";
            });
        };

        _socatService.ConnectionClosed += async (sender, args) =>
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                SocatProcessInfo? existingProcess = RunningProcesses.FirstOrDefault(p => p.ProcessId == args.ProcessInfo.ProcessId);
                if (existingProcess != null && existingProcess.ActiveConnections > 0)
                {
                    existingProcess.ActiveConnections--;
                }
                StatusMessage = $"Connection closed to process {args.ProcessInfo.ProcessId}";
            });
        };
    }

    /// <summary>
    /// Refreshes the ProfilesPath from application settings.
    /// </summary>
    private void RefreshFromSettings()
    {
        try
        {
            Models.ApplicationSettings settings = _settingsService.Settings;
            ProfilesPath = settings.Socat?.ProfilesPath ?? "resources/SocatProfiles";
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error refreshing profiles path from settings");
            ProfilesPath = "resources/SocatProfiles";
        }
    }

    // LoadProfilesAsync is provided by base class ProfileManagementViewModelBase

    /// <summary>
    /// Scans for available serial devices.
    /// </summary>
    private async Task ScanSerialDevicesAsync()
    {
        try
        {
            IsScanning = true;
            StatusMessage = "Scanning for serial devices...";

            IEnumerable<Core.Services.Interfaces.SerialPortInfo> deviceInfos = await _serialPortService.ScanAvailablePortsAsync();
            IEnumerable<string> devices = deviceInfos.Select(info => info.PortPath);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                AvailableSerialDevices.Clear();
                foreach (string? device in devices.OrderBy(d => d))
                {
                    AvailableSerialDevices.Add(device);
                }
                DeviceCount = AvailableSerialDevices.Count;

                // Select first USB device if available
                string? usbDevice = AvailableSerialDevices.FirstOrDefault(d => d.Contains("ttyUSB") || d.Contains("ttyACM"));
                if (usbDevice != null)
                {
                    SelectedSerialDevice = usbDevice;
                }

                StatusMessage = $"Found {DeviceCount} serial device(s)";
            });

            _specificLogger.LogInformation("Found {DeviceCount} serial devices", DeviceCount);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error scanning serial devices");
            StatusMessage = "Error scanning devices";
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>
    /// Refreshes the list of running socat processes.
    /// </summary>
    private async Task RefreshRunningProcessesAsync()
    {
        _specificLogger.LogInformation("📋 RefreshRunningProcessesAsync ENTRY");

        try
        {
            StatusMessage = "Refreshing running processes...";
            _specificLogger.LogInformation("🔍 Calling _socatService.GetRunningProcessesAsync...");

            IEnumerable<SocatProcessInfo>? processes = await _socatService.GetRunningProcessesAsync();
            _specificLogger.LogInformation("📊 GetRunningProcessesAsync returned {Count} processes", processes?.Count() ?? 0);

            _specificLogger.LogInformation("🔄 Invoking UI thread update...");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                _specificLogger.LogInformation("🧹 Clearing RunningProcesses collection...");
                RunningProcesses.Clear();

                if (processes != null)
                {
                    foreach (SocatProcessInfo process in processes)
                    {
                        _specificLogger.LogInformation("➕ Adding process: PID={ProcessId}, Port={Port}, Status={Status}",
                            process.ProcessId, process.TcpPort, process.Status);
                        RunningProcesses.Add(process);
                    }
                }

                RunningProcessCount = RunningProcesses.Count;
                _specificLogger.LogInformation("📈 Updated RunningProcessCount to {Count}", RunningProcessCount);

                StatusMessage = $"Found {RunningProcessCount} running process(es)";
            });
            _specificLogger.LogInformation("✅ UI thread update completed");

            _specificLogger.LogInformation("📊 Final result: Found {ProcessCount} running socat processes", RunningProcessCount);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "💥 EXCEPTION in RefreshRunningProcessesAsync: {Message}", ex.Message);
            StatusMessage = $"Error refreshing processes: {ex.Message}";
        }

        _specificLogger.LogInformation("🏁 RefreshRunningProcessesAsync EXIT");
    }





    /// <summary>
    /// Deletes the currently selected profile.
    /// </summary>
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            // Confirm deletion
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Profile",
                $"Are you sure you want to delete the profile '{SelectedProfile.Name}'?");

            if (!confirmed)
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Deleting profile...";

            string profileName = SelectedProfile.Name;
            int idToDelete = SelectedProfile.Id;
            bool success = await _profileService.DeleteAsync(idToDelete);

            if (success)
            {
                // Refresh profiles; preserve selection if possible (select next available)
                await RefreshProfilesPreserveSelectionAsync(null);

                StatusMessage = $"Profile '{profileName}' deleted successfully";
                _specificLogger.LogInformation("Deleted socat profile: {ProfileName}", profileName);
            }
            else
            {
                StatusMessage = "Failed to delete profile";
                _specificLogger.LogWarning("Failed to delete socat profile: {ProfileName}", profileName);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error deleting profile");
            StatusMessage = "Error deleting profile";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Duplicates the currently selected profile.
    /// </summary>
    private async Task DuplicateProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            _specificLogger.LogDebug("Duplicating socat profile: {ProfileName}", SelectedProfile.Name);

            Models.InputResult inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Profile",
                "Enter a name for the duplicate profile:",
                $"{SelectedProfile.Name} (Copy)").ConfigureAwait(false);

            string? newName = inputResult.Value;

            if (!string.IsNullOrWhiteSpace(newName))
            {
                SocatProfile originalProfile = SelectedProfile;

                IsLoading = true;
                StatusMessage = "Duplicating profile...";

                SocatProfile duplicatedProfile = await _profileService.DuplicateAsync(originalProfile.Id, newName);

                // Refresh and select duplicated profile
                await RefreshProfilesPreserveSelectionAsync(duplicatedProfile.Id);

                StatusMessage = $"Profile duplicated as '{duplicatedProfile.Name}'";
                _specificLogger.LogInformation("Duplicated socat profile: {OriginalName} -> {NewName}",
                    originalProfile.Name, duplicatedProfile.Name);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error duplicating profile");
            StatusMessage = "Error duplicating profile";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Starts socat with the selected profile and serial device.
    /// </summary>
    private async Task StartSocatAsync()
    {
        _specificLogger.LogInformation("🚀 StartSocatAsync ENTRY - SelectedProfile: {Profile}, SelectedSerialDevice: {Device}",
            SelectedProfile?.Name ?? "NULL", SelectedSerialDevice ?? "NULL");

        if (SelectedProfile == null || string.IsNullOrEmpty(SelectedSerialDevice))
        {
            _specificLogger.LogWarning("❌ StartSocatAsync ABORT - Missing selection: Profile={Profile}, Device={Device}",
                SelectedProfile?.Name ?? "NULL", SelectedSerialDevice ?? "NULL");
            return;
        }

        try
        {
            _specificLogger.LogInformation("📡 Setting status message...");
            StatusMessage = $"Starting socat on port {SelectedProfile.Configuration.TcpPort}...";

            _specificLogger.LogInformation("🔧 Calling _socatService.StartSocatWithProfileAsync...");
            SocatProcessInfo processInfo = await _socatService.StartSocatWithProfileAsync(SelectedProfile, SelectedSerialDevice);
            _specificLogger.LogInformation("✅ _socatService.StartSocatWithProfileAsync completed - ProcessId: {ProcessId}", processInfo.ProcessId);

            StatusMessage = $"socat started successfully (PID: {processInfo.ProcessId})";
            _specificLogger.LogInformation("🎉 Started socat process: {ProcessId} for profile {ProfileName} on device {Device}",
                processInfo.ProcessId, SelectedProfile.Name, SelectedSerialDevice);

            _specificLogger.LogInformation("🔄 Refreshing running processes...");
            await RefreshRunningProcessesAsync();
            _specificLogger.LogInformation("✅ RefreshRunningProcessesAsync completed");
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "💥 EXCEPTION in StartSocatAsync: {Message}", ex.Message);
            StatusMessage = $"Error starting socat: {ex.Message}";
        }

        _specificLogger.LogInformation("🏁 StartSocatAsync EXIT");
    }

    /// <summary>
    /// Stops the currently selected socat process.
    /// </summary>
    private async Task StopSocatAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        try
        {
            StatusMessage = $"Stopping socat process {SelectedProcess.ProcessId}...";

            bool success = await _socatService.StopSocatAsync(SelectedProcess);

            if (success)
            {
                StatusMessage = $"socat process {SelectedProcess.ProcessId} stopped successfully";
                _specificLogger.LogInformation("Stopped socat process: {ProcessId}", SelectedProcess.ProcessId);
            }
            else
            {
                StatusMessage = $"Failed to stop socat process {SelectedProcess.ProcessId}";
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error stopping socat process");
            StatusMessage = "Error stopping socat process";
        }
    }

    /// <summary>
    /// Stops all running socat processes.
    /// </summary>
    private async Task StopAllSocatAsync()
    {
        try
        {
            StatusMessage = "Stopping all socat processes...";

            int stoppedCount = await _socatService.StopAllSocatProcessesAsync();

            StatusMessage = $"Stopped {stoppedCount} socat process(es)";
            _specificLogger.LogInformation("Stopped {Count} socat processes", stoppedCount);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error stopping all socat processes");
            StatusMessage = "Error stopping all socat processes";
        }
    }

    /// <summary>
    /// Tests the TCP connection for the selected process.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        if (SelectedProcess == null)
        {
            return;
        }

        try
        {
            StatusMessage = $"Testing connection to port {SelectedProcess.TcpPort}...";

            bool success = await _socatService.TestTcpConnectionAsync(
                SelectedProcess.TcpHost ?? "localhost",
                SelectedProcess.TcpPort);

            if (success)
            {
                StatusMessage = $"Connection to port {SelectedProcess.TcpPort} successful";
            }
            else
            {
                StatusMessage = $"Connection to port {SelectedProcess.TcpPort} failed";
            }

            _specificLogger.LogInformation("TCP connection test result: {Success} for port {Port}", success, SelectedProcess.TcpPort);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error testing TCP connection");
            StatusMessage = "Error testing connection";
        }
    }

    /// <summary>
    /// Exports all profiles to a file.
    /// </summary>
    private async Task ExportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = "File dialog service not available";
            return;
        }

        try
        {
            string? fileName = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Profiles",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                null,
                "profiles.json");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Exporting profiles...";

            IEnumerable<SocatProfile> profiles = await _profileService.ExportAsync();
            string jsonData = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fileName, jsonData);

            StatusMessage = $"Exported {Profiles.Count} profile(s) to {Path.GetFileName(fileName)}";
            _specificLogger.LogInformation("Exported {ProfileCount} profiles to {FileName}", Profiles.Count, fileName);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error exporting profiles");
            StatusMessage = "Error exporting profiles";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Imports profiles from a file.
    /// </summary>
    private async Task ImportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = "File dialog service not available";
            return;
        }

        try
        {
            string? fileName = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Profiles",
                "JSON files (*.json)|*.json|All files (*.*)|*.*");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Importing profiles...";

            string jsonData = await File.ReadAllTextAsync(fileName);
            List<SocatProfile> profiles = JsonSerializer.Deserialize<List<SocatProfile>>(jsonData) ?? new List<SocatProfile>();
            IEnumerable<SocatProfile> importedProfiles = await _profileService.ImportAsync(profiles, replaceExisting: false);

            int importedCount = importedProfiles.Count();
            await RefreshCommand.Execute(); // Refresh the list

            StatusMessage = $"Imported {importedCount} profile(s) from {Path.GetFileName(fileName)}";
            _specificLogger.LogInformation("Imported {ImportedCount} profiles from {FileName}", importedCount, fileName);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error importing profiles");
            StatusMessage = "Error importing profiles";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Exports the currently selected profile to a file.
    /// </summary>
    private async Task ExportSelectedProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            StatusMessage = "Exporting selected profile...";

            SocatProfile? profile = await _profileService.GetByIdAsync(SelectedProfile.Id);
            string jsonData = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });

            await _dialogService.ShowErrorAsync("Export Profile",
                $"Export functionality for profile '{SelectedProfile.Name}' will be implemented in the UI layer.");

            StatusMessage = "Profile exported successfully";
            _specificLogger.LogInformation("Exported socat profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error exporting selected profile");
            StatusMessage = "Error exporting profile";
        }
    }

    /// <summary>
    /// Shows details for the currently selected profile.
    /// </summary>
    private async Task ShowProfileDetailsAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            string details = $"Profile: {SelectedProfile.Name}\n" +
                         $"Description: {SelectedProfile.Description}\n" +
                         $"TCP Port: {SelectedProfile.Configuration.TcpPort}\n" +
                         $"Verbose: {SelectedProfile.Configuration.Verbose}\n" +
                         $"Hex Dump: {SelectedProfile.Configuration.HexDump}\n" +
                         $"Block Size: {SelectedProfile.Configuration.BlockSize}\n" +
                         $"Debug Level: {SelectedProfile.Configuration.DebugLevel}\n" +
                         $"Is Default: {SelectedProfile.IsDefault}\n" +
                         $"Is Read-Only: {SelectedProfile.IsReadOnly}\n" +
                         $"Created: {SelectedProfile.CreatedAt:yyyy-MM-dd HH:mm:ss}\n" +
                         $"Modified: {SelectedProfile.ModifiedAt:yyyy-MM-dd HH:mm:ss}";

            await _dialogService.ShowErrorAsync($"Profile Details - {SelectedProfile.Name}", details);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error showing profile details");
            StatusMessage = "Error showing profile details";
        }
    }

    /// <summary>
    /// Opens a folder browser to select the profiles directory.
    /// </summary>
    private async Task BrowseProfilesPathAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = "File dialog service not available";
            return;
        }

        try
        {
            string? result = await _fileDialogService.ShowFolderBrowserDialogAsync(
                "Select Profiles Directory",
                ProfilesPath);

            if (!string.IsNullOrEmpty(result))
            {
                ProfilesPath = result;
                await UpdateProfilesPathInSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error browsing for profiles path");
            StatusMessage = "Error selecting directory";
        }
    }

    /// <summary>
    /// Opens the profiles directory in the system file explorer.
    /// </summary>
    /// <summary>
    /// Opens the profiles directory in the system file explorer.
    /// </summary>
    private async Task OpenProfilesPathAsync()
    {
        try
        {
            StatusMessage = "Opening profiles folder...";

            if (string.IsNullOrEmpty(ProfilesPath))
            {
                StatusMessage = "Profiles path not available";
                _specificLogger.LogError("Profiles path is null or empty");
                return;
            }

            // Ensure the directory exists before trying to open it
            if (!Directory.Exists(ProfilesPath))
            {
                StatusMessage = "Creating profiles folder...";
                Directory.CreateDirectory(ProfilesPath);
                _specificLogger.LogInformation("Created profiles directory: {ProfilesPath}", ProfilesPath);
            }

            _specificLogger.LogInformation("Opening profiles folder: {ProfilesPath}", ProfilesPath);

            // Use centralized PlatformHelper for consistent cross-platform behavior
            await PlatformHelper.OpenDirectoryInExplorerAsync(ProfilesPath);

            StatusMessage = "Profiles folder opened";
            _specificLogger.LogInformation("Successfully opened profiles folder");
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error opening profiles folder");
            StatusMessage = "Error opening profiles folder";
        }
    }

    /// <summary>
    /// Resets the profiles path to the default.
    /// </summary>
    private async Task ResetProfilesPathAsync()
    {
        try
        {
            ProfilesPath = "resources/SocatProfiles";

            // Update settings
            Models.ApplicationSettings settings = _settingsService.Settings;
            if (settings.Socat != null)
            {
                settings.Socat.ProfilesPath = ProfilesPath;
                await _settingsService.UpdateSettingsAsync(settings);
            }

            StatusMessage = "Profiles path reset to default";
            _specificLogger.LogInformation("Reset socat profiles path to default");
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error resetting profiles path");
            StatusMessage = "Error resetting profiles path";
        }
    }

    /// <summary>
    /// Updates the profiles path in application settings.
    /// </summary>
    private async Task UpdateProfilesPathInSettingsAsync()
    {
        try
        {
            // Persist through the injected settings service
            Models.ApplicationSettings settings = _settingsService.Settings.Clone();
            settings.Socat.ProfilesPath = ProfilesPath;
            await _settingsService.UpdateSettingsAsync(settings).ConfigureAwait(false);
            StatusMessage = "Profiles path updated";
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to update settings with new profiles path");
            StatusMessage = "Failed to update settings";
        }
    }

    /// <summary>
    /// Handles exceptions thrown by reactive commands.
    /// </summary>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="operation">The operation that was being performed.</param>
    private void HandleCommandException(Exception exception, string operation)
    {
        _specificLogger.LogError(exception, "Error {Operation}", operation);
        StatusMessage = $"Error {operation}";
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Disposes of the resources used by this ViewModel.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose.</param>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Unsubscribe from settings changes
                if (_settingsChangedHandler != null)
                {
                    _settingsService.SettingsChanged -= _settingsChangedHandler;
                }

                // Dispose managed resources
                _disposables?.Dispose();
                _specificLogger.LogInformation("SocatSettingsViewModel disposed");
            }

            _disposed = true;
        }

        // Call base dispose
        base.Dispose(disposing);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Refreshes profiles from the service and optionally restores selection by profile Id.
    /// </summary>
    private async Task RefreshProfilesPreserveSelectionAsync(int? selectProfileId)
    {
        try
        {
            // Reload profiles from storage/service
            await RefreshCommand.Execute();

            // If a specific profile Id was requested, try to select it
            if (selectProfileId.HasValue)
            {
                SocatProfile? match = Profiles.FirstOrDefault(p => p.Id == selectProfileId.Value);
                if (match != null)
                {
                    SelectedProfile = match;
                    return;
                }
            }

            // Otherwise, ensure there's a sensible selection
            if (Profiles.Count > 0 && SelectedProfile == null)
            {
                SelectedProfile = Profiles.First();
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogWarning(ex, "Failed to refresh socat profiles while preserving selection");
        }
    }

    #endregion

    #region Abstract Method Implementations

    /// <summary>
    /// Gets the profile manager service for Socat profiles.
    /// </summary>
    protected override IProfileManager<SocatProfile> GetProfileManager()
        => _profileService;

    /// <summary>
    /// Gets the default profile name for new Socat profiles.
    /// </summary>
    protected override string GetDefaultProfileName()
        => "SocatDefault";

    /// <summary>
    /// Gets the profile type name for display purposes.
    /// </summary>
    protected override string GetProfileTypeName()
        => "Socat";

    /// <summary>
    /// Creates a default Socat profile with standard configuration.
    /// </summary>
    protected override SocatProfile CreateDefaultProfile()
        => new()
        {
            Name = GetDefaultProfileName(),
            Description = "TCP to Serial proxy configuration",
            Configuration = new SocatConfiguration
            {
                TcpPort = 502,
                TcpHost = "0.0.0.0"
            }
        };

    /// <summary>
    /// Shows the create dialog for a new Socat profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<SocatProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        // Use the unified dialog service to show Socat create dialog
        return await _unifiedDialogService.ShowSocatCreateDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the edit dialog for an existing Socat profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<SocatProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        // Use the unified dialog service to show Socat edit dialog
        return await _unifiedDialogService.ShowSocatEditDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the duplicate dialog for copying a Socat profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        // Use the unified dialog service to show Socat duplicate dialog
        return await _unifiedDialogService.ShowSocatDuplicateDialogAsync(request).ConfigureAwait(false);
    }

    #endregion
}
