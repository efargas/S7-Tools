using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace S7Tools.Views.Layout;

public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
