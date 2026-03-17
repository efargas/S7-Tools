namespace S7Tools.Services;

/// <summary>
/// Service for managing dockable panel state and layout persistence.
/// Tracks panel visibility and sizing preferences.
/// </summary>
public class DockingService
{
    private readonly Dictionary<string, PanelState> _panelStates = new();

    /// <summary>
    /// Registers a panel with default state.
    /// </summary>
    public void RegisterPanel(string panelId, bool isVisible = true, double size = 200)
    {
        _panelStates[panelId] = new PanelState { IsVisible = isVisible, Size = size };
    }

    /// <summary>
    /// Gets the current state of a panel.
    /// </summary>
    public PanelState GetPanelState(string panelId)
    {
        return _panelStates.TryGetValue(panelId, out var state) ? state : new PanelState();
    }

    /// <summary>
    /// Sets the visibility of a panel.
    /// </summary>
    public void SetPanelVisible(string panelId, bool isVisible)
    {
        if (_panelStates.TryGetValue(panelId, out var state))
            state.IsVisible = isVisible;
    }

    /// <summary>
    /// Sets the size of a panel.
    /// </summary>
    public void SetPanelSize(string panelId, double size)
    {
        if (_panelStates.TryGetValue(panelId, out var state))
            state.Size = size;
    }
}

/// <summary>
/// Represents the visual state of a dockable panel.
/// </summary>
public class PanelState
{
    /// <summary>Gets or sets whether the panel is visible.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Gets or sets the panel size in pixels.</summary>
    public double Size { get; set; } = 200;
}
