using ReactiveUI;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for advanced settings configuration.
/// </summary>
public class AdvancedSettingsViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the AdvancedSettingsViewModel class.
    /// </summary>
    public AdvancedSettingsViewModel()
    {
        // Placeholder for future advanced settings
    }

    private string _message = "Advanced settings will be available in a future update.";
    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }
}
