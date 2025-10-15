using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Models;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.Views;

namespace S7Tools.Services;

/// <summary>
/// Service implementation for displaying profile editing dialogs.
/// </summary>
public class ProfileEditDialogService : IProfileEditDialogService
{
    private static bool _handlerRegistered;
    private static readonly object _lockObject = new();

    // Dependencies for enhanced dialog system
    private readonly ISerialPortProfileService _serialPortProfileService;
    private readonly ISocatProfileService _socatProfileService;
    private readonly IPowerSupplyProfileService _powerSupplyProfileService;
    private readonly ISerialPortService _serialPortService;
    private readonly ISocatService _socatService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ProfileEditDialogService> _logger;

    /// <summary>
    /// Static interaction shared across all service instances.
    /// </summary>
    private static readonly Interaction<S7Tools.Models.ProfileEditRequest, ProfileEditResult> _staticInteraction = new();

    /// <summary>
    /// Gets the interaction for showing profile editing dialogs.
    /// </summary>
    public Interaction<S7Tools.Models.ProfileEditRequest, ProfileEditResult> ShowProfileEditDialog => _staticInteraction;

    /// <summary>
    /// Initializes a new instance of the ProfileEditDialogService class.
    /// </summary>
    /// <param name="serialPortProfileService">The serial port profile service.</param>
    /// <param name="socatProfileService">The socat profile service.</param>
    /// <param name="powerSupplyProfileService">The power supply profile service.</param>
    /// <param name="serialPortService">The serial port service.</param>
    /// <param name="socatService">The socat service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="logger">The logger.</param>
    public ProfileEditDialogService(
        ISerialPortProfileService serialPortProfileService,
        ISocatProfileService socatProfileService,
        IPowerSupplyProfileService powerSupplyProfileService,
        ISerialPortService serialPortService,
        ISocatService socatService,
        IClipboardService clipboardService,
        IDialogService dialogService,
        ILogger<ProfileEditDialogService> logger)
    {
        _serialPortProfileService = serialPortProfileService ?? throw new ArgumentNullException(nameof(serialPortProfileService));
        _socatProfileService = socatProfileService ?? throw new ArgumentNullException(nameof(socatProfileService));
        _powerSupplyProfileService = powerSupplyProfileService ?? throw new ArgumentNullException(nameof(powerSupplyProfileService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _socatService = socatService ?? throw new ArgumentNullException(nameof(socatService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RegisterInteractionHandler();
    }

    /// <summary>
    /// Registers the interaction handler for profile edit dialogs (thread-safe, one-time only).
    /// </summary>
    private void RegisterInteractionHandler()
    {
        lock (_lockObject)
        {
            if (_handlerRegistered)
            {
                return;
            }

            _staticInteraction.RegisterHandler(async interaction =>
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: ProfileEditDialogService interaction handler called");
                try
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: Creating ProfileEditDialog for {interaction.Input.ProfileType}");

                    // Create and setup profile edit dialog
                    var dialog = new Views.ProfileEditDialog();
                    dialog.SetupDialog(interaction.Input);

                    System.Diagnostics.Debug.WriteLine($"DEBUG: Dialog created and setup completed");

                    // Get the main window as parent
                    var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : null;

                    if (mainWindow != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Showing dialog with main window as parent");
                        await dialog.ShowDialog(mainWindow);
                        System.Diagnostics.Debug.WriteLine($"DEBUG: Dialog closed, result: {dialog.Result.IsSuccess}");
                        interaction.SetOutput(dialog.Result);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR: MainWindow not found for dialog parent");
                        interaction.SetOutput(ProfileEditResult.Cancelled());
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Exception in ProfileEditDialogService interaction handler: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
                    interaction.SetOutput(ProfileEditResult.Cancelled());
                }
            });

            _handlerRegistered = true;
        }
    }

    /// <summary>
    /// Shows a profile editing dialog for a serial port profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SerialPortProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    public async Task<ProfileEditResult> ShowSerialProfileEditAsync(string title, SerialPortProfileViewModel profileViewModel)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(profileViewModel);

        var request = new S7Tools.Models.ProfileEditRequest(title, profileViewModel, ProfileType.Serial);
        return await ShowProfileEditDialog.Handle(request).FirstAsync();
    }

    /// <summary>
    /// Shows a profile editing dialog for a socat profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SocatProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    public async Task<ProfileEditResult> ShowSocatProfileEditAsync(string title, SocatProfileViewModel profileViewModel)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(profileViewModel);

        var request = new S7Tools.Models.ProfileEditRequest(title, profileViewModel, ProfileType.Socat);
        return await ShowProfileEditDialog.Handle(request).FirstAsync();
    }

    /// <inheritdoc />
    public async Task<ProfileEditResult> ShowPowerSupplyProfileEditAsync(string title, PowerSupplyProfileViewModel profileViewModel)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(profileViewModel);

        var request = new S7Tools.Models.ProfileEditRequest(title, profileViewModel, ProfileType.PowerSupply);
        return await ShowProfileEditDialog.Handle(request).FirstAsync();
    }

