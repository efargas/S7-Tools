using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Dialogs.Models;

public class ProfileEditResult
{
    public bool IsSuccess { get; private set; }
    public ViewModelBase? ProfileViewModel { get; private set; }

    private ProfileEditResult() {}

    public static ProfileEditResult Success(ViewModelBase profileViewModel)
    {
        return new ProfileEditResult 
        { 
            IsSuccess = true, 
            ProfileViewModel = profileViewModel 
        };
    }

    public static ProfileEditResult Cancelled()
    {
        return new ProfileEditResult 
        { 
            IsSuccess = false 
        };
    }
}
