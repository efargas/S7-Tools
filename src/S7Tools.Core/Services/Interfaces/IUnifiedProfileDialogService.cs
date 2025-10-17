using System.Threading.Tasks;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Request model for profile creation dialog operations.
/// </summary>
/// <remarks>
/// Encapsulates the data needed to initialize a create profile dialog with default values.
/// Supports the new requirement for pre-populated default values in create operations.
/// </remarks>
public class ProfileCreateRequest
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = "Create Profile";

    /// <summary>
    /// Gets or sets the default name for the new profile.
    /// </summary>
    /// <remarks>
    /// Should be set to standardized default names: "SerialDefault", "SocatDefault", "PowerSupplyDefault".
    /// </remarks>
    public string DefaultName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default description for the new profile.
    /// </summary>
    public string DefaultDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to auto-generate a unique name if the default name is taken.
    /// </summary>
    public bool AutoGenerateUniqueName { get; set; } = true;
}

/// <summary>
/// Request model for profile edit dialog operations.
/// </summary>
public class ProfileEditRequest
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = "Edit Profile";

    /// <summary>
    /// Gets or sets the ID of the profile to edit.
    /// </summary>
    public int ProfileId { get; set; }
}

/// <summary>
/// Request model for profile duplicate operations.
/// </summary>
/// <remarks>
/// Supports the simplified duplicate workflow: input dialog for name → direct list addition.
/// No edit dialog step is needed as per the updated requirements.
/// </remarks>
public class ProfileDuplicateRequest
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = "Duplicate Profile";

    /// <summary>
    /// Gets or sets the ID of the source profile to duplicate.
    /// </summary>
    public int SourceProfileId { get; set; }

    /// <summary>
    /// Gets or sets the suggested name for the duplicate profile.
    /// </summary>
    /// <remarks>
    /// Typically the source profile name with " (Copy)" suffix or similar.
    /// </remarks>
    public string SuggestedName { get; set; } = string.Empty;
}

/// <summary>
/// Result model for profile dialog operations.
/// </summary>
/// <typeparam name="T">The type of profile or result data.</typeparam>
public class ProfileDialogResult<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the result data (profile, name, etc.).
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful result with the specified data.
    /// </summary>
    public static ProfileDialogResult<T> Success(T result) => new() { IsSuccess = true, Result = result };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static ProfileDialogResult<T> Cancelled() => new() { IsSuccess = false };

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static ProfileDialogResult<T> Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Enhanced interface for unified profile dialog operations across all profile types.
/// Supports the updated dialog behavior requirements with pre-populated defaults and simplified duplicate workflow.
/// </summary>
/// <remarks>
/// This interface extends the existing ProfileEditDialogService to support:
/// - Create operations with pre-populated default values (SerialDefault/SocatDefault/PowerSupplyDefault)
/// - Simplified duplicate operations (input dialog only, no edit dialog step)
/// - Consistent dialog patterns across all profile types
/// - Unified error handling and validation feedback
///
/// Design principles:
/// - Interface Segregation: Focused on dialog operations only
/// - Single Responsibility: Manages dialog interactions for profile operations
/// - Dependency Inversion: Depends on profile abstractions
/// </remarks>
public interface IUnifiedProfileDialogService
{
    #region Serial Port Profile Operations

    /// <summary>
    /// Shows a create dialog for a new serial port profile with pre-populated default values.
    /// </summary>
    /// <param name="request">Create request with default values and configuration.</param>
    /// <returns>Dialog result with the created profile or cancellation status.</returns>
    /// <remarks>
    /// Dialog behavior:
    /// - Pre-populates form with default values (SerialDefault name, standard configuration)
    /// - Validates name uniqueness in real-time
    /// - Auto-assigns ID when saved
    /// - Returns created profile ready for service persistence
    /// </remarks>
    Task<ProfileDialogResult<Core.Models.SerialPortProfile>> ShowSerialCreateDialogAsync(ProfileCreateRequest request);

    /// <summary>
    /// Shows an edit dialog for an existing serial port profile.
    /// </summary>
    /// <param name="request">Edit request with profile ID and configuration.</param>
    /// <returns>Dialog result with the updated profile or cancellation status.</returns>
    /// <remarks>
    /// Dialog behavior:
    /// - Pre-populates form with existing profile data
    /// - Preserves profile ID
    /// - Validates name uniqueness (excluding current profile)
    /// - Updates ModifiedAt timestamp when saved
    /// </remarks>
    Task<ProfileDialogResult<Core.Models.SerialPortProfile>> ShowSerialEditDialogAsync(ProfileEditRequest request);

    /// <summary>
    /// Shows an input dialog for duplicating a serial port profile (simplified workflow).
    /// </summary>
    /// <param name="request">Duplicate request with source profile ID and suggested name.</param>
    /// <returns>Dialog result with the new profile name or cancellation status.</returns>
    /// <remarks>
    /// Simplified duplicate workflow:
    /// - Shows input dialog for new profile name only
    /// - Validates name uniqueness
    /// - Returns new name for direct profile creation by service
    /// - No edit dialog step as per updated requirements
    /// </remarks>
    Task<ProfileDialogResult<string>> ShowSerialDuplicateDialogAsync(ProfileDuplicateRequest request);

