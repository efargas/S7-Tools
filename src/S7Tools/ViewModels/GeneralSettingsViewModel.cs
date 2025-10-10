using ReactiveUI;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for general settings configuration.
/// </summary>
public class GeneralSettingsViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the GeneralSettingsViewModel class.
    /// </summary>
    public GeneralSettingsViewModel()
    {
        // Placeholder for future general settings
    }

    private string _message = "General settings will be available in a future update.";
    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }
}
