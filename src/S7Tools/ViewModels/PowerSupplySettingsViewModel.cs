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
/// ViewModel for the Power Supply settings category, providing comprehensive profile management
/// and power supply control capabilities for Modbus TCP devices.
/// </summary>
public class PowerSupplySettingsViewModel : ProfileManagementViewModelBase<PowerSupplyProfile>
{
    #region Fields

    private readonly IPowerSupplyProfileService _profileService;
    private readonly IPowerSupplyService _powerSupplyService;
    private readonly IDialogService _dialogService;
    private readonly IUnifiedProfileDialogService _unifiedDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<PowerSupplySettingsViewModel> _specificLogger;
    private readonly S7Tools.Services.Interfaces.ISettingsService _settingsService;
    private readonly S7Tools.Services.Interfaces.IUIThreadService _uiThreadService;
    private EventHandler<S7Tools.Models.ApplicationSettings>? _settingsChangedHandler;
    private readonly CompositeDisposable _disposables = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the PowerSupplySettingsViewModel class.
    /// </summary>
    /// <param name="unifiedDialogService">The unified profile dialog service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="profileService">The power supply profile service.</param>
    /// <param name="powerSupplyService">The power supply service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="settingsService">The settings service used to persist application settings.</param>
    public PowerSupplySettingsViewModel(
        IUnifiedProfileDialogService unifiedDialogService,
        ILogger<ProfileManagementViewModelBase<PowerSupplyProfile>> logger,
        S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
        IPowerSupplyProfileService profileService,
        IPowerSupplyService powerSupplyService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IFileDialogService? fileDialogService,
        S7Tools.Services.Interfaces.ISettingsService settingsService)
        : base(logger, unifiedDialogService, dialogService, uiThreadService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _powerSupplyService = powerSupplyService ?? throw new ArgumentNullException(nameof(powerSupplyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _unifiedDialogService = unifiedDialogService;
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _uiThreadService = uiThreadService;

        // Store specific logger (use constructor parameter, not create new factory)
        _specificLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { }).CreateLogger<PowerSupplySettingsViewModel>();

        // DON'T initialize Profiles collection - base class provides it
        // Profiles collection is provided by base class ProfileManagementViewModelBase

        // Initialize path commands (to be implemented in next increment)
        InitializePathCommands();

        // Initialize profile commands (to be implemented in next increment)
        InitializeProfileCommands();

        // Initialize connection commands (to be implemented in next increment)
        InitializeConnectionCommands();

        // Initialize power control commands (to be implemented in next increment)
        InitializePowerControlCommands();

        // Initialize ProfilesPath from settings and subscribe to changes
        RefreshFromSettings();
        _settingsChangedHandler = (_, __) => RefreshFromSettings();
        _settingsService.SettingsChanged += _settingsChangedHandler;

        // Setup property change subscriptions
        SetupPropertySubscriptions();

        // DON'T call LoadProfilesAsync() - base class handles profile loading automatically

        _specificLogger.LogInformation("PowerSupplySettingsViewModel initialized");
    }

    #endregion

    #region Properties

    // Profile collection properties (Profiles, SelectedProfile, IsLoading, StatusMessage, etc.)
    // and CRUD commands are inherited from ProfileManagementViewModelBase<PowerSupplyProfile>

    private bool _isConnected;
    /// <summary>
    /// Gets or sets a value indicating whether the power supply is currently connected.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    private bool _isPowerOn;
    /// <summary>
    /// Gets or sets a value indicating whether the power is currently ON.
    /// </summary>
    public bool IsPowerOn
    {
        get => _isPowerOn;
        private set => this.RaiseAndSetIfChanged(ref _isPowerOn, value);
    }

    private string _connectionStatus = "Disconnected";
    /// <summary>
    /// Gets or sets the connection status message.
    /// </summary>
    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    private string _powerStatus = "Unknown";
    /// <summary>
    /// Gets or sets the power status message.
    /// </summary>
    public string PowerStatus
    {
        get => _powerStatus;
        private set => this.RaiseAndSetIfChanged(ref _powerStatus, value);
    }

    #endregion

    #region Commands - PowerSupply Specific

    // Connection Management Commands
    /// <summary>Gets the command to connect to the power supply.</summary>
    public ReactiveCommand<Unit, Unit> ConnectCommand { get; private set; } = null!;
    /// <summary>Gets the command to disconnect from the power supply.</summary>
    public ReactiveCommand<Unit, Unit> DisconnectCommand { get; private set; } = null!;
    /// <summary>Gets the command to test the power supply connection.</summary>
    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; private set; } = null!;

