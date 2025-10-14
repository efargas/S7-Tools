using S7Tools.ViewModels;

namespace S7Tools.Models;

/// <summary>
/// Represents a request for profile editing through a dialog.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="ProfileViewModel">The profile ViewModel containing the profile data and validation logic.</param>
/// <param name="ProfileType">The type of profile being edited (Serial or Socat).</param>
public record ProfileEditRequest(
    string Title,
    ViewModelBase ProfileViewModel,
    ProfileType ProfileType);

/// <summary>
/// Represents the result of a profile editing dialog.
/// </summary>
/// <param name="IsSuccess">True if the user saved the changes, false if cancelled.</param>
/// <param name="ProfileViewModel">The updated profile ViewModel, or null if cancelled.</param>
public record ProfileEditResult(bool IsSuccess, ViewModelBase? ProfileViewModel)
{
    /// <summary>
    /// Creates a successful profile edit result.
    /// </summary>
    /// <param name="profileViewModel">The updated profile ViewModel.</param>
    /// <returns>A ProfileEditResult representing success.</returns>
    public static ProfileEditResult Success(ViewModelBase profileViewModel) => new(true, profileViewModel);

    /// <summary>
    /// Creates a cancelled profile edit result.
    /// </summary>
    /// <returns>A ProfileEditResult representing cancellation.</returns>
    public static ProfileEditResult Cancelled() => new(false, null);

    /// <summary>
    /// Creates a failed profile edit result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A ProfileEditResult representing failure.</returns>
    public static ProfileEditResult Failed(string error) => new(false, null);
}

/// <summary>
/// Enumeration of profile types for dialog selection.
/// </summary>
public enum ProfileType
{
    /// <summary>
    /// Serial port profile type.
    /// </summary>
    Serial,

    /// <summary>
    /// Socat profile type.
    /// </summary>
    Socat,

    /// <summary>
    /// Power supply profile type.
    /// </summary>
    PowerSupply
}
