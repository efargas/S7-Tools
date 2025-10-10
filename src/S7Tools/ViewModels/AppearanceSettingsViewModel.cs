using ReactiveUI;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for appearance settings configuration.
/// </summary>
public class AppearanceSettingsViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the AppearanceSettingsViewModel class.
    /// </summary>
    public AppearanceSettingsViewModel()
    {
        // Placeholder for future appearance settings
    }

    private string _message = "Appearance settings will be available in a future update.";
    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }
}
