using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Dialogs.Models;

/// <summary>
/// Represents the ProfileEditRequest.
/// </summary>
public class ProfileEditRequest
{
    /// <summary>
    /// Gets or sets the Title.
    /// </summary>
    public string Title { get; }
    /// <summary>
    /// Gets or sets the ProfileViewModel.
    /// </summary>
    public ViewModelBase ProfileViewModel { get; }
    /// <summary>
    /// Gets or sets the ProfileType.
    /// </summary>
    public ProfileType ProfileType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileEditRequest"/> class.
    /// </summary>
    public ProfileEditRequest(string title, ViewModelBase profileViewModel, ProfileType profileType)
    {
        Title = title;
        ProfileViewModel = profileViewModel;
        ProfileType = profileType;
    }
}
