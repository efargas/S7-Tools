using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for editing PowerSupply profiles.
/// Handles creation, editing, and validation of PowerSupply profile configurations.
/// </summary>
public class PowerSupplyProfileViewModel : ViewModelBase, INotifyPropertyChanged
{
    #region Private Fields

    private readonly IPowerSupplyProfileService _profileService;
    private readonly ILogger<PowerSupplyProfileViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private PowerSupplyProfile? _originalProfile;

    // Profile properties
    private string _profileName = string.Empty;
    private string _profileDescription = string.Empty;
    private bool _isDefault;
    private bool _isReadOnly;

    // Configuration properties
    private PowerSupplyType _powerSupplyType = PowerSupplyType.ModbusTcp;

    // Status properties
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
        set => this.RaiseAndSetIfChanged(ref _powerSupplyType, value);
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
    /// Gets whether the profile has unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get => _hasChanges;
        private set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
    }

    /// <summary>
    /// Gets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
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

        // Load profile properties
        ProfileName = profile.Name;
        ProfileDescription = profile.Description;
        IsDefault = profile.IsDefault;
        IsReadOnly = profile.IsReadOnly;

        // Load configuration properties
        PowerSupplyType = profile.Configuration.Type;

        // Reset status
        HasChanges = false;
        StatusMessage = "Profile loaded";
        _logger.LogDebug("Loaded power supply profile: {ProfileName}", profile.Name);
    }

    /// <summary>
    /// Creates a new profile from the current configuration.
    /// </summary>
    /// <returns>A new PowerSupplyProfile instance.</returns>
    public PowerSupplyProfile CreateProfile()
    {
        var profile = new PowerSupplyProfile
        {
            Name = ProfileName,
            Description = ProfileDescription,
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            Configuration = CreateConfigurationForType(PowerSupplyType)
        };

        return profile;
    }

    /// <summary>
    /// Creates the appropriate configuration object based on the power supply type.
    /// </summary>
    /// <param name="type">The power supply type.</param>
    /// <returns>A configuration object for the specified type.</returns>
    private static PowerSupplyConfiguration CreateConfigurationForType(PowerSupplyType type)
    {
        return type switch
        {
            PowerSupplyType.ModbusTcp => new ModbusTcpConfiguration(),
            PowerSupplyType.ModbusRtu => throw new NotImplementedException("Modbus RTU configuration not yet implemented"),
            PowerSupplyType.Snmp => throw new NotImplementedException("SNMP configuration not yet implemented"),
            PowerSupplyType.HttpRest => throw new NotImplementedException("HTTP REST configuration not yet implemented"),
            PowerSupplyType.Custom => throw new NotImplementedException("Custom configuration not yet implemented"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown power supply type")
        };
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

        // Individual subscriptions for each property that affects validation/changes
        this.WhenAnyValue(x => x.ProfileName).Skip(1).Subscribe(_ => OnPropertyChanged()).DisposeWith(_disposables);
        this.WhenAnyValue(x => x.ProfileDescription).Skip(1).Subscribe(_ => OnPropertyChanged()).DisposeWith(_disposables);
        this.WhenAnyValue(x => x.IsDefault).Skip(1).Subscribe(_ => OnPropertyChanged()).DisposeWith(_disposables);
        this.WhenAnyValue(x => x.PowerSupplyType).Skip(1).Subscribe(_ => OnPropertyChanged()).DisposeWith(_disposables);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    private void ValidateConfiguration()
    {
        var isValid = true;
        var statusMessage = "Ready";

        // Validate profile name
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            isValid = false;
            statusMessage = "Profile name is required";
        }
        else if (ProfileName.Length > 255)
        {
            isValid = false;
            statusMessage = "Profile name is too long (max 255 characters)";
        }

        // Validate description
        if (!string.IsNullOrEmpty(ProfileDescription) && ProfileDescription.Length > 1000)
        {
            isValid = false;
            statusMessage = "Profile description is too long (max 1000 characters)";
        }

        IsValid = isValid;
        StatusMessage = statusMessage;
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
                profile.CreatedAt = _originalProfile.CreatedAt;
                profile.ModifiedAt = DateTime.UtcNow;
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
            // Reset to default values for new profile
            ProfileName = string.Empty;
            ProfileDescription = string.Empty;
            IsDefault = false;
            IsReadOnly = false;
            PowerSupplyType = PowerSupplyType.ModbusTcp;
            HasChanges = false;
        }

        StatusMessage = "Changes cancelled";
        _logger.LogDebug("Profile editing cancelled");
    }

    /// <summary>
    /// Handles exceptions from reactive commands.
    /// </summary>
    private void HandleCommandException(Exception ex, string operation)
    {
        _logger.LogError(ex, "Error {Operation}", operation);
        StatusMessage = $"Error {operation}";
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
        if (!_disposed)
        {
            if (disposing)
            {
                _disposables?.Dispose();
                _logger.LogInformation("PowerSupplyProfileViewModel disposed");
            }
            _disposed = true;
        }
    }

    #endregion
}
