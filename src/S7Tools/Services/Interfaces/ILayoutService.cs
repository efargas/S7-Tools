using System.ComponentModel;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for managing the application layout state and configuration.
/// </summary>
public interface ILayoutService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is visible.
    /// </summary>
    bool IsSidebarVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bottom panel is visible.
    /// </summary>
    bool IsBottomPanelVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the activity bar is visible.
    /// </summary>
    bool IsActivityBarVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the status bar is visible.
    /// </summary>
    bool IsStatusBarVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu bar is visible.
    /// </summary>
    bool IsMenuBarVisible { get; set; }

    /// <summary>
    /// Gets or sets the width of the sidebar in pixels.
    /// </summary>
    double SidebarWidth { get; set; }

    /// <summary>
    /// Gets or sets the height of the bottom panel in pixels.
    /// </summary>
    double BottomPanelHeight { get; set; }

    /// <summary>
    /// Gets or sets the width of the activity bar in pixels.
    /// </summary>
    double ActivityBarWidth { get; set; }

    /// <summary>
    /// Gets or sets the minimum width of the sidebar in pixels.
    /// </summary>
    double MinSidebarWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum width of the sidebar in pixels.
    /// </summary>
    double MaxSidebarWidth { get; set; }

    /// <summary>
    /// Gets or sets the minimum height of the bottom panel in pixels.
    /// </summary>
    double MinBottomPanelHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum height of the bottom panel in pixels.
    /// </summary>
    double MaxBottomPanelHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is collapsed.
    /// </summary>
    bool IsSidebarCollapsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bottom panel is collapsed.
    /// </summary>
    bool IsBottomPanelCollapsed { get; set; }

    /// <summary>
    /// Event raised when the layout configuration changes.
    /// </summary>
    event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

    /// <summary>
    /// Toggles the visibility of the sidebar.
    /// </summary>
    void ToggleSidebar();

    /// <summary>
    /// Toggles the visibility of the bottom panel.
    /// </summary>
    void ToggleBottomPanel();

    /// <summary>
    /// Toggles the visibility of the activity bar.
    /// </summary>
    void ToggleActivityBar();

    /// <summary>
    /// Toggles the visibility of the status bar.
    /// </summary>
    void ToggleStatusBar();

    /// <summary>
    /// Toggles the visibility of the menu bar.
    /// </summary>
    void ToggleMenuBar();

    /// <summary>
    /// Collapses the sidebar to its minimum width.
    /// </summary>
    void CollapseSidebar();

    /// <summary>
    /// Expands the sidebar to its previous width.
    /// </summary>
    void ExpandSidebar();

    /// <summary>
    /// Collapses the bottom panel to its minimum height.
    /// </summary>
    void CollapseBottomPanel();

    /// <summary>
    /// Expands the bottom panel to its previous height.
    /// </summary>
    void ExpandBottomPanel();

    /// <summary>
    /// Resets the layout to default values.
    /// </summary>
    void ResetLayout();

    /// <summary>
    /// Saves the current layout configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveLayoutAsync();

    /// <summary>
    /// Loads the layout configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous load operation.</returns>
    Task LoadLayoutAsync();

    /// <summary>
    /// Gets the current layout configuration as a serializable object.
    /// </summary>
    /// <returns>The current layout configuration.</returns>
    LayoutConfiguration GetCurrentConfiguration();

    /// <summary>
    /// Applies a layout configuration.
    /// </summary>
    /// <param name="configuration">The configuration to apply.</param>
    void ApplyConfiguration(LayoutConfiguration configuration);
}

/// <summary>
/// Represents the layout configuration that can be saved and restored.
/// </summary>
public sealed class LayoutConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is visible.
    /// </summary>
    public bool IsSidebarVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the bottom panel is visible.
    /// </summary>
    public bool IsBottomPanelVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the activity bar is visible.
    /// </summary>
    public bool IsActivityBarVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the status bar is visible.
    /// </summary>
    public bool IsStatusBarVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the menu bar is visible.
    /// </summary>
    public bool IsMenuBarVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the width of the sidebar in pixels.
    /// </summary>
    public double SidebarWidth { get; set; } = 300;

    /// <summary>
    /// Gets or sets the height of the bottom panel in pixels.
    /// </summary>
    public double BottomPanelHeight { get; set; } = 200;

    /// <summary>
    /// Gets or sets the width of the activity bar in pixels.
    /// </summary>
    public double ActivityBarWidth { get; set; } = 48;

    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is collapsed.
    /// </summary>
    public bool IsSidebarCollapsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bottom panel is collapsed.
    /// </summary>
    public bool IsBottomPanelCollapsed { get; set; }
}

/// <summary>
/// Event arguments for layout change events.
/// </summary>
public sealed class LayoutChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the LayoutChangedEventArgs class.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="oldValue">The old value of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    public LayoutChangedEventArgs(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the name of the property that changed.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the old value of the property.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the new value of the property.
    /// </summary>
    public object? NewValue { get; }
}
