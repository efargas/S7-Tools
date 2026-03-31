using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// Shell ViewModel for Memory Dump Viewer that handles navigation between:
/// - Streamed PLC Memory Viewer
/// - File PLC Memory Viewer
/// </summary>
public sealed class MemoryDumpViewerViewModel : ViewModelBase, IDockableViewModel, IDisposable
{
    // IDockableViewModel implementation
    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "MemoryDump";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Memory Dump Viewer";
    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;
    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDumpViewerViewModel"/> class.
    /// </summary>
    public MemoryDumpViewerViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        FileExplorer = _serviceProvider.GetRequiredService<FileMemoryDumpViewModel>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Streamed PLC Memory Viewer"
        });

        // Default selection
        SelectedCategory = Categories[0];
        // We don't cache instances here anymore. The dockable will be resolved on demand.

        SelectCategoryCommand = ReactiveCommand.Create<string>(category =>
        {
            if (!string.IsNullOrWhiteSpace(category))
            {
                SelectedCategory = category;
            }
        });
    }

    /// <summary>
    /// Gets or sets the FileExplorer.
    /// </summary>
    public FileMemoryDumpViewModel FileExplorer { get; }

    /// <summary>
    /// Gets or sets the Categories.
    /// </summary>
    public ObservableCollection<string> Categories { get; }

    private string _selectedCategory = string.Empty;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            // No longer sets SelectedCategoryViewModel directly. UI/Navigation should call GetDockableForOpen()
        }
    }

    public ViewModelBase? SelectedCategoryViewModel
    {
        get => null; // Kept for compatibility if XAML tries to bind it, but shouldn't be used
    }

    /// <summary>
    /// Action set by NavigationViewModel to open a docked document tab.
    /// Propagated to child view models so they can open documents.
    /// </summary>
    private Action<IDockableViewModel>? _openDocumentAction;
    public Action<IDockableViewModel>? OpenDocumentAction
    {
        get => _openDocumentAction;
        set
        {
            _openDocumentAction = value;
            if (FileExplorer != null)
            {
                FileExplorer.OpenDocumentAction = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the SelectCategoryCommand.
    /// </summary>
    public ReactiveCommand<string, Unit> SelectCategoryCommand { get; }

    /// <summary>
    /// Executes the GetDockableForOpen operation.
    /// </summary>
    public IDockableViewModel? GetDockableForOpen()
    {
        return SelectedCategory switch
        {
            "Streamed PLC Memory Viewer" => _serviceProvider.GetRequiredService<StreamedMemoryDumpViewModel>(),
            _ => _serviceProvider.GetRequiredService<StreamedMemoryDumpViewModel>()
        };
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
    }
}
