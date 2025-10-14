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
using S7Tools.Helpers;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Power Supply settings category, providing comprehensive profile management
/// and power supply control capabilities for Modbus TCP devices.
/// </summary>
public class PowerSupplySettingsViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly IPowerSupplyProfileService _profileService;
    private readonly IPowerSupplyService _powerSupplyService;
    private readonly IDialogService _dialogService;
    private readonly IProfileEditDialogService _profileEditDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<PowerSupplySettingsViewModel> _logger;
    private readonly S7Tools.Services.Interfaces.ISettingsService _settingsService;
    private readonly S7Tools.Services.Interfaces.IUIThreadService _uiThreadService;
    private EventHandler<S7Tools.Models.ApplicationSettings>? _settingsChangedHandler;
    private readonly CompositeDisposable _disposables = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the PowerSupplySettingsViewModel class.
    /// </summary>
    /// <param name="profileService">The power supply profile service.</param>
    /// <param name="powerSupplyService">The power supply service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="profileEditDialogService">The profile edit dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="settingsService">The settings service used to persist application settings.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="logger">The logger.</param>
    public PowerSupplySettingsViewModel(
        IPowerSupplyProfileService profileService,
        IPowerSupplyService powerSupplyService,
        IDialogService dialogService,
        IProfileEditDialogService profileEditDialogService,
        IClipboardService clipboardService,
        IFileDialogService? fileDialogService,
        S7Tools.Services.Interfaces.ISettingsService settingsService,
        S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
        ILogger<PowerSupplySettingsViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _powerSupplyService = powerSupplyService ?? throw new ArgumentNullException(nameof(powerSupplyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _profileEditDialogService = profileEditDialogService ?? throw new ArgumentNullException(nameof(profileEditDialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize collections
        Profiles = new ObservableCollection<PowerSupplyProfile>();

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

        // Load initial data
        _ = Task.Run(async () => await LoadProfilesAsync().ConfigureAwait(false));

        _logger.LogInformation("PowerSupplySettingsViewModel initialized");
    }

    #endregion

    #region Properties

    private ObservableCollection<PowerSupplyProfile> _profiles;
    /// <summary>
    /// Gets the collection of power supply profiles.
    /// </summary>
    public ObservableCollection<PowerSupplyProfile> Profiles
    {
        get => _profiles;
        private set => this.RaiseAndSetIfChanged(ref _profiles, value);
    }

    private PowerSupplyProfile? _selectedProfile;
    /// <summary>
    /// Gets or sets the currently selected power supply profile.
    /// </summary>
    public PowerSupplyProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    private int _profileCount;
    /// <summary>
    /// Gets the total number of profiles.
    /// </summary>
    public int ProfileCount
    {
        get => _profileCount;
        private set => this.RaiseAndSetIfChanged(ref _profileCount, value);
    }

    private string _profilesPath = string.Empty;
    /// <summary>
    /// Gets or sets the path to the profiles directory.
    /// </summary>
    public string ProfilesPath
    {
        get => _profilesPath;
        set => this.RaiseAndSetIfChanged(ref _profilesPath, value);
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

    private string? _statusMessage;
    /// <summary>
    /// Gets or sets the status message displayed to the user.
    /// </summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    #endregion

    #region Commands - Placeholder Properties

    // Profile Management Commands
    public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> EditProfileCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> DuplicateProfileCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> SetDefaultProfileCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> RefreshProfilesCommand { get; private set; } = null!;

    // Connection Management Commands
    public ReactiveCommand<Unit, Unit> ConnectCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> DisconnectCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; private set; } = null!;

    // Power Control Commands
    public ReactiveCommand<Unit, Unit> TurnOnCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> TurnOffCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ReadStateCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> PowerCycleCommand { get; private set; } = null!;

    // Import/Export Commands
    public ReactiveCommand<Unit, Unit> ExportProfilesCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ImportProfilesCommand { get; private set; } = null!;

    // Path Management Commands
    public ReactiveCommand<Unit, Unit> BrowseProfilesPathCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> OpenProfilesPathCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ResetProfilesPathCommand { get; private set; } = null!;

    #endregion

    #region Command Initialization Methods

    /// <summary>
    /// Initializes path management commands.
    /// </summary>
    private void InitializePathCommands()
    {
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
    /// </summary>
    private void InitializeProfileCommands()
    {
        // Create profile command - always enabled
        CreateProfileCommand = ReactiveCommand.CreateFromTask(CreateProfileAsync);
        CreateProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "creating profile"))
            .DisposeWith(_disposables);

        // Edit profile command - enabled when profile is selected and not read-only
        var canEditProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && !profile.IsReadOnly);

        EditProfileCommand = ReactiveCommand.CreateFromTask(EditProfileAsync, canEditProfile);
        EditProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "editing profile"))
            .DisposeWith(_disposables);

        // Delete profile command - enabled when profile is selected and not read-only
        var canDeleteProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && !profile.IsReadOnly);

        DeleteProfileCommand = ReactiveCommand.CreateFromTask(DeleteProfileAsync, canDeleteProfile);
        DeleteProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "deleting profile"))
            .DisposeWith(_disposables);

        // Duplicate profile command - enabled when profile is selected
        var canDuplicateProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        DuplicateProfileCommand = ReactiveCommand.CreateFromTask(DuplicateProfileAsync, canDuplicateProfile);
        DuplicateProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "duplicating profile"))
            .DisposeWith(_disposables);

        // Set default profile command - enabled when profile is selected and not already default
        var canSetDefault = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && !profile.IsDefault);

        SetDefaultProfileCommand = ReactiveCommand.CreateFromTask(SetDefaultProfileAsync, canSetDefault);
        SetDefaultProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "setting default profile"))
            .DisposeWith(_disposables);

        // Refresh profiles command - always enabled
        RefreshProfilesCommand = ReactiveCommand.CreateFromTask(RefreshProfilesAsync);
        RefreshProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "refreshing profiles"))
            .DisposeWith(_disposables);

        // Export profiles command - enabled when profiles exist
        var canExportProfiles = this.WhenAnyValue(x => x.ProfileCount)
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

    #region Data Loading Methods

    /// <summary>
    /// Loads profiles from the profile service.
    /// </summary>
    private async Task LoadProfilesAsync()
    {
        try
        {
            IsLoading = true;
            _logger.LogDebug("Loading power supply profiles");

            var profiles = await _profileService.GetAllProfilesAsync().ConfigureAwait(false);

            // Marshal to UI thread for collection update
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                Profiles.Clear();
                foreach (var profile in profiles)
                {
                    Profiles.Add(profile);
                }

                ProfileCount = Profiles.Count;

                // Select default profile or first profile
                SelectedProfile = Profiles.FirstOrDefault(p => p.IsDefault) ?? Profiles.FirstOrDefault();

                _logger.LogInformation("Loaded {Count} power supply profiles", ProfileCount);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load power supply profiles");
            StatusMessage = $"Failed to load profiles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

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
            _logger.LogError(ex, "Failed to refresh settings");
        }
    }

    #endregion

    #region Profile Management Commands Implementation

    /// <summary>
    /// Creates a new power supply profile.
    /// </summary>
    private async Task CreateProfileAsync()
    {
        try
        {
            _logger.LogDebug("Creating new power supply profile");
            StatusMessage = "Creating profile...";

            // Get profile name from user
            var inputResult = await _dialogService.ShowInputAsync(
                "Create Profile",
                "Enter a name for the new profile:",
                "New Power Supply Profile").ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(inputResult.Value))
            {
                var newProfile = PowerSupplyProfile.CreateUserProfile(inputResult.Value, "User-created profile");
                var createdProfile = await _profileService.CreateProfileAsync(newProfile).ConfigureAwait(false);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    Profiles.Add(createdProfile);
                    ProfileCount = Profiles.Count;
                    SelectedProfile = createdProfile;
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{createdProfile.Name}' created successfully";
                _logger.LogInformation("Created power supply profile: {ProfileName}", createdProfile.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create power supply profile");
            throw;
        }
    }

    /// <summary>
    /// Edits the selected power supply profile.
    /// </summary>
    private async Task EditProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            _logger.LogDebug("Editing power supply profile: {ProfileName}", SelectedProfile.Name);
            StatusMessage = $"Editing profile '{SelectedProfile.Name}'...";

            // For now, just show a placeholder message
            // Full profile editing UI will be implemented in Phase 4
            StatusMessage = $"Profile editing UI coming in Phase 4. Profile: {SelectedProfile.Name}";
            _logger.LogInformation("Edit requested for power supply profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit power supply profile");
            throw;
        }
    }

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
            _logger.LogDebug("Deleting power supply profile: {ProfileName}", profileName);

            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Profile",
                $"Are you sure you want to delete the profile '{profileName}'?").ConfigureAwait(false);

            if (confirmed)
            {
                var profileId = SelectedProfile.Id;
                await _profileService.DeleteProfileAsync(profileId).ConfigureAwait(false);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    Profiles.Remove(SelectedProfile);
                    ProfileCount = Profiles.Count;
                    SelectedProfile = Profiles.FirstOrDefault();
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{profileName}' deleted successfully";
                _logger.LogInformation("Deleted power supply profile: {ProfileName}", profileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete power supply profile");
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
            _logger.LogDebug("Duplicating power supply profile: {ProfileName}", SelectedProfile.Name);

            var inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Profile",
                "Enter a name for the duplicate profile:",
                $"{SelectedProfile.Name} (Copy)").ConfigureAwait(false);
            
            var newName = inputResult.Value;

            if (!string.IsNullOrWhiteSpace(newName))
            {
                var duplicatedProfile = await _profileService.DuplicateProfileAsync(
                    SelectedProfile.Id, 
                    newName).ConfigureAwait(false);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    Profiles.Add(duplicatedProfile);
                    ProfileCount = Profiles.Count;
                    SelectedProfile = duplicatedProfile;
                }).ConfigureAwait(false);

                StatusMessage = $"Profile duplicated as '{newName}'";
                _logger.LogInformation("Duplicated power supply profile: {ProfileName} -> {NewName}", 
                    SelectedProfile.Name, newName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate power supply profile");
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
            _logger.LogDebug("Setting default power supply profile: {ProfileName}", SelectedProfile.Name);

            await _profileService.SetDefaultProfileAsync(SelectedProfile.Id).ConfigureAwait(false);
            await LoadProfilesAsync().ConfigureAwait(false);

            StatusMessage = $"Profile '{SelectedProfile.Name}' set as default";
            _logger.LogInformation("Set default power supply profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default power supply profile");
            throw;
        }
    }

    /// <summary>
    /// Refreshes the profiles list.
    /// </summary>
    private async Task RefreshProfilesAsync()
    {
        try
        {
            _logger.LogDebug("Refreshing power supply profiles");
            await LoadProfilesAsync().ConfigureAwait(false);
            StatusMessage = "Profiles refreshed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh power supply profiles");
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
            _logger.LogDebug("Exporting power supply profiles");

            var filePath = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Power Supply Profiles",
                "*.json",
                null,
                "power-supply-profiles.json").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(filePath))
            {
                var profileIds = Profiles.Select(p => p.Id);
                var json = await _profileService.ExportProfilesToJsonAsync(profileIds).ConfigureAwait(false);
                await System.IO.File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);

                StatusMessage = $"Exported {Profiles.Count} profiles to {filePath}";
                _logger.LogInformation("Exported {Count} power supply profiles to {FilePath}", 
                    Profiles.Count, filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export power supply profiles");
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
            _logger.LogDebug("Importing power supply profiles");

            var filePath = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Power Supply Profiles",
                "*.json").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(filePath))
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var importedProfiles = await _profileService.ImportProfilesFromJsonAsync(json, false).ConfigureAwait(false);
                var count = importedProfiles.Count();

                await LoadProfilesAsync().ConfigureAwait(false);

                StatusMessage = $"Imported {count} profiles from {filePath}";
                _logger.LogInformation("Imported {Count} power supply profiles from {FilePath}", 
                    count, filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import power supply profiles");
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
            _logger.LogInformation("Connecting to power supply: {ProfileName}", SelectedProfile.Name);
            StatusMessage = "Connecting...";

            var success = await _powerSupplyService.ConnectAsync(SelectedProfile.Configuration).ConfigureAwait(false);

            if (success)
            {
                UpdateConnectionStatus();
                StatusMessage = $"Connected to {SelectedProfile.Name}";
                _logger.LogInformation("Connected to power supply successfully");

                // Read initial power state
                await ReadStateAsync().ConfigureAwait(false);
            }
            else
            {
                StatusMessage = "Connection failed";
                _logger.LogWarning("Failed to connect to power supply");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to power supply");
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
            _logger.LogInformation("Disconnecting from power supply");
            StatusMessage = "Disconnecting...";

            await _powerSupplyService.DisconnectAsync().ConfigureAwait(false);

            UpdateConnectionStatus();
            UpdatePowerStatus(false);
            PowerStatus = "Unknown";
            StatusMessage = "Disconnected";
            _logger.LogInformation("Disconnected from power supply successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from power supply");
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
            _logger.LogInformation("Testing connection to power supply: {ProfileName}", SelectedProfile.Name);
            StatusMessage = "Testing connection...";

            var success = await _powerSupplyService.TestConnectionAsync(SelectedProfile.Configuration).ConfigureAwait(false);

            if (success)
            {
                StatusMessage = "Connection test successful ✓";
                _logger.LogInformation("Connection test successful");

                // Connection test successful - could show a dialog if needed
            }
            else
            {
                StatusMessage = "Connection test failed ✗";
                _logger.LogWarning("Connection test failed");

                // Connection test failed - could show a dialog if needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection");
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
            _logger.LogInformation("Turning power ON");
            StatusMessage = "Turning power ON...";

            var success = await _powerSupplyService.TurnOnAsync().ConfigureAwait(false);

            if (success)
            {
                UpdatePowerStatus(true);
                StatusMessage = "Power turned ON ✓";
                _logger.LogInformation("Power turned ON successfully");
            }
            else
            {
                StatusMessage = "Failed to turn power ON";
                _logger.LogWarning("Failed to turn power ON");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error turning power ON");
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
            _logger.LogInformation("Turning power OFF");
            StatusMessage = "Turning power OFF...";

            var success = await _powerSupplyService.TurnOffAsync().ConfigureAwait(false);

            if (success)
            {
                UpdatePowerStatus(false);
                StatusMessage = "Power turned OFF ✓";
                _logger.LogInformation("Power turned OFF successfully");
            }
            else
            {
                StatusMessage = "Failed to turn power OFF";
                _logger.LogWarning("Failed to turn power OFF");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error turning power OFF");
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
            _logger.LogDebug("Reading power state");
            StatusMessage = "Reading power state...";

            var powerOn = await _powerSupplyService.ReadPowerStateAsync().ConfigureAwait(false);

            UpdatePowerStatus(powerOn);
            StatusMessage = $"Power state: {(powerOn ? "ON" : "OFF")}";
            _logger.LogInformation("Power state read: {State}", powerOn ? "ON" : "OFF");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading power state");
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
            _logger.LogInformation("Starting power cycle");
            StatusMessage = "Power cycling...";

            var success = await _powerSupplyService.PowerCycleAsync(5000).ConfigureAwait(false);

            if (success)
            {
                UpdatePowerStatus(true);
                StatusMessage = "Power cycle completed ✓";
                _logger.LogInformation("Power cycle completed successfully");
            }
            else
            {
                StatusMessage = "Power cycle failed";
                _logger.LogWarning("Power cycle failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during power cycle");
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
            _logger.LogDebug("Browsing for profiles path");

            var folderPath = await _fileDialogService.ShowFolderBrowserDialogAsync(
                "Select Power Supply Profiles Folder").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(folderPath))
            {
                ProfilesPath = folderPath;
                
                var settings = _settingsService.Settings;
                settings.PowerSupply.ProfilesPath = folderPath;
                await _settingsService.SaveSettingsAsync().ConfigureAwait(false);

                StatusMessage = $"Profiles path set to: {folderPath}";
                _logger.LogInformation("Profiles path changed to: {Path}", folderPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing profiles path");
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
            _logger.LogDebug("Opening profiles path: {Path}", ProfilesPath);

            var fullPath = System.IO.Path.GetFullPath(ProfilesPath);
            
            if (!System.IO.Directory.Exists(fullPath))
            {
                System.IO.Directory.CreateDirectory(fullPath);
            }

            // Open in file explorer based on OS
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start("explorer.exe", fullPath);
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", fullPath);
            }
            else if (OperatingSystem.IsLinux())
            {
                System.Diagnostics.Process.Start("xdg-open", fullPath);
            }
            StatusMessage = $"Opened profiles path: {fullPath}";
            _logger.LogInformation("Opened profiles path in file explorer: {Path}", fullPath);

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening profiles path");
            throw;
        }
    }

    /// <summary>
    /// Resets the profiles path to the default value.
    /// </summary>
    private async Task ResetProfilesPathAsync()
    {
        try
        {
            _logger.LogDebug("Resetting profiles path to default");

            var defaultPath = "resources/PowerSupplyProfiles";
            ProfilesPath = defaultPath;

            var settings = _settingsService.Settings;
            settings.PowerSupply.ProfilesPath = defaultPath;
            await _settingsService.SaveSettingsAsync().ConfigureAwait(false);

            StatusMessage = $"Profiles path reset to default: {defaultPath}";
            _logger.LogInformation("Profiles path reset to default: {Path}", defaultPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting profiles path");
            throw;
        }
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
        _logger.LogError(ex, "Error {Operation}", operation);
        StatusMessage = $"Error {operation}: {ex.Message}";
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Disposes the ViewModel and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_settingsChangedHandler != null)
        {
            _settingsService.SettingsChanged -= _settingsChangedHandler;
        }

        _disposables.Dispose();
        _disposed = true;
    }

    #endregion
}
