using System;
using ReactiveUI;

namespace S7Tools.ViewModels;

/// <summary>
/// Represents a navigation item in the main window's navigation pane.
/// </summary>
public class NavigationItemViewModel : ReactiveObject
{
    /// <summary>
    /// Gets the header text for the navigation item.
    /// </summary>
    public string Header { get; }
    /// <summary>
    /// Gets the icon for the navigation item.
    /// </summary>
    public string Icon { get; }
    /// <summary>
    /// Gets the type of the view model associated with this navigation item.
    /// </summary>
    public Type ContentViewModelType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationItemViewModel"/> class.
    /// </summary>
    /// <param name="header">The header text.</param>
    /// <param name="icon">The icon.</param>
    /// <param name="contentViewModelType">The type of the content view model.</param>
    public NavigationItemViewModel(string header, string icon, Type contentViewModelType)
    {
        Header = header;
        Icon = icon;
        ContentViewModelType = contentViewModelType;
    }
}