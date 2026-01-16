using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace S7Tools.Views.Components;

public partial class TaskLogsPanelView : UserControl
{
    public TaskLogsPanelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
