using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace S7Tools.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        // The InitializeComponent method is no longer used in Avalonia 11 for UserControls.
        // Instead, the XAML is loaded explicitly.
        AvaloniaXamlLoader.Load(this);
    }
}
