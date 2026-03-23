using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Dialogs.Models;

public class ProfileEditRequest
{
    public string Title { get; }
    public ViewModelBase ProfileViewModel { get; }
    public ProfileType ProfileType { get; }

    public ProfileEditRequest(string title, ViewModelBase profileViewModel, ProfileType profileType)
    {
        Title = title;
        ProfileViewModel = profileViewModel;
        ProfileType = profileType;
    }
}
