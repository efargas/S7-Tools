using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for visualizing PLC memory dumps from a file.
/// Hosts the file folder explorer to open multiple memory dumps in tabs.
/// </summary>
public partial class FileMemoryDumpViewModel : ViewModelBase
{
    private readonly ILogger<FileMemoryDumpViewModel> _logger;
    private readonly IFileDialogService _fileDialogService;
    private readonly IServiceProvider _serviceProvider;
    private readonly S7Tools.Core.Interfaces.Services.IApplicationSettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMemoryDumpViewModel"/> class.
    /// </summary>
    public FileMemoryDumpViewModel(
        ILogger<FileMemoryDumpViewModel> logger,
        IFileDialogService fileDialogService,
        IServiceProvider serviceProvider,
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Load default folder from settings, resolving relative paths via IPathService
        string defaultFolder = _settingsService.Current.MemoryDump.DefaultFolder;
        if (!string.IsNullOrEmpty(defaultFolder))
        {
            IPathService pathService = _serviceProvider.GetRequiredService<IPathService>();
            string resolvedFolder = pathService.ResolvePath(defaultFolder);
            if (Directory.Exists(resolvedFolder))
            {
                RootFolderPath = resolvedFolder;
                LoadTree();
            }
        }

        FileTreeItems.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(HasItems));
    }

    /// <summary>
    /// Gets or sets the Title.
    /// </summary>
    public string Title => "File PLC Memory Viewer";
    /// <summary>
    /// Gets or sets the Description.
    /// </summary>
    public string Description => "Select a folder to explore and open memory dump files within the system.";

    private string _rootFolderPath = string.Empty;
    public string RootFolderPath
    {
        get => _rootFolderPath;
        set => this.RaiseAndSetIfChanged(ref _rootFolderPath, value);
    }

    /// <summary>
    /// Gets or sets the HasItems.
    /// </summary>
    public bool HasItems => FileTreeItems.Count > 0;

    /// <summary>
    /// Gets or sets the FileTreeItems.
    /// </summary>
    public ObservableCollection<FileTreeItemViewModel> FileTreeItems { get; } = new();

    /// <summary>
    /// Action set by the parent (MemoryDumpViewerViewModel -> NavigationViewModel)
    /// to instruct the main docking system to open a new tab.
    /// </summary>
    public Action<IDockableViewModel>? OpenDocumentAction { get; set; }

    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        try
        {
            string? folderPath = await _fileDialogService.ShowFolderBrowserDialogAsync("Select Folder containing Memory Dumps");

            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                RootFolderPath = folderPath;
                await _settingsService.UpdateSettingsAsync(s => s.MemoryDump.DefaultFolder = folderPath);
                LoadTree();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting folder");
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadTree();
    }

    private void LoadTree()
    {
        FileTreeItems.Clear();

        if (string.IsNullOrEmpty(RootFolderPath))
        {
            return;
        }

        try
        {
            var root = new FileTreeItemViewModel(RootFolderPath, true);
            root.IsExpanded = true;
            FileTreeItems.Add(root);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file tree for {Path}", RootFolderPath);
        }
    }

    [RelayCommand]
    private void OpenFile(FileTreeItemViewModel? item)
    {
        if (item == null || item.IsDirectory || item.IsDummyNode || string.IsNullOrEmpty(item.FullPath) || OpenDocumentAction == null)
        {
            return;
        }

        try
        {
            FileMemoryDumpDocumentViewModel docVm = _serviceProvider.GetRequiredService<FileMemoryDumpDocumentViewModel>();
            docVm.OpenFile(item.FullPath);

            OpenDocumentAction.Invoke(docVm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening document for file {Path}", item.FullPath);
        }
    }
}
