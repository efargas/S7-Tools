using System.Collections;
using Avalonia;
using Avalonia.Controls;

namespace S7Tools.Controls;

/// <summary>
/// Reusable sidebar section control with expandable header and list of items.
/// </summary>
public partial class SidebarSection : UserControl
{
    /// <summary>
    /// Defines the Title property.
    /// </summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<SidebarSection, string>(nameof(Title), "Section");

    /// <summary>
    /// Defines the Icon property.
    /// </summary>
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<SidebarSection, string>(nameof(Icon), "fa-solid fa-folder");

    /// <summary>
    /// Defines the ItemsSource property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<SidebarSection, IEnumerable?>(nameof(ItemsSource));

    /// <summary>
    /// Defines the SelectedItem property.
    /// </summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<SidebarSection, object?>(nameof(SelectedItem), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>
    /// Defines the IsExpanded property.
    /// </summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<SidebarSection, bool>(nameof(IsExpanded), true);

    /// <summary>
    /// Gets or sets the section title.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon value (Font Awesome icon name).
    /// </summary>
    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the items source for the list.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the section is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public SidebarSection()
    {
        InitializeComponent();
    }
}
