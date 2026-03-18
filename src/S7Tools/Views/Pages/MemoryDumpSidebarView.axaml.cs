using Avalonia.Controls;

namespace S7Tools.Views.Pages;

public partial class MemoryDumpSidebarView : UserControl
{
    public MemoryDumpSidebarView()
    {
        InitializeComponent();
    }

    private void OnTreeViewDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is TreeView treeView && 
            treeView.SelectedItem is S7Tools.ViewModels.Pages.FileTreeItemViewModel fileItem &&
            DataContext is S7Tools.ViewModels.Pages.MemoryDumpViewerViewModel mainVm)
        {
            if (mainVm.FileExplorer.OpenFileCommand.CanExecute(fileItem))
            {
                mainVm.FileExplorer.OpenFileCommand.Execute(fileItem);
            }
        }
    }

    private void OnCategoryTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is S7Tools.ViewModels.Pages.MemoryDumpViewerViewModel mainVm)
        {
            if (mainVm.SelectedCategoryViewModel is S7Tools.Core.Interfaces.ViewModels.IDockableViewModel dockable)
            {
                mainVm.OpenDocumentAction?.Invoke(dockable);
            }
        }
    }
}