    // Enhanced methods for Phase 6 - Unified Dialog System

    /// <inheritdoc />
    public async Task<ProfileEditResult> CreateSerialProfileAsync(string defaultName = "SerialDefault")
    {
        try
        {
            _logger.LogInformation("Creating new serial port profile with default name: {DefaultName}", defaultName);

            // Create ViewModel with default values
            var profileViewModel = new SerialPortProfileViewModel(
                _serialPortProfileService,
                _serialPortService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SerialPortProfileViewModel>.Instance);

            // Set the default name
            profileViewModel.ProfileName = defaultName;

            // CRITICAL: For new profiles, mark as having changes so SaveCommand is enabled
            profileViewModel.HasChanges = true;

            // Show the edit dialog for the new profile
            var result = await ShowSerialProfileEditAsync("Create Serial Port Profile", profileViewModel);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Serial port profile created successfully");
            }
            else
            {
                _logger.LogInformation("Serial port profile creation cancelled or failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating serial port profile");
            return ProfileEditResult.Cancelled();
        }
    }

    /// <inheritdoc />
    public async Task<ProfileEditResult> CreateSocatProfileAsync(string defaultName = "SocatDefault")
    {
        try
        {
            _logger.LogInformation("Creating new socat profile with default name: {DefaultName}", defaultName);

            // Create ViewModel with default values - including ISocatService dependency
            var profileViewModel = new SocatProfileViewModel(
                _socatProfileService,
                _socatService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SocatProfileViewModel>.Instance);

            // Set the default name
            profileViewModel.ProfileName = defaultName;

            // CRITICAL: For new profiles, mark as having changes so SaveCommand is enabled
            profileViewModel.HasChanges = true;

            // Show the edit dialog for the new profile
            var result = await ShowSocatProfileEditAsync("Create Socat Profile", profileViewModel);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Socat profile created successfully");
            }
            else
            {
                _logger.LogInformation("Socat profile creation cancelled or failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating socat profile");
            return ProfileEditResult.Cancelled();
        }
    }

    /// <inheritdoc />
    public async Task<ProfileEditResult> CreatePowerSupplyProfileAsync(string defaultName = "PowerSupplyDefault")
    {
        try
        {
            _logger.LogInformation("Creating new power supply profile with default name: {DefaultName}", defaultName);
            System.Diagnostics.Debug.WriteLine($"DEBUG: CreatePowerSupplyProfileAsync called with name: {defaultName}");

            // Create ViewModel with default values
            var profileViewModel = new PowerSupplyProfileViewModel(
                _powerSupplyProfileService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PowerSupplyProfileViewModel>.Instance);

            // Set the default name
            profileViewModel.ProfileName = defaultName;

            // CRITICAL: For new profiles, mark as having changes so SaveCommand is enabled
            profileViewModel.HasChanges = true;

            // Show the edit dialog for the new profile
            var result = await ShowPowerSupplyProfileEditAsync("Create Power Supply Profile", profileViewModel);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Power supply profile created successfully");
            }
            else
            {
                _logger.LogInformation("Power supply profile creation cancelled or failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating power supply profile");
            return ProfileEditResult.Cancelled();
        }
    }

