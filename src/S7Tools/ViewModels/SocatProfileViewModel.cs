using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
/// ViewModel for editing individual socat profiles, providing comprehensive configuration
/// management with real-time validation and socat command preview.
/// </summary>
public class SocatProfileViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly ISocatProfileService _profileService;
    private readonly ISocatService _socatService;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<SocatProfileViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();
    private SocatProfile? _originalProfile;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SocatProfileViewModel class.
    /// </summary>
    /// <param name="profileService">The socat profile service.</param>
    /// <param name="socatService">The socat service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="logger">The logger.</param>
    public SocatProfileViewModel(
        ISocatProfileService profileService,
        ISocatService socatService,
        IClipboardService clipboardService,
        ILogger<SocatProfileViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize collections
        ValidationErrors = new ObservableCollection<string>();
        AvailableTcpPorts = new ObservableCollection<int> { 1238, 1239, 1240, 1241, 1242, 2000, 3000, 4000, 5000, 8000, 9000 };
        AvailableBlockSizes = new ObservableCollection<int> { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        AvailableDebugLevels = new ObservableCollection<int> { 0, 1, 2, 3 };
        AvailablePresets = new ObservableCollection<string> { "Default", "High Speed", "Debug", "Minimal" };

        // Initialize commands
        InitializeCommands();

        // Set up validation
        SetupValidation();

        _logger.LogInformation("SocatProfileViewModel initialized");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public ObservableCollection<string> ValidationErrors { get; }

    /// <summary>
    /// Gets the collection of available TCP ports.
    /// </summary>
    public ObservableCollection<int> AvailableTcpPorts { get; }

    /// <summary>
    /// Gets the collection of available block sizes.
    /// </summary>
    public ObservableCollection<int> AvailableBlockSizes { get; }

    /// <summary>
    /// Gets the collection of available debug levels.
    /// </summary>
    public ObservableCollection<int> AvailableDebugLevels { get; }

    /// <summary>
    /// Gets the collection of available preset configurations.
    /// </summary>
    public ObservableCollection<string> AvailablePresets { get; }

    #region Profile Properties

    private string _profileName = string.Empty;
    /// <summary>
    /// Gets or sets the profile name.
    /// </summary>
    public string ProfileName
    {
        get => _profileName;
        set => this.RaiseAndSetIfChanged(ref _profileName, value);
    }

    private string _profileDescription = string.Empty;
    /// <summary>
    /// Gets or sets the profile description.
    /// </summary>
    public string ProfileDescription
    {
        get => _profileDescription;
        set => this.RaiseAndSetIfChanged(ref _profileDescription, value);
    }

    private bool _isDefault;
    /// <summary>
    /// Gets or sets a value indicating whether this is the default profile.
    /// </summary>
    public bool IsDefault
    {
        get => _isDefault;
        set => this.RaiseAndSetIfChanged(ref _isDefault, value);
    }

    private bool _isReadOnly;
    /// <summary>
    /// Gets or sets a value indicating whether this profile is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
    }

    #endregion

    #region TCP Configuration Properties

    private int _tcpPort = 1238;
    /// <summary>
    /// Gets or sets the TCP port number for listening.
    /// </summary>
    public int TcpPort
    {
        get => _tcpPort;
        set => this.RaiseAndSetIfChanged(ref _tcpPort, value);
    }

    private string _tcpHost = string.Empty;
    /// <summary>
    /// Gets or sets the TCP host/interface to bind to.
    /// </summary>
    public string TcpHost
    {
        get => _tcpHost;
        set => this.RaiseAndSetIfChanged(ref _tcpHost, value);
    }

    private bool _enableFork = true;
    /// <summary>
    /// Gets or sets whether to enable fork mode for multiple concurrent connections.
    /// </summary>
    public bool EnableFork
    {
        get => _enableFork;
        set => this.RaiseAndSetIfChanged(ref _enableFork, value);
    }

    private bool _enableReuseAddr = true;
    /// <summary>
    /// Gets or sets whether to enable address reuse.
    /// </summary>
    public bool EnableReuseAddr
    {
        get => _enableReuseAddr;
        set => this.RaiseAndSetIfChanged(ref _enableReuseAddr, value);
    }

    #endregion

    #region socat Flags Properties

    private bool _verbose = true;
    /// <summary>
    /// Gets or sets whether to enable verbose logging.
    /// </summary>
    public bool Verbose
    {
        get => _verbose;
        set => this.RaiseAndSetIfChanged(ref _verbose, value);
    }

    private bool _hexDump = true;
    /// <summary>
    /// Gets or sets whether to enable hex dump of transferred data.
    /// </summary>
    public bool HexDump
    {
        get => _hexDump;
        set => this.RaiseAndSetIfChanged(ref _hexDump, value);
    }

    private int _blockSize = 4;
    /// <summary>
    /// Gets or sets the block size for data transfers.
    /// </summary>
    public int BlockSize
    {
        get => _blockSize;
        set => this.RaiseAndSetIfChanged(ref _blockSize, value);
    }

    private int _debugLevel = 2;
    /// <summary>
    /// Gets or sets the debug level (number of -d flags).
    /// </summary>
    public int DebugLevel
    {
        get => _debugLevel;
        set => this.RaiseAndSetIfChanged(ref _debugLevel, value);
    }

    #endregion

    #region Serial Device Properties

    private bool _serialRawMode = true;
    /// <summary>
    /// Gets or sets whether to enable raw mode for the serial device.
    /// </summary>
    public bool SerialRawMode
    {
        get => _serialRawMode;
        set => this.RaiseAndSetIfChanged(ref _serialRawMode, value);
    }

    private bool _serialDisableEcho = true;
    /// <summary>
    /// Gets or sets whether to disable echo on the serial device.
    /// </summary>
    public bool SerialDisableEcho
    {
        get => _serialDisableEcho;
        set => this.RaiseAndSetIfChanged(ref _serialDisableEcho, value);
    }

    #endregion

    #region Process Management Properties

    private bool _autoConfigureSerial = true;
    /// <summary>
    /// Gets or sets whether to automatically configure the serial port with stty before starting socat.
    /// </summary>
    public bool AutoConfigureSerial
    {
        get => _autoConfigureSerial;
        set => this.RaiseAndSetIfChanged(ref _autoConfigureSerial, value);
    }

    private int _connectionTimeout;
    /// <summary>
    /// Gets or sets the timeout for TCP connections in seconds.
    /// </summary>
    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => this.RaiseAndSetIfChanged(ref _connectionTimeout, value);
    }

    private bool _autoRestart;
    /// <summary>
    /// Gets or sets whether to restart socat automatically if it terminates unexpectedly.
    /// </summary>
    public bool AutoRestart
    {
        get => _autoRestart;
        set => this.RaiseAndSetIfChanged(ref _autoRestart, value);
    }

    #endregion

    #region Status Properties

    private bool _isValid = true;
    /// <summary>
    /// Gets or sets a value indicating whether the current configuration is valid.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    private bool _hasChanges;
    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get => _hasChanges;
        set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
    }

    private string _socatCommand = string.Empty;
    /// <summary>
    /// Gets or sets the generated socat command preview.
    /// </summary>
    public string SocatCommand
    {
        get => _socatCommand;
        set => this.RaiseAndSetIfChanged(ref _socatCommand, value);
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

    #endregion

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to save the profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to cancel editing and revert changes.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to reset to default configuration.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetToDefaultCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to load a preset configuration.
    /// </summary>
    public ReactiveCommand<string, Unit> LoadPresetCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to validate the current configuration.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ValidateCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to copy the socat command to clipboard.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CopySocatCommandCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to test the TCP port availability.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestPortCommand { get; private set; } = null!;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads a profile for editing.
    /// </summary>
    /// <param name="profile">The profile to load.</param>
    public void LoadProfile(SocatProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _originalProfile = profile.ClonePreserveId();
        // Load profile properties
        ProfileName = profile.Name;
        ProfileDescription = profile.Description;
        IsDefault = profile.IsDefault;
        IsReadOnly = profile.IsReadOnly;

        // Load configuration properties
        SocatConfiguration config = profile.Configuration;
        TcpPort = config.TcpPort;
        TcpHost = config.TcpHost;
        EnableFork = config.EnableFork;
        EnableReuseAddr = config.EnableReuseAddr;
        Verbose = config.Verbose;
        HexDump = config.HexDump;
        BlockSize = config.BlockSize;
        DebugLevel = config.DebugLevel;
        SerialRawMode = config.SerialRawMode;
        SerialDisableEcho = config.SerialDisableEcho;
        AutoConfigureSerial = config.AutoConfigureSerial;
        ConnectionTimeout = config.ConnectionTimeout;
        AutoRestart = config.AutoRestart;

        HasChanges = false;
        UpdateSocatCommand();
        ValidateConfiguration();

        _logger.LogInformation("Loaded profile for editing: {ProfileName}", profile.Name);
    }

    /// <summary>
    /// Creates a new profile from the current configuration.
    /// </summary>
    /// <returns>A new SocatProfile with the current configuration.</returns>
    public SocatProfile CreateProfile()
    {
        var profile = new SocatProfile
        {
            Name = ProfileName,
            Description = ProfileDescription,
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            Configuration = CreateConfiguration()
        };

        return profile;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes all reactive commands.
    /// </summary>
    private void InitializeCommands()
    {
        // Save command - enabled when valid and has changes and not read-only
        IObservable<bool> canSave = this.WhenAnyValue(x => x.IsValid, x => x.HasChanges, x => x.IsReadOnly)
            .Select(tuple => tuple.Item1 && tuple.Item2 && !tuple.Item3);

        IScheduler backgroundScheduler = RxApp.TaskpoolScheduler;
        IScheduler uiScheduler = RxApp.MainThreadScheduler;

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave, backgroundScheduler);
        SaveCommand
            .ThrownExceptions
            .ObserveOn(uiScheduler)
            .Subscribe(ex => HandleCommandException(ex, "saving profile"))
            .DisposeWith(_disposables);

        // Cancel command - always enabled
        CancelCommand = ReactiveCommand.Create(Cancel);

        // Reset to default command - enabled when not read-only
        IObservable<bool> canReset = this.WhenAnyValue(x => x.IsReadOnly)
            .Select(readOnly => !readOnly);

        ResetToDefaultCommand = ReactiveCommand.Create(ResetToDefault, canReset);

        // Load preset command - enabled when not read-only
        LoadPresetCommand = ReactiveCommand.Create<string>(LoadPreset, canReset);

        // Validate command - always enabled
        ValidateCommand = ReactiveCommand.Create(ValidateConfiguration);

        // Copy socat command - always enabled
        CopySocatCommandCommand = ReactiveCommand.CreateFromTask(CopySocatCommandAsync);
        CopySocatCommandCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "copying socat command"))
            .DisposeWith(_disposables);

        // Test port command - always enabled
        TestPortCommand = ReactiveCommand.CreateFromTask(TestPortAsync);
        TestPortCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing TCP port"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Sets up validation for configuration changes using individual property subscriptions.
    /// This approach is more efficient as it avoids creating large tuples for every change.
    /// </summary>
    private void SetupValidation()
    {
        // Create a shared action for handling property changes
        void OnPropertyChanged()
        {
            HasChanges = true;
            UpdateSocatCommand();
            ValidateConfiguration();
        }

        // Subscribe to profile property changes
        this.WhenAnyValue(x => x.ProfileName)
            .Skip(1) // Skip initial value
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.ProfileDescription)
            .Skip(1) // Skip initial value
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        // Subscribe to TCP configuration property changes
        this.WhenAnyValue(x => x.TcpPort)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.TcpHost)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.EnableFork)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.EnableReuseAddr)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        // Subscribe to socat flags property changes
        this.WhenAnyValue(x => x.Verbose)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.HexDump)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.BlockSize)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DebugLevel)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        // Subscribe to serial device property changes
        this.WhenAnyValue(x => x.SerialRawMode)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.SerialDisableEcho)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        // Subscribe to process management property changes
        this.WhenAnyValue(x => x.AutoConfigureSerial)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.ConnectionTimeout)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.AutoRestart)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Creates a SocatConfiguration from the current property values.
    /// </summary>
    /// <returns>A new SocatConfiguration.</returns>
    private SocatConfiguration CreateConfiguration()
    {
        return new SocatConfiguration
        {
            TcpPort = TcpPort,
            TcpHost = TcpHost,
            EnableFork = EnableFork,
            EnableReuseAddr = EnableReuseAddr,
            Verbose = Verbose,
            HexDump = HexDump,
            BlockSize = BlockSize,
            DebugLevel = DebugLevel,
            SerialRawMode = SerialRawMode,
            SerialDisableEcho = SerialDisableEcho,
            AutoConfigureSerial = AutoConfigureSerial,
            ConnectionTimeout = ConnectionTimeout,
            AutoRestart = AutoRestart
        };
    }

    /// <summary>
    /// Updates the socat command preview.
    /// </summary>
    private void UpdateSocatCommand()
    {
        try
        {
            SocatConfiguration config = CreateConfiguration();
            SocatCommand = _socatService.GenerateSocatCommand(config, "/dev/ttyUSB0");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating socat command");
            SocatCommand = "Error generating command";
        }
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    private void ValidateConfiguration()
    {
        ValidationErrors.Clear();

        try
        {
            // Validate profile name
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                ValidationErrors.Add("Profile name is required");
            }
            else if (ProfileName.Length > 100)
            {
                ValidationErrors.Add("Profile name cannot exceed 100 characters");
            }

            // Validate description
            if (!string.IsNullOrEmpty(ProfileDescription) && ProfileDescription.Length > 500)
            {
                ValidationErrors.Add("Profile description cannot exceed 500 characters");
            }

            // Validate configuration
            SocatConfiguration config = CreateConfiguration();
            List<string> configErrors = config.Validate();
            foreach (string error in configErrors)
            {
                ValidationErrors.Add(error);
            }

            // Validate socat command
            if (!string.IsNullOrEmpty(SocatCommand) && SocatCommand != "Error generating command")
            {
                SocatCommandValidationResult commandValidation = _socatService.ValidateSocatCommand(SocatCommand);
                foreach (string error in commandValidation.Errors)
                {
                    ValidationErrors.Add($"Command validation: {error}");
                }
            }

            IsValid = ValidationErrors.Count == 0;
            StatusMessage = IsValid ? "Configuration is valid" : $"{ValidationErrors.Count} validation error(s)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            ValidationErrors.Add("Error validating configuration");
            IsValid = false;
            StatusMessage = "Validation error";
        }
    }

    /// <summary>
    /// Saves the current profile configuration.
    /// This method is exposed publicly to allow direct invocation from the UI layer,
    /// avoiding ReactiveCommand execution deadlocks on the UI thread.
    /// </summary>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveAsync()
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: SocatProfileViewModel.SaveAsync called");
        try
        {
            StatusMessage = "Saving profile...";
            System.Diagnostics.Debug.WriteLine($"DEBUG: Creating profile from ViewModel data");

            SocatProfile profile = CreateProfile();
            System.Diagnostics.Debug.WriteLine($"DEBUG: Profile created with name: {profile.Name}");

            if (_originalProfile != null)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Updating existing profile ID: {_originalProfile.Id}");
                profile.Id = _originalProfile.Id;
                profile.CreatedAt = _originalProfile.CreatedAt;
                profile.ModifiedAt = DateTime.UtcNow;
                await _profileService.UpdateAsync(profile).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"DEBUG: Profile updated successfully");
            }
            else
            {
                Console.WriteLine($"🔥🔥🔥 ViewModel: Creating new profile via service...");
                System.Diagnostics.Debug.WriteLine($"DEBUG: Creating new profile via service");
                _logger.LogInformation("🔥 ABOUT TO CALL _profileService.CreateAsync for profile: {ProfileName}", profile.Name);
                Console.WriteLine($"🔥 ViewModel: About to await _profileService.CreateAsync for profile: {profile.Name}");
                System.Diagnostics.Debug.WriteLine($"🔥 CRITICAL: Calling _profileService.CreateAsync NOW...");
                SocatProfile createdProfile = await _profileService.CreateAsync(profile).ConfigureAwait(false);
                Console.WriteLine($"✅✅✅ ViewModel: CreateAsync RETURNED! Profile ID: {createdProfile.Id}");
                System.Diagnostics.Debug.WriteLine($"DEBUG: Profile created successfully with ID: {createdProfile.Id}");
                _logger.LogInformation("✅ _profileService.CreateAsync RETURNED for profile: {ProfileName}, ID: {ProfileId}", createdProfile.Name, createdProfile.Id);

                Console.WriteLine($"ViewModel: Setting _originalProfile to createdProfile...");
                // Update _originalProfile with the created profile so subsequent saves are updates
                _originalProfile = createdProfile;
                Console.WriteLine($"ViewModel: _originalProfile set successfully ✓");
            }

            Console.WriteLine($"ViewModel: Setting HasChanges = false...");
            HasChanges = false;
            StatusMessage = "Profile saved successfully";
            _logger.LogInformation("Profile saved: {ProfileName}", profile.Name);
            Console.WriteLine($"✅ ViewModel: SocatProfileViewModel.SaveAsync completed successfully!");
            System.Diagnostics.Debug.WriteLine($"DEBUG: SocatProfileViewModel.SaveAsync completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in SocatProfileViewModel.SaveAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
            _logger.LogError(ex, "Error saving profile");
            StatusMessage = "Error saving profile";
            throw; // Re-throw to allow caller to handle the exception
        }
    }

    /// <summary>
    /// Cancels editing and reverts to original values.
    /// </summary>
    private void Cancel()
    {
        if (_originalProfile != null)
        {
            LoadProfile(_originalProfile);
        }
        else
        {
            ResetToDefault();
        }

        HasChanges = false;
        StatusMessage = "Changes cancelled";
        _logger.LogInformation("Profile editing cancelled");
    }

    /// <summary>
    /// Resets the configuration to default values.
    /// </summary>
    private void ResetToDefault()
    {
        var defaultConfig = SocatConfiguration.CreateDefault();
        ApplyConfiguration(defaultConfig);

        StatusMessage = "Reset to default configuration";
        _logger.LogInformation("Configuration reset to defaults");
    }

    /// <summary>
    /// Loads a preset configuration.
    /// </summary>
    /// <param name="presetName">The name of the preset to load.</param>
    private void LoadPreset(string presetName)
    {
        try
        {
            SocatConfiguration config = presetName switch
            {
                "Default" => SocatConfiguration.CreateDefault(),
                "High Speed" => SocatConfiguration.CreateHighSpeed(),
                "Debug" => SocatConfiguration.CreateDebug(),
                "Minimal" => SocatConfiguration.CreateMinimal(),
                _ => SocatConfiguration.CreateDefault()
            };

            ApplyConfiguration(config);

            StatusMessage = $"Loaded {presetName} preset";
            _logger.LogInformation("Loaded preset: {PresetName}", presetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading preset: {PresetName}", presetName);
            StatusMessage = "Error loading preset";
        }
    }

    /// <summary>
    /// Applies a configuration to the current properties.
    /// </summary>
    /// <param name="config">The configuration to apply.</param>
    private void ApplyConfiguration(SocatConfiguration config)
    {
        TcpPort = config.TcpPort;
        TcpHost = config.TcpHost;
        EnableFork = config.EnableFork;
        EnableReuseAddr = config.EnableReuseAddr;
        Verbose = config.Verbose;
        HexDump = config.HexDump;
        BlockSize = config.BlockSize;
        DebugLevel = config.DebugLevel;
        SerialRawMode = config.SerialRawMode;
        SerialDisableEcho = config.SerialDisableEcho;
        AutoConfigureSerial = config.AutoConfigureSerial;
        ConnectionTimeout = config.ConnectionTimeout;
        AutoRestart = config.AutoRestart;
    }

    /// <summary>
    /// Copies the socat command to the clipboard.
    /// </summary>
    private async Task CopySocatCommandAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(SocatCommand) || SocatCommand == "Error generating command")
            {
                StatusMessage = "No valid socat command to copy";
                return;
            }

            await _clipboardService.SetTextAsync(SocatCommand);
            StatusMessage = "socat command copied to clipboard";
            _logger.LogInformation("socat command copied to clipboard: {Command}", SocatCommand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying socat command to clipboard");
            StatusMessage = "Error copying to clipboard";
        }
    }

    /// <summary>
    /// Tests the TCP port availability.
    /// </summary>
    private async Task TestPortAsync()
    {
        try
        {
            StatusMessage = $"Testing TCP port {TcpPort}...";

            bool isInUse = await _socatService.IsPortInUseAsync(TcpPort);

            if (isInUse)
            {
                StatusMessage = $"TCP port {TcpPort} is currently in use";
                SocatProcessInfo? processInfo = await _socatService.GetProcessByPortAsync(TcpPort);
                if (processInfo != null)
                {
                    StatusMessage += $" by process {processInfo.ProcessId}";
                }
            }
            else
            {
                StatusMessage = $"TCP port {TcpPort} is available";
            }

            _logger.LogInformation("TCP port test completed: {Port} - {Status}", TcpPort, isInUse ? "In Use" : "Available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing TCP port: {Port}", TcpPort);
            StatusMessage = "Error testing TCP port";
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
                // Dispose managed resources
                _disposables?.Dispose();
                _logger.LogInformation("SocatProfileViewModel disposed");
            }

            // Dispose unmanaged resources (if any)
            _disposed = true;
        }
    }

    /// <summary>
    /// Public implementation of Dispose pattern callable by consumers.
    /// </summary>
    public void Dispose()
    {
        // Dispose of managed resources but do not dispose unmanaged.
        Dispose(true);

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    #endregion
}
