using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace S7Tools.Views;

/// <summary>
/// View for power supply settings, providing UI for profile management,
/// device connection, and power control operations.
/// </summary>
public partial class PowerSupplySettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PowerSupplySettingsView"/> class.
    /// </summary>
    public PowerSupplySettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the component by loading the XAML.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
