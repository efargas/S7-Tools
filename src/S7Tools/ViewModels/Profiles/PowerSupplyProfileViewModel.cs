using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using S7Tools.Core.Constants;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Resources;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Profiles;

/// <summary>
/// Handles creation, editing, and validation of PowerSupply profile configurations.
/// </summary>
public class PowerSupplyProfileViewModel : ViewModelBase, INotifyPropertyChanged, IDisposable
{
    #region Private Fields

    private readonly IPowerSupplyProfileService _profileService;
    private readonly ILogger<PowerSupplyProfileViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private PowerSupplyProfile? _originalProfile;

    private string _profileName = string.Empty;
    private string _profileDescription = string.Empty;
    private bool _isDefault;
    private bool _isReadOnly;

    private PowerSupplyType _powerSupplyType = PowerSupplyType.ModbusTcp;

    private string _modbusTcpHost = "192.168.1.100";
    private int _modbusTcpPort = 502;
    private int _modbusTcpDeviceId = 1;
    private int _modbusTcpOnOffCoil;
    private ModbusAddressingMode _modbusTcpAddressingMode = ModbusAddressingMode.Base1; // Default to Base-1 (user-friendly)

    private bool _isValid = true;
    private bool _hasChanges;
    private string _statusMessage = string.Empty;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the PowerSupplyProfileViewModel class.
    /// </summary>
    /// <param name="profileService">The power supply profile service.</param>
    /// <param name="logger">The logger instance.</param>
    public PowerSupplyProfileViewModel(
        IPowerSupplyProfileService profileService,
        ILogger<PowerSupplyProfileViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeCommands();
        SetupValidation();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the profile name.
    /// </summary>
    public string ProfileName
    {
        get => _profileName;
        set => this.RaiseAndSetIfChanged(ref _profileName, value);
    }

    /// <summary>
    /// Gets or sets the profile description.
    /// </summary>
    public string ProfileDescription
    {
        get => _profileDescription;
        set => this.RaiseAndSetIfChanged(ref _profileDescription, value);
    }

    /// <summary>
    /// Gets or sets whether this profile is the default.
    /// </summary>
    public bool IsDefault
    {
        get => _isDefault;
        set => this.RaiseAndSetIfChanged(ref _isDefault, value);
    }

    /// <summary>
    /// Gets or sets whether this profile is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
    }

    /// <summary>
    /// Gets or sets the power supply type.
    /// </summary>
    public PowerSupplyType PowerSupplyType
    {
        get => _powerSupplyType;
        set
        {
            PowerSupplyType oldValue = _powerSupplyType;
            this.RaiseAndSetIfChanged(ref _powerSupplyType, value);
            if (oldValue != value)
            {
                this.RaisePropertyChanged(nameof(IsModbusTcp));
            }
        }
    }

    /// <summary>
    /// Gets whether the current power supply type is ModbusTcp.
    /// Used to control visibility of ModbusTcp configuration fields.
    /// </summary>
    public bool IsModbusTcp => PowerSupplyType is PowerSupplyType.ModbusTcp;

    /// <summary>
    /// Gets or sets the power supply type as ComboBox index.
    /// 0 = ModbusTcp, 1 = SerialRs232, 2 = SerialRs485, 3 = EthernetIp
    /// </summary>
    public int PowerSupplyTypeIndex
    {
        get => (int)PowerSupplyType;
        set => PowerSupplyType = (PowerSupplyType)value;
    }

    /// <summary>
    /// Gets or sets the ModbusTcp host/IP address.
    /// </summary>
    public string ModbusTcpHost
    {
        get => _modbusTcpHost;
        set => this.RaiseAndSetIfChanged(ref _modbusTcpHost, value);
    }

    /// <summary>
    /// Gets or sets the ModbusTcp port number.
    /// </summary>
    public int ModbusTcpPort
    {
        get => _modbusTcpPort;
        set => this.RaiseAndSetIfChanged(ref _modbusTcpPort, value);
    }

    /// <summary>
    /// Gets or sets the ModbusTcp device ID (slave address).
    /// </summary>
    public int ModbusTcpDeviceId
    {
        get => _modbusTcpDeviceId;
        set => this.RaiseAndSetIfChanged(ref _modbusTcpDeviceId, value);
    }

    /// <summary>
    /// Gets or sets the ModbusTcp On/Off coil address.
    /// </summary>
    public int ModbusTcpOnOffCoil
    {
        get => _modbusTcpOnOffCoil;
        set => this.RaiseAndSetIfChanged(ref _modbusTcpOnOffCoil, value);
    }

    /// <summary>
    /// Gets or sets the ModbusTcp addressing mode (Base-0 or Base-1).
    /// </summary>
    /// <remarks>
    /// Base-0: Addresses start at 0 (protocol addressing)
    /// Base-1: Addresses start at 1 (user-friendly, converted internally)
    /// </remarks>
    public ModbusAddressingMode ModbusTcpAddressingMode
    {
        get => _modbusTcpAddressingMode;
        set => this.RaiseAndSetIfChanged(ref _modbusTcpAddressingMode, value);
    }