    // Power Control Commands
    /// <summary>Gets the command to turn on the power supply.</summary>
    public ReactiveCommand<Unit, Unit> TurnOnCommand { get; private set; } = null!;
    /// <summary>Gets the command to turn off the power supply.</summary>
    public ReactiveCommand<Unit, Unit> TurnOffCommand { get; private set; } = null!;
    /// <summary>Gets the command to read the power supply state.</summary>
    public ReactiveCommand<Unit, Unit> ReadStateCommand { get; private set; } = null!;
    /// <summary>Gets the command to power cycle the power supply.</summary>
    public ReactiveCommand<Unit, Unit> PowerCycleCommand { get; private set; } = null!;

    // Import/Export Commands
    /// <summary>Gets the command to export power supply profiles.</summary>
    public ReactiveCommand<Unit, Unit> ExportProfilesCommand { get; private set; } = null!;
    /// <summary>Gets the command to import power supply profiles.</summary>
    public ReactiveCommand<Unit, Unit> ImportProfilesCommand { get; private set; } = null!;

    // Path Management Commands (PowerSupply-specific implementations)
    /// <summary>Gets the command to browse for profiles path.</summary>
    public ReactiveCommand<Unit, Unit> BrowseProfilesPathCommand { get; private set; } = null!;
    /// <summary>Gets the command to open profiles path in explorer.</summary>
    public ReactiveCommand<Unit, Unit> OpenProfilesPathCommand { get; private set; } = null!;
    /// <summary>Gets the command to reset profiles path to default.</summary>
    public ReactiveCommand<Unit, Unit> ResetProfilesPathCommand { get; private set; } = null!;

    // Profile Management Commands (Create, Edit, Delete, Duplicate, etc.) are inherited from base class

    #endregion

    #region Command Initialization Methods

