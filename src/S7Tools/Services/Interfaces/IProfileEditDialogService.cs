using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using S7Tools.Models;
using S7Tools.ViewModels;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for displaying profile editing dialogs.
/// </summary>
public interface IProfileEditDialogService
{
    /// <summary>
    /// Gets the interaction for showing profile editing dialogs.
    /// </summary>
    Interaction<ProfileEditRequest, ProfileEditResult> ShowProfileEditDialog { get; }

    // Legacy methods - maintained for backward compatibility

    /// <summary>
    /// Shows a profile editing dialog for a serial port profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SerialPortProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> ShowSerialProfileEditAsync(string title, SerialPortProfileViewModel profileViewModel);

    /// <summary>
    /// Shows a profile editing dialog for a socat profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The SocatProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> ShowSocatProfileEditAsync(string title, SocatProfileViewModel profileViewModel);

    /// <summary>
    /// Shows a profile editing dialog for a power supply profile.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="profileViewModel">The PowerSupplyProfileViewModel to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> ShowPowerSupplyProfileEditAsync(string title, PowerSupplyProfileViewModel profileViewModel);

    // Enhanced methods for Phase 6 - Unified Dialog System

    /// <summary>
    /// Shows a profile creation dialog for a new serial port profile with default values.
    /// </summary>
    /// <param name="defaultName">The default name for the new profile.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> CreateSerialProfileAsync(string defaultName = "SerialDefault");

    /// <summary>
    /// Shows a profile creation dialog for a new socat profile with default values.
    /// </summary>
    /// <param name="defaultName">The default name for the new profile.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> CreateSocatProfileAsync(string defaultName = "SocatDefault");

    /// <summary>
    /// Shows a profile creation dialog for a new power supply profile with default values.
    /// </summary>
    /// <param name="defaultName">The default name for the new profile.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> CreatePowerSupplyProfileAsync(string defaultName = "PowerSupplyDefault");

    /// <summary>
    /// Shows a dialog to edit an existing serial port profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> EditSerialProfileAsync(int profileId);

    /// <summary>
    /// Shows a dialog to edit an existing socat profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> EditSocatProfileAsync(int profileId);

    /// <summary>
    /// Shows a dialog to edit an existing power supply profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to edit.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the edit result.</returns>
    Task<ProfileEditResult> EditPowerSupplyProfileAsync(int profileId);

    /// <summary>
    /// Shows a name input dialog for duplicating a serial port profile.
    /// </summary>
    /// <param name="sourceProfileId">The ID of the source profile to duplicate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the new profile name.</returns>
    Task<ProfileDuplicateResult> DuplicateSerialProfileAsync(int sourceProfileId);

    /// <summary>
    /// Shows a name input dialog for duplicating a socat profile.
    /// </summary>
    /// <param name="sourceProfileId">The ID of the source profile to duplicate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the new profile name.</returns>
    Task<ProfileDuplicateResult> DuplicateSocatProfileAsync(int sourceProfileId);

    /// <summary>
    /// Shows a name input dialog for duplicating a power supply profile.
    /// </summary>
    /// <param name="sourceProfileId">The ID of the source profile to duplicate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the new profile name.</returns>
    Task<ProfileDuplicateResult> DuplicatePowerSupplyProfileAsync(int sourceProfileId);
}

/// <summary>
/// Result of a profile duplication operation.
/// </summary>
public class ProfileDuplicateResult
{
    /// <summary>
    /// Gets a value indicating whether the duplication was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the new profile name if successful.
    /// </summary>
    public string? NewName { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was cancelled.
    /// </summary>
    public bool IsCancelled => !IsSuccess && string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Creates a successful duplication result.
    /// </summary>
    /// <param name="newName">The new profile name.</param>
    /// <returns>A successful duplication result.</returns>
    public static ProfileDuplicateResult Success(string newName) => new() { IsSuccess = true, NewName = newName };

    /// <summary>
    /// Creates a cancelled duplication result.
    /// </summary>
    /// <returns>A cancelled duplication result.</returns>
    public static ProfileDuplicateResult Cancelled() => new() { IsSuccess = false };

    /// <summary>
    /// Creates a failed duplication result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed duplication result.</returns>
    public static ProfileDuplicateResult Failed(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
