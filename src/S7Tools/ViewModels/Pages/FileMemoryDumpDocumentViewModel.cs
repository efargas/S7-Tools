using S7Tools.ViewModels.Base;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.ViewModels.Hex;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel representing a single memory dump file opened in a dockable tab.
/// </summary>
public partial class FileMemoryDumpDocumentViewModel : ViewModelBase, IDockableViewModel, IDisposable
{
    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    public string DockId => FilePath;

    public string DockTitle => Path.GetFileName(FilePath);

    public bool CanClose => true;

    public bool CanFloat => true;

    private HexViewerViewModel? _hexViewer;
    public HexViewerViewModel? HexViewer
    {
        get => _hexViewer;
        set => this.RaiseAndSetIfChanged(ref _hexViewer, value);
    }

    public FileMemoryDumpDocumentViewModel(IServiceProvider serviceProvider)
    {
        // Resolve a transient instance of HexViewerViewModel
        HexViewer = serviceProvider.GetRequiredService<HexViewerViewModel>();
    }

    public void OpenFile(string path)
    {
        FilePath = path;
        this.RaisePropertyChanged(nameof(DockId));
        this.RaisePropertyChanged(nameof(DockTitle));

        HexViewer?.OpenStream(path);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HexViewer?.Dispose();
        }
    }
}