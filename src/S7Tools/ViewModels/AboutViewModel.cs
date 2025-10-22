using ReactiveUI;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the About view displaying application information.
/// </summary>
public class AboutViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the greeting message for the About view.
    /// </summary>
    public string Greeting => "About S7Tools.";
}