    /// <summary>
    /// Initializes path management commands.
    /// Implements PowerSupply-specific path operations while base class provides profile CRUD commands.
    /// </summary>
    private void InitializePathCommands()
    {
        // Initialize PowerSupply-specific path commands
        BrowseProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowseProfilesPathAsync);
        BrowseProfilesPathCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "browsing profiles path"))
            .DisposeWith(_disposables);

        OpenProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenProfilesPathAsync);
        OpenProfilesPathCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "opening profiles path"))
            .DisposeWith(_disposables);

        ResetProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetProfilesPathAsync);
        ResetProfilesPathCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "resetting profiles path"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes profile management commands.
    /// All profile commands (Create, Edit, Delete, Duplicate, etc.) are inherited from base class.
    /// </summary>
    private void InitializeProfileCommands()
    {
        // Profile management commands are provided by base class
        // No additional initialization needed for CRUD operations

        // Initialize PowerSupply-specific export/import commands
        var canExportProfiles = this.WhenAnyValue(x => x.Profiles.Count)
            .Select(count => count > 0);

        ExportProfilesCommand = ReactiveCommand.CreateFromTask(ExportProfilesAsync, canExportProfiles);
        ExportProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting profiles"))
            .DisposeWith(_disposables);

        // Import profiles command - always enabled
        ImportProfilesCommand = ReactiveCommand.CreateFromTask(ImportProfilesAsync);
        ImportProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "importing profiles"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes connection management commands.
    /// </summary>
    private void InitializeConnectionCommands()
    {
        // Connect command - enabled when not connected and profile is selected
        var canConnect = this.WhenAnyValue(x => x.IsConnected, x => x.SelectedProfile)
            .Select(tuple => !tuple.Item1 && tuple.Item2 != null);

        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync, canConnect);
        ConnectCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "connecting"))
            .DisposeWith(_disposables);

        // Disconnect command - enabled when connected
        var canDisconnect = this.WhenAnyValue(x => x.IsConnected);

        DisconnectCommand = ReactiveCommand.CreateFromTask(DisconnectAsync, canDisconnect);
        DisconnectCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "disconnecting"))
            .DisposeWith(_disposables);

        // Test connection command - enabled when profile is selected
        var canTestConnection = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync, canTestConnection);
        TestConnectionCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing connection"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes power control commands.
    /// </summary>
    private void InitializePowerControlCommands()
    {
        // Turn ON command - enabled when connected and power is off
        var canTurnOn = this.WhenAnyValue(x => x.IsConnected, x => x.IsPowerOn)
            .Select(tuple => tuple.Item1 && !tuple.Item2);

        TurnOnCommand = ReactiveCommand.CreateFromTask(TurnOnAsync, canTurnOn);
        TurnOnCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "turning power ON"))
            .DisposeWith(_disposables);

        // Turn OFF command - enabled when connected and power is on
        var canTurnOff = this.WhenAnyValue(x => x.IsConnected, x => x.IsPowerOn)
            .Select(tuple => tuple.Item1 && tuple.Item2);

        TurnOffCommand = ReactiveCommand.CreateFromTask(TurnOffAsync, canTurnOff);
        TurnOffCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "turning power OFF"))
            .DisposeWith(_disposables);

        // Read state command - enabled when connected
        var canReadState = this.WhenAnyValue(x => x.IsConnected);

        ReadStateCommand = ReactiveCommand.CreateFromTask(ReadStateAsync, canReadState);
        ReadStateCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "reading power state"))
            .DisposeWith(_disposables);

        // Power cycle command - enabled when connected
        var canPowerCycle = this.WhenAnyValue(x => x.IsConnected);

        PowerCycleCommand = ReactiveCommand.CreateFromTask(PowerCycleAsync, canPowerCycle);
        PowerCycleCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "power cycling"))
            .DisposeWith(_disposables);
    }

    #endregion

    #region Property Change Subscriptions

    /// <summary>
    /// Sets up property change subscriptions for reactive behavior.
    /// </summary>
    private void SetupPropertySubscriptions()
    {
        // Monitor connection status changes
        this.WhenAnyValue(x => x.IsConnected)
            .Skip(1) // Skip initial value
            .Subscribe(connected =>
            {
                _specificLogger.LogDebug("Connection status changed: {Status}", connected ? "Connected" : "Disconnected");
                UpdateConnectionStatus();

                // Clear power status when disconnected
                if (!connected)
                {
                    UpdatePowerStatus(false);
                    PowerStatus = "Unknown";
                }
            })
            .DisposeWith(_disposables);

        // Monitor power status changes
        this.WhenAnyValue(x => x.IsPowerOn)
            .Skip(1) // Skip initial value
            .Subscribe(powerOn =>
            {
                _specificLogger.LogDebug("Power status changed: {Status}", powerOn ? "ON" : "OFF");
            })
            .DisposeWith(_disposables);

        // Monitor selected profile changes
        this.WhenAnyValue(x => x.SelectedProfile)
            .Subscribe(profile =>
            {
                if (profile != null)
                {
                    _specificLogger.LogDebug("Selected profile changed: {ProfileName}", profile.Name);
                    StatusMessage = $"Selected: {profile.Name}";
                }
            })
            .DisposeWith(_disposables);

        // Monitor profiles collection count
        this.WhenAnyValue(x => x.Profiles)
            .Select(profiles => profiles?.Count ?? 0)
            .Subscribe(count =>
            {
                _specificLogger.LogDebug("Profile count changed: {Count}", count);
            })
            .DisposeWith(_disposables);

        // Monitor profiles path changes
        this.WhenAnyValue(x => x.ProfilesPath)
            .Skip(1) // Skip initial value
            .Subscribe(path =>
            {
                _specificLogger.LogDebug("Profiles path changed: {Path}", path);
            })
            .DisposeWith(_disposables);
    }

    #endregion

    #region Data Loading Methods

    // Profile loading is handled by base class ProfileManagementViewModelBase
    // Override LoadProfilesAsync is not needed - base class calls GetProfileManager().GetAllAsync()

    /// <summary>
    /// Refreshes settings from the settings service.
    /// </summary>
    private void RefreshFromSettings()
    {
        try
        {
            var settings = _settingsService.Settings;
            ProfilesPath = settings.PowerSupply.ProfilesPath;
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to refresh settings");
        }
    }

    #endregion

    #region Profile Management Commands Implementation



    /// <summary>
    /// Deletes the selected power supply profile after confirmation.
    /// </summary>
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            var profileName = SelectedProfile.Name;
            _specificLogger.LogDebug("Deleting power supply profile: {ProfileName}", profileName);

            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Profile",
                $"Are you sure you want to delete the profile '{profileName}'?").ConfigureAwait(false);

            if (confirmed)
            {
                var profileId = SelectedProfile.Id;
                await _profileService.DeleteAsync(profileId).ConfigureAwait(false);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    Profiles.Remove(SelectedProfile);
                    SelectedProfile = Profiles.FirstOrDefault();
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{profileName}' deleted successfully";
                _specificLogger.LogInformation("Deleted power supply profile: {ProfileName}", profileName);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to delete power supply profile");
            throw;
        }
    }

    /// <summary>
    /// Duplicates the selected power supply profile.
    /// </summary>
    private async Task DuplicateProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            _specificLogger.LogDebug("Duplicating power supply profile: {ProfileName}", SelectedProfile.Name);

            var inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Profile",
                "Enter a name for the duplicate profile:",
                $"{SelectedProfile.Name} (Copy)").ConfigureAwait(false);

            var newName = inputResult.Value;

            if (!string.IsNullOrWhiteSpace(newName))
            {
                var duplicatedProfile = await _profileService.DuplicateAsync(
                    SelectedProfile.Id,
                    newName).ConfigureAwait(false);

                // Refresh and select duplicated profile
                await RefreshProfilesPreserveSelectionAsync(duplicatedProfile.Id);

                StatusMessage = $"Profile duplicated as '{newName}'";
                _specificLogger.LogInformation("Duplicated power supply profile: {ProfileName} -> {NewName}",
                    SelectedProfile.Name, newName);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to duplicate power supply profile");
            throw;
        }
    }

    /// <summary>
    /// Sets the selected profile as the default profile.
    /// </summary>
    private async Task SetDefaultProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            _specificLogger.LogDebug("Setting default power supply profile: {ProfileName}", SelectedProfile.Name);

            await _profileService.SetDefaultAsync(SelectedProfile.Id).ConfigureAwait(false);
            _ = RefreshCommand.Execute();

            StatusMessage = $"Profile '{SelectedProfile.Name}' set as default";
            _specificLogger.LogInformation("Set default power supply profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to set default power supply profile");
            throw;
        }
    }

    /// <summary>
    /// Refreshes profiles while preserving the selection or selecting a specific profile by ID.
    /// </summary>
    /// <param name="selectProfileId">Optional profile ID to select after refresh.</param>
    private async Task RefreshProfilesPreserveSelectionAsync(int? selectProfileId)
    {
        try
        {
            // Reload profiles from storage/service
            await RefreshCommand.Execute();

            // If a specific profile Id was requested, try to select it
            if (selectProfileId.HasValue)
            {
                var match = Profiles.FirstOrDefault(p => p.Id == selectProfileId.Value);
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
            _specificLogger.LogError(ex, "Error refreshing profiles with selection preservation");
            StatusMessage = "Error refreshing profiles";
        }
    }

    /// <summary>
    /// Refreshes the profiles list.
    /// </summary>
    private async Task RefreshProfilesAsync()
    {
        try
        {
            _specificLogger.LogDebug("Refreshing power supply profiles");
            await RefreshCommand.Execute();
            StatusMessage = "Profiles refreshed";
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to refresh power supply profiles");
            throw;
        }
    }

    /// <summary>
    /// Exports profiles to a JSON file.
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
            _specificLogger.LogDebug("Exporting power supply profiles");

            var filePath = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Power Supply Profiles",
                "*.json",
                null,
                "power-supply-profiles.json").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(filePath))
            {
                var profiles = await _profileService.ExportAsync().ConfigureAwait(false);
                var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);

                StatusMessage = $"Exported {Profiles.Count} profiles to {filePath}";
                _specificLogger.LogInformation("Exported {Count} power supply profiles to {FilePath}",
                    Profiles.Count, filePath);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to export power supply profiles");
            throw;
        }
    }

    /// <summary>
    /// Imports profiles from a JSON file.
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
            _specificLogger.LogDebug("Importing power supply profiles");

            var filePath = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Power Supply Profiles",
                "*.json").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(filePath))
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var profiles = JsonSerializer.Deserialize<List<PowerSupplyProfile>>(json) ?? new List<PowerSupplyProfile>();
                var importedProfiles = await _profileService.ImportAsync(profiles, replaceExisting: false).ConfigureAwait(false);
                var count = importedProfiles.Count();

                _ = RefreshCommand.Execute();

                StatusMessage = $"Imported {count} profiles from {filePath}";
                _specificLogger.LogInformation("Imported {Count} power supply profiles from {FilePath}",
                    count, filePath);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to import power supply profiles");
            throw;
        }
    }

    #endregion

    #region Connection Management Commands Implementation

    /// <summary>
    /// Connects to the power supply using the selected profile.
    /// </summary>
    private async Task ConnectAsync()
    {
        if (SelectedProfile?.Configuration == null)
        {
            StatusMessage = "No profile selected or configuration missing";
            return;
        }

        try
        {
            _specificLogger.LogInformation("Connecting to power supply: {ProfileName}", SelectedProfile.Name);
            StatusMessage = "Connecting...";

            var success = await _powerSupplyService.ConnectAsync(SelectedProfile.Configuration).ConfigureAwait(false);

            if (success)
            {
                UpdateConnectionStatus();
                StatusMessage = $"Connected to {SelectedProfile.Name}";
                _specificLogger.LogInformation("Connected to power supply successfully");

                // Read initial power state
                await ReadStateAsync().ConfigureAwait(false);
            }
            else
            {
                StatusMessage = "Connection failed";
                _specificLogger.LogWarning("Failed to connect to power supply");
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error connecting to power supply");
            StatusMessage = $"Connection error: {ex.Message}";
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the power supply.
    /// </summary>
    private async Task DisconnectAsync()
    {
        try
        {
            _specificLogger.LogInformation("Disconnecting from power supply");
            StatusMessage = "Disconnecting...";

            await _powerSupplyService.DisconnectAsync().ConfigureAwait(false);

            UpdateConnectionStatus();
            UpdatePowerStatus(false);
            PowerStatus = "Unknown";
            StatusMessage = "Disconnected";
            _specificLogger.LogInformation("Disconnected from power supply successfully");
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error disconnecting from power supply");
            throw;
        }
    }

    /// <summary>
    /// Tests the connection to the power supply.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        if (SelectedProfile?.Configuration == null)
        {
            StatusMessage = "No profile selected or configuration missing";
            return;
        }

        try
        {
            _specificLogger.LogInformation("Testing connection to power supply: {ProfileName}", SelectedProfile.Name);
            StatusMessage = "Testing connection...";

            var success = await _powerSupplyService.TestConnectionAsync(SelectedProfile.Configuration).ConfigureAwait(false);

            if (success)
            {
                StatusMessage = "Connection test successful ✓";
                _specificLogger.LogInformation("Connection test successful");

                // Connection test successful - could show a dialog if needed
            }
            else
            {
                StatusMessage = "Connection test failed ✗";
                _specificLogger.LogWarning("Connection test failed");

                // Connection test failed - could show a dialog if needed
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error testing connection");
            StatusMessage = $"Connection test error: {ex.Message}";
            throw;
        }
    }

    #endregion

    #region Power Control Commands Implementation

    /// <summary>
    /// Turns the power ON.
    /// </summary>
    private async Task TurnOnAsync()
    {
        try
        {
            _specificLogger.LogInformation("Turning power ON");
            StatusMessage = "Turning power ON...";

            var success = await _powerSupplyService.TurnOnAsync().ConfigureAwait(false);

            if (success)
            {
                UpdatePowerStatus(true);
                StatusMessage = "Power turned ON ✓";
                _specificLogger.LogInformation("Power turned ON successfully");
            }
            else
            {
                StatusMessage = "Failed to turn power ON";
                _specificLogger.LogWarning("Failed to turn power ON");
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error turning power ON");
            throw;
        }
    }

    /// <summary>
    /// Turns the power OFF.
    /// </summary>
    private async Task TurnOffAsync()
    {
        try
        {
            _specificLogger.LogInformation("Turning power OFF");
            StatusMessage = "Turning power OFF...";

            var success = await _powerSupplyService.TurnOffAsync().ConfigureAwait(false);

            if (success)
            {
                UpdatePowerStatus(false);
                StatusMessage = "Power turned OFF ✓";
                _specificLogger.LogInformation("Power turned OFF successfully");
            }
            else
            {
                StatusMessage = "Failed to turn power OFF";
                _specificLogger.LogWarning("Failed to turn power OFF");
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error turning power OFF");
            throw;
        }
    }

    /// <summary>
    /// Reads the current power state.
    /// </summary>
    private async Task ReadStateAsync()
    {
        try
        {
            _specificLogger.LogDebug("Reading power state");
            StatusMessage = "Reading power state...";

            var powerOn = await _powerSupplyService.ReadPowerStateAsync().ConfigureAwait(false);

            UpdatePowerStatus(powerOn);
            StatusMessage = $"Power state: {(powerOn ? "ON" : "OFF")}";
            _specificLogger.LogInformation("Power state read: {State}", powerOn ? "ON" : "OFF");
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error reading power state");
            throw;
        }
    }

    /// <summary>
    /// Performs a power cycle (OFF → Wait → ON).
    /// </summary>
    private async Task PowerCycleAsync()
    {
        try
        {
            _specificLogger.LogInformation("Starting power cycle");
            StatusMessage = "Power cycling...";

            var success = await _powerSupplyService.PowerCycleAsync(5000).ConfigureAwait(false);

            if (success)
            {
                UpdatePowerStatus(true);
                StatusMessage = "Power cycle completed ✓";
                _specificLogger.LogInformation("Power cycle completed successfully");
            }
            else
            {
                StatusMessage = "Power cycle failed";
                _specificLogger.LogWarning("Power cycle failed");
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error during power cycle");
            throw;
        }
    }

    #endregion

    #region Path Management Commands Implementation

    /// <summary>
    /// Browses for a profiles path.
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
            _specificLogger.LogDebug("Browsing for profiles path");

            var folderPath = await _fileDialogService.ShowFolderBrowserDialogAsync(
                "Select Power Supply Profiles Folder").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(folderPath))
            {
                ProfilesPath = folderPath;

                var settings = _settingsService.Settings;
                settings.PowerSupply.ProfilesPath = folderPath;
                await _settingsService.SaveSettingsAsync().ConfigureAwait(false);

                StatusMessage = $"Profiles path set to: {folderPath}";
                _specificLogger.LogInformation("Profiles path changed to: {Path}", folderPath);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error browsing profiles path");
            throw;
        }
    }

    /// <summary>
    /// Opens the profiles path in the file explorer.
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
    /// Resets the profiles path to the default value.
    /// </summary>
    private async Task ResetProfilesPathAsync()
    {
        try
        {
            _specificLogger.LogDebug("Resetting profiles path to default");

            var defaultPath = "resources/PowerSupplyProfiles";
            ProfilesPath = defaultPath;

            var settings = _settingsService.Settings;
            settings.PowerSupply.ProfilesPath = defaultPath;
            await _settingsService.SaveSettingsAsync().ConfigureAwait(false);

            StatusMessage = $"Profiles path reset to default: {defaultPath}";
            _specificLogger.LogInformation("Profiles path reset to default: {Path}", defaultPath);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error resetting profiles path");
            throw;
        }
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates the selected profile configuration.
    /// </summary>
    /// <returns>True if the profile is valid, false otherwise.</returns>
    private bool ValidateSelectedProfile()
    {
        if (SelectedProfile == null)
        {
            StatusMessage = "No profile selected";
            return false;
        }

        if (SelectedProfile.Configuration == null)
        {
            StatusMessage = "Selected profile has no configuration";
            return false;
        }

        var validationErrors = SelectedProfile.Validate();
        if (validationErrors.Count > 0)
        {
            StatusMessage = $"Profile validation failed: {string.Join(", ", validationErrors)}";
            _specificLogger.LogWarning("Profile validation failed: {Errors}", string.Join(", ", validationErrors));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a profile name for uniqueness.
    /// </summary>
    /// <param name="name">The profile name to validate.</param>
    /// <param name="excludeId">Optional profile ID to exclude from uniqueness check (for edits).</param>
    /// <returns>True if the name is valid and unique, false otherwise.</returns>
    private Task<bool> ValidateProfileNameAsync(string name, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Profile name cannot be empty";
            return Task.FromResult(false);
        }

        if (name.Length > 100)
        {
            StatusMessage = "Profile name cannot exceed 100 characters";
            return Task.FromResult(false);
        }

        // Check if name is already in use (excluding current profile if editing)
        var existingProfile = Profiles.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));

        if (existingProfile != null)
        {
            StatusMessage = $"Profile name '{name}' is already in use";
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Validates the connection state before power operations.
    /// </summary>
    /// <returns>True if connected, false otherwise.</returns>
    private bool ValidateConnectionState()
    {
        if (!IsConnected)
        {
            StatusMessage = "Not connected to power supply";
            _specificLogger.LogWarning("Operation attempted while not connected");
            return false;
        }

        return true;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Updates the connection status display.
    /// </summary>
    private void UpdateConnectionStatus()
    {
        IsConnected = _powerSupplyService.IsConnected;
        ConnectionStatus = IsConnected ? "Connected" : "Disconnected";
    }

    /// <summary>
    /// Updates the power status display.
    /// </summary>
    private void UpdatePowerStatus(bool powerOn)
    {
        IsPowerOn = powerOn;
        PowerStatus = powerOn ? "ON" : "OFF";
    }

    /// <summary>
    /// Handles command exceptions with logging and user notification.
    /// </summary>
    private void HandleCommandException(Exception ex, string operation)
    {
        _specificLogger.LogError(ex, "Error {Operation}", operation);
        StatusMessage = $"Error {operation}: {ex.Message}";
    }

    #endregion

    #region ProfileManagementViewModelBase Implementation

    /// <summary>
    /// Gets the profile manager for power supply profiles.
    /// </summary>
    protected override IProfileManager<PowerSupplyProfile> GetProfileManager()
        => _profileService;

    /// <summary>
    /// Gets the default profile name for power supply profiles.
    /// </summary>
    protected override string GetDefaultProfileName()
        => "PowerSupply";

    /// <summary>
    /// Gets the profile type name for power supply profiles.
    /// </summary>
    protected override string GetProfileTypeName()
        => "PowerSupply";

    /// <summary>
    /// Creates a default power supply profile with standard configuration.
    /// </summary>
    protected override PowerSupplyProfile CreateDefaultProfile()
        => new()
        {
            Name = GetDefaultProfileName(),
            Description = "Modbus TCP power supply configuration",
            Configuration = new ModbusTcpConfiguration
            {
                Type = PowerSupplyType.ModbusTcp,
                Host = "192.168.1.100",
                Port = 502,
                DeviceId = 1
            }
        };

    /// <summary>
    /// Shows the create dialog for a new power supply profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<PowerSupplyProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplySettingsViewModel.ShowCreateDialogAsync called with name: {request.DefaultName}");

        try
        {
            // Use the unified dialog service to show PowerSupply create dialog
            var result = await _unifiedDialogService.ShowPowerSupplyCreateDialogAsync(request).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplySettingsViewModel.ShowCreateDialogAsync result: {result.IsSuccess}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in PowerSupplySettingsViewModel.ShowCreateDialogAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Shows the edit dialog for an existing power supply profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<PowerSupplyProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplySettingsViewModel.ShowEditDialogAsync called");

        try
        {
            // Use the unified dialog service to show PowerSupply edit dialog
            var result = await _unifiedDialogService.ShowPowerSupplyEditDialogAsync(request).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplySettingsViewModel.ShowEditDialogAsync result: {result.IsSuccess}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in PowerSupplySettingsViewModel.ShowEditDialogAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Shows the duplicate dialog for copying a power supply profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplySettingsViewModel.ShowDuplicateDialogAsync called");

        try
        {
            // Use the unified dialog service to show PowerSupply duplicate dialog
            var result = await _unifiedDialogService.ShowPowerSupplyDuplicateDialogAsync(request).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplySettingsViewModel.ShowDuplicateDialogAsync result: {result.IsSuccess}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in PowerSupplySettingsViewModel.ShowDuplicateDialogAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
            throw;
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of resources when the ViewModel is no longer needed.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from settings changes
            if (_settingsChangedHandler != null)
            {
                _settingsService.SettingsChanged -= _settingsChangedHandler;
            }

            // Dispose managed resources specific to PowerSupplySettingsViewModel
            _disposables?.Dispose();
            _specificLogger.LogInformation("PowerSupplySettingsViewModel disposed");
        }

        // Call base dispose
        base.Dispose(disposing);
    }

    #endregion
}
