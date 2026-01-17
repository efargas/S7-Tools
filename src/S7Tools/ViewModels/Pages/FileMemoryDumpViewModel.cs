using System;
using Microsoft.Extensions.Logging;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for visualizing PLC memory dumps from a file.
/// </summary>
public class FileMemoryDumpViewModel : ViewModelBase
{
    private readonly ILogger<FileMemoryDumpViewModel> _logger;

    public FileMemoryDumpViewModel(ILogger<FileMemoryDumpViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Title => "File PLC Memory Viewer";
    public string Description => "This view will allow visualizing memory dumps loaded from local files.";
}
