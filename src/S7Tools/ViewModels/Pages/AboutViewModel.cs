using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Resources;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for the About view displaying application information.
/// </summary>
public class AboutViewModel : ViewModelBase, IDockableViewModel
{
    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "About";

    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "About";

    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;

    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    /// <summary>
    /// Gets the greeting message for the About view.
    /// </summary>
    public string Greeting => UIStrings.About_Greeting;
}
