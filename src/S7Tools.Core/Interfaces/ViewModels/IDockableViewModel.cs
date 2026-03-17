namespace S7Tools.Core.Interfaces.ViewModels;

/// <summary>
/// Interface for ViewModels that can be hosted as dockable documents or tool windows
/// in the Dock.Avalonia docking system.
/// </summary>
public interface IDockableViewModel
{
    /// <summary>
    /// Unique identifier for this dockable item (used to prevent duplicate tabs).
    /// </summary>
    string DockId { get; }

    /// <summary>
    /// Title displayed on the dock tab header.
    /// </summary>
    string DockTitle { get; }

    /// <summary>
    /// Whether the user can close this dockable item.
    /// </summary>
    bool CanClose { get; }

    /// <summary>
    /// Whether this dockable item can be floated into a separate window.
    /// </summary>
    bool CanFloat { get; }
}
