using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.ViewModels.Base;
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

    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => FilePath;

    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;

    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    private HexViewerViewModel? _hexViewer;
    public HexViewerViewModel? HexViewer
    {
        get => _hexViewer;
        set => this.RaiseAndSetIfChanged(ref _hexViewer, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMemoryDumpDocumentViewModel"/> class.
    /// </summary>
    public FileMemoryDumpDocumentViewModel(IServiceProvider serviceProvider)
    {
        // Resolve a transient instance of HexViewerViewModel
        HexViewer = serviceProvider.GetRequiredService<HexViewerViewModel>();
    }

    /// <summary>
    /// Executes the OpenFile operation.
    /// </summary>
    public void OpenFile(string path)
    {
        FilePath = path;
        this.RaisePropertyChanged(nameof(DockId));
        this.RaisePropertyChanged(nameof(DockTitle));

        HexViewer?.OpenStream(path);
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HexViewer?.Dispose();
        }
    }
}
