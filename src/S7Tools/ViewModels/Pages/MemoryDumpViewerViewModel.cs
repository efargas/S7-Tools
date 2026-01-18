using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// Shell ViewModel for Memory Dump Viewer that handles navigation between:
/// - Streamed PLC Memory Viewer
/// - File PLC Memory Viewer
/// </summary>
public sealed class MemoryDumpViewerViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StreamedMemoryDumpViewModel _streamedViewModel;
    private readonly FileMemoryDumpViewModel _fileViewModel;

    public MemoryDumpViewerViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _streamedViewModel = _serviceProvider.GetRequiredService<StreamedMemoryDumpViewModel>();
        _fileViewModel = _serviceProvider.GetRequiredService<FileMemoryDumpViewModel>();

        Categories = new ObservableCollection<string>(new[]
        {
            "Streamed PLC Memory Viewer",
            "File PLC Memory Viewer"
        });

        // Default selection
        SelectedCategory = Categories[0];
        SelectedCategoryViewModel = GetCategoryViewModel(SelectedCategory);

        SelectCategoryCommand = ReactiveCommand.Create<string>(category =>
        {
            if (!string.IsNullOrWhiteSpace(category))
            {
                SelectedCategory = category;
            }
        });
    }

    public ObservableCollection<string> Categories { get; }

    private string _selectedCategory = string.Empty;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            SelectedCategoryViewModel = GetCategoryViewModel(value);
        }
    }

    private ViewModelBase? _selectedCategoryViewModel;
    public ViewModelBase? SelectedCategoryViewModel
    {
        get => _selectedCategoryViewModel!;
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryViewModel, value);
    }

    public ReactiveCommand<string, Unit> SelectCategoryCommand { get; }

    private ViewModelBase GetCategoryViewModel(string category)
    {
        return category switch
        {
            "Streamed PLC Memory Viewer" => _streamedViewModel,
            "File PLC Memory Viewer" => _fileViewModel,
            _ => _streamedViewModel
        };
    }

    public void Dispose()
    {
        _streamedViewModel?.Dispose();
        _fileViewModel?.Dispose();
    }
}
