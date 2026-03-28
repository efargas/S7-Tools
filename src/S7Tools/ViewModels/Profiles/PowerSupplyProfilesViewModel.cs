using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models;
using S7Tools.Helpers;
using S7Tools.Resources.Strings;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
namespace S7Tools.ViewModels.Profiles;

/// <summary>
/// ViewModel for the Power Supply settings category, providing comprehensive profile management
/// and power supply control capabilities for Modbus TCP devices.
/// </summary>
public class PowerSupplyProfilesViewModel : ProfileManagementViewModelBase<PowerSupplyProfile>, IDockableViewModel
{

    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "PowerSupplyProfiles";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Power Supply";
    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;
    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;
    #region Fields

    private readonly IPowerSupplyProfileService _profileService;
    private readonly IPowerSupplyService _powerSupplyService;
    private readonly IDialogService _dialogService;
    private readonly IUnifiedProfileDialogService _unifiedDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<PowerSupplyProfilesViewModel> _specificLogger;
    private readonly S7Tools.Core.Interfaces.Services.IApplicationSettingsService _settingsService;
    private readonly S7Tools.Services.Interfaces.IUIThreadService _uiThreadService;
    private readonly IPathService _pathService;

    private readonly CompositeDisposable _disposables = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the PowerSupplyProfilesViewModel class.
    /// </summary>
    /// <param name="unifiedDialogService">The unified profile dialog service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="specificLogger">The logger specific to this ViewModel type.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="profileService">The power supply profile service.</param>
    /// <param name="powerSupplyService">The power supply service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="settingsService">The settings service used to persist application settings.</param>
    /// <param name="pathService">The path service for dynamic path resolution.</param>
    public PowerSupplyProfilesViewModel(
        IUnifiedProfileDialogService unifiedDialogService,
        ILogger<ProfileManagementViewModelBase<PowerSupplyProfile>> logger,
        ILogger<PowerSupplyProfilesViewModel> specificLogger,
        S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
        IPowerSupplyProfileService profileService,
        IPowerSupplyService powerSupplyService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IFileDialogService? fileDialogService,
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService,
        IPathService pathService)
        : base(logger, unifiedDialogService, dialogService, uiThreadService, fileDialogService!)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _powerSupplyService = powerSupplyService ?? throw new ArgumentNullException(nameof(powerSupplyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _unifiedDialogService = unifiedDialogService;
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _uiThreadService = uiThreadService;
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

        _specificLogger = specificLogger ?? throw new ArgumentNullException(nameof(specificLogger));

        // DON'T initialize Profiles collection - base class provides it
        // Profiles collection is provided by base class ProfileManagementViewModelBase



        // Initialize profile commands (to be implemented in next increment)
        InitializeProfileCommands();

        // Initialize connection commands (to be implemented in next increment)
        InitializeConnectionCommands();

        // Initialize power control commands (to be implemented in next increment)
        InitializePowerControlCommands();
        // Path commands have been moved to PathSettingsViewModel


        // Setup property change subscriptions
        SetupPropertySubscriptions();

        // Load initial data
        _ = Task.Run(async () =>
        {
            await base.InitializeAsync();
        });

        // Reflect existing connection state if service is already connected
        UpdateConnectionStatus();

        _specificLogger.LogInformation("PowerSupplyProfilesViewModel initialized");
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

    private string _powerStatus = UIStrings.Status_PowerUnknown;
    /// <summary>
    /// Gets or sets the power status message.
    /// </summary>
    public string PowerStatus
    {
        get => _powerStatus;
        private set => this.RaiseAndSetIfChanged(ref _powerStatus, value);
    }

    private bool _isBusy;
    /// <summary>
    /// Gets or sets whether a power or connection operation is in progress.
    /// Used to gate command execution and update UI.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
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



    // Path commands are in PathSettingsViewModel

    // Profile Management Commands (Create, Edit, Delete, Duplicate, etc.) are inherited from base class

    #endregion

    #region Command Initialization Methods



    /// <summary>
    /// Initializes profile management commands.
    /// All profile commands (Create, Edit, Delete, Duplicate, etc.) are inherited from base class.
    /// </summary>
    private void InitializeProfileCommands()
    {
        // Profile management commands are provided by base class
        // No additional initialization needed for CRUD operations


    }

    /// <summary>
    /// Initializes connection management commands.
    /// </summary>
    private void InitializeConnectionCommands()
    {
        IScheduler uiScheduler = RxApp.MainThreadScheduler;

        // Connect: enabled when not connected, a profile selected, and not busy
        IObservable<bool> canConnect = this.WhenAnyValue(x => x.IsConnected, x => x.SelectedProfile, x => x.IsBusy)
            .Select(tuple => !tuple.Item1 && tuple.Item2 != null && !tuple.Item3);

        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync, canConnect);
        ConnectCommand.ThrownExceptions
            .ObserveOn(uiScheduler)
            .Subscribe(ex => HandleCommandException(ex, "connecting"))
            .DisposeWith(_disposables);

        // Disconnect: enabled when connected and not busy
        IObservable<bool> canDisconnect = this.WhenAnyValue(x => x.IsConnected, x => x.IsBusy)
            .Select(tuple => tuple.Item1 && !tuple.Item2);

        DisconnectCommand = ReactiveCommand.CreateFromTask(DisconnectAsync, canDisconnect);
        DisconnectCommand.ThrownExceptions
            .ObserveOn(uiScheduler)
            .Subscribe(ex => HandleCommandException(ex, "disconnecting"))
            .DisposeWith(_disposables);

        // Test connection: enabled when profile selected and not busy
        IObservable<bool> canTestConnection = this.WhenAnyValue(x => x.SelectedProfile, x => x.IsBusy)
            .Select(tuple => tuple.Item1 != null && !tuple.Item2);

        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync, canTestConnection);
        TestConnectionCommand.ThrownExceptions
            .ObserveOn(uiScheduler)
            .Subscribe(ex => HandleCommandException(ex, "testing connection"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes power control commands.
    /// </summary>
    private void InitializePowerControlCommands()
    {
        IScheduler uiScheduler = RxApp.MainThreadScheduler;

        // Turn ON: enabled when connected, power is OFF, and not busy
        IObservable<bool> canTurnOn = this.WhenAnyValue(x => x.IsConnected, x => x.IsPowerOn, x => x.IsBusy)
            .Select(tuple => tuple.Item1 && !tuple.Item2 && !tuple.Item3);
        TurnOnCommand = ReactiveCommand.CreateFromTask(TurnOnAsync, canTurnOn);
        TurnOnCommand.ThrownExceptions
            .ObserveOn(uiScheduler)
            .Subscribe(ex => HandleCommandException(ex, "turning power ON"))
            .DisposeWith(_disposables);

        // Turn OFF: enabled when connected, power is ON, and not busy
        IObservable<bool> canTurnOff = this.WhenAnyValue(x => x.IsConnected, x => x.IsPowerOn, x => x.IsBusy)
            .Select(tuple => tuple.Item1 && tuple.Item2 && !tuple.Item3);
        TurnOffCommand = ReactiveCommand.CreateFromTask(TurnOffAsync, canTurnOff);
        TurnOffCommand.ThrownExceptions
            .ObserveOn(uiScheduler)
            .Subscribe(ex => HandleCommandException(ex, "turning power OFF"))
            .DisposeWith(_disposables);

        // Read state: enabled when connected and not busy
        IObservable<bool> canReadState = this.WhenAnyValue(x => x.IsConnected, x => x.IsBusy)
            .Select(tuple => tuple.Item1 && !tuple.Item2);
        ReadStateCommand = ReactiveCommand.CreateFromTask(ReadStateAsync, canReadState);
        ReadStateCommand.ThrownExceptions
            .ObserveOn(uiScheduler)
            .Subscribe(ex => HandleCommandException(ex, "reading power state"))
            .DisposeWith(_disposables);

        // Power cycle: enabled when connected and not busy
        IObservable<bool> canPowerCycle = this.WhenAnyValue(x => x.IsConnected, x => x.IsBusy)
            .Select(tuple => tuple.Item1 && !tuple.Item2);
        PowerCycleCommand = ReactiveCommand.CreateFromTask(PowerCycleAsync, canPowerCycle);
        PowerCycleCommand.ThrownExceptions
            .ObserveOn(uiScheduler)
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
                // Update connection status text based on current property value
                ConnectionStatus = connected ? "Connected" : "Disconnected";

                // Clear power status when disconnected
                if (!connected)
                {
                    IsPowerOn = false;
                    PowerStatus = UIStrings.Status_PowerUnknown;
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
                    StatusMessage = string.Format(UIStrings.Status_ProfileSelected, profile.Name);
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


    }

    #endregion


    #region Data Loading Methods

    // Profile loading is handled by base class ProfileManagementViewModelBase
    // Override LoadProfilesAsync is not needed - base class calls GetProfileManager().GetAllAsync()



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
            string profileName = SelectedProfile.Name;
            _specificLogger.LogDebug("Deleting power supply profile: {ProfileName}", profileName);

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Profile",
                $"Are you sure you want to delete the profile '{profileName}'?").ConfigureAwait(false);

            if (confirmed)
            {
                int profileId = SelectedProfile.Id;
                await _profileService.DeleteAsync(profileId).ConfigureAwait(false);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    Profiles.Remove(SelectedProfile);
                    SelectedProfile = Profiles.FirstOrDefault();
                }).ConfigureAwait(false);

                StatusMessage = string.Format(UIStrings.Status_ProfileDeletedSuccessfully, profileName);
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

            global::S7Tools.ViewModels.Dialogs.Models.InputResult inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Profile",
                "Enter a name for the duplicate profile:",
                $"{SelectedProfile.Name} (Copy)").ConfigureAwait(false);

            string? newName = inputResult.Value;

            if (!string.IsNullOrWhiteSpace(newName))
            {
                PowerSupplyProfile duplicatedProfile = await _profileService.DuplicateAsync(
                    SelectedProfile.Id,
                    newName).ConfigureAwait(false);

                // Refresh and select duplicated profile
                await RefreshProfilesPreserveSelectionAsync(duplicatedProfile.Id);

                StatusMessage = string.Format(UIStrings.Status_ProfileDuplicated, newName);
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

            StatusMessage = string.Format(UIStrings.Status_ProfileSetAsDefault, SelectedProfile.Name);
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
    private Task RefreshProfilesPreserveSelectionAsync(int? selectProfileId)
    {
        try
        {
            // Reload profiles from storage/service
            _ = RefreshCommand.Execute();

            // If a specific profile Id was requested, try to select it
            if (selectProfileId.HasValue)
            {
                PowerSupplyProfile? match = Profiles.FirstOrDefault(p => p.Id == selectProfileId.Value);
                if (match != null)
                {
                    SelectedProfile = match;
                    return Task.CompletedTask;
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
            StatusMessage = UIStrings.Status_ErrorRefreshingProfiles;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Refreshes the profiles list.
    /// </summary>
    private Task RefreshProfilesAsync()
    {
        try
        {
            _specificLogger.LogDebug("Refreshing power supply profiles");
            _ = RefreshCommand.Execute();
            StatusMessage = UIStrings.Status_ProfilesRefreshed;
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to refresh power supply profiles");
            throw;
        }
        return Task.CompletedTask;
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
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = Constants.StatusMessages.NoProfileSelected;
            });
            return;
        }

        await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
        try
        {
            _specificLogger.LogInformation("Connecting to power supply: {ProfileName}", SelectedProfile.Name);
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = Constants.StatusMessages.Connecting);

            bool success = await _powerSupplyService.ConnectAsync(SelectedProfile.Configuration).ConfigureAwait(false);

            if (success)
            {
                UpdateConnectionStatus();
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    StatusMessage = string.Format(UIStrings.Status_ConnectedToProfile, SelectedProfile!.Name);
                });
                _specificLogger.LogInformation("Connected to power supply successfully");

                // Read initial power state
                await ReadStateCoreAsync().ConfigureAwait(false);
            }
            else
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = Constants.StatusMessages.ConnectionFailed);
                _specificLogger.LogWarning("Failed to connect to power supply");
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "connecting");
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
        }
    }

    /// <summary>
    /// Disconnects from the power supply.
    /// </summary>
    private async Task DisconnectAsync()
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
        try
        {
            _specificLogger.LogInformation("Disconnecting from power supply");
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = Constants.StatusMessages.Disconnecting);

            await _powerSupplyService.DisconnectAsync().ConfigureAwait(false);

            UpdateConnectionStatus();
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                IsPowerOn = false;
                PowerStatus = UIStrings.Status_PowerUnknown;
                StatusMessage = Constants.StatusMessages.Disconnected;
            });
            _specificLogger.LogInformation("Disconnected from power supply successfully");
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "disconnecting");
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
        }
    }

    /// <summary>
    /// Tests the connection to the power supply.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        if (SelectedProfile?.Configuration == null)
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = Constants.StatusMessages.NoProfileSelected;
            });
            return;
        }

        await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
        try
        {
            _specificLogger.LogInformation("Testing connection to power supply: {ProfileName}", SelectedProfile.Name);
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = Constants.StatusMessages.TestingConnection);

            bool success = await _powerSupplyService.TestConnectionAsync(SelectedProfile.Configuration).ConfigureAwait(false);

            if (success)
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = Constants.StatusMessages.ConnectionTestSuccessful);
                _specificLogger.LogInformation("Connection test successful");
            }
            else
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = Constants.StatusMessages.ConnectionTestFailed);
                _specificLogger.LogWarning("Connection test failed");
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "testing connection");
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
        }
    }

    #endregion

    #region Power Control Commands Implementation

    /// <summary>
    /// Turns the power ON.
    /// </summary>
    private async Task TurnOnAsync()
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
        try
        {
            _specificLogger.LogInformation("Turning power ON");
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_TurningPowerOn);

            bool success = await _powerSupplyService.TurnOnAsync().ConfigureAwait(false);

            if (success)
            {
                await Task.Delay(_settingsService.Current.PowerSupply.PowerStateChangeDelayMs).ConfigureAwait(false);
                await ReadStateCoreAsync().ConfigureAwait(false);
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_PowerTurnedOn);
                _specificLogger.LogInformation("Power turned ON successfully");
            }
            else
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_FailedToTurnPowerOn);
                _specificLogger.LogWarning("Failed to turn power ON");
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "turning power ON");
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
        }
    }

    /// <summary>
    /// Turns the power OFF.
    /// </summary>
    private async Task TurnOffAsync()
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
        try
        {
            _specificLogger.LogInformation("Turning power OFF");
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_TurningPowerOff);

            bool success = await _powerSupplyService.TurnOffAsync().ConfigureAwait(false);

            if (success)
            {
                await Task.Delay(_settingsService.Current.PowerSupply.PowerStateChangeDelayMs).ConfigureAwait(false);
                await ReadStateCoreAsync().ConfigureAwait(false);
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_PowerTurnedOff);
                _specificLogger.LogInformation("Power turned OFF successfully");
            }
            else
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_FailedToTurnPowerOff);
                _specificLogger.LogWarning("Failed to turn power OFF");
            }
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "turning power OFF");
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
        }
    }

    /// <summary>
    /// Reads the current power state.
    /// </summary>
    private async Task ReadStateAsync()
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
        try
        {
            await ReadStateCoreAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "reading power state");
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
        }
    }

    private async Task ReadStateCoreAsync()
    {
        try
        {
            _specificLogger.LogDebug("Reading power state");
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_ReadingPowerState);

            bool powerOn = await _powerSupplyService.ReadPowerStateAsync().ConfigureAwait(false);
            UpdatePowerStatus(powerOn);
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
                StatusMessage = string.Format(UIStrings.Status_PowerStateOnOff, powerOn ? UIStrings.Value_PowerOn : UIStrings.Value_PowerOff)
            );
            _specificLogger.LogInformation("Power state read: {State}", powerOn ? "ON" : "OFF");
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error reading power state");
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = string.Format(UIStrings.Status_ReadStateError, ex.Message));
        }
    }

    /// <summary>
    /// Performs a power cycle (OFF → Wait → ON).
    /// </summary>
    private async Task PowerCycleAsync()
    {
        await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = true);
        try
        {
            int delayMs = _settingsService.Current.PowerSupply.PowerStateChangeDelayMs;

            _specificLogger.LogInformation("Starting power cycle (delay={Delay}ms)", delayMs);

            // Step 1: Turn OFF
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_PowerCycleTurningOff);
            bool offOk = await _powerSupplyService.TurnOffAsync().ConfigureAwait(false);
            if (!offOk)
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_PowerCycleFailedCouldNotTurnOff);
                _specificLogger.LogWarning("Power cycle failed at OFF step");
                return;
            }

            // Step 2: Wait before ON
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = string.Format(UIStrings.Status_PowerCycleWaitingBeforeTurningOn, delayMs));
            await Task.Delay(delayMs).ConfigureAwait(false);

            // Step 3: Turn ON
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_PowerCycleTurningOn);
            bool onOk = await _powerSupplyService.TurnOnAsync().ConfigureAwait(false);
            if (!onOk)
            {
                await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = UIStrings.Status_PowerCycleFailedCouldNotTurnOn);
                _specificLogger.LogWarning("Power cycle failed at ON step");
                return;
            }

            // Step 4: Wait to stabilize
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = string.Format(UIStrings.Status_PowerCycleWaitingToStabilize, delayMs));
            await Task.Delay(delayMs).ConfigureAwait(false);

            // Step 5: Read state
            await ReadStateCoreAsync().ConfigureAwait(false);
            await _uiThreadService.InvokeOnUIThreadAsync(() => StatusMessage = IsPowerOn
                ? "Power cycle completed ✓ (State: ON)"
                : "Power cycle completed ✓ (State: OFF)");
            _specificLogger.LogInformation("Power cycle completed successfully; final state: {State}", IsPowerOn ? "ON" : "OFF");
        }
        catch (Exception ex)
        {
            HandleCommandException(ex, "power cycling");
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() => IsBusy = false);
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
            StatusMessage = UIStrings.Status_NoProfileSelected;
            return false;
        }

        if (SelectedProfile.Configuration == null)
        {
            StatusMessage = UIStrings.Status_SelectedProfileHasNoConfiguration;
            return false;
        }

        List<string> validationErrors = SelectedProfile.Validate();
        if (validationErrors.Count > 0)
        {
            StatusMessage = string.Format(UIStrings.Status_ProfileValidationFailed, string.Join(", ", validationErrors));
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
            StatusMessage = UIStrings.Status_ProfileNameCannotBeEmpty;
            return Task.FromResult(false);
        }

        if (name.Length > 100)
        {
            StatusMessage = UIStrings.Status_ProfileNameTooLong;
            return Task.FromResult(false);
        }

        // Check if name is already in use (excluding current profile if editing)
        PowerSupplyProfile? existingProfile = Profiles.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));

        if (existingProfile != null)
        {
            StatusMessage = string.Format(UIStrings.Status_ProfileNameAlreadyInUse, name);
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
            StatusMessage = UIStrings.Status_NotConnectedToPowerSupply;
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
        _ = _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            bool connected = _powerSupplyService.IsConnected;
            IsConnected = connected;
            ConnectionStatus = connected ? "Connected" : "Disconnected";
        });
    }

    /// <summary>
    /// Updates the power status display.
    /// </summary>
    private void UpdatePowerStatus(bool powerOn)
    {
        _ = _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            IsPowerOn = powerOn;
            PowerStatus = powerOn ? "ON" : "OFF";
        });
    }

    /// <summary>
    /// Handles command exceptions with logging and user notification.
    /// </summary>
    private void HandleCommandException(Exception ex, string operation)
    {
        _specificLogger.LogError(ex, "Error {Operation}", operation);
        _ = _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorOperation, operation, ex.Message);
        });
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
        System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplyProfilesViewModel.ShowCreateDialogAsync called with name: {request.DefaultName}");

        try
        {
            // Use the unified dialog service to show PowerSupply create dialog
            ProfileDialogResult<PowerSupplyProfile> result = await _unifiedDialogService.ShowPowerSupplyCreateDialogAsync(request).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplyProfilesViewModel.ShowCreateDialogAsync result: {result.IsSuccess}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in PowerSupplyProfilesViewModel.ShowCreateDialogAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Shows the edit dialog for an existing power supply profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<PowerSupplyProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplyProfilesViewModel.ShowEditDialogAsync called");

        try
        {
            // Use the unified dialog service to show PowerSupply edit dialog
            ProfileDialogResult<PowerSupplyProfile> result = await _unifiedDialogService.ShowPowerSupplyEditDialogAsync(request).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplyProfilesViewModel.ShowEditDialogAsync result: {result.IsSuccess}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in PowerSupplyProfilesViewModel.ShowEditDialogAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Shows the duplicate dialog for copying a power supply profile.
    /// </summary>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplyProfilesViewModel.ShowDuplicateDialogAsync called");

        try
        {
            // Use the unified dialog service to show PowerSupply duplicate dialog
            ProfileDialogResult<string> result = await _unifiedDialogService.ShowPowerSupplyDuplicateDialogAsync(request).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"DEBUG: PowerSupplyProfilesViewModel.ShowDuplicateDialogAsync result: {result.IsSuccess}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in PowerSupplyProfilesViewModel.ShowDuplicateDialogAsync: {ex.Message}");
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


            // Dispose managed resources specific to PowerSupplyProfilesViewModel
            _disposables?.Dispose();
            _specificLogger.LogInformation("PowerSupplyProfilesViewModel disposed");
        }

        // Call base dispose
        base.Dispose(disposing);
    }

    #endregion
}
