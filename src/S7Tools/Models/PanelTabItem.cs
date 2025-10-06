using ReactiveUI;

namespace S7Tools.Models;

/// <summary>
/// Represents a tab item in the bottom panel.
/// </summary>
public class PanelTabItem : ReactiveObject
{
    private string _id = string.Empty;
    private string _header = string.Empty;
    private object? _content;
    private bool _isSelected = false;
    private string _icon = string.Empty;
    private bool _isClosable = false;

    /// <summary>
    /// Gets or sets the unique identifier for this tab item.
    /// </summary>
    public string Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    /// <summary>
    /// Gets or sets the header text for the tab.
    /// </summary>
    public string Header
    {
        get => _header;
        set => this.RaiseAndSetIfChanged(ref _header, value);
    }

    /// <summary>
    /// Gets or sets the content to display when this tab is selected.
    /// </summary>
    public object? Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this tab is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// Gets or sets the icon for the tab (FontAwesome icon name).
    /// </summary>
    public string Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this tab can be closed by the user.
    /// </summary>
    public bool IsClosable
    {
        get => _isClosable;
        set => this.RaiseAndSetIfChanged(ref _isClosable, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PanelTabItem"/> class.
    /// </summary>
    public PanelTabItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PanelTabItem"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="header">The header text.</param>
    /// <param name="content">The content.</param>
    /// <param name="icon">The icon name.</param>
    /// <param name="isClosable">Whether the tab is closable.</param>
    public PanelTabItem(string id, string header, object? content = null, string icon = "", bool isClosable = false)
    {
        Id = id;
        Header = header;
        Content = content;
        Icon = icon;
        IsClosable = isClosable;
    }
}