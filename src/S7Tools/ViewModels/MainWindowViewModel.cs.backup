using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System;
using System.Reactive.Linq;
using S7Tools.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using System.Linq;
using Avalonia.Media;
using S7Tools.Models;
using S7Tools.Views;
using S7Tools.Infrastructure.Logging.Core.Storage;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the main window with VSCode-style layout.
/// </summary>
public class MainWindowViewModel : ReactiveObject
{
    private readonly IGreetingService _greetingService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly ITagRepository _tagRepository;
    private readonly IActivityBarService _activityBarService;
    private readonly ILayoutService _layoutService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ILogDataStore? _logDataStore;
    private readonly IUIThreadService? _uiThreadService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly IViewModelFactory? _viewModelFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is used by the designer.
    /// A null-forgiving operator is used for services that are not essential for the designer view.
    /// </remarks>
    public MainWindowViewModel() : this(
        new GreetingService(), 
        new ClipboardService(), 
        new DialogService(), 
        new PlcDataService(),
        new ActivityBarService(),
        new LayoutService(),
        CreateDesignTimeLogger())
    {
    }

    /// <summary>
    /// Creates a design-time logger for the designer.
    /// </summary>
    /// <returns>A logger instance for design-time use.</returns>
    private static ILogger<MainWindowViewModel> CreateDesignTimeLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });
        return loggerFactory.CreateLogger<MainWindowViewModel>();
    }

    /// <summary>
    /// Gets the activity bar items.
    /// </summary>
    public IReadOnlyList<ActivityBarItem> ActivityBarItems => _activityBarService.Items;

    /// <summary>
    /// Gets or sets the selected activity bar item.
    /// </summary>
    public ActivityBarItem? SelectedActivityBarItem
    {
        get => _activityBarService.SelectedItem;
        set => _activityBarService.SelectedItem = value;
    }

    private object? _currentContent;
    /// <summary>
    /// Gets or sets the current content displayed in the sidebar.
    /// </summary>
    public object? CurrentContent
    {
        get => _currentContent;
        set => this.RaiseAndSetIfChanged(ref _currentContent, value);
    }

    private object? _detailContent;
    /// <summary>
    /// Gets or sets the detail content displayed in the main editor area.
    /// </summary>
    public object? DetailContent
    {
        get => _detailContent;
        set => this.RaiseAndSetIfChanged(ref _detailContent, value);
    }

    private string _sidebarTitle = "EXPLORER";
    /// <summary>
    /// Gets or sets the title displayed in the sidebar header.
    /// </summary>
    public string SidebarTitle
    {
        get => _sidebarTitle;
        set => this.RaiseAndSetIfChanged(ref _sidebarTitle, value);
    }

    private string _testInputText = "This is some text to test clipboard operations.";
    /// <summary>
    /// Gets or sets the test input text for clipboard operations.
    /// </summary>
    public string TestInputText
    {
        get => _testInputText;
        set => this.RaiseAndSetIfChanged(ref _testInputText, value);
    }

    // Settings Properties
    private string _defaultLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "S7Tools", "Logs");
    /// <summary>
    /// Gets or sets the default log path.
    /// </summary>
    public string DefaultLogPath
    {
        get => _defaultLogPath;
        set => this.RaiseAndSetIfChanged(ref _defaultLogPath, value);
    }

    private string _exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "S7Tools", "Exports");
    /// <summary>
    /// Gets or sets the export path.
    /// </summary>
    public string ExportPath
    {
        get => _exportPath;
        set => this.RaiseAndSetIfChanged(ref _exportPath, value);
    }

    private string _minimumLogLevel = "Information";
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public string MinimumLogLevel
    {
        get => _minimumLogLevel;
        set => this.RaiseAndSetIfChanged(ref _minimumLogLevel, value);
    }

    private bool _autoScrollLogs = true;
    /// <summary>
    /// Gets or sets a value indicating whether to auto-scroll logs.
    /// </summary>
    public bool AutoScrollLogs
    {
        get => _autoScrollLogs;
        set => this.RaiseAndSetIfChanged(ref _autoScrollLogs, value);
    }

    private bool _enableRollingLogs = true;
    /// <summary>
    /// Gets or sets a value indicating whether to enable rolling log files.
    /// </summary>
    public bool EnableRollingLogs
    {
        get => _enableRollingLogs;
        set => this.RaiseAndSetIfChanged(ref _enableRollingLogs, value);
    }

    private bool _showTimestampInLogs = true;
    /// <summary>
    /// Gets or sets a value indicating whether to show timestamp in logs.
    /// </summary>
    public bool ShowTimestampInLogs
    {
        get => _showTimestampInLogs;
        set => this.RaiseAndSetIfChanged(ref _showTimestampInLogs, value);
    }

    private bool _showCategoryInLogs = true;
    /// <summary>
    /// Gets or sets a value indicating whether to show category in logs.
    /// </summary>
    public bool ShowCategoryInLogs
    {
        get => _showCategoryInLogs;
        set => this.RaiseAndSetIfChanged(ref _showCategoryInLogs, value);
    }

    private bool _showLogLevelInLogs = true;
    /// <summary>
    /// Gets or sets a value indicating whether to show log level in logs.
    /// </summary>
    public bool ShowLogLevelInLogs
    {
        get => _showLogLevelInLogs;
        set => this.RaiseAndSetIfChanged(ref _showLogLevelInLogs, value);
    }

    private string _settingsStatusMessage = "Settings ready";
    /// <summary>
    /// Gets or sets the settings status message.
    /// </summary>
    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _settingsStatusMessage, value);
    }

    private string _currentSettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "S7Tools", "settings.json");
    /// <summary>
    /// Gets or sets the current settings file path.
    /// </summary>
    public string CurrentSettingsFilePath
    {
        get => _currentSettingsFilePath;
        set => this.RaiseAndSetIfChanged(ref _currentSettingsFilePath, value);
    }

    private DateTime _settingsLastModified = DateTime.Now;
    /// <summary>
    /// Gets or sets the settings last modified date.
    /// </summary>
    public DateTime SettingsLastModified
    {
        get => _settingsLastModified;
        set => this.RaiseAndSetIfChanged(ref _settingsLastModified, value);
    }

    private GridLength _bottomPanelGridLength = new GridLength(200, GridUnitType.Pixel);
    /// <summary>
    /// Gets or sets the height of the bottom panel.
    /// </summary>
    public GridLength BottomPanelGridLength
    {
        get => _bottomPanelGridLength;
        set => this.RaiseAndSetIfChanged(ref _bottomPanelGridLength, value);
    }

    private bool _isSidebarVisible = true;
    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is visible.
    /// </summary>
    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set => this.RaiseAndSetIfChanged(ref _isSidebarVisible, value);
    }

    /// <summary>
    /// Gets a value indicating whether the bottom panel is expanded.
    /// </summary>
    public bool IsBottomPanelExpanded => BottomPanelGridLength.Value > 35;

    private string _statusMessage = "Ready";
    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private string _logStatsMessage = "";
    /// <summary>
    /// Gets or sets the log statistics message.
    /// </summary>
    public string LogStatsMessage
    {
        get => _logStatsMessage;
        set => this.RaiseAndSetIfChanged(ref _logStatsMessage, value);
    }

    private bool _showLogStats;
    /// <summary>
    /// Gets or sets a value indicating whether to show log statistics.
    /// </summary>
    public bool ShowLogStats
    {
        get => _showLogStats;
        set => this.RaiseAndSetIfChanged(ref _showLogStats, value);
    }

    private string _lastButtonPressed = "";
    /// <summary>
    /// Gets or sets the last button pressed message.
    /// </summary>
    public string LastButtonPressed
    {
        get => _lastButtonPressed;
        set => this.RaiseAndSetIfChanged(ref _lastButtonPressed, value);
    }

    /// <summary>
    /// Gets the bottom panel tabs.
    /// </summary>
    public ObservableCollection<PanelTabItem> Tabs { get; }
    
    private PanelTabItem? _selectedTab;
    /// <summary>
    /// Gets or sets the selected bottom panel tab.
    /// </summary>
    public PanelTabItem? SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    /// <summary>
    /// Gets the command to toggle the bottom panel visibility.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleBottomPanelCommand { get; }

    /// <summary>
    /// Gets the command to toggle the sidebar visibility.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleSidebarCommand { get; }

    /// <summary>
    /// Gets the command to exit the application.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    
    /// <summary>
    /// Gets the command to cut text to clipboard.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CutCommand { get; }
    
    /// <summary>
    /// Gets the command to copy text to clipboard.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    
    /// <summary>
    /// Gets the command to paste text from clipboard.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PasteCommand { get; }

    /// <summary>
    /// Gets the command to select an activity bar item.
    /// </summary>
    public ReactiveCommand<string, Unit> SelectActivityBarItemCommand { get; }

    /// <summary>
    /// Gets the command to navigate to an activity bar item via keyboard (always expands sidebar).
    /// </summary>
    public ReactiveCommand<string, Unit> NavigateToActivityBarItemCommand { get; }

    /// <summary>
    /// Gets the command to select a bottom panel tab (expands panel if collapsed).
    /// </summary>
    public ReactiveCommand<PanelTabItem, Unit> SelectBottomPanelTabCommand { get; }

    /// <summary>
    /// Gets the command to test trace logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestTraceLogCommand { get; }

    /// <summary>
    /// Gets the command to test debug logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestDebugLogCommand { get; }

    /// <summary>
    /// Gets the command to test information logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestInfoLogCommand { get; }

    /// <summary>
    /// Gets the command to test warning logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestWarningLogCommand { get; }

    /// <summary>
    /// Gets the command to test error logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestErrorLogCommand { get; }

    /// <summary>
    /// Gets the command to test critical logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestCriticalLogCommand { get; }

    /// <summary>
    /// Gets the command to export logs.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportLogsCommand { get; }

    /// <summary>
    /// Gets the command to browse for default log path.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseDefaultLogPathCommand { get; }

    /// <summary>
    /// Gets the command to browse for export path.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseExportPathCommand { get; }

    /// <summary>
    /// Gets the command to save settings.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

    /// <summary>
    /// Gets the command to load settings.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadSettingsCommand { get; }

    /// <summary>
    /// Gets the command to reset settings to defaults.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand { get; }

    /// <summary>
    /// Gets the command to open settings folder.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSettingsFolderCommand { get; }

    /// <summary>
    /// Interaction to signal the view to close the application.
    /// </summary>
    public Interaction<Unit, Unit> CloseApplicationInteraction { get; }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        /// <param name="greetingService">The greeting service.</param>
        /// <param name="clipboardService">The clipboard service.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="tagRepository">The tag repository service.</param>
        /// <param name="activityBarService">The activity bar service.</param>
        /// <param name="layoutService">The layout service.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="logDataStore">The log data store (optional).</param>
        /// <param name="uiThreadService">The UI thread service (optional).</param>
        /// <param name="fileDialogService">The file dialog service (optional).</param>
        /// <param name="viewModelFactory">The ViewModel factory (optional).</param>
        public MainWindowViewModel(
        IGreetingService greetingService, 
        IClipboardService clipboardService, 
        IDialogService dialogService, 
        ITagRepository tagRepository,
        IActivityBarService activityBarService,
        ILayoutService layoutService,
        ILogger<MainWindowViewModel> logger,
        ILogDataStore? logDataStore = null,
        IUIThreadService? uiThreadService = null,
        IFileDialogService? fileDialogService = null,
        IViewModelFactory? viewModelFactory = null)
        {
        _greetingService = greetingService;
        _clipboardService = clipboardService;
        _dialogService = dialogService;
        _tagRepository = tagRepository;
        _activityBarService = activityBarService;
        _layoutService = layoutService;
        _logger = logger;
        _logDataStore = logDataStore;
        _uiThreadService = uiThreadService;
        _fileDialogService = fileDialogService;
        _viewModelFactory = viewModelFactory;
        
        // Activity bar items are already initialized by the service
        // No need to add them again
        
        // Initialize bottom panel tabs
        Tabs = new ObservableCollection<PanelTabItem>
        {
        new PanelTabItem("problems", "PROBLEMS", "No problems detected.", "fa-solid fa-exclamation-triangle"),
        new PanelTabItem("output", "OUTPUT", "Output console ready...", "fa-solid fa-terminal"),
        new PanelTabItem("debug", "DEBUG CONSOLE", "Debug console ready...", "fa-solid fa-bug"),
        new PanelTabItem("logviewer", "LOG VIEWER", CreateLogViewerContent(), "fa-solid fa-file-text")
        };
        SelectedTab = Tabs.FirstOrDefault();
        
        // Set the first tab as selected
        if (SelectedTab != null)
        {
        SelectedTab.IsSelected = true;
        }
        
        // Initialize commands
        ToggleBottomPanelCommand = ReactiveCommand.Create(() =>
        {
            // VSCode-like behavior: Toggle between collapsed (35px for tab headers) and expanded (200px default)
            if (BottomPanelGridLength.Value <= 35)
            {
                // Expand to previous height or default 200px
                BottomPanelGridLength = new GridLength(200, GridUnitType.Pixel);
            }
            else
            {
                // Collapse to show only tab headers (35px)
                BottomPanelGridLength = new GridLength(35, GridUnitType.Pixel);
            }
            this.RaisePropertyChanged(nameof(IsBottomPanelExpanded));
        });

        ToggleSidebarCommand = ReactiveCommand.Create(() =>
        {
        IsSidebarVisible = !IsSidebarVisible;
        });
        
        SelectActivityBarItemCommand = ReactiveCommand.Create<string>(itemId =>
        {
        if (!string.IsNullOrEmpty(itemId))
        {
        var currentSelectedItem = _activityBarService.SelectedItem;
        
        // VSCode behavior: clicking on selected item toggles sidebar
        if (currentSelectedItem != null && currentSelectedItem.Id == itemId)
        {
        // Toggle sidebar visibility
        IsSidebarVisible = !IsSidebarVisible;
        }
        else
        {
        // Select new item and ensure sidebar is visible
        _activityBarService.SelectItem(itemId);
        IsSidebarVisible = true;
        }
        }
        });

        NavigateToActivityBarItemCommand = ReactiveCommand.Create<string>(itemId =>
        {
        if (!string.IsNullOrEmpty(itemId))
        {
        // Keyboard navigation always selects the item and ensures sidebar is visible
        _activityBarService.SelectItem(itemId);
        IsSidebarVisible = true;
        }
        });

        SelectBottomPanelTabCommand = ReactiveCommand.Create<PanelTabItem>(tab =>
        {
            if (tab != null)
            {
                var currentSelectedTab = SelectedTab;
                
                // VSCode behavior: clicking on selected tab toggles bottom panel
                if (currentSelectedTab != null && currentSelectedTab.Id == tab.Id)
                {
                    // Toggle bottom panel between collapsed (35px) and expanded (200px)
                    if (BottomPanelGridLength.Value <= 35)
                    {
                        BottomPanelGridLength = new GridLength(200, GridUnitType.Pixel);
                    }
                    else
                    {
                        BottomPanelGridLength = new GridLength(35, GridUnitType.Pixel);
                    }
                    this.RaisePropertyChanged(nameof(IsBottomPanelExpanded));
                }
                else
                {
                    // Select new tab and ensure bottom panel is expanded
                    if (BottomPanelGridLength.Value <= 35)
                    {
                        BottomPanelGridLength = new GridLength(200, GridUnitType.Pixel);
                        this.RaisePropertyChanged(nameof(IsBottomPanelExpanded));
                    }
                    
                    // Update IsSelected property on all tabs
                    foreach (var tabItem in Tabs)
                    {
                        tabItem.IsSelected = (tabItem.Id == tab.Id);
                    }
                    
                    // Select the tab
                    SelectedTab = tab;
                }
            }
        });
        
        ExitCommand = ReactiveCommand.CreateFromTask(async () =>
        {
        if (_dialogService != null)
        {
        var result = await _dialogService.ShowConfirmationAsync("Exit Application", "Are you sure you want to exit?");
        if (result && CloseApplicationInteraction != null)
        {
        await CloseApplicationInteraction.Handle(Unit.Default);
        }
        }
        });
        
        CutCommand = ReactiveCommand.CreateFromTask(async () =>
        {
        if (!string.IsNullOrEmpty(TestInputText))
        {
        await _clipboardService.SetTextAsync(TestInputText);
        TestInputText = string.Empty;
        }
        });
        
        CopyCommand = ReactiveCommand.CreateFromTask(async () =>
        {
        if (!string.IsNullOrEmpty(TestInputText))
        {
        await _clipboardService.SetTextAsync(TestInputText);
        }
        });
        
        PasteCommand = ReactiveCommand.CreateFromTask(async () =>
        {
        var text = await _clipboardService.GetTextAsync();
        if (!string.IsNullOrEmpty(text))
        {
        TestInputText += text;
        }
        });

        // Initialize logging test commands
        TestTraceLogCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogTrace("This is a TRACE level log message generated at {Timestamp}", DateTime.Now);
            LastButtonPressed = "TRACE Log Generated";
            StatusMessage = "Generated TRACE log message";
            ClearButtonPressedAfterDelay();
        });

        TestDebugLogCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogDebug("This is a DEBUG level log message with some debug info: {DebugData}", new { UserId = 123, Action = "ButtonClick" });
            LastButtonPressed = "DEBUG Log Generated";
            StatusMessage = "Generated DEBUG log message";
            ClearButtonPressedAfterDelay();
        });

        TestInfoLogCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogInformation("This is an INFORMATION level log message. User performed action: {Action}", "Test Info Log");
            LastButtonPressed = "INFO Log Generated";
            StatusMessage = "Generated INFORMATION log message";
            ClearButtonPressedAfterDelay();
        });

        TestWarningLogCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogWarning("This is a WARNING level log message. Something might need attention: {Warning}", "Test warning condition");
            LastButtonPressed = "WARNING Log Generated";
            StatusMessage = "Generated WARNING log message";
            ClearButtonPressedAfterDelay();
        });

        TestErrorLogCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogError("This is an ERROR level log message. An error occurred: {Error}", "Simulated error for testing");
            LastButtonPressed = "ERROR Log Generated";
            StatusMessage = "Generated ERROR log message";
            ClearButtonPressedAfterDelay();
        });

        TestCriticalLogCommand = ReactiveCommand.Create(() =>
        {
            _logger.LogCritical("This is a CRITICAL level log message. System is in critical state: {CriticalIssue}", "Simulated critical issue");
            LastButtonPressed = "CRITICAL Log Generated";
            StatusMessage = "Generated CRITICAL log message";
            ClearButtonPressedAfterDelay();
        });

        ExportLogsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                var exportText = "Log Export - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                await _clipboardService.SetTextAsync(exportText);
                _logger.LogInformation("Log export copied to clipboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export logs to clipboard");
            }
        });

        // Settings Commands
        BrowseDefaultLogPathCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                if (_fileDialogService != null)
                {
                    var selectedPath = await _fileDialogService.ShowFolderBrowserDialogAsync(
                        "Select Default Log Path", 
                        DefaultLogPath);
                    
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        DefaultLogPath = selectedPath;
                        SettingsStatusMessage = "Default log path updated successfully";
                        _logger.LogInformation("Default log path updated to: {Path}", selectedPath);
                    }
                    else
                    {
                        SettingsStatusMessage = "Folder selection cancelled";
                    }
                }
                else
                {
                    SettingsStatusMessage = "File dialog service not available";
                    _logger.LogWarning("File dialog service not available for default log path selection");
                }
            }
            catch (Exception ex)
            {
                SettingsStatusMessage = $"Failed to browse for default log path: {ex.Message}";
                _logger.LogError(ex, "Failed to browse for default log path");
            }
        });

        BrowseExportPathCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                if (_fileDialogService != null)
                {
                    var selectedPath = await _fileDialogService.ShowFolderBrowserDialogAsync(
                        "Select Export Path", 
                        ExportPath);
                    
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        ExportPath = selectedPath;
                        SettingsStatusMessage = "Export path updated successfully";
                        _logger.LogInformation("Export path updated to: {Path}", selectedPath);
                    }
                    else
                    {
                        SettingsStatusMessage = "Folder selection cancelled";
                    }
                }
                else
                {
                    SettingsStatusMessage = "File dialog service not available";
                    _logger.LogWarning("File dialog service not available for export path selection");
                }
            }
            catch (Exception ex)
            {
                SettingsStatusMessage = $"Failed to browse for export path: {ex.Message}";
                _logger.LogError(ex, "Failed to browse for export path");
            }
        });

        SaveSettingsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                SettingsStatusMessage = "Settings saved successfully";
                SettingsLastModified = DateTime.Now;
                _logger.LogInformation("Settings saved to {Path}", CurrentSettingsFilePath);
            }
            catch (Exception ex)
            {
                SettingsStatusMessage = $"Failed to save settings: {ex.Message}";
                _logger.LogError(ex, "Failed to save settings");
            }
        });

        LoadSettingsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                SettingsStatusMessage = "Settings loaded successfully";
                SettingsLastModified = DateTime.Now;
                _logger.LogInformation("Settings loaded from {Path}", CurrentSettingsFilePath);
            }
            catch (Exception ex)
            {
                SettingsStatusMessage = $"Failed to load settings: {ex.Message}";
                _logger.LogError(ex, "Failed to load settings");
            }
        });

        ResetSettingsCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                // Reset to default values
                DefaultLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "S7Tools", "Logs");
                ExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "S7Tools", "Exports");
                MinimumLogLevel = "Information";
                AutoScrollLogs = true;
                EnableRollingLogs = true;
                ShowTimestampInLogs = true;
                ShowCategoryInLogs = true;
                ShowLogLevelInLogs = true;
                
                SettingsStatusMessage = "Settings reset to defaults";
                _logger.LogInformation("Settings reset to default values");
            }
            catch (Exception ex)
            {
                SettingsStatusMessage = $"Failed to reset settings: {ex.Message}";
                _logger.LogError(ex, "Failed to reset settings");
            }
        });

        OpenSettingsFolderCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                var settingsDir = Path.GetDirectoryName(CurrentSettingsFilePath);
                if (!string.IsNullOrEmpty(settingsDir))
                {
                    SettingsStatusMessage = $"Settings folder: {settingsDir}";
                    _logger.LogInformation("Open settings folder requested: {Path}", settingsDir);
                }
            }
            catch (Exception ex)
            {
                SettingsStatusMessage = $"Failed to open settings folder: {ex.Message}";
                _logger.LogError(ex, "Failed to open settings folder");
            }
        });
        
        CloseApplicationInteraction = new Interaction<Unit, Unit>();
                
        // Subscribe to activity bar selection changes
        _activityBarService.SelectionChanged += OnActivityBarSelectionChanged;
        
        // Set initial content
        if (_activityBarService.Items.Count > 0)
        {
        _activityBarService.SelectItem(_activityBarService.Items[0].Id);
        }
        }
        
                
        /// <summary>
        /// Handles activity bar selection changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnActivityBarSelectionChanged(object? sender, ActivityBarSelectionChangedEventArgs e)
        {
        if (e.CurrentItem != null)
        {
        NavigateToActivityBarItem(e.CurrentItem.Id);
        }
        }
        
        /// <summary>
        /// Navigates to the content associated with the specified activity bar item.
        /// </summary>
        /// <param name="itemId">The activity bar item ID.</param>
        private void NavigateToActivityBarItem(string itemId)
        {
        switch (itemId)
        {
        case "explorer":
            SidebarTitle = "EXPLORER";
            CurrentContent = CreateViewModel<HomeViewModel>();
            DetailContent = CreateLoggingTestViewModel();
            ShowLogStats = false;
            break;
        case "connections":
            SidebarTitle = "CONNECTIONS";
            var connectionsViewModel = CreateViewModel<ConnectionsViewModel>();
            CurrentContent = connectionsViewModel;
            DetailContent = connectionsViewModel?.DetailContent;
            ShowLogStats = false;
            break;
        case "logviewer":
            SidebarTitle = "LOG VIEWER";
            CurrentContent = CreateViewModel<HomeViewModel>();
            DetailContent = "Log Viewer functionality coming soon...";
            ShowLogStats = true;
            if (_logDataStore != null)
            {
                LogStatsMessage = $"Logs: {_logDataStore.Count}";
            }
            break;
        case "settings":
            SidebarTitle = "SETTINGS";
            CurrentContent = CreateViewModel<SettingsViewModel>();
            DetailContent = CreateSettingsConfigViewModel();
            ShowLogStats = false;
            break;
        default:
            SidebarTitle = "EXPLORER";
            CurrentContent = null;
            DetailContent = null;
            break;
        }
        }
        
        /// <summary>
        /// Creates a ViewModel using the factory or fallback to design-time creation.
        /// </summary>
        /// <typeparam name="T">The ViewModel type to create.</typeparam>
        /// <returns>The created ViewModel instance.</returns>
        private T? CreateViewModel<T>() where T : ViewModelBase
        {
            try
            {
                if (_viewModelFactory != null)
                {
                    return _viewModelFactory.Create<T>();
                }
                else
                {
                    // Fallback for design-time or when factory is not available
                    return (T?)Activator.CreateInstance(typeof(T));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ViewModel of type {ViewModelType}", typeof(T).Name);
                return null;
            }
        }
        
        /// <summary>
        /// Creates a ViewModel for logging test functionality.
        /// This is a temporary solution until we have a proper LoggingTestViewModel.
        /// </summary>
        /// <returns>A ViewModel representing the logging test functionality.</returns>
        private object CreateLoggingTestViewModel()
        {
            // For now, return this MainWindowViewModel as it contains the logging test functionality
            // In a proper implementation, this would be a separate LoggingTestViewModel
            return this;
        }
        
        /// <summary>
        /// Creates a ViewModel for settings configuration.
        /// This is a temporary solution until we have a proper SettingsConfigViewModel.
        /// </summary>
        /// <returns>A ViewModel representing the settings configuration.</returns>
        private object CreateSettingsConfigViewModel()
        {
            // For now, return this MainWindowViewModel as it contains the settings functionality
            // In a proper implementation, this would be a separate SettingsConfigViewModel
            return this;
        }
        
        /// <summary>
        /// Navigates to the specified view model type.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        public void NavigateTo(Type viewModelType)
        {
        if (viewModelType == null) return;
        
        // Using Activator.CreateInstance for simplicity. In a real app, you would use a DI container.
        var viewModel = (ViewModelBase)Activator.CreateInstance(viewModelType)!;
        CurrentContent = viewModel;
        
        if (viewModel is HomeViewModel homeViewModel)
        {
        DetailContent = homeViewModel.DetailContent;
        }
        else if (viewModel is ConnectionsViewModel connectionsViewModel)
        {
        DetailContent = connectionsViewModel.DetailContent;
        }
        else
        {
        DetailContent = null;
        }
        }

        /// <summary>
        /// Creates the LogViewer content for the bottom panel.
        /// </summary>
        /// <returns>The LogViewer view.</returns>
        private object CreateLogViewerContent()
        {
            try
            {
                // Create LogViewerView with proper DataContext from DI
                var logViewerView = new LogViewerView();
                
                // If we have the required services, create a proper LogViewerViewModel
                if (_logDataStore != null && _uiThreadService != null)
                {
                    var logViewerViewModel = new LogViewerViewModel(
                        _logDataStore,
                        _uiThreadService,
                        _clipboardService,
                        _dialogService
                    );
                    logViewerView.DataContext = logViewerViewModel;
                }
                else
                {
                    // Use design-time ViewModel if services are not available
                    logViewerView.DataContext = new LogViewerViewModel();
                    _logger.LogWarning("LogViewer created with design-time services due to missing dependencies");
                }
                
                return logViewerView;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create LogViewerViewModel");
                // Return a simple placeholder if we can't create the proper view
                return new TextBlock 
                { 
                    Text = "LogViewer initialization failed. Check logs for details.", 
                    Foreground = Brushes.Red,
                    Margin = new Avalonia.Thickness(10),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                };
            }
        }

        /// <summary>
        /// Clears the button pressed message after a delay.
        /// </summary>
        private async void ClearButtonPressedAfterDelay()
        {
            await Task.Delay(3000); // Clear after 3 seconds
            LastButtonPressed = "";
            StatusMessage = "Ready";
        }
}
