namespace S7Tools.ViewModels.Jobs;

/// <summary>
/// Temporary placeholder ViewModel for job wizard functionality.
/// This will be replaced with a proper JobWizardViewModel once the wizard UI is implemented.
/// </summary>
public class JobWizardPlaceholderViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the title of the wizard.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the description/message of the wizard.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobWizardPlaceholderViewModel"/> class.
    /// </summary>
    /// <param name="title">The title of the wizard.</param>
    /// <param name="message">The message to display.</param>
    public JobWizardPlaceholderViewModel(string title, string message)
    {
        Title = title ?? string.Empty;
        Message = message ?? string.Empty;
    }
}
