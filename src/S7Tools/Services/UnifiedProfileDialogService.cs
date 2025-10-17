using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;

namespace S7Tools.Services;

/// <summary>
/// Unified service implementation for displaying profile editing dialogs across all profile types.
/// Delegates to existing type-specific dialog services while providing a consistent interface.
/// </summary>
/// <remarks>
/// This service implements the adapter pattern to integrate with the existing ProfileEditDialogService
/// infrastructure while providing the unified interface required by ProfileManagementViewModelBase.
///
/// Architecture principles:
/// - Single Responsibility: Coordinates profile dialog operations
/// - Open/Closed Principle: Extensible for new profile types without modification
/// - Dependency Inversion: Depends on abstractions, delegates to existing services
/// - Adapter Pattern: Bridges new unified interface with existing implementations
/// </remarks>
public class UnifiedProfileDialogService : IUnifiedProfileDialogService
{
    private readonly IProfileEditDialogService _profileEditDialogService;
    private readonly ISerialPortProfileService _serialPortProfileService;
    private readonly ISocatProfileService _socatProfileService;
    private readonly IPowerSupplyProfileService _powerSupplyProfileService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<UnifiedProfileDialogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedProfileDialogService"/> class.
    /// </summary>
    /// <param name="profileEditDialogService">The existing profile edit dialog service.</param>
    /// <param name="serialPortProfileService">The serial port profile service.</param>
    /// <param name="socatProfileService">The socat profile service.</param>
    /// <param name="powerSupplyProfileService">The power supply profile service.</param>
    /// <param name="dialogService">The general dialog service.</param>
    /// <param name="logger">The logger.</param>
    public UnifiedProfileDialogService(
        IProfileEditDialogService profileEditDialogService,
        ISerialPortProfileService serialPortProfileService,
        ISocatProfileService socatProfileService,
        IPowerSupplyProfileService powerSupplyProfileService,
        IDialogService dialogService,
        ILogger<UnifiedProfileDialogService> logger)
    {
        _profileEditDialogService = profileEditDialogService ?? throw new ArgumentNullException(nameof(profileEditDialogService));
        _serialPortProfileService = serialPortProfileService ?? throw new ArgumentNullException(nameof(serialPortProfileService));
        _socatProfileService = socatProfileService ?? throw new ArgumentNullException(nameof(socatProfileService));
        _powerSupplyProfileService = powerSupplyProfileService ?? throw new ArgumentNullException(nameof(powerSupplyProfileService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Serial Port Profile Operations

    /// <inheritdoc />
    public async Task<ProfileDialogResult<SerialPortProfile>> ShowSerialCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing create dialog for serial port profile with default name: {DefaultName}", request.DefaultName);

            var result = await _profileEditDialogService.CreateSerialProfileAsync(request.DefaultName).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SerialPortProfileViewModel viewModel)
            {
                var profile = viewModel.CreateProfile();
                _logger.LogInformation("Serial port profile created successfully: {ProfileName}", profile.Name);
                return ProfileDialogResult<SerialPortProfile>.Success(profile);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Serial port profile creation cancelled or failed");
                return ProfileDialogResult<SerialPortProfile>.Cancelled();
            }

            _logger.LogWarning("Serial port profile creation failed: unexpected result type");
            return ProfileDialogResult<SerialPortProfile>.Failure("Unexpected result type from dialog");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing serial port profile create dialog");
            return ProfileDialogResult<SerialPortProfile>.Failure($"Error creating profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDialogResult<SerialPortProfile>> ShowSerialEditDialogAsync(ProfileEditRequest request)
    {
        try
        {
            _logger.LogDebug("Showing edit dialog for serial port profile ID: {ProfileId}", request.ProfileId);

            var result = await _profileEditDialogService.EditSerialProfileAsync(request.ProfileId).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SerialPortProfileViewModel viewModel)
            {
                var profile = viewModel.CreateProfile();
                _logger.LogInformation("Serial port profile edited successfully: {ProfileName}", profile.Name);
                return ProfileDialogResult<SerialPortProfile>.Success(profile);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Serial port profile edit cancelled or failed");
                return ProfileDialogResult<SerialPortProfile>.Cancelled();
            }

            _logger.LogWarning("Serial port profile edit failed: unexpected result type");
            return ProfileDialogResult<SerialPortProfile>.Failure("Unexpected result type from dialog");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing serial port profile edit dialog for ID: {ProfileId}", request.ProfileId);
            return ProfileDialogResult<SerialPortProfile>.Failure($"Error editing profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDialogResult<string>> ShowSerialDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing duplicate dialog for serial port profile ID: {SourceProfileId}", request.SourceProfileId);

            var result = await _profileEditDialogService.DuplicateSerialProfileAsync(request.SourceProfileId).ConfigureAwait(false);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.NewName))
            {
                _logger.LogInformation("Serial port profile duplicate name entered: {NewName}", result.NewName);
                return ProfileDialogResult<string>.Success(result.NewName);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Serial port profile duplicate cancelled or failed");
                return ProfileDialogResult<string>.Cancelled();
            }

            _logger.LogWarning("Serial port profile duplicate failed: {ErrorMessage}", result.ErrorMessage);
            return ProfileDialogResult<string>.Failure(result.ErrorMessage ?? "Profile duplication failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing serial port profile duplicate dialog for ID: {SourceProfileId}", request.SourceProfileId);
            return ProfileDialogResult<string>.Failure($"Error duplicating profile: {ex.Message}");
        }
    }

