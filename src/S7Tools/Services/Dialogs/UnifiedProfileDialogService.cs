using System.Reactive.Linq;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Dialogs.Models;
using S7Tools.ViewModels.Profiles;
using CoreProfileEditRequest = S7Tools.Core.Interfaces.Services.ProfileEditRequest;

namespace S7Tools.Services;

/// <summary>
/// Unified service implementation for displaying profile editing dialogs across all profile types.
/// </summary>
public class UnifiedProfileDialogService : IUnifiedProfileDialogService
{
    private static bool _handlerRegistered;
    private static readonly object _lockObject = new();

    private readonly ISerialPortProfileService _serialPortProfileService;
    private readonly ISocatProfileService _socatProfileService;
    private readonly IPowerSupplyProfileService _powerSupplyProfileService;
    private readonly ISerialPortService _serialPortService;
    private readonly ISocatService _socatService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<UnifiedProfileDialogService> _logger;

    private static readonly Interaction<S7Tools.ViewModels.Dialogs.Models.ProfileEditRequest, ProfileEditResult> _staticInteraction = new();

    /// <summary>
    /// Gets or sets the ShowProfileEditDialog.
    /// </summary>
    public Interaction<S7Tools.ViewModels.Dialogs.Models.ProfileEditRequest, ProfileEditResult> ShowProfileEditDialog => _staticInteraction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedProfileDialogService"/> class.
    /// </summary>
    public UnifiedProfileDialogService(
        ISerialPortProfileService serialPortProfileService,
        ISocatProfileService socatProfileService,
        IPowerSupplyProfileService powerSupplyProfileService,
        ISerialPortService serialPortService,
        ISocatService socatService,
        IClipboardService clipboardService,
        IDialogService dialogService,
        ILogger<UnifiedProfileDialogService> logger)
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
                try
                {
                    var dialog = new Views.Dialogs.ProfileEditDialog();
                    dialog.SetupDialog(interaction.Input);

                    Window? mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : null;

                    if (mainWindow != null)
                    {
                        await dialog.ShowDialog(mainWindow);
                        interaction.SetOutput(dialog.Result);
                    }
                    else
                    {
                        interaction.SetOutput(ProfileEditResult.Cancelled());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in UnifiedProfileDialogService interaction handler");
                    interaction.SetOutput(ProfileEditResult.Cancelled());
                }
            });

            _handlerRegistered = true;
        }
    }

    private async Task<ProfileEditResult> ShowEditDialogAsync(string title, ViewModelBase profileViewModel, ProfileType profileType)
    {
        var request = new global::S7Tools.ViewModels.Dialogs.Models.ProfileEditRequest(title, profileViewModel, profileType);
        return await ShowProfileEditDialog.Handle(request).FirstAsync();
    }

    #region Serial Port Profile Operations

    /// <summary>
    /// Executes the ShowSerialCreateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<SerialPortProfile>> ShowSerialCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing create dialog for serial port profile with default name: {DefaultName}", request.DefaultName);

            var profileViewModel = new SerialPortProfileViewModel(
                _serialPortProfileService,
                _serialPortService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SerialPortProfileViewModel>.Instance)
            {
                ProfileName = request.DefaultName,
                HasChanges = true
            };

            ProfileEditResult result = await ShowEditDialogAsync("Create Serial Port Profile", profileViewModel, ProfileType.Serial).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SerialPortProfileViewModel viewModel)
            {
                SerialPortProfile profile = viewModel.CreateProfile();
                return ProfileDialogResult<SerialPortProfile>.Success(profile);
            }

            return ProfileDialogResult<SerialPortProfile>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return ProfileDialogResult<SerialPortProfile>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Executes the ShowSerialEditDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<SerialPortProfile>> ShowSerialEditDialogAsync(CoreProfileEditRequest request)
    {
        try
        {
            SerialPortProfile? profile = await _serialPortProfileService.GetByIdAsync(request.ProfileId);
            if (profile == null)
            {
                return ProfileDialogResult<SerialPortProfile>.Failure("Profile not found");
            }

            var profileViewModel = new SerialPortProfileViewModel(
                _serialPortProfileService,
                _serialPortService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SerialPortProfileViewModel>.Instance);

            profileViewModel.LoadProfile(profile);

            ProfileEditResult result = await ShowEditDialogAsync("Edit Serial Port Profile", profileViewModel, ProfileType.Serial).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SerialPortProfileViewModel viewModel)
            {
                SerialPortProfile savedProfile = viewModel.CreateProfile();
                return ProfileDialogResult<SerialPortProfile>.Success(savedProfile);
            }

            return ProfileDialogResult<SerialPortProfile>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing profile");
            return ProfileDialogResult<SerialPortProfile>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Executes the ShowSerialDuplicateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<string>> ShowSerialDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            SerialPortProfile? sourceProfile = await _serialPortProfileService.GetByIdAsync(request.SourceProfileId);
            if (sourceProfile == null)
            {
                return ProfileDialogResult<string>.Failure("Source profile not found");
            }

            InputResult inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Serial Port Profile",
                "Enter a name for the duplicated profile:",
                $"{sourceProfile.Name}_Copy",
                "Profile name");

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                return ProfileDialogResult<string>.Cancelled();
            }

            string newName = inputResult.Value.Trim();
            if (!await _serialPortProfileService.IsNameUniqueAsync(newName))
            {
                return ProfileDialogResult<string>.Failure("Profile name already exists");
            }

            return ProfileDialogResult<string>.Success(newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile");
            return ProfileDialogResult<string>.Failure(ex.Message);
        }
    }

    #endregion

    #region Socat Profile Operations

    /// <summary>
    /// Executes the ShowSocatCreateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<SocatProfile>> ShowSocatCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            var profileViewModel = new SocatProfileViewModel(
                _socatProfileService,
                _socatService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SocatProfileViewModel>.Instance)
            {
                ProfileName = request.DefaultName,
                HasChanges = true
            };

            ProfileEditResult result = await ShowEditDialogAsync("Create Socat Profile", profileViewModel, ProfileType.Socat).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SocatProfileViewModel viewModel)
            {
                SocatProfile profile = viewModel.CreateProfile();
                return ProfileDialogResult<SocatProfile>.Success(profile);
            }

            return ProfileDialogResult<SocatProfile>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return ProfileDialogResult<SocatProfile>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Executes the ShowSocatEditDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<SocatProfile>> ShowSocatEditDialogAsync(CoreProfileEditRequest request)
    {
        try
        {
            SocatProfile? profile = await _socatProfileService.GetByIdAsync(request.ProfileId);
            if (profile == null)
            {
                return ProfileDialogResult<SocatProfile>.Failure("Profile not found");
            }

            var profileViewModel = new SocatProfileViewModel(
                _socatProfileService,
                _socatService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SocatProfileViewModel>.Instance);

            profileViewModel.LoadProfile(profile);

            ProfileEditResult result = await ShowEditDialogAsync("Edit Socat Profile", profileViewModel, ProfileType.Socat).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SocatProfileViewModel viewModel)
            {
                SocatProfile savedProfile = viewModel.CreateProfile();
                return ProfileDialogResult<SocatProfile>.Success(savedProfile);
            }

            return ProfileDialogResult<SocatProfile>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing profile");
            return ProfileDialogResult<SocatProfile>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Executes the ShowSocatDuplicateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<string>> ShowSocatDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            SocatProfile? sourceProfile = await _socatProfileService.GetByIdAsync(request.SourceProfileId);
            if (sourceProfile == null)
            {
                return ProfileDialogResult<string>.Failure("Source profile not found");
            }

            InputResult inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Socat Profile",
                "Enter a name for the duplicated profile:",
                $"{sourceProfile.Name}_Copy",
                "Profile name");

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                return ProfileDialogResult<string>.Cancelled();
            }

            string newName = inputResult.Value.Trim();
            if (!await _socatProfileService.IsNameUniqueAsync(newName))
            {
                return ProfileDialogResult<string>.Failure("Profile name already exists");
            }

            return ProfileDialogResult<string>.Success(newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile");
            return ProfileDialogResult<string>.Failure(ex.Message);
        }
    }

    #endregion

    #region Power Supply Profile Operations

    /// <summary>
    /// Executes the ShowPowerSupplyCreateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<PowerSupplyProfile>> ShowPowerSupplyCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            var profileViewModel = new PowerSupplyProfileViewModel(
                _powerSupplyProfileService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PowerSupplyProfileViewModel>.Instance)
            {
                ProfileName = request.DefaultName,
                HasChanges = true
            };

            ProfileEditResult result = await ShowEditDialogAsync("Create Power Supply Profile", profileViewModel, ProfileType.PowerSupply).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is PowerSupplyProfileViewModel viewModel)
            {
                PowerSupplyProfile profile = viewModel.CreateProfile();
                return ProfileDialogResult<PowerSupplyProfile>.Success(profile);
            }

            return ProfileDialogResult<PowerSupplyProfile>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return ProfileDialogResult<PowerSupplyProfile>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Executes the ShowPowerSupplyEditDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<PowerSupplyProfile>> ShowPowerSupplyEditDialogAsync(CoreProfileEditRequest request)
    {
        try
        {
            PowerSupplyProfile? profile = await _powerSupplyProfileService.GetByIdAsync(request.ProfileId);
            if (profile == null)
            {
                return ProfileDialogResult<PowerSupplyProfile>.Failure("Profile not found");
            }

            var profileViewModel = new PowerSupplyProfileViewModel(
                _powerSupplyProfileService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PowerSupplyProfileViewModel>.Instance);

            profileViewModel.LoadProfile(profile);

            ProfileEditResult result = await ShowEditDialogAsync("Edit Power Supply Profile", profileViewModel, ProfileType.PowerSupply).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is PowerSupplyProfileViewModel viewModel)
            {
                PowerSupplyProfile savedProfile = viewModel.CreateProfile();
                return ProfileDialogResult<PowerSupplyProfile>.Success(savedProfile);
            }

            return ProfileDialogResult<PowerSupplyProfile>.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing profile");
            return ProfileDialogResult<PowerSupplyProfile>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Executes the ShowPowerSupplyDuplicateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<string>> ShowPowerSupplyDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            PowerSupplyProfile? sourceProfile = await _powerSupplyProfileService.GetByIdAsync(request.SourceProfileId);
            if (sourceProfile == null)
            {
                return ProfileDialogResult<string>.Failure("Source profile not found");
            }

            InputResult inputResult = await _dialogService.ShowInputAsync(
                "Duplicate Power Supply Profile",
                "Enter a name for the duplicated profile:",
                $"{sourceProfile.Name}_Copy",
                "Profile name");

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                return ProfileDialogResult<string>.Cancelled();
            }

            string newName = inputResult.Value.Trim();
            if (!await _powerSupplyProfileService.IsNameUniqueAsync(newName))
            {
                return ProfileDialogResult<string>.Failure("Profile name already exists");
            }

            return ProfileDialogResult<string>.Success(newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile");
            return ProfileDialogResult<string>.Failure(ex.Message);
        }
    }

    #endregion

    #region Job Profile Operations

    /// <summary>
    /// Executes the ShowJobCreateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>> ShowJobCreateDialogAsync(ProfileCreateRequest request)
    {
        await Task.CompletedTask;
        return ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>.Cancelled();
    }

    /// <summary>
    /// Executes the ShowJobEditDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>> ShowJobEditDialogAsync(CoreProfileEditRequest request)
    {
        await Task.CompletedTask;
        return ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>.Cancelled();
    }

    /// <summary>
    /// Executes the ShowJobDuplicateDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<string>> ShowJobDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            ProfileDialogResult<string> result = await ShowNameInputDialogAsync(
                "Duplicate Job",
                "Enter a name for the duplicated job:",
                request.SuggestedName).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile");
            return ProfileDialogResult<string>.Failure(ex.Message);
        }
    }

    #endregion

    #region Common Dialog Operations

    /// <summary>
    /// Executes the ShowDeleteConfirmationDialogAsync operation.
    /// </summary>
    public async Task<bool> ShowDeleteConfirmationDialogAsync(string profileName, string profileType)
    {
        try
        {
            string title = $"Delete {profileType} Profile";
            string message = $"Are you sure you want to delete the profile '{profileName}'?\n\nThis action cannot be undone.";

            return await _dialogService.ShowConfirmationAsync(title, message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing delete confirmation");
            return false;
        }
    }

    /// <summary>
    /// Executes the ShowNameInputDialogAsync operation.
    /// </summary>
    public async Task<ProfileDialogResult<string>> ShowNameInputDialogAsync(
        string title,
        string prompt,
        string defaultValue = "",
        Func<string, Task<ProfileValidationResult>>? validator = null)
    {
        try
        {
            global::S7Tools.ViewModels.Dialogs.Models.InputResult result = await _dialogService.ShowInputAsync(title, prompt, defaultValue).ConfigureAwait(false);

            if (!result.IsCancelled && !string.IsNullOrEmpty(result.Value))
            {
                if (validator != null)
                {
                    ProfileValidationResult validationResult = await validator(result.Value).ConfigureAwait(false);
                    if (!validationResult.IsValid)
                    {
                        return ProfileDialogResult<string>.Failure(validationResult.ErrorMessage);
                    }
                }

                return ProfileDialogResult<string>.Success(result.Value);
            }

            if (result.IsCancelled)
            {
                return ProfileDialogResult<string>.Cancelled();
            }

            return ProfileDialogResult<string>.Failure("Name input failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing input dialog");
            return ProfileDialogResult<string>.Failure(ex.Message);
        }
    }

    #endregion
}
