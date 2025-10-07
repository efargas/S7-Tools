using S7Tools.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace S7Tools.Services;

/// <summary>
/// Service for managing the activity bar items and selection state.
/// </summary>
public sealed class ActivityBarService : IActivityBarService
{
    private readonly ObservableCollection<ActivityBarItem> _items;
    private ActivityBarItem? _selectedItem;

    /// <summary>
    /// Initializes a new instance of the ActivityBarService class.
    /// </summary>
    public ActivityBarService()
    {
        _items = new ObservableCollection<ActivityBarItem>();
        
        // Initialize with default items
        foreach (var item in GetDefaultItems())
        {
            _items.Add(item);
        }

        // Select the first item by default
        if (_items.Count > 0)
        {
            _selectedItem = _items[0];
            _selectedItem.IsSelected = true;
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event EventHandler<ActivityBarSelectionChangedEventArgs>? SelectionChanged;

    /// <inheritdoc />
    public event EventHandler<ActivityBarItemActivatedEventArgs>? ItemActivated;

    /// <inheritdoc />
    public IReadOnlyList<ActivityBarItem> Items => _items.ToList().AsReadOnly();

    /// <inheritdoc />
    public ActivityBarItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem is value)
            {
                return;
            }

            var previousItem = _selectedItem;
            
            // Deselect previous item
            if (_selectedItem is not null)
            {
                _selectedItem.IsSelected = false;
            }

            _selectedItem = value;

            // Select new item
            if (_selectedItem is not null)
            {
                _selectedItem.IsSelected = true;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItemId)));
            
            SelectionChanged?.Invoke(this, new ActivityBarSelectionChangedEventArgs(previousItem, _selectedItem));
        }
    }

    /// <inheritdoc />
    public string? SelectedItemId => _selectedItem?.Id;

    /// <inheritdoc />
    public void AddItem(ActivityBarItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (string.IsNullOrEmpty(item.Id))
        {
            throw new ArgumentException("Activity bar item must have a valid ID.", nameof(item));
        }

        if (_items.Any(i => i.Id is item.Id))
        {
            throw new ArgumentException($"Activity bar item with ID '{item.Id}' already exists.", nameof(item));
        }

        _items.Add(item);
        
        // Sort items by order
        var sortedItems = _items.OrderBy(i => i.Order).ToList();
        _items.Clear();
        foreach (var sortedItem in sortedItems)
        {
            _items.Add(sortedItem);
        }
    }

    /// <inheritdoc />
    public bool RemoveItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        var item = _items.FirstOrDefault(i => i.Id is itemId);
        if (item is null)
        {
            return false;
        }

        // If removing the selected item, select another item
        if (item is _selectedItem)
        {
            var nextItem = _items.FirstOrDefault(i => i is not item && i.IsVisible && i.IsEnabled);
            SelectedItem = nextItem;
        }

        return _items.Remove(item);
    }

    /// <inheritdoc />
    public ActivityBarItem? GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        return _items.FirstOrDefault(i => i.Id is itemId);
    }

    /// <inheritdoc />
    public bool SelectItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        var item = GetItem(itemId);
        if (item is null || !item.IsVisible || !item.IsEnabled)
        {
            return false;
        }

        SelectedItem = item;
        ItemActivated?.Invoke(this, new ActivityBarItemActivatedEventArgs(item));
        return true;
    }

    /// <inheritdoc />
    public bool UpdateItem(string itemId, Action<ActivityBarItem> updateAction)
    {
        if (string.IsNullOrEmpty(itemId) || updateAction is null)
        {
            return false;
        }

        var item = GetItem(itemId);
        if (item is null)
        {
            return false;
        }

        updateAction(item);
        return true;
    }

    /// <inheritdoc />
    public void ClearItems()
    {
        _selectedItem = null;
        _items.Clear();
        
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItemId)));
    }

    /// <inheritdoc />
    public void ReorderItems(IEnumerable<string> itemIds)
    {
        if (itemIds is null)
        {
            throw new ArgumentNullException(nameof(itemIds));
        }

        var orderedIds = itemIds.ToList();
        var itemsDict = _items.ToDictionary(i => i.Id);

        _items.Clear();

        var order = 0;
        foreach (var itemId in orderedIds)
        {
            if (itemsDict.TryGetValue(itemId, out var item))
            {
                item.Order = order++;
                _items.Add(item);
            }
        }

        // Add any items that weren't in the ordered list
        foreach (var item in itemsDict.Values.Where(i => !orderedIds.Contains(i.Id)))
        {
            item.Order = order++;
            _items.Add(item);
        }
    }

    /// <inheritdoc />
    public bool SetItemVisibility(string itemId, bool isVisible)
    {
        var item = GetItem(itemId);
        if (item is null)
        {
            return false;
        }

        item.IsVisible = isVisible;

        // If hiding the selected item, select another visible item
        if (!isVisible && item is _selectedItem)
        {
            var nextItem = _items.FirstOrDefault(i => i is not item && i.IsVisible && i.IsEnabled);
            SelectedItem = nextItem;
        }

        return true;
    }

    /// <inheritdoc />
    public bool SetItemEnabled(string itemId, bool isEnabled)
    {
        var item = GetItem(itemId);
        if (item is null)
        {
            return false;
        }

        item.IsEnabled = isEnabled;

        // If disabling the selected item, select another enabled item
        if (!isEnabled && item is _selectedItem)
        {
            var nextItem = _items.FirstOrDefault(i => i is not item && i.IsVisible && i.IsEnabled);
            SelectedItem = nextItem;
        }

        return true;
    }

    /// <inheritdoc />
    public IEnumerable<ActivityBarItem> GetDefaultItems()
    {
        return new[]
        {
            new ActivityBarItem("explorer", "Explorer", "Explorer (Ctrl+Shift+E)", "fa-solid fa-folder")
            {
                Order = 0
            },
            new ActivityBarItem("connections", "Connections", "PLC Connections", "fa-solid fa-plug")
            {
                Order = 1
            },
            new ActivityBarItem("logviewer", "Log Viewer", "Application Logs", "fa-solid fa-list-alt")
            {
                Order = 2
            },
            new ActivityBarItem("settings", "Settings", "Application Settings", "fa-solid fa-cog")
            {
                Order = 3
            }
        };
    }
}