    #endregion

    #region Socat Profile Operations

    /// <summary>
    /// Shows a create dialog for a new socat profile with pre-populated default values.
    /// </summary>
    /// <param name="request">Create request with default values and configuration.</param>
    /// <returns>Dialog result with the created profile or cancellation status.</returns>
    Task<ProfileDialogResult<Core.Models.SocatProfile>> ShowSocatCreateDialogAsync(ProfileCreateRequest request);

    /// <summary>
    /// Shows an edit dialog for an existing socat profile.
    /// </summary>
    /// <param name="request">Edit request with profile ID and configuration.</param>
    /// <returns>Dialog result with the updated profile or cancellation status.</returns>
    Task<ProfileDialogResult<Core.Models.SocatProfile>> ShowSocatEditDialogAsync(ProfileEditRequest request);

    /// <summary>
    /// Shows an input dialog for duplicating a socat profile (simplified workflow).
    /// </summary>
    /// <param name="request">Duplicate request with source profile ID and suggested name.</param>
    /// <returns>Dialog result with the new profile name or cancellation status.</returns>
    Task<ProfileDialogResult<string>> ShowSocatDuplicateDialogAsync(ProfileDuplicateRequest request);

    #endregion

    #region Power Supply Profile Operations

    /// <summary>
    /// Shows a create dialog for a new power supply profile with pre-populated default values.
    /// </summary>
    /// <param name="request">Create request with default values and configuration.</param>
    /// <returns>Dialog result with the created profile or cancellation status.</returns>
    Task<ProfileDialogResult<Core.Models.PowerSupplyProfile>> ShowPowerSupplyCreateDialogAsync(ProfileCreateRequest request);

    /// <summary>
    /// Shows an edit dialog for an existing power supply profile.
    /// </summary>
    /// <param name="request">Edit request with profile ID and configuration.</param>
    /// <returns>Dialog result with the updated profile or cancellation status.</returns>
    Task<ProfileDialogResult<Core.Models.PowerSupplyProfile>> ShowPowerSupplyEditDialogAsync(ProfileEditRequest request);

    /// <summary>
    /// Shows an input dialog for duplicating a power supply profile (simplified workflow).
    /// </summary>
    /// <param name="request">Duplicate request with source profile ID and suggested name.</param>
    /// <returns>Dialog result with the new profile name or cancellation status.</returns>
    Task<ProfileDialogResult<string>> ShowPowerSupplyDuplicateDialogAsync(ProfileDuplicateRequest request);

    #endregion

    #region Common Dialog Operations

    /// <summary>
    /// Shows a confirmation dialog for profile deletion.
    /// </summary>
    /// <param name="profileName">The name of the profile to delete.</param>
    /// <param name="profileType">The type of profile (for context).</param>
    /// <returns>True if deletion is confirmed, false if cancelled.</returns>
    /// <remarks>
    /// Provides consistent delete confirmation across all profile types.
    /// Shows appropriate warnings for read-only or default profiles.
    /// </remarks>
    Task<bool> ShowDeleteConfirmationDialogAsync(string profileName, string profileType);

    /// <summary>
    /// Shows an input dialog for name entry with validation.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The input prompt text.</param>
    /// <param name="defaultValue">The default input value.</param>
    /// <param name="validator">Optional async validator function.</param>
    /// <returns>Dialog result with the entered name or cancellation status.</returns>
    /// <remarks>
    /// Generic input dialog used for various naming operations.
    /// Supports real-time validation feedback.
    /// Used by duplicate operations and other naming scenarios.
    /// </remarks>
    Task<ProfileDialogResult<string>> ShowNameInputDialogAsync(
        string title,
        string prompt,
        string defaultValue = "",
        System.Func<string, Task<ProfileValidationResult>>? validator = null);

    #endregion

    #region Job Profile Operations

    /// <summary>
    /// Shows a create dialog for a new job profile with pre-populated default values.
    /// </summary>
    /// <param name="request">Create request with default values and configuration.</param>
    /// <returns>Dialog result with the created profile or cancellation status.</returns>
    Task<ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>> ShowJobCreateDialogAsync(ProfileCreateRequest request);

    /// <summary>
    /// Shows an edit dialog for an existing job profile.
    /// </summary>
    /// <param name="request">Edit request with profile ID and configuration.</param>
    /// <returns>Dialog result with the updated profile or cancellation status.</returns>
    Task<ProfileDialogResult<S7Tools.Core.Models.Jobs.JobProfile>> ShowJobEditDialogAsync(ProfileEditRequest request);

    /// <summary>
    /// Shows a duplicate dialog for creating a copy of an existing job profile.
    /// </summary>
    /// <param name="request">Duplicate request with source profile and suggested name.</param>
    /// <returns>Dialog result with the new profile name or cancellation status.</returns>
    Task<ProfileDialogResult<string>> ShowJobDuplicateDialogAsync(ProfileDuplicateRequest request);

    #endregion
}
