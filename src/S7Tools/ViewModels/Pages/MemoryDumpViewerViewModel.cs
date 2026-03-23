using S7Tools.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// Shell ViewModel for Memory Dump Viewer that handles navigation between:
/// - Streamed PLC Memory Viewer
/// - File PLC Memory Viewer
/// </summary>
public sealed class MemoryDumpViewerViewModel : ViewModelBase, IDockableViewModel, IDisposable
{
    // IDockableViewModel implementation
    public string DockId => "MemoryDump";
    public string DockTitle => "Memory Dump Viewer";
    public bool CanClose => true;
    public bool CanFloat => true;

    private readonly IServiceProvider _serviceProvider;

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

    public FileMemoryDumpViewModel FileExplorer { get; }

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

    public ReactiveCommand<string, Unit> SelectCategoryCommand { get; }

    public IDockableViewModel? GetDockableForOpen()
    {
        return SelectedCategory switch
        {
            "Streamed PLC Memory Viewer" => _serviceProvider.GetRequiredService<StreamedMemoryDumpViewModel>(),
            _ => _serviceProvider.GetRequiredService<StreamedMemoryDumpViewModel>()
        };
    }

    public void Dispose()
    {
    }
}