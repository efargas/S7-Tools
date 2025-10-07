using System.ComponentModel;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for managing the activity bar items and selection state.
/// </summary>
public interface IActivityBarService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets all available activity bar items.
    /// </summary>
    IReadOnlyList<ActivityBarItem> Items { get; }

    /// <summary>
    /// Gets or sets the currently selected activity bar item.
    /// </summary>
    ActivityBarItem? SelectedItem { get; set; }

    /// <summary>
    /// Gets the ID of the currently selected activity bar item.
    /// </summary>
    string? SelectedItemId { get; }

    /// <summary>
    /// Event raised when the selected activity bar item changes.
    /// </summary>
    event EventHandler<ActivityBarSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Event raised when an activity bar item is activated (clicked).
    /// </summary>
    event EventHandler<ActivityBarItemActivatedEventArgs>? ItemActivated;

    /// <summary>
    /// Adds a new activity bar item.
    /// </summary>
    /// <param name="item">The item to add.</param>
    void AddItem(ActivityBarItem item);

    /// <summary>
    /// Removes an activity bar item by its ID.
    /// </summary>
    /// <param name="itemId">The ID of the item to remove.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    bool RemoveItem(string itemId);

    /// <summary>
    /// Gets an activity bar item by its ID.
    /// </summary>
    /// <param name="itemId">The ID of the item to get.</param>
    /// <returns>The activity bar item if found; otherwise, null.</returns>
    ActivityBarItem? GetItem(string itemId);

    /// <summary>
    /// Selects an activity bar item by its ID.
    /// </summary>
    /// <param name="itemId">The ID of the item to select.</param>
    /// <returns>True if the item was selected; otherwise, false.</returns>
    bool SelectItem(string itemId);

    /// <summary>
    /// Updates the properties of an existing activity bar item.
    /// </summary>
    /// <param name="itemId">The ID of the item to update.</param>
    /// <param name="updateAction">The action to perform on the item.</param>
    /// <returns>True if the item was updated; otherwise, false.</returns>
    bool UpdateItem(string itemId, Action<ActivityBarItem> updateAction);

    /// <summary>
    /// Clears all activity bar items.
    /// </summary>
    void ClearItems();

    /// <summary>
    /// Reorders activity bar items.
    /// </summary>
    /// <param name="itemIds">The ordered list of item IDs.</param>
    void ReorderItems(IEnumerable<string> itemIds);

    /// <summary>
    /// Sets the visibility of an activity bar item.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="isVisible">True to make the item visible; false to hide it.</param>
    /// <returns>True if the visibility was changed; otherwise, false.</returns>
    bool SetItemVisibility(string itemId, bool isVisible);

    /// <summary>
    /// Sets the enabled state of an activity bar item.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="isEnabled">True to enable the item; false to disable it.</param>
    /// <returns>True if the enabled state was changed; otherwise, false.</returns>
    bool SetItemEnabled(string itemId, bool isEnabled);

    /// <summary>
    /// Gets the default activity bar items for the application.
    /// </summary>
    /// <returns>The default activity bar items.</returns>
    IEnumerable<ActivityBarItem> GetDefaultItems();
}

/// <summary>
/// Represents an item in the activity bar.
/// </summary>
public sealed class ActivityBarItem : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _tooltip = string.Empty;
    private string _iconPath = string.Empty;
    private string _iconData = string.Empty;
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private bool _isSelected = false;
    private int _order = 0;
    private object? _tag;

    /// <summary>
    /// Initializes a new instance of the ActivityBarItem class.
    /// </summary>
    public ActivityBarItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ActivityBarItem class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the item.</param>
    /// <param name="title">The title of the item.</param>
    /// <param name="tooltip">The tooltip text for the item.</param>
    /// <param name="iconPath">The path to the icon resource.</param>
    public ActivityBarItem(string id, string title, string tooltip, string iconPath)
    {
        Id = id;
        Title = title;
        Tooltip = tooltip;
        IconPath = iconPath;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the unique identifier for this activity bar item.
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the title of the activity bar item.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the tooltip text for the activity bar item.
    /// </summary>
    public string Tooltip
    {
        get => _tooltip;
        set => SetProperty(ref _tooltip, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the path to the icon resource for the activity bar item.
    /// </summary>
    public string IconPath
    {
        get => _iconPath;
        set => SetProperty(ref _iconPath, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the icon data (e.g., SVG content) for the activity bar item.
    /// </summary>
    public string IconData
    {
        get => _iconData;
        set => SetProperty(ref _iconData, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the activity bar item is visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the activity bar item is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the activity bar item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Gets or sets the display order of the activity bar item.
    /// </summary>
    public int Order
    {
        get => _order;
        set => SetProperty(ref _order, value);
    }

    /// <summary>
    /// Gets or sets additional data associated with the activity bar item.
    /// </summary>
    public object? Tag
    {
        get => _tag;
        set => SetProperty(ref _tag, value);
    }

    private void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Event arguments for activity bar selection change events.
/// </summary>
public sealed class ActivityBarSelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the ActivityBarSelectionChangedEventArgs class.
    /// </summary>
    /// <param name="previousItem">The previously selected item.</param>
    /// <param name="currentItem">The currently selected item.</param>
    public ActivityBarSelectionChangedEventArgs(ActivityBarItem? previousItem, ActivityBarItem? currentItem)
    {
        PreviousItem = previousItem;
        CurrentItem = currentItem;
    }

    /// <summary>
    /// Gets the previously selected activity bar item.
    /// </summary>
    public ActivityBarItem? PreviousItem { get; }

    /// <summary>
    /// Gets the currently selected activity bar item.
    /// </summary>
    public ActivityBarItem? CurrentItem { get; }
}

/// <summary>
/// Event arguments for activity bar item activation events.
/// </summary>
public sealed class ActivityBarItemActivatedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the ActivityBarItemActivatedEventArgs class.
    /// </summary>
    /// <param name="item">The activated item.</param>
    public ActivityBarItemActivatedEventArgs(ActivityBarItem item)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    /// <summary>
    /// Gets the activated activity bar item.
    /// </summary>
    public ActivityBarItem Item { get; }
}