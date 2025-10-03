using ReactiveUI;

namespace S7Tools.ViewModels;

/// <summary>
/// Represents the ViewModel for a single tab in the main view.
/// </summary>
public class TabViewModel : ReactiveObject
{
    /// <summary>
    /// Gets or sets the header text for the tab.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    private object? _content;
    /// <summary>
    /// Gets or sets the content of the tab.
    /// </summary>
    public object? Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }
}