    #endregion

    #region Socat Profile Operations

    /// <inheritdoc />
    public async Task<ProfileDialogResult<SocatProfile>> ShowSocatCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing create dialog for socat profile with default name: {DefaultName}", request.DefaultName);

            var result = await _profileEditDialogService.CreateSocatProfileAsync(request.DefaultName).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SocatProfileViewModel viewModel)
            {
                var profile = viewModel.CreateProfile();
                _logger.LogInformation("Socat profile created successfully: {ProfileName}", profile.Name);
                return ProfileDialogResult<SocatProfile>.Success(profile);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Socat profile creation cancelled or failed");
                return ProfileDialogResult<SocatProfile>.Cancelled();
            }

            _logger.LogWarning("Socat profile creation failed: unexpected result type");
            return ProfileDialogResult<SocatProfile>.Failure("Unexpected result type from dialog");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing socat profile create dialog");
            return ProfileDialogResult<SocatProfile>.Failure($"Error creating profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDialogResult<SocatProfile>> ShowSocatEditDialogAsync(ProfileEditRequest request)
    {
        try
        {
            _logger.LogDebug("Showing edit dialog for socat profile ID: {ProfileId}", request.ProfileId);

            var result = await _profileEditDialogService.EditSocatProfileAsync(request.ProfileId).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is SocatProfileViewModel viewModel)
            {
                var profile = viewModel.CreateProfile();
                _logger.LogInformation("Socat profile edited successfully: {ProfileName}", profile.Name);
                return ProfileDialogResult<SocatProfile>.Success(profile);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Socat profile edit cancelled or failed");
                return ProfileDialogResult<SocatProfile>.Cancelled();
            }

            _logger.LogWarning("Socat profile edit failed: unexpected result type");
            return ProfileDialogResult<SocatProfile>.Failure("Unexpected result type from dialog");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing socat profile edit dialog for ID: {ProfileId}", request.ProfileId);
            return ProfileDialogResult<SocatProfile>.Failure($"Error editing profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDialogResult<string>> ShowSocatDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing duplicate dialog for socat profile ID: {SourceProfileId}", request.SourceProfileId);

            var result = await _profileEditDialogService.DuplicateSocatProfileAsync(request.SourceProfileId).ConfigureAwait(false);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.NewName))
            {
                _logger.LogInformation("Socat profile duplicate name entered: {NewName}", result.NewName);
                return ProfileDialogResult<string>.Success(result.NewName);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Socat profile duplicate cancelled or failed");
                return ProfileDialogResult<string>.Cancelled();
            }

            _logger.LogWarning("Socat profile duplicate failed: {ErrorMessage}", result.ErrorMessage);
            return ProfileDialogResult<string>.Failure(result.ErrorMessage ?? "Profile duplication failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing socat profile duplicate dialog for ID: {SourceProfileId}", request.SourceProfileId);
            return ProfileDialogResult<string>.Failure($"Error duplicating profile: {ex.Message}");
        }
    }

    #endregion

    #region Power Supply Profile Operations

    /// <inheritdoc />
    public async Task<ProfileDialogResult<PowerSupplyProfile>> ShowPowerSupplyCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing create dialog for power supply profile with default name: {DefaultName}", request.DefaultName);
            System.Diagnostics.Debug.WriteLine($"DEBUG: ShowPowerSupplyCreateDialogAsync called with name: {request.DefaultName}");

            var result = await _profileEditDialogService.CreatePowerSupplyProfileAsync(request.DefaultName).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is PowerSupplyProfileViewModel viewModel)
            {
                var profile = viewModel.CreateProfile();
                _logger.LogInformation("Power supply profile created successfully: {ProfileName}", profile.Name);
                return ProfileDialogResult<PowerSupplyProfile>.Success(profile);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Power supply profile creation cancelled or failed");
                return ProfileDialogResult<PowerSupplyProfile>.Cancelled();
            }

            _logger.LogWarning("Power supply profile creation failed: unexpected result type");
            return ProfileDialogResult<PowerSupplyProfile>.Failure("Unexpected result type from dialog");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing power supply profile create dialog");
            return ProfileDialogResult<PowerSupplyProfile>.Failure($"Error creating profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDialogResult<PowerSupplyProfile>> ShowPowerSupplyEditDialogAsync(ProfileEditRequest request)
    {
        try
        {
            _logger.LogDebug("Showing edit dialog for power supply profile ID: {ProfileId}", request.ProfileId);

            var result = await _profileEditDialogService.EditPowerSupplyProfileAsync(request.ProfileId).ConfigureAwait(false);

            if (result.IsSuccess && result.ProfileViewModel is PowerSupplyProfileViewModel viewModel)
            {
                var profile = viewModel.CreateProfile();
                _logger.LogInformation("Power supply profile edited successfully: {ProfileName}", profile.Name);
                return ProfileDialogResult<PowerSupplyProfile>.Success(profile);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Power supply profile edit cancelled or failed");
                return ProfileDialogResult<PowerSupplyProfile>.Cancelled();
            }

            _logger.LogWarning("Power supply profile edit failed: unexpected result type");
            return ProfileDialogResult<PowerSupplyProfile>.Failure("Unexpected result type from dialog");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing power supply profile edit dialog for ID: {ProfileId}", request.ProfileId);
            return ProfileDialogResult<PowerSupplyProfile>.Failure($"Error editing profile: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDialogResult<string>> ShowPowerSupplyDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing duplicate dialog for power supply profile ID: {SourceProfileId}", request.SourceProfileId);

            var result = await _profileEditDialogService.DuplicatePowerSupplyProfileAsync(request.SourceProfileId).ConfigureAwait(false);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.NewName))
            {
                _logger.LogInformation("Power supply profile duplicate name entered: {NewName}", result.NewName);
                return ProfileDialogResult<string>.Success(result.NewName);
            }

            if (!result.IsSuccess)
            {
                _logger.LogDebug("Power supply profile duplicate cancelled or failed");
                return ProfileDialogResult<string>.Cancelled();
            }

            _logger.LogWarning("Power supply profile duplicate failed: {ErrorMessage}", result.ErrorMessage);
            return ProfileDialogResult<string>.Failure(result.ErrorMessage ?? "Profile duplication failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing power supply profile duplicate dialog for ID: {SourceProfileId}", request.SourceProfileId);
            return ProfileDialogResult<string>.Failure($"Error duplicating profile: {ex.Message}");
        }
    }

    #endregion

    #region Job Profile Operations

    /// <inheritdoc />
    public Task<ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>> ShowJobCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing job create dialog with title: {Title}, default name: {DefaultName}",
                request.Title, request.DefaultName);

            // TODO: Implement actual job create dialog
            // For now, return a successful result with a default job profile
            var profile = new S7Tools.Core.Models.Jobs.JobProfile
            {
                Id = 0, // Will be assigned by the manager
                Name = request.DefaultName,
                Description = request.DefaultDescription,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Job create dialog completed successfully for profile: {ProfileName}", profile.Name);
            return Task.FromResult(ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>.Success(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing job create dialog");
            return Task.FromResult(ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>.Failure($"Error creating profile: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>> ShowJobEditDialogAsync(ProfileEditRequest request)
    {
        try
        {
            _logger.LogDebug("Showing job edit dialog for profile ID: {ProfileId}", request.ProfileId);

            // TODO: Implement actual job edit dialog
            // For now, return cancelled as we don't have the actual dialog implementation
            _logger.LogDebug("Job edit dialog cancelled - not yet implemented");
            return Task.FromResult(ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>.Cancelled());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing job edit dialog for ID: {ProfileId}", request.ProfileId);
            return Task.FromResult(ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>.Failure($"Error editing profile: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<ProfileDialogResult<string>> ShowJobDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing job duplicate dialog for source profile ID: {SourceProfileId}, suggested name: {SuggestedName}",
                request.SourceProfileId, request.SuggestedName);

            // TODO: Implement actual job duplicate dialog
            // For now, return cancelled as we don't have the actual dialog implementation
            _logger.LogDebug("Job duplicate dialog cancelled - not yet implemented");
            return Task.FromResult(ProfileDialogResult<string>.Cancelled());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing job duplicate dialog for ID: {SourceProfileId}", request.SourceProfileId);
            return Task.FromResult(ProfileDialogResult<string>.Failure($"Error duplicating profile: {ex.Message}"));
        }
    }

    #endregion

    #region Common Dialog Operations

    /// <inheritdoc />
    public async Task<bool> ShowDeleteConfirmationDialogAsync(string profileName, string profileType)
    {
        try
        {
            _logger.LogDebug("Showing delete confirmation dialog for {ProfileType} profile: {ProfileName}", profileType, profileName);

            var title = $"Delete {profileType} Profile";
            var message = $"Are you sure you want to delete the profile '{profileName}'?\n\nThis action cannot be undone.";

            var result = await _dialogService.ShowConfirmationAsync(title, message).ConfigureAwait(false);

            _logger.LogDebug("Delete confirmation result for {ProfileName}: {Result}", profileName, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing delete confirmation dialog for profile: {ProfileName}", profileName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDialogResult<string>> ShowNameInputDialogAsync(
        string title,
        string prompt,
        string defaultValue = "",
        Func<string, Task<ProfileValidationResult>>? validator = null)
    {
        try
        {
            _logger.LogDebug("Showing name input dialog with title: {Title}", title);

            var result = await _dialogService.ShowInputAsync(title, prompt, defaultValue).ConfigureAwait(false);

            if (!result.IsCancelled && !string.IsNullOrEmpty(result.Value))
            {
                // Apply validation if provided
                if (validator != null)
                {
                    var validationResult = await validator(result.Value).ConfigureAwait(false);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogDebug("Name input validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                        return ProfileDialogResult<string>.Failure(validationResult.ErrorMessage);
                    }
                }

                _logger.LogDebug("Name input dialog completed successfully: {Value}", result.Value);
                return ProfileDialogResult<string>.Success(result.Value);
            }

            if (result.IsCancelled)
            {
                _logger.LogDebug("Name input dialog cancelled by user");
                return ProfileDialogResult<string>.Cancelled();
            }

            _logger.LogDebug("Name input dialog failed: empty value");
            return ProfileDialogResult<string>.Failure("Name input failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while showing name input dialog");
            return ProfileDialogResult<string>.Failure($"Error showing input dialog: {ex.Message}");
        }
    }

    #endregion
}
