using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Dialogs.Models;

/// <summary>
/// Represents the ProfileEditResult.
/// </summary>
public class ProfileEditResult
{
    /// <summary>
    /// Gets or sets the IsSuccess.
    /// </summary>
    public bool IsSuccess { get; private set; }
    /// <summary>
    /// Gets or sets the ProfileViewModel.
    /// </summary>
    public ViewModelBase? ProfileViewModel { get; private set; }

    private ProfileEditResult() { }

    /// <summary>
    /// Executes the Success operation.
    /// </summary>
    public static ProfileEditResult Success(ViewModelBase profileViewModel)
    {
        return new ProfileEditResult
        {
            IsSuccess = true,
            ProfileViewModel = profileViewModel
        };
    }

    /// <summary>
    /// Executes the Cancelled operation.
    /// </summary>
    public static ProfileEditResult Cancelled()
    {
        return new ProfileEditResult
        {
            IsSuccess = false
        };
    }
}
