namespace S7Tools.ViewModels.Jobs;

/// <summary>
/// Temporary placeholder ViewModel for job wizard functionality.
/// This will be replaced with a proper JobWizardViewModel once the wizard UI is implemented.
/// </summary>
public class JobWizardPlaceholderViewModel(string title, string message) : ViewModelBase
{
    /// <summary>
    /// Gets the title of the wizard.
    /// </summary>
    public string Title { get; } = title ?? string.Empty;

    /// <summary>
    /// Gets the description/message of the wizard.
    /// </summary>
    public string Message { get; } = message ?? string.Empty;
}
