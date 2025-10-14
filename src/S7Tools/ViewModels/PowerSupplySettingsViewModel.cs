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

    #region Command Initialization Methods (Placeholders)

    /// <summary>
    /// Initializes path management commands.
    /// </summary>
    private void InitializePathCommands()
    {
        // Placeholder - will be implemented in Increment 2
        BrowseProfilesPathCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        OpenProfilesPathCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        ResetProfilesPathCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
    }

    /// <summary>
    /// Initializes profile management commands.
    /// </summary>
    private void InitializeProfileCommands()
    {
        // Placeholder - will be implemented in Increment 2
        CreateProfileCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        EditProfileCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        DeleteProfileCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        DuplicateProfileCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        SetDefaultProfileCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        RefreshProfilesCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        ExportProfilesCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        ImportProfilesCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
    }

    /// <summary>
    /// Initializes connection management commands.
    /// </summary>
    private void InitializeConnectionCommands()
    {
        // Placeholder - will be implemented in Increment 2
        ConnectCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        DisconnectCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        TestConnectionCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
    }

    /// <summary>
    /// Initializes power control commands.
    /// </summary>
    private void InitializePowerControlCommands()
    {
        // Placeholder - will be implemented in Increment 2
        TurnOnCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        TurnOffCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        ReadStateCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
        PowerCycleCommand = ReactiveCommand.CreateFromTask(async () => await Task.CompletedTask);
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
