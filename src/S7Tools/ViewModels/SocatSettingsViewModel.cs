using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Servers settings category, providing comprehensive socat profile management
/// and process control capabilities for Serial-to-TCP proxy functionality.
/// </summary>
public class SocatSettingsViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly ISocatProfileService _profileService;
    private readonly ISocatService _socatService;
    private readonly ISerialPortService _serialPortService;
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<SocatSettingsViewModel> _logger;
    private readonly S7Tools.Services.Interfaces.ISettingsService _settingsService;
    private readonly S7Tools.Services.Interfaces.IUIThreadService _uiThreadService;
    private EventHandler<S7Tools.Models.ApplicationSettings>? _settingsChangedHandler;
    private readonly CompositeDisposable _disposables = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SocatSettingsViewModel class.
    /// </summary>
    /// <param name="profileService">The socat profile service.</param>
    /// <param name="socatService">The socat service.</param>
    /// <param name="serialPortService">The serial port service for device discovery.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="settingsService">The settings service used to persist application settings.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="logger">The logger.</param>
    public SocatSettingsViewModel(
        ISocatProfileService profileService,
        ISocatService socatService,
        ISerialPortService serialPortService,
        IDialogService dialogService,
        IFileDialogService? fileDialogService,
        S7Tools.Services.Interfaces.ISettingsService settingsService,
        S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
        ILogger<SocatSettingsViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
            await LoadProfilesAsync();
            await ScanSerialDevicesAsync();
            await RefreshRunningProcessesAsync();
        });

        _logger.LogInformation("SocatSettingsViewModel initialized");

        // Update socat command preview when either profile or device selection changes
        this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedSerialDevice)
            .Subscribe(values =>
            {
                var (profile, selectedDevice) = values;
                try
                {
                    if (profile == null)
                    {
                        SelectedProfileSocatCommand = string.Empty;
                    }
                    else
                    {
                        // Use the actual selected device if available, otherwise use placeholder
                        var deviceToUse = !string.IsNullOrEmpty(selectedDevice) ? selectedDevice : "/dev/ttyUSB0";

                        try
                        {
                            // Generate command with the actual selected device for accurate preview
                            SelectedProfileSocatCommand = _socatService.GenerateSocatCommandForProfile(profile, deviceToUse);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate socat command for profile '{ProfileName}' with device '{Device}'. Falling back to configuration command.", profile?.Name, deviceToUse);
                            // Fallback to configuration command generation
                            SelectedProfileSocatCommand = _socatService.GenerateSocatCommand(profile.Configuration, deviceToUse);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating socat command preview for selected profile '{ProfileName}' and device '{Device}'.", profile?.Name, selectedDevice);
                    SelectedProfileSocatCommand = "Error generating command";
                }
            })
            .DisposeWith(_disposables);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of available socat profiles.
    /// </summary>
    public ObservableCollection<SocatProfile> Profiles { get; }

    /// <summary>
    /// Gets the collection of available serial devices.
    /// </summary>
    public ObservableCollection<string> AvailableSerialDevices { get; }

    /// <summary>
    /// Gets the collection of running socat processes.
    /// </summary>
    public ObservableCollection<SocatProcessInfo> RunningProcesses { get; }

    private SocatProfile? _selectedProfile;
    /// <summary>
    /// Gets or sets the currently selected profile.
    /// </summary>
    public SocatProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

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

    private bool _isLoading;
    /// <summary>
    /// Gets or sets a value indicating whether a loading operation is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private string _statusMessage = "Ready";
    /// <summary>
    /// Gets or sets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
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

    private string _newProfileName = string.Empty;
    /// <summary>
    /// Gets or sets the name for a new profile being created.
    /// </summary>
    public string NewProfileName
    {
        get => _newProfileName;
        set => this.RaiseAndSetIfChanged(ref _newProfileName, value);
    }

    private string _newProfileDescription = string.Empty;
    /// <summary>
    /// Gets or sets the description for a new profile being created.
    /// </summary>
    public string NewProfileDescription
    {
        get => _newProfileDescription;
        set => this.RaiseAndSetIfChanged(ref _newProfileDescription, value);
    }

    private int _profileCount;
    /// <summary>
    /// Gets or sets the total number of profiles.
    /// </summary>
    public int ProfileCount
    {
        get => _profileCount;
        set => this.RaiseAndSetIfChanged(ref _profileCount, value);
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

    private string _profilesPath = string.Empty;
    /// <summary>
    /// Gets or sets the current profiles directory path shown in the UI.
    /// This value is persisted to application settings when changed.
    /// </summary>
    public string ProfilesPath
    {
        get => _profilesPath;
        set => this.RaiseAndSetIfChanged(ref _profilesPath, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to create a new profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to edit the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to delete the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to duplicate the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DuplicateProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to set the selected profile as default.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SetDefaultProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to refresh the profiles list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshProfilesCommand { get; private set; } = null!;

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
        // Create profile command - enabled when name is not empty
        var canCreateProfile = this.WhenAnyValue(x => x.NewProfileName)
            .Select(name => !string.IsNullOrWhiteSpace(name));

        CreateProfileCommand = ReactiveCommand.CreateFromTask(CreateProfileAsync, canCreateProfile);
        CreateProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "creating profile"))
            .DisposeWith(_disposables);

        // Edit profile command - enabled when a profile is selected and not read-only
        var canEditProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && profile.CanModify());

        EditProfileCommand = ReactiveCommand.CreateFromTask(EditProfileAsync, canEditProfile);
        EditProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "editing profile"))
            .DisposeWith(_disposables);

        // Delete profile command - enabled when a profile is selected and can be deleted
        var canDeleteProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && profile.CanDelete());

        DeleteProfileCommand = ReactiveCommand.CreateFromTask(DeleteProfileAsync, canDeleteProfile);
        DeleteProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "deleting profile"))
            .DisposeWith(_disposables);

        // Duplicate profile command - enabled when a profile is selected
        var canDuplicateProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        DuplicateProfileCommand = ReactiveCommand.CreateFromTask(DuplicateProfileAsync, canDuplicateProfile);
        DuplicateProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "duplicating profile"))
            .DisposeWith(_disposables);

        // Set default profile command - enabled when a profile is selected and not already default
        var canSetDefaultProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && !profile.IsDefault);

        SetDefaultProfileCommand = ReactiveCommand.CreateFromTask(SetDefaultProfileAsync, canSetDefaultProfile);
        SetDefaultProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "setting default profile"))
            .DisposeWith(_disposables);

        // Refresh profiles command - always enabled
        RefreshProfilesCommand = ReactiveCommand.CreateFromTask(LoadProfilesAsync);
        RefreshProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "refreshing profiles"))
            .DisposeWith(_disposables);

        // Scan serial devices command - always enabled
        ScanSerialDevicesCommand = ReactiveCommand.CreateFromTask(ScanSerialDevicesAsync);
        ScanSerialDevicesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "scanning serial devices"))
            .DisposeWith(_disposables);

        // Start socat command - enabled when both profile and device are selected
        var canStartSocat = this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedSerialDevice)
            .Select(tuple => tuple.Item1 != null && !string.IsNullOrEmpty(tuple.Item2));

        StartSocatCommand = ReactiveCommand.CreateFromTask(StartSocatAsync, canStartSocat);
        StartSocatCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "starting socat"))
            .DisposeWith(_disposables);

        // Stop socat command - enabled when a process is selected
        var canStopSocat = this.WhenAnyValue(x => x.SelectedProcess)
            .Select(process => process != null && process.IsRunning);

        StopSocatCommand = ReactiveCommand.CreateFromTask(StopSocatAsync, canStopSocat);
        StopSocatCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "stopping socat"))
            .DisposeWith(_disposables);

        // Stop all socat command - enabled when there are running processes
        var canStopAllSocat = this.WhenAnyValue(x => x.RunningProcessCount)
            .Select(count => count > 0);

        StopAllSocatCommand = ReactiveCommand.CreateFromTask(StopAllSocatAsync, canStopAllSocat);
        StopAllSocatCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "stopping all socat processes"))
            .DisposeWith(_disposables);

        // Refresh processes command - always enabled
        RefreshProcessesCommand = ReactiveCommand.CreateFromTask(RefreshRunningProcessesAsync);
        RefreshProcessesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "refreshing processes"))
            .DisposeWith(_disposables);

        // Test connection command - enabled when a process is selected
        var canTestConnection = this.WhenAnyValue(x => x.SelectedProcess)
            .Select(process => process != null && process.IsRunning);

        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync, canTestConnection);
        TestConnectionCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing connection"))
            .DisposeWith(_disposables);

        // Export profiles command - enabled when there are profiles
        var canExport = this.WhenAnyValue(x => x.ProfileCount)
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
        var canExportSelected = this.WhenAnyValue(x => x.SelectedProfile)
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
                var existingProcess = RunningProcesses.FirstOrDefault(p => p.ProcessId == args.ProcessInfo.ProcessId);
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
                _logger.LogError(args.Error, "socat process error");
            });
        };

        _socatService.ConnectionEstablished += async (sender, args) =>
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                var existingProcess = RunningProcesses.FirstOrDefault(p => p.ProcessId == args.ProcessInfo.ProcessId);
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
                var existingProcess = RunningProcesses.FirstOrDefault(p => p.ProcessId == args.ProcessInfo.ProcessId);
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
            var settings = _settingsService.Settings;
            ProfilesPath = settings.Socat?.ProfilesPath ?? "resources/SocatProfiles";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing profiles path from settings");
            ProfilesPath = "resources/SocatProfiles";
        }
    }

    /// <summary>
    /// Loads all profiles from the profile service.
    /// </summary>
    private async Task LoadProfilesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading profiles...";

            var profiles = await _profileService.GetAllProfilesAsync();

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                Profiles.Clear();
                foreach (var profile in profiles)
                {
                    Profiles.Add(profile);
                }
                ProfileCount = Profiles.Count;

                // Select default profile if available
                var defaultProfile = Profiles.FirstOrDefault(p => p.IsDefault);
                if (defaultProfile != null)
                {
                    SelectedProfile = defaultProfile;
                }

                StatusMessage = $"Loaded {ProfileCount} profile(s)";
            });

            _logger.LogInformation("Loaded {ProfileCount} socat profiles", ProfileCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profiles");
            StatusMessage = "Error loading profiles";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Scans for available serial devices.
    /// </summary>
    private async Task ScanSerialDevicesAsync()
    {
        try
        {
            IsScanning = true;
            StatusMessage = "Scanning for serial devices...";

            var deviceInfos = await _serialPortService.ScanAvailablePortsAsync();
            var devices = deviceInfos.Select(info => info.PortPath);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                AvailableSerialDevices.Clear();
                foreach (var device in devices.OrderBy(d => d))
                {
                    AvailableSerialDevices.Add(device);
                }
                DeviceCount = AvailableSerialDevices.Count;

                // Select first USB device if available
                var usbDevice = AvailableSerialDevices.FirstOrDefault(d => d.Contains("ttyUSB") || d.Contains("ttyACM"));
                if (usbDevice != null)
                {
                    SelectedSerialDevice = usbDevice;
                }

                StatusMessage = $"Found {DeviceCount} serial device(s)";
            });

            _logger.LogInformation("Found {DeviceCount} serial devices", DeviceCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning serial devices");
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
        try
        {
            StatusMessage = "Refreshing running processes...";

            var processes = await _socatService.GetRunningProcessesAsync();

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                RunningProcesses.Clear();
                foreach (var process in processes)
                {
                    RunningProcesses.Add(process);
                }
                RunningProcessCount = RunningProcesses.Count;

                StatusMessage = $"Found {RunningProcessCount} running process(es)";
            });

            _logger.LogInformation("Found {ProcessCount} running socat processes", RunningProcessCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing running processes");
            StatusMessage = "Error refreshing processes";
        }
    }

    /// <summary>
    /// Creates a new profile with the specified name and description.
    /// </summary>
    private async Task CreateProfileAsync()
    {
        try
        {
            StatusMessage = "Creating profile...";

            var profile = SocatProfile.CreateUserProfile(NewProfileName, NewProfileDescription);
            var createdProfile = await _profileService.CreateProfileAsync(profile);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                Profiles.Add(createdProfile);
                SelectedProfile = createdProfile;
                ProfileCount = Profiles.Count;

                // Clear input fields
                NewProfileName = string.Empty;
                NewProfileDescription = string.Empty;

                StatusMessage = $"Profile '{createdProfile.Name}' created successfully";
            });

            _logger.LogInformation("Created socat profile: {ProfileName}", createdProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            StatusMessage = "Error creating profile";
        }
    }

    /// <summary>
    /// Edits the currently selected profile.
    /// </summary>
    private async Task EditProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            // TODO: Open profile edit dialog/view
            // For now, just show a message
            await _dialogService.ShowErrorAsync("Edit Profile",
                $"Edit functionality for profile '{SelectedProfile.Name}' will be implemented in the UI layer.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing profile");
            StatusMessage = "Error editing profile";
        }
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
            var profileToDelete = SelectedProfile;
            var result = await _dialogService.ShowConfirmationAsync("Delete Profile",
                $"Are you sure you want to delete profile '{profileToDelete.Name}'?");

            if (result)
            {
                StatusMessage = "Deleting profile...";

                await _profileService.DeleteProfileAsync(profileToDelete.Id);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    Profiles.Remove(profileToDelete);
                    ProfileCount = Profiles.Count;
                    SelectedProfile = Profiles.FirstOrDefault();

                    StatusMessage = "Profile deleted successfully";
                });

                _logger.LogInformation("Deleted socat profile: {ProfileName}", profileToDelete.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile");
            StatusMessage = "Error deleting profile";
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
            var originalProfile = SelectedProfile;
            var newName = $"{originalProfile.Name} (Copy)";
            var duplicatedProfile = await _profileService.DuplicateProfileAsync(originalProfile.Id, newName);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                Profiles.Add(duplicatedProfile);
                SelectedProfile = duplicatedProfile;
                ProfileCount = Profiles.Count;

                StatusMessage = $"Profile duplicated as '{duplicatedProfile.Name}'";
            });

            _logger.LogInformation("Duplicated socat profile: {OriginalName} -> {NewName}",
                originalProfile.Name, duplicatedProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile");
            StatusMessage = "Error duplicating profile";
        }
    }

    /// <summary>
    /// Sets the currently selected profile as the default.
    /// </summary>
    private async Task SetDefaultProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            StatusMessage = "Setting default profile...";

            await _profileService.SetDefaultProfileAsync(SelectedProfile.Id);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                // Update all profiles' default status
                foreach (var profile in Profiles)
                {
                    profile.IsDefault = profile.Id == SelectedProfile.Id;
                }

                StatusMessage = $"'{SelectedProfile.Name}' set as default profile";
            });

            _logger.LogInformation("Set default socat profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default profile");
            StatusMessage = "Error setting default profile";
        }
    }

    /// <summary>
    /// Starts socat with the selected profile and serial device.
    /// </summary>
    private async Task StartSocatAsync()
    {
        if (SelectedProfile == null || string.IsNullOrEmpty(SelectedSerialDevice))
        {
            return;
        }

        try
        {
            StatusMessage = $"Starting socat on port {SelectedProfile.Configuration.TcpPort}...";

            var processInfo = await _socatService.StartSocatWithProfileAsync(SelectedProfile, SelectedSerialDevice);

            StatusMessage = $"socat started successfully (PID: {processInfo.ProcessId})";
            _logger.LogInformation("Started socat process: {ProcessId} for profile {ProfileName} on device {Device}",
                processInfo.ProcessId, SelectedProfile.Name, SelectedSerialDevice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting socat");
            StatusMessage = "Error starting socat";
        }
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

            var success = await _socatService.StopSocatAsync(SelectedProcess);

            if (success)
            {
                StatusMessage = $"socat process {SelectedProcess.ProcessId} stopped successfully";
                _logger.LogInformation("Stopped socat process: {ProcessId}", SelectedProcess.ProcessId);
            }
            else
            {
                StatusMessage = $"Failed to stop socat process {SelectedProcess.ProcessId}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping socat process");
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

            var stoppedCount = await _socatService.StopAllSocatProcessesAsync();

            StatusMessage = $"Stopped {stoppedCount} socat process(es)";
            _logger.LogInformation("Stopped {Count} socat processes", stoppedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping all socat processes");
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

            var success = await _socatService.TestTcpConnectionAsync(
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

            _logger.LogInformation("TCP connection test result: {Success} for port {Port}", success, SelectedProcess.TcpPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing TCP connection");
            StatusMessage = "Error testing connection";
        }
    }

    /// <summary>
    /// Exports all profiles to a file.
    /// </summary>
    private async Task ExportProfilesAsync()
    {
        try
        {
            StatusMessage = "Exporting profiles...";

            var jsonData = await _profileService.ExportAllProfilesToJsonAsync();

            // TODO: Save to file using file dialog
            await _dialogService.ShowErrorAsync("Export Profiles",
                "Export functionality will be implemented in the UI layer.");

            StatusMessage = "Profiles exported successfully";
            _logger.LogInformation("Exported all socat profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting profiles");
            StatusMessage = "Error exporting profiles";
        }
    }

    /// <summary>
    /// Imports profiles from a file.
    /// </summary>
    private async Task ImportProfilesAsync()
    {
        try
        {
            StatusMessage = "Importing profiles...";

            // TODO: Load from file using file dialog
            await _dialogService.ShowErrorAsync("Import Profiles",
                "Import functionality will be implemented in the UI layer.");

            StatusMessage = "Profiles imported successfully";
            _logger.LogInformation("Imported socat profiles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing profiles");
            StatusMessage = "Error importing profiles";
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

            var jsonData = await _profileService.ExportProfileToJsonAsync(SelectedProfile.Id);

            // TODO: Save to file using file dialog
            await _dialogService.ShowErrorAsync("Export Profile",
                $"Export functionality for profile '{SelectedProfile.Name}' will be implemented in the UI layer.");

            StatusMessage = "Profile exported successfully";
            _logger.LogInformation("Exported socat profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting selected profile");
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
            var details = $"Profile: {SelectedProfile.Name}\n" +
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
            _logger.LogError(ex, "Error showing profile details");
            StatusMessage = "Error showing profile details";
        }
    }

    /// <summary>
    /// Opens a folder browser to select the profiles directory.
    /// </summary>
    private async Task BrowseProfilesPathAsync()
    {
        try
        {
            // TODO: Implement folder dialog
            await _dialogService.ShowErrorAsync("Browse Profiles Path",
                "Folder selection will be implemented in the UI layer.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing profiles path");
            StatusMessage = "Error browsing profiles path";
        }
    }

    /// <summary>
    /// Opens the profiles directory in the system file explorer.
    /// </summary>
    private async Task OpenProfilesPathAsync()
    {
        try
        {
            // TODO: Implement system file explorer opening
            await _dialogService.ShowErrorAsync("Open Profiles Path",
                $"Opening '{ProfilesPath}' in file explorer will be implemented in the UI layer.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening profiles path");
            StatusMessage = "Error opening profiles path";
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
            var settings = _settingsService.Settings;
            if (settings.Socat != null)
            {
                settings.Socat.ProfilesPath = ProfilesPath;
                await _settingsService.UpdateSettingsAsync(settings);
            }

            StatusMessage = "Profiles path reset to default";
            _logger.LogInformation("Reset socat profiles path to default");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting profiles path");
            StatusMessage = "Error resetting profiles path";
        }
    }

    /// <summary>
    /// Handles exceptions thrown by reactive commands.
    /// </summary>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="operation">The operation that was being performed.</param>
    private void HandleCommandException(Exception exception, string operation)
    {
        _logger.LogError(exception, "Error {Operation}", operation);
        StatusMessage = $"Error {operation}";
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Disposes of the resources used by this ViewModel.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
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
                _logger.LogInformation("SocatSettingsViewModel disposed");
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Public implementation of Dispose pattern callable by consumers.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
