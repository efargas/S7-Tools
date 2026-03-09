using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Hex;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for visualizing PLC memory dumps from a file.
/// Hosts the HexViewerViewModel and manages file loading.
/// </summary>
public partial class FileMemoryDumpViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger<FileMemoryDumpViewModel> _logger;
    private readonly IFileDialogService _fileDialogService;
    private readonly IServiceProvider _serviceProvider;

    private HexViewerViewModel? _hexViewer;
    public HexViewerViewModel? HexViewer
    {
        get => _hexViewer;
        set => this.RaiseAndSetIfChanged(ref _hexViewer, value);
    }

    public FileMemoryDumpViewModel(
        ILogger<FileMemoryDumpViewModel> logger,
        IFileDialogService fileDialogService,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Initialize Hex Viewer
        HexViewer = _serviceProvider.GetRequiredService<HexViewerViewModel>();
    }

    public string Title => "File PLC Memory Viewer";
    public string Description => "Load memory dump files (.bin, .dmp) to inspect content, analyze data structures, and debug PLC memory states.";

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        try
        {
            var filters = "All files (*.*)|*.*|Binary files (*.bin)|*.bin|Dump files (*.dmp)|*.dmp";
            var path = await _fileDialogService.ShowOpenFileDialogAsync("Open Memory Dump File", filters);

            if (!string.IsNullOrEmpty(path))
            {
                // We need to adapt the string path to IStorageFile for consistency if possible,
                // but our FileDialogService returns string path.
                // HexViewer expects IStorageFile? No, wait. 
                // Let's check HexViewerViewModel.

                // HexViewerViewModel.LoadFileAsync takes IStorageFile. 
                // This mismatch is because IFileDialogService is older style returning string.
                // We should update HexViewerViewModel to accept string path or adapt here.
                // Actually, let's update HexViewerViewModel to take string path OR update FileDialogService.
                // Given the context, updating HexViewerViewModel to take string path is easier as we just used FileStream(path).

                // Wait, I wrote HexViewerViewModel.LoadFileAsync(IStorageFile file).
                // I should overload it or change it.
                // I'll assume for this step I can call a new method LoadFileAsync(string path).
                // I will add that to HexViewerViewModel in a subsequent step if needed, or modify it now.

                // Actually, I can just create a wrapper IStorageFile or change HexViewerViewModel.
                // Changing HexViewerViewModel is cleaner since it uses FileHexBuffer which takes a string path!

                if (HexViewer != null)
                {
                    HexViewer.OpenStream(path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening file");
        }
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