    /// <summary>
    /// Gets or sets the ModbusTcp addressing mode as ComboBox index.
    /// 0 = Base-0, 1 = Base-1
    /// </summary>
    public int ModbusTcpAddressingModeIndex
    {
        get => (int)ModbusTcpAddressingMode;
        set => ModbusTcpAddressingMode = (ModbusAddressingMode)value;
    }

    /// <summary>
    /// Gets whether the profile configuration is valid.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the profile has unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get => _hasChanges;
        set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
    }

    /// <summary>
    /// Gets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to save the current profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; private set; } = null!;

    /// <summary>
    /// Command to cancel editing and revert changes.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; } = null!;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads a profile for editing.
    /// </summary>
    /// <param name="profile">The profile to load.</param>
    public void LoadProfile(PowerSupplyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _originalProfile = profile.ClonePreserveId();

        ProfileName = profile.Name;
        ProfileDescription = profile.Description;
        IsDefault = profile.IsDefault;
        IsReadOnly = profile.IsReadOnly;
        PowerSupplyType = profile.Configuration.Type;

        if (profile.Configuration is ModbusTcpConfiguration modbusTcpConfig)
        {
            ModbusTcpHost = modbusTcpConfig.Host;
            ModbusTcpPort = modbusTcpConfig.Port;
            ModbusTcpDeviceId = modbusTcpConfig.DeviceId;
            ModbusTcpOnOffCoil = modbusTcpConfig.OnOffCoil;
            ModbusTcpAddressingMode = modbusTcpConfig.AddressingMode;
        }

        HasChanges = false;
        StatusMessage = UIStrings.Status_ProfileLoaded;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Loaded power supply profile: {ProfileName}", profile.Name);
        }
    }

    /// <summary>
    /// Creates a new profile from the current configuration.
    /// </summary>
    /// <returns>A new PowerSupplyProfile instance.</returns>
    public PowerSupplyProfile CreateProfile()
    {
        var profile = new PowerSupplyProfile
        {
            Id = _originalProfile?.Id ?? 0, // CRITICAL: Preserve ID for editing existing profiles, 0 for new profiles
            Name = ProfileName,
            Description = ProfileDescription,
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            Configuration = CreateConfigurationForType(PowerSupplyType),
            CreatedAt = _originalProfile?.CreatedAt ?? DateTime.UtcNow, // Preserve CreatedAt for existing profiles
            ModifiedAt = DateTime.UtcNow // Always update ModifiedAt
        };

        return profile;
    }

    /// <summary>
    /// Creates the appropriate configuration object based on the power supply type.
    /// </summary>
    /// <param name="type">The power supply type.</param>
    /// <returns>A configuration object for the specified type.</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when attempting to create a configuration for SerialRs232, SerialRs485, or EthernetIp types,
    /// which are planned for future implementation.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown power supply type is provided.</exception>
    private PowerSupplyConfiguration CreateConfigurationForType(PowerSupplyType type) => type switch
    {
        PowerSupplyType.ModbusTcp => new ModbusTcpConfiguration
        {
            Host = ModbusTcpHost,
            Port = ModbusTcpPort,
            DeviceId = (byte)ModbusTcpDeviceId,
            OnOffCoil = (ushort)ModbusTcpOnOffCoil,
            AddressingMode = ModbusTcpAddressingMode
        },
        PowerSupplyType.SerialRs232 or PowerSupplyType.SerialRs485 or PowerSupplyType.EthernetIp =>
            throw new NotImplementedException($"{type} power supply configuration is planned for a future release. Currently, only Modbus TCP is supported."),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown power supply type")
    };

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

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
        SaveCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "saving profile"))
            .DisposeWith(_disposables);

        // Cancel command - always enabled
        CancelCommand = ReactiveCommand.Create(Cancel);
        CancelCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "cancelling edit"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Sets up validation and change detection.
    /// </summary>
    private void SetupValidation()
    {
        // Shared handler for all property changes
        void OnPropertyChanged()
        {
            HasChanges = true;
            ValidateConfiguration();
        }

        Observable.Merge(
            this.WhenAnyValue(x => x.ProfileName).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.ProfileDescription).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.IsDefault).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.PowerSupplyType).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.ModbusTcpHost).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.ModbusTcpPort).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.ModbusTcpDeviceId).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.ModbusTcpOnOffCoil).Select(_ => Unit.Default),
            this.WhenAnyValue(x => x.ModbusTcpAddressingMode).Select(_ => Unit.Default)
        )
        .Skip(1)
        .Subscribe(_ => OnPropertyChanged())
        .DisposeWith(_disposables);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    private void ValidateConfiguration()
    {
        bool isValid = true;
        string statusMessage = UIStrings.Status_Ready;

        // Validate profile name
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            isValid = false;
            statusMessage = UIStrings.Validation_ProfileNameRequired;
        }
        else if (ProfileName.Length > 255)
        {
            isValid = false;
            statusMessage = UIStrings.Validation_ProfileNameTooLong;
        }

        // Validate description
        if (!string.IsNullOrEmpty(ProfileDescription) && ProfileDescription.Length > 1000)
        {
            isValid = false;
            statusMessage = UIStrings.Validation_ProfileDescriptionTooLong;
        }

        // Validate ModbusTcp configuration if applicable
        if (PowerSupplyType == PowerSupplyType.ModbusTcp && isValid)
        {
            if (string.IsNullOrWhiteSpace(ModbusTcpHost))
            {
                isValid = false;
                statusMessage = UIStrings.Validation_ModbusTcpHostRequired;
            }
            else if (!NetworkConstants.IsValidPort(ModbusTcpPort))
            {
                isValid = false;
                statusMessage = UIStrings.Validation_ModbusTcpPortRange;
            }
            else if (ModbusTcpDeviceId < 0 || ModbusTcpDeviceId > 255)
            {
                isValid = false;
                statusMessage = UIStrings.Validation_ModbusTcpDeviceIdRange;
            }
            else if (ModbusTcpOnOffCoil < 0 || ModbusTcpOnOffCoil > 65535)
            {
                isValid = false;
                statusMessage = UIStrings.Validation_ModbusTcpCoilRange;
            }
        }

        IsValid = isValid;
        StatusMessage = statusMessage;
    }

    /// <summary>
    /// Saves the current profile configuration.
    /// This method is exposed publicly to allow direct invocation from the UI layer,
    /// avoiding ReactiveCommand execution deadlocks on the UI thread.
    /// </summary>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveAsync()
    {
        try
        {
            StatusMessage = UIStrings.Status_SavingProfile;

            PowerSupplyProfile profile = CreateProfile();

            if (_originalProfile != null)
            {
                profile.Id = _originalProfile.Id;
                profile.CreatedAt = _originalProfile.CreatedAt;
                profile.ModifiedAt = DateTime.UtcNow;
                await _profileService.UpdateAsync(profile).ConfigureAwait(false);
                _logger.LogInformation("Profile updated successfully: {ProfileName} (ID: {ProfileId})", profile.Name, profile.Id);
            }
            else
            {
                _logger.LogInformation("Creating new profile: {ProfileName}", profile.Name);
                PowerSupplyProfile createdProfile = await _profileService.CreateAsync(profile).ConfigureAwait(false);
                _logger.LogInformation("Profile created successfully: {ProfileName} (ID: {ProfileId})", createdProfile.Name, createdProfile.Id);

                // Update _originalProfile with the created profile so subsequent saves are updates
                _originalProfile = createdProfile;
            }

            HasChanges = false;
            StatusMessage = UIStrings.Status_ProfileSaved;
            _logger.LogInformation("Profile save operation completed: {ProfileName}", profile.Name);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while saving profile: {ProfileName}", ProfileName);
            StatusMessage = string.Format(UIStrings.Status_SaveFailed, ex.Message);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid profile data while saving: {ProfileName}", ProfileName);
            StatusMessage = string.Format(UIStrings.Status_InvalidProfileData, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving profile: {ProfileName}", ProfileName);
            StatusMessage = string.Format(UIStrings.Status_SaveFailed, ex.Message);
            throw;
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
            // Reset to default values for new profile
            ProfileName = string.Empty;
            ProfileDescription = string.Empty;
            IsDefault = false;
            IsReadOnly = false;
            PowerSupplyType = PowerSupplyType.ModbusTcp;
            HasChanges = false;
        }

        StatusMessage = UIStrings.Status_ChangesCancelled;
        _logger.LogDebug("Profile editing cancelled");
    }

    /// <summary>
    /// Handles exceptions from reactive commands.
    /// </summary>
    private void HandleCommandException(Exception ex, string operation)
    {
        string errorMessage = ex switch
        {
            InvalidOperationException => string.Format(UIStrings.Error_InvalidOperation, operation),
            ArgumentException => string.Format(UIStrings.Error_InvalidData, operation),
            UnauthorizedAccessException => string.Format(UIStrings.Error_AccessDeniedOperation, operation),
            _ => string.Format(UIStrings.Error_Generic, operation)
        };

        _logger.LogError(ex, "Error {Operation}: {Message}", operation, ex.Message);
        StatusMessage = string.Format(UIStrings.Status_Error, errorMessage, ex.Message);
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _disposables?.Dispose();
            _logger.LogInformation("PowerSupplyProfileViewModel disposed");
        }
        _disposed = true;
    }

    #endregion
}

// Partial class implementation for disposal pattern
// Removed duplicate partial Dispose implementation to avoid conflicts
