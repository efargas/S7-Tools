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
/// ViewModel for editing individual serial port profiles, providing comprehensive configuration
/// management with real-time validation and stty command preview.
/// </summary>
public class SerialPortProfileViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly ISerialPortProfileService _profileService;
    private readonly ISerialPortService _portService;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<SerialPortProfileViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();
    private SerialPortProfile? _originalProfile;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SerialPortProfileViewModel class.
    /// </summary>
    /// <param name="profileService">The serial port profile service.</param>
    /// <param name="portService">The serial port service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="logger">The logger.</param>
    public SerialPortProfileViewModel(
        ISerialPortProfileService profileService,
        ISerialPortService portService,
        IClipboardService clipboardService,
        ILogger<SerialPortProfileViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _portService = portService ?? throw new ArgumentNullException(nameof(portService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize collections
        ValidationErrors = new ObservableCollection<string>();
        AvailableBaudRates = new ObservableCollection<int> { 50, 75, 110, 134, 150, 200, 300, 600, 1200, 1800, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 500000, 576000, 921600, 1000000, 1152000, 1500000, 2000000, 2500000, 3000000, 3500000, 4000000 };
        AvailableCharacterSizes = new ObservableCollection<int> { 5, 6, 7, 8 };
        AvailableParityModes = new ObservableCollection<ParityMode> { ParityMode.None, ParityMode.Even, ParityMode.Odd, ParityMode.Mark, ParityMode.Space };
        AvailableStopBits = new ObservableCollection<StopBits> { StopBits.One, StopBits.OnePointFive, StopBits.Two };
        AvailablePresets = new ObservableCollection<string> { "Default", "Text", "S7Tools Standard", "High Speed", "Low Latency" };

        // Initialize commands
        InitializeCommands();

        // Set up validation
        SetupValidation();

        _logger.LogInformation("SerialPortProfileViewModel initialized");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public ObservableCollection<string> ValidationErrors { get; }

    /// <summary>
    /// Gets the collection of available baud rates.
    /// </summary>
    public ObservableCollection<int> AvailableBaudRates { get; }

    /// <summary>
    /// Gets the collection of available character sizes.
    /// </summary>
    public ObservableCollection<int> AvailableCharacterSizes { get; }

    /// <summary>
    /// Gets the collection of available parity modes.
    /// </summary>
    public ObservableCollection<ParityMode> AvailableParityModes { get; }

    /// <summary>
    /// Gets the collection of available stop bits options.
    /// </summary>
    public ObservableCollection<StopBits> AvailableStopBits { get; }

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

    #region Configuration Properties

    private int _baudRate = 38400;
    /// <summary>
    /// Gets or sets the baud rate.
    /// </summary>
    public int BaudRate
    {
        get => _baudRate;
        set => this.RaiseAndSetIfChanged(ref _baudRate, value);
    }

    private int _characterSize = 8;
    /// <summary>
    /// Gets or sets the character size.
    /// </summary>
    public int CharacterSize
    {
        get => _characterSize;
        set => this.RaiseAndSetIfChanged(ref _characterSize, value);
    }

    private ParityMode _parity = ParityMode.Even;
    /// <summary>
    /// Gets or sets the parity mode.
    /// </summary>
    public ParityMode Parity
    {
        get => _parity;
        set => this.RaiseAndSetIfChanged(ref _parity, value);
    }

    private StopBits _stopBits = StopBits.One;
    /// <summary>
    /// Gets or sets the stop bits.
    /// </summary>
    public StopBits StopBits
    {
        get => _stopBits;
        set => this.RaiseAndSetIfChanged(ref _stopBits, value);
    }

    private bool _enableReceiver = true;
    /// <summary>
    /// Gets or sets a value indicating whether to enable the receiver.
    /// </summary>
    public bool EnableReceiver
    {
        get => _enableReceiver;
        set => this.RaiseAndSetIfChanged(ref _enableReceiver, value);
    }

    private bool _disableHardwareFlowControl = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable hardware flow control.
    /// </summary>
    public bool DisableHardwareFlowControl
    {
        get => _disableHardwareFlowControl;
        set => this.RaiseAndSetIfChanged(ref _disableHardwareFlowControl, value);
    }

    private bool _parityEnabled = true;
    /// <summary>
    /// Gets or sets a value indicating whether parity is enabled.
    /// </summary>
    public bool ParityEnabled
    {
        get => _parityEnabled;
        set => this.RaiseAndSetIfChanged(ref _parityEnabled, value);
    }

    private bool _oddParity;
    /// <summary>
    /// Gets or sets a value indicating whether to use odd parity.
    /// </summary>
    public bool OddParity
    {
        get => _oddParity;
        set => this.RaiseAndSetIfChanged(ref _oddParity, value);
    }

    private bool _ignoreBreak = true;
    /// <summary>
    /// Gets or sets a value indicating whether to ignore break conditions.
    /// </summary>
    public bool IgnoreBreak
    {
        get => _ignoreBreak;
        set => this.RaiseAndSetIfChanged(ref _ignoreBreak, value);
    }

    private bool _disableBreakInterrupt = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable break interrupt.
    /// </summary>
    public bool DisableBreakInterrupt
    {
        get => _disableBreakInterrupt;
        set => this.RaiseAndSetIfChanged(ref _disableBreakInterrupt, value);
    }

    private bool _disableMapCRtoNL = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable CR to NL mapping.
    /// </summary>
    public bool DisableMapCRtoNL
    {
        get => _disableMapCRtoNL;
        set => this.RaiseAndSetIfChanged(ref _disableMapCRtoNL, value);
    }

    private bool _disableBellOnQueueFull = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable bell on queue full.
    /// </summary>
    public bool DisableBellOnQueueFull
    {
        get => _disableBellOnQueueFull;
        set => this.RaiseAndSetIfChanged(ref _disableBellOnQueueFull, value);
    }

    private bool _disableXonXoffFlowControl = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable XON/XOFF flow control.
    /// </summary>
    public bool DisableXonXoffFlowControl
    {
        get => _disableXonXoffFlowControl;
        set => this.RaiseAndSetIfChanged(ref _disableXonXoffFlowControl, value);
    }

    private bool _disableOutputProcessing = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable output processing.
    /// </summary>
    public bool DisableOutputProcessing
    {
        get => _disableOutputProcessing;
        set => this.RaiseAndSetIfChanged(ref _disableOutputProcessing, value);
    }

    private bool _disableMapNLtoCRNL = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable NL to CR-NL mapping.
    /// </summary>
    public bool DisableMapNLtoCRNL
    {
        get => _disableMapNLtoCRNL;
        set => this.RaiseAndSetIfChanged(ref _disableMapNLtoCRNL, value);
    }

    private bool _disableCanonicalMode = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable canonical mode.
    /// </summary>
    public bool DisableCanonicalMode
    {
        get => _disableCanonicalMode;
        set => this.RaiseAndSetIfChanged(ref _disableCanonicalMode, value);
    }

    private bool _disableSignalGeneration = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable signal generation.
    /// </summary>
    public bool DisableSignalGeneration
    {
        get => _disableSignalGeneration;
        set => this.RaiseAndSetIfChanged(ref _disableSignalGeneration, value);
    }

    private bool _disableExtendedProcessing = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable extended processing.
    /// </summary>
    public bool DisableExtendedProcessing
    {
        get => _disableExtendedProcessing;
        set => this.RaiseAndSetIfChanged(ref _disableExtendedProcessing, value);
    }

    private bool _disableEcho = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable echo.
    /// </summary>
    public bool DisableEcho
    {
        get => _disableEcho;
        set => this.RaiseAndSetIfChanged(ref _disableEcho, value);
    }

    private bool _disableEchoErase = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable echo erase.
    /// </summary>
    public bool DisableEchoErase
    {
        get => _disableEchoErase;
        set => this.RaiseAndSetIfChanged(ref _disableEchoErase, value);
    }

    private bool _disableEchoKill = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable echo kill.
    /// </summary>
    public bool DisableEchoKill
    {
        get => _disableEchoKill;
        set => this.RaiseAndSetIfChanged(ref _disableEchoKill, value);
    }

    private bool _disableEchoControl = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable echo control.
    /// </summary>
    public bool DisableEchoControl
    {
        get => _disableEchoControl;
        set => this.RaiseAndSetIfChanged(ref _disableEchoControl, value);
    }

    private bool _disableEchoKillErase = true;
    /// <summary>
    /// Gets or sets a value indicating whether to disable echo kill erase.
    /// </summary>
    public bool DisableEchoKillErase
    {
        get => _disableEchoKillErase;
        set => this.RaiseAndSetIfChanged(ref _disableEchoKillErase, value);
    }

    private bool _rawMode = true;
    /// <summary>
    /// Gets or sets a value indicating whether to enable raw mode.
    /// </summary>
    public bool RawMode
    {
        get => _rawMode;
        set => this.RaiseAndSetIfChanged(ref _rawMode, value);
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

    private string _sttyCommand = string.Empty;
    /// <summary>
    /// Gets or sets the generated stty command preview.
    /// </summary>
    public string SttyCommand
    {
        get => _sttyCommand;
        set => this.RaiseAndSetIfChanged(ref _sttyCommand, value);
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
    /// Gets the command to copy the stty command to clipboard.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CopySttyCommandCommand { get; private set; } = null!;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads a profile for editing.
    /// </summary>
    /// <param name="profile">The profile to load.</param>
    public void LoadProfile(SerialPortProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _originalProfile = profile.Clone();

        // Load profile properties
        ProfileName = profile.Name;
        ProfileDescription = profile.Description;
        IsDefault = profile.IsDefault;
        IsReadOnly = profile.IsReadOnly;

        // Load configuration properties
        var config = profile.Configuration;
        BaudRate = config.BaudRate;
        CharacterSize = config.CharacterSize;
        Parity = config.Parity;
        StopBits = config.StopBits;
        EnableReceiver = config.EnableReceiver;
        DisableHardwareFlowControl = config.DisableHardwareFlowControl;
        ParityEnabled = config.ParityEnabled;
        OddParity = config.OddParity;
        IgnoreBreak = config.IgnoreBreak;
        DisableBreakInterrupt = config.DisableBreakInterrupt;
        DisableMapCRtoNL = config.DisableMapCRtoNL;
        DisableBellOnQueueFull = config.DisableBellOnQueueFull;
        DisableXonXoffFlowControl = config.DisableXonXoffFlowControl;
        DisableOutputProcessing = config.DisableOutputProcessing;
        DisableMapNLtoCRNL = config.DisableMapNLtoCRNL;
        DisableCanonicalMode = config.DisableCanonicalMode;
        DisableSignalGeneration = config.DisableSignalGeneration;
        DisableExtendedProcessing = config.DisableExtendedProcessing;
        DisableEcho = config.DisableEcho;
        DisableEchoErase = config.DisableEchoErase;
        DisableEchoKill = config.DisableEchoKill;
        DisableEchoControl = config.DisableEchoControl;
        DisableEchoKillErase = config.DisableEchoKillErase;
        RawMode = config.RawMode;

        HasChanges = false;
        UpdateSttyCommand();
        ValidateConfiguration();

        _logger.LogInformation("Loaded profile for editing: {ProfileName}", profile.Name);
    }

    /// <summary>
    /// Creates a new profile from the current configuration.
    /// </summary>
    /// <returns>A new SerialPortProfile with the current configuration.</returns>
    public SerialPortProfile CreateProfile()
    {
        var profile = new SerialPortProfile
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
        var canSave = this.WhenAnyValue(x => x.IsValid, x => x.HasChanges, x => x.IsReadOnly)
            .Select(tuple => tuple.Item1 && tuple.Item2 && !tuple.Item3);

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
        SaveCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "saving profile"))
            .DisposeWith(_disposables);

        // Cancel command - always enabled
        CancelCommand = ReactiveCommand.Create(Cancel);

        // Reset to default command - enabled when not read-only
        var canReset = this.WhenAnyValue(x => x.IsReadOnly)
            .Select(readOnly => !readOnly);

        ResetToDefaultCommand = ReactiveCommand.Create(ResetToDefault, canReset);

        // Load preset command - enabled when not read-only
        LoadPresetCommand = ReactiveCommand.Create<string>(LoadPreset, canReset);

        // Validate command - always enabled
        ValidateCommand = ReactiveCommand.Create(ValidateConfiguration);

        // Copy stty command - always enabled
        CopySttyCommandCommand = ReactiveCommand.CreateFromTask(CopySttyCommandAsync);
        CopySttyCommandCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "copying stty command"))
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
            UpdateSttyCommand();
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

        // Subscribe to basic configuration property changes
        this.WhenAnyValue(x => x.BaudRate)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.CharacterSize)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.Parity)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.StopBits)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.EnableReceiver)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableHardwareFlowControl)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.ParityEnabled)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.OddParity)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.IgnoreBreak)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableBreakInterrupt)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableMapCRtoNL)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableBellOnQueueFull)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableXonXoffFlowControl)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableOutputProcessing)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableMapNLtoCRNL)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableCanonicalMode)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableSignalGeneration)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableExtendedProcessing)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableEcho)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableEchoErase)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableEchoKill)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableEchoControl)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.DisableEchoKillErase)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.RawMode)
            .Skip(1)
            .Subscribe(_ => OnPropertyChanged())
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Creates a SerialPortConfiguration from the current property values.
    /// </summary>
    /// <returns>A new SerialPortConfiguration.</returns>
    private SerialPortConfiguration CreateConfiguration()
    {
        return new SerialPortConfiguration
        {
            BaudRate = BaudRate,
            CharacterSize = CharacterSize,
            Parity = Parity,
            StopBits = StopBits,
            EnableReceiver = EnableReceiver,
            DisableHardwareFlowControl = DisableHardwareFlowControl,
            ParityEnabled = ParityEnabled,
            OddParity = OddParity,
            IgnoreBreak = IgnoreBreak,
            DisableBreakInterrupt = DisableBreakInterrupt,
            DisableMapCRtoNL = DisableMapCRtoNL,
            DisableBellOnQueueFull = DisableBellOnQueueFull,
            DisableXonXoffFlowControl = DisableXonXoffFlowControl,
            DisableOutputProcessing = DisableOutputProcessing,
            DisableMapNLtoCRNL = DisableMapNLtoCRNL,
            DisableCanonicalMode = DisableCanonicalMode,
            DisableSignalGeneration = DisableSignalGeneration,
            DisableExtendedProcessing = DisableExtendedProcessing,
            DisableEcho = DisableEcho,
            DisableEchoErase = DisableEchoErase,
            DisableEchoKill = DisableEchoKill,
            DisableEchoControl = DisableEchoControl,
            DisableEchoKillErase = DisableEchoKillErase,
            RawMode = RawMode
        };
    }

    /// <summary>
    /// Updates the stty command preview.
    /// </summary>
    private void UpdateSttyCommand()
    {
        try
        {
            var config = CreateConfiguration();
            SttyCommand = _portService.GenerateSttyCommand("/dev/ttyUSB0", config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stty command");
            SttyCommand = "Error generating command";
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
            var config = CreateConfiguration();
            var configErrors = config.Validate();
            foreach (var error in configErrors)
            {
                ValidationErrors.Add(error);
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
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            StatusMessage = "Saving profile...";

            var profile = CreateProfile();

            if (_originalProfile != null)
            {
                profile.Id = _originalProfile.Id;
                await _profileService.UpdateProfileAsync(profile);
            }
            else
            {
                await _profileService.CreateProfileAsync(profile);
            }

            HasChanges = false;
            StatusMessage = "Profile saved successfully";
            _logger.LogInformation("Profile saved: {ProfileName}", profile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving profile");
            StatusMessage = "Error saving profile";
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
        var defaultConfig = SerialPortConfiguration.CreateDefault();

        BaudRate = defaultConfig.BaudRate;
        CharacterSize = defaultConfig.CharacterSize;
        Parity = defaultConfig.Parity;
        StopBits = defaultConfig.StopBits;
        EnableReceiver = defaultConfig.EnableReceiver;
        DisableHardwareFlowControl = defaultConfig.DisableHardwareFlowControl;
        ParityEnabled = defaultConfig.ParityEnabled;
        OddParity = defaultConfig.OddParity;
        IgnoreBreak = defaultConfig.IgnoreBreak;
        DisableBreakInterrupt = defaultConfig.DisableBreakInterrupt;
        DisableMapCRtoNL = defaultConfig.DisableMapCRtoNL;
        DisableBellOnQueueFull = defaultConfig.DisableBellOnQueueFull;
        DisableXonXoffFlowControl = defaultConfig.DisableXonXoffFlowControl;
        DisableOutputProcessing = defaultConfig.DisableOutputProcessing;
        DisableMapNLtoCRNL = defaultConfig.DisableMapNLtoCRNL;
        DisableCanonicalMode = defaultConfig.DisableCanonicalMode;
        DisableSignalGeneration = defaultConfig.DisableSignalGeneration;
        DisableExtendedProcessing = defaultConfig.DisableExtendedProcessing;
        DisableEcho = defaultConfig.DisableEcho;
        DisableEchoErase = defaultConfig.DisableEchoErase;
        DisableEchoKill = defaultConfig.DisableEchoKill;
        DisableEchoControl = defaultConfig.DisableEchoControl;
        DisableEchoKillErase = defaultConfig.DisableEchoKillErase;
        RawMode = defaultConfig.RawMode;

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
            SerialPortConfiguration config = presetName switch
            {
                "Default" => SerialPortConfiguration.CreateDefault(),
                "Text" => SerialPortConfiguration.CreateTextMode(),
                "S7Tools Standard" => SerialPortConfiguration.CreateDefault(),
                "High Speed" => CreateHighSpeedConfiguration(),
                "Low Latency" => CreateLowLatencyConfiguration(),
                _ => SerialPortConfiguration.CreateDefault()
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
    /// Creates a high-speed configuration optimized for fast data transfer.
    /// </summary>
    /// <returns>A SerialPortConfiguration optimized for high-speed communication.</returns>
    private SerialPortConfiguration CreateHighSpeedConfiguration()
    {
        var config = SerialPortConfiguration.CreateDefault();
        config.BaudRate = 115200; // Higher baud rate
        config.CharacterSize = 8;
        config.Parity = ParityMode.None; // No parity for speed
        config.StopBits = StopBits.One;
        return config;
    }

    /// <summary>
    /// Creates a low-latency configuration optimized for minimal delay.
    /// </summary>
    /// <returns>A SerialPortConfiguration optimized for low-latency communication.</returns>
    private SerialPortConfiguration CreateLowLatencyConfiguration()
    {
        var config = SerialPortConfiguration.CreateDefault();
        config.BaudRate = 57600; // Moderate speed for reliability
        config.CharacterSize = 8;
        config.Parity = ParityMode.None;
        config.StopBits = StopBits.One;
        config.RawMode = true; // Ensure raw mode for minimal processing
        return config;
    }

    /// <summary>
    /// Applies a configuration to the current properties.
    /// </summary>
    /// <param name="config">The configuration to apply.</param>
    private void ApplyConfiguration(SerialPortConfiguration config)
    {
        BaudRate = config.BaudRate;
        CharacterSize = config.CharacterSize;
        Parity = config.Parity;
        StopBits = config.StopBits;
        EnableReceiver = config.EnableReceiver;
        DisableHardwareFlowControl = config.DisableHardwareFlowControl;
        ParityEnabled = config.ParityEnabled;
        OddParity = config.OddParity;
        IgnoreBreak = config.IgnoreBreak;
        DisableBreakInterrupt = config.DisableBreakInterrupt;
        DisableMapCRtoNL = config.DisableMapCRtoNL;
        DisableBellOnQueueFull = config.DisableBellOnQueueFull;
        DisableXonXoffFlowControl = config.DisableXonXoffFlowControl;
        DisableOutputProcessing = config.DisableOutputProcessing;
        DisableMapNLtoCRNL = config.DisableMapNLtoCRNL;
        DisableCanonicalMode = config.DisableCanonicalMode;
        DisableSignalGeneration = config.DisableSignalGeneration;
        DisableExtendedProcessing = config.DisableExtendedProcessing;
        DisableEcho = config.DisableEcho;
        DisableEchoErase = config.DisableEchoErase;
        DisableEchoKill = config.DisableEchoKill;
        DisableEchoControl = config.DisableEchoControl;
        DisableEchoKillErase = config.DisableEchoKillErase;
        RawMode = config.RawMode;
    }

    /// <summary>
    /// Copies the stty command to the clipboard.
    /// </summary>
    private async Task CopySttyCommandAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(SttyCommand) || SttyCommand == "Error generating command")
            {
                StatusMessage = "No valid stty command to copy";
                return;
            }

            await _clipboardService.SetTextAsync(SttyCommand);
            StatusMessage = "stty command copied to clipboard";
            _logger.LogInformation("stty command copied to clipboard: {Command}", SttyCommand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying stty command to clipboard");
            StatusMessage = "Error copying to clipboard";
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

    private bool _disposed = false;

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
                _logger.LogInformation("SerialPortProfileViewModel disposed");
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
