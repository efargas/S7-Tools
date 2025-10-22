using Avalonia;
using Avalonia.Controls;

namespace S7Tools.Views;

public partial class PropertyDisplayItem : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PropertyDisplayItem, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<PropertyDisplayItem, string>(nameof(Value), string.Empty);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public PropertyDisplayItem()
    {
        InitializeComponent();
    }
}
