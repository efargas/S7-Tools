using ReactiveUI;
using S7Tools.Resources;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the About view displaying application information.
/// </summary>
public class AboutViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the greeting message for the About view.
    /// </summary>
    public string Greeting => UIStrings.About_Greeting;
}
