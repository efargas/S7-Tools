using System.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;

namespace S7Tools.Views.Controls;

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

    /// <summary>
    /// Defines the ItemTemplate property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<SidebarSection, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// Gets or sets the data template used to display each item.
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public SidebarSection()
    {
        InitializeComponent();
        // DataContext = this; // Removed to allow parent DataContext inheritance
    }

    /// <summary>
    /// Called when a ListBox item is tapped. Forces SelectedItem change notification
    /// even if the same item is clicked, which triggers dock tab reopen if closed.
    /// </summary>
    public void OnListBoxTapped(object? sender, RoutedEventArgs e)
    {
        // When the user taps a sidebar item (even the same one), signal to the
        // NavigationViewModel that the dock tab should be re-opened if closed.
        // NavigationViewModel subscribes to PropertyChanged on the sidebar ViewModel
        // via OnSidebarPropertyChanged, and calls OpenDocumentAction to reopen closed tabs.
        //
        // We use the IReactiveObject extension method which accepts a property name.
        // "SelectedCategoryViewModel" is the property all main views bind to.
        if (DataContext is ReactiveUI.IReactiveObject reactiveObj)
        {
            reactiveObj.RaisePropertyChanged(
                new System.ComponentModel.PropertyChangedEventArgs("SidebarItemTapped"));
        }
    }
}
