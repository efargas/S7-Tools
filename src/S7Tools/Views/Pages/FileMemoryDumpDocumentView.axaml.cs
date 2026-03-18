using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace S7Tools.Views.Pages;

public partial class FileMemoryDumpDocumentView : UserControl
{
    public FileMemoryDumpDocumentView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
