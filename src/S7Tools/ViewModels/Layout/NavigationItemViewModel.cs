using System;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace S7Tools.ViewModels.Layout;

/// <summary>
/// Represents the NavigationItemViewModel.
/// </summary>
public class NavigationItemViewModel : ReactiveObject
{
    /// <summary>
    /// Gets or sets the Header.
    /// </summary>
    public string Header { get; }
    /// <summary>
    /// Gets or sets the Icon.
    /// </summary>
    public string Icon { get; }
    /// <summary>
    /// Gets or sets the ContentViewModelType.
    /// </summary>
    public Type ContentViewModelType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationItemViewModel"/> class.
    /// </summary>
    public NavigationItemViewModel(string header, string icon, Type contentViewModelType)
    {
        Header = header;
        Icon = icon;
        ContentViewModelType = contentViewModelType;
    }
}
