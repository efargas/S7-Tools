using Avalonia;
using Avalonia.Controls;

namespace S7Tools.Views;

public partial class PropertyDisplayItem : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PropertyDisplayItem, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<PropertyDisplayItem, object?>(nameof(Value));

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public PropertyDisplayItem()
    {
        InitializeComponent();
    }
}