    /// <inheritdoc />
    public async Task<ProfileEditResult> EditSerialProfileAsync(int profileId)
    {
        try
        {
            _logger.LogInformation("Editing serial port profile with ID: {ProfileId}", profileId);

            // Load the existing profile
            var profile = await _serialPortProfileService.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("Serial port profile with ID {ProfileId} not found", profileId);
                return ProfileEditResult.Failed("Profile not found");
            }

            // Create ViewModel with existing profile data
            var profileViewModel = new SerialPortProfileViewModel(
                _serialPortProfileService,
                _serialPortService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SerialPortProfileViewModel>.Instance);

            // Load the existing profile data into the ViewModel
            profileViewModel.LoadProfile(profile);

            // Show the edit dialog
            var result = await ShowSerialProfileEditAsync("Edit Serial Port Profile", profileViewModel);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Serial port profile edited successfully");
            }
            else
            {
                _logger.LogInformation("Serial port profile edit cancelled or failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing serial port profile with ID: {ProfileId}", profileId);
            return ProfileEditResult.Failed($"Error editing profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileEditResult> EditSocatProfileAsync(int profileId)
    {
        try
        {
            _logger.LogInformation("Editing socat profile with ID: {ProfileId}", profileId);

            // Load the existing profile
            var profile = await _socatProfileService.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("Socat profile with ID {ProfileId} not found", profileId);
                return ProfileEditResult.Failed("Profile not found");
            }

            // Create ViewModel with existing profile data
            var profileViewModel = new SocatProfileViewModel(
                _socatProfileService,
                _socatService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SocatProfileViewModel>.Instance);

            // Load the existing profile data into the ViewModel
            profileViewModel.LoadProfile(profile);

            // Show the edit dialog
            var result = await ShowSocatProfileEditAsync("Edit Socat Profile", profileViewModel);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Socat profile edited successfully");
            }
            else
            {
                _logger.LogInformation("Socat profile edit cancelled or failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing socat profile with ID: {ProfileId}", profileId);
            return ProfileEditResult.Failed($"Error editing profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileEditResult> EditPowerSupplyProfileAsync(int profileId)
    {
        try
        {
            _logger.LogInformation("Editing power supply profile with ID: {ProfileId}", profileId);

            // Load the existing profile
            var profile = await _powerSupplyProfileService.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("Power supply profile with ID {ProfileId} not found", profileId);
                return ProfileEditResult.Failed("Profile not found");
            }

            // Create ViewModel with existing profile data
            var profileViewModel = new PowerSupplyProfileViewModel(
                _powerSupplyProfileService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PowerSupplyProfileViewModel>.Instance);

            // Load the existing profile data into the ViewModel
            profileViewModel.LoadProfile(profile);

            // Show the edit dialog
            var result = await ShowPowerSupplyProfileEditAsync("Edit Power Supply Profile", profileViewModel);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Power supply profile edited successfully");
            }
            else
            {
                _logger.LogInformation("Power supply profile edit cancelled or failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing power supply profile with ID: {ProfileId}", profileId);
            return ProfileEditResult.Failed($"Error editing profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDuplicateResult> DuplicateSerialProfileAsync(int sourceProfileId)
    {
        try
        {
            _logger.LogInformation("Duplicating serial port profile with ID: {ProfileId}", sourceProfileId);

            // Load the source profile
            var sourceProfile = await _serialPortProfileService.GetByIdAsync(sourceProfileId);
            if (sourceProfile == null)
            {
                _logger.LogWarning("Source serial port profile with ID {ProfileId} not found", sourceProfileId);
                return ProfileDuplicateResult.Failed("Source profile not found");
            }

            // Show input dialog for new name using the correct method
            var inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Serial Port Profile",
                "Enter a name for the duplicated profile:",
                $"{sourceProfile.Name}_Copy",
                "Profile name");

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                _logger.LogInformation("Serial port profile duplication cancelled");
                return ProfileDuplicateResult.Cancelled();
            }

            var newName = inputResult.Value.Trim();

            // Check if name is available
            var isAvailable = await _serialPortProfileService.IsNameUniqueAsync(newName);
            if (!isAvailable)
            {
                _logger.LogWarning("Serial port profile name already exists: {ProfileName}", newName);
                return ProfileDuplicateResult.Failed("Profile name already exists");
            }

            _logger.LogInformation("Serial port profile duplication name confirmed: {NewName}", newName);
            return ProfileDuplicateResult.Success(newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating serial port profile with ID: {ProfileId}", sourceProfileId);
            return ProfileDuplicateResult.Failed($"Error duplicating profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDuplicateResult> DuplicateSocatProfileAsync(int sourceProfileId)
    {
        try
        {
            _logger.LogInformation("Duplicating socat profile with ID: {ProfileId}", sourceProfileId);

            // Load the source profile
            var sourceProfile = await _socatProfileService.GetByIdAsync(sourceProfileId);
            if (sourceProfile == null)
            {
                _logger.LogWarning("Source socat profile with ID {ProfileId} not found", sourceProfileId);
                return ProfileDuplicateResult.Failed("Source profile not found");
            }

            // Show input dialog for new name
            var inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Socat Profile",
                "Enter a name for the duplicated profile:",
                $"{sourceProfile.Name}_Copy",
                "Profile name");

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                _logger.LogInformation("Socat profile duplication cancelled");
                return ProfileDuplicateResult.Cancelled();
            }

            var newName = inputResult.Value.Trim();

            // Check if name is available
            var isAvailable = await _socatProfileService.IsNameUniqueAsync(newName);
            if (!isAvailable)
            {
                _logger.LogWarning("Socat profile name already exists: {ProfileName}", newName);
                return ProfileDuplicateResult.Failed("Profile name already exists");
            }

            _logger.LogInformation("Socat profile duplication name confirmed: {NewName}", newName);
            return ProfileDuplicateResult.Success(newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating socat profile with ID: {ProfileId}", sourceProfileId);
            return ProfileDuplicateResult.Failed($"Error duplicating profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDuplicateResult> DuplicatePowerSupplyProfileAsync(int sourceProfileId)
    {
        try
        {
            _logger.LogInformation("Duplicating power supply profile with ID: {ProfileId}", sourceProfileId);

            // Load the source profile
            var sourceProfile = await _powerSupplyProfileService.GetByIdAsync(sourceProfileId);
            if (sourceProfile == null)
            {
                _logger.LogWarning("Source power supply profile with ID {ProfileId} not found", sourceProfileId);
                return ProfileDuplicateResult.Failed("Source profile not found");
            }

            // Show input dialog for new name
            var inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Power Supply Profile",
                "Enter a name for the duplicated profile:",
                $"{sourceProfile.Name}_Copy",
                "Profile name");

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                _logger.LogInformation("Power supply profile duplication cancelled");
                return ProfileDuplicateResult.Cancelled();
            }

            var newName = inputResult.Value.Trim();

            // Check if name is available
            var isAvailable = await _powerSupplyProfileService.IsNameUniqueAsync(newName);
            if (!isAvailable)
            {
                _logger.LogWarning("Power supply profile name already exists: {ProfileName}", newName);
                return ProfileDuplicateResult.Failed("Profile name already exists");
            }

            _logger.LogInformation("Power supply profile duplication name confirmed: {NewName}", newName);
            return ProfileDuplicateResult.Success(newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating power supply profile with ID: {ProfileId}", sourceProfileId);
            return ProfileDuplicateResult.Failed($"Error duplicating profile: {ex.Message}");
        }
    }
}
