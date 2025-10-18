using System;
using System.Reactive;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for managing navigation, sidebar, and main content area.
/// Handles VSCode-style activity bar navigation and content switching.
/// </summary>
public class NavigationViewModel : ReactiveObject
{
    private readonly IActivityBarService _activityBarService;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ILogger<NavigationViewModel> _logger;
    private readonly ILogDataStore? _logDataStore;

    private object? _currentContent;
    private object? _detailContent;
    private object? _mainContent;
    private string _sidebarTitle = "EXPLORER";
    private string _mainContentTitle = "";
    private bool _isSidebarVisible = true;
    private bool _showLogStats;
    private bool _showMainContentHeader;
    private string _logStatsMessage = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewModel"/> class for design-time.
    /// </summary>
    public NavigationViewModel() : this(
        new ActivityBarService(),
        new DesignTimeViewModelFactory(),
        CreateDesignTimeLogger())
    {
    }

    /// <summary>
    /// Creates a design-time logger for the designer.
    /// </summary>
    /// <returns>A logger instance for design-time use.</returns>
    private static ILogger<NavigationViewModel> CreateDesignTimeLogger()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { });
        return loggerFactory.CreateLogger<NavigationViewModel>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewModel"/> class.
    /// </summary>
    /// <param name="activityBarService">The activity bar service.</param>
    /// <param name="viewModelFactory">The ViewModel factory.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="logDataStore">The log data store (optional).</param>
    public NavigationViewModel(
        IActivityBarService activityBarService,
        IViewModelFactory viewModelFactory,
        ILogger<NavigationViewModel> logger,
        ILogDataStore? logDataStore = null)
    {
        _activityBarService = activityBarService;
        _viewModelFactory = viewModelFactory;
        _logger = logger;
        _logDataStore = logDataStore;

        // Initialize commands
        SelectActivityBarItemCommand = ReactiveCommand.Create<string>(SelectActivityBarItem);
        NavigateToActivityBarItemCommand = ReactiveCommand.Create<string>(NavigateToActivityBarItemViaKeyboard);
        ToggleSidebarCommand = ReactiveCommand.Create(ToggleSidebar);

        // Subscribe to activity bar selection changes
        _activityBarService.SelectionChanged += OnActivityBarSelectionChanged;

        // Set initial content
        if (_activityBarService.Items.Count > 0)
        {
            _activityBarService.SelectItem(_activityBarService.Items[0].Id);
        }

        _logger.LogDebug("NavigationViewModel initialized");
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

    /// <summary>
    /// Gets or sets the current content displayed in the sidebar.
    /// </summary>
    public object? CurrentContent
    {
        get => _currentContent;
        set => this.RaiseAndSetIfChanged(ref _currentContent, value);
    }

    /// <summary>
    /// Gets or sets the detail content displayed in the main editor area.
    /// </summary>
    public object? DetailContent
    {
        get => _detailContent;
        set => this.RaiseAndSetIfChanged(ref _detailContent, value);
    }

    /// <summary>
    /// Gets or sets the main content displayed in the main editor area using ViewLocator pattern.
    /// </summary>
    public object? MainContent
    {
        get => _mainContent;
        set => this.RaiseAndSetIfChanged(ref _mainContent, value);
    }

    /// <summary>
    /// Gets or sets the title displayed in the sidebar header.
    /// </summary>
    public string SidebarTitle
    {
        get => _sidebarTitle;
        set => this.RaiseAndSetIfChanged(ref _sidebarTitle, value);
    }

    /// <summary>
    /// Gets or sets the title displayed in the main content header.
    /// </summary>
    public string MainContentTitle
    {
        get => _mainContentTitle;
        set => this.RaiseAndSetIfChanged(ref _mainContentTitle, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the main content header.
    /// </summary>
    public bool ShowMainContentHeader
    {
        get => _showMainContentHeader;
        set => this.RaiseAndSetIfChanged(ref _showMainContentHeader, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is visible.
    /// </summary>
    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set => this.RaiseAndSetIfChanged(ref _isSidebarVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show log statistics.
    /// </summary>
    public bool ShowLogStats
    {
        get => _showLogStats;
        set => this.RaiseAndSetIfChanged(ref _showLogStats, value);
    }

    /// <summary>
    /// Gets or sets the log statistics message.
    /// </summary>
    public string LogStatsMessage
    {
        get => _logStatsMessage;
        set => this.RaiseAndSetIfChanged(ref _logStatsMessage, value);
    }

    /// <summary>
    /// Gets the command to select an activity bar item.
    /// </summary>
    public ReactiveCommand<string, Unit> SelectActivityBarItemCommand { get; }

    /// <summary>
    /// Gets the command to navigate to an activity bar item via keyboard (always expands sidebar).
    /// </summary>
    public ReactiveCommand<string, Unit> NavigateToActivityBarItemCommand { get; }

    /// <summary>
    /// Gets the command to toggle the sidebar visibility.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleSidebarCommand { get; }

    /// <summary>
    /// Handles activity bar selection changes.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnActivityBarSelectionChanged(object? sender, ActivityBarSelectionChangedEventArgs e)
    {
        if (e.CurrentItem != null)
        {
            NavigateToActivityBarItemContent(e.CurrentItem.Id);
        }
    }

    /// <summary>
    /// Selects an activity bar item with VSCode-like behavior.
    /// Clicking on selected item toggles sidebar visibility.
    /// </summary>
    /// <param name="itemId">The activity bar item ID.</param>
    private void SelectActivityBarItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        ActivityBarItem? currentSelectedItem = _activityBarService.SelectedItem;

        // VSCode behavior: clicking on selected item toggles sidebar
        if (currentSelectedItem != null && currentSelectedItem.Id == itemId)
        {
            // Toggle sidebar visibility
            IsSidebarVisible = !IsSidebarVisible;
            _logger.LogDebug("Toggled sidebar visibility to {Visible} for item {ItemId}", IsSidebarVisible, itemId);
        }
        else
        {
            // Select new item and ensure sidebar is visible
            _activityBarService.SelectItem(itemId);
            IsSidebarVisible = true;
            _logger.LogDebug("Selected activity bar item {ItemId} and ensured sidebar is visible", itemId);
        }
    }

    /// <summary>
    /// Navigates to an activity bar item via keyboard (always expands sidebar).
    /// </summary>
    /// <param name="itemId">The activity bar item ID.</param>
    private void NavigateToActivityBarItemViaKeyboard(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        // Keyboard navigation always selects the item and ensures sidebar is visible
        _activityBarService.SelectItem(itemId);
        IsSidebarVisible = true;
        _logger.LogDebug("Navigated to activity bar item {ItemId} via keyboard", itemId);
    }

    /// <summary>
    /// Toggles the sidebar visibility.
    /// </summary>
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
        _logger.LogDebug("Toggled sidebar visibility to {Visible}", IsSidebarVisible);
    }

    /// <summary>
    /// Navigates to the content associated with the specified activity bar item.
    /// </summary>
    /// <param name="itemId">The activity bar item ID.</param>
    private void NavigateToActivityBarItemContent(string itemId)
    {
        try
        {
            switch (itemId)
            {
                case "explorer":
                    SidebarTitle = "EXPLORER";
                    MainContentTitle = "Welcome";
                    ShowMainContentHeader = true;
                    CurrentContent = CreateViewModel<HomeViewModel>();
                    MainContent = CreateLoggingTestViewModel();
                    DetailContent = CreateLoggingTestViewModel();
                    ShowLogStats = false;
                    _logger.LogDebug("Navigated to Explorer");
                    break;

                case "connections":
                    SidebarTitle = "CONNECTIONS";
                    MainContentTitle = "PLC Connections";
                    ShowMainContentHeader = true;
                    ConnectionsViewModel? connectionsViewModel = CreateViewModel<ConnectionsViewModel>();
                    CurrentContent = connectionsViewModel;
                    MainContent = connectionsViewModel?.DetailContent;
                    DetailContent = connectionsViewModel?.DetailContent;
                    ShowLogStats = false;
                    _logger.LogDebug("Navigated to Connections");
                    break;

                case "logviewer":
                    SidebarTitle = "LOG VIEWER";
                    MainContentTitle = "Log Viewer";
                    ShowMainContentHeader = true;
                    CurrentContent = CreateViewModel<HomeViewModel>();
                    MainContent = "Log Viewer functionality coming soon...";
                    DetailContent = "Log Viewer functionality coming soon...";
                    ShowLogStats = true;
                    UpdateLogStats();
                    _logger.LogDebug("Navigated to Log Viewer");
                    break;

                case "settings":
                    SidebarTitle = "SETTINGS";
                    MainContentTitle = "Settings Configuration";
                    ShowMainContentHeader = true;
                    SettingsViewModel? settingsViewModel = CreateViewModel<SettingsViewModel>();
                    CurrentContent = settingsViewModel; // Categories in sidebar
                    MainContent = settingsViewModel; // Content in main area
                    DetailContent = settingsViewModel;
                    ShowLogStats = false;
                    _logger.LogDebug("Navigated to Settings");
                    break;

                case "taskmanager":
                    SidebarTitle = "TASK MANAGER";
                    MainContentTitle = "Task Manager";
                    ShowMainContentHeader = true;
                    TaskManagerShellViewModel? taskManagerShell = CreateViewModel<TaskManagerShellViewModel>();
                    CurrentContent = taskManagerShell; // Sidebar categories
                    MainContent = taskManagerShell; // Main content resolved via ViewLocator
                    DetailContent = taskManagerShell;
                    ShowLogStats = false;
                    _logger.LogDebug("Navigated to Task Manager");
                    break;

                case "jobs":
                    SidebarTitle = "JOBS MANAGEMENT";
                    MainContentTitle = "Jobs Management";
                    ShowMainContentHeader = true;
                    JobsManagementViewModel? jobsViewModel = CreateViewModel<JobsManagementViewModel>();
                    // Show Jobs-specific sidebar (menu) and main content
                    CurrentContent = jobsViewModel; // Sidebar will use JobsSidebarView DataTemplate
                    MainContent = jobsViewModel; // Jobs management in main area
                    DetailContent = jobsViewModel;
                    ShowLogStats = false;
                    _logger.LogDebug("Navigated to Jobs Management");
                    break;

                default:
                    SidebarTitle = "EXPLORER";
                    MainContentTitle = "";
                    ShowMainContentHeader = false;
                    CurrentContent = null;
                    MainContent = null;
                    DetailContent = null;
                    ShowLogStats = false;
                    _logger.LogWarning("Unknown activity bar item: {ItemId}", itemId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to activity bar item: {ItemId}", itemId);
            // Set fallback content
            SidebarTitle = "ERROR";
            MainContentTitle = "Error";
            ShowMainContentHeader = true;
            CurrentContent = null;
            MainContent = $"Navigation failed: {ex.Message}";
            DetailContent = $"Navigation failed: {ex.Message}";
            ShowLogStats = false;
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
            return _viewModelFactory.Create<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ViewModel of type {ViewModelType}", typeof(T).Name);
            return null;
        }
    }

    /// <summary>
    /// Creates a ViewModel for logging test functionality.
    /// This returns a reference to the parent MainWindowViewModel for now.
    /// In a proper implementation, this would be a separate LoggingTestViewModel.
    /// </summary>
    /// <returns>A ViewModel representing the logging test functionality.</returns>
    private object CreateLoggingTestViewModel()
    {
        // TODO: Create a proper LoggingTestViewModel
        // For now, we'll need to get this from the parent or create a placeholder
        return "Logging test functionality - needs dedicated ViewModel";
    }

    /// <summary>
    /// Creates a ViewModel for settings configuration.
    /// This returns a reference to the parent MainWindowViewModel for now.
    /// In a proper implementation, this would be a separate SettingsConfigViewModel.
    /// </summary>
    /// <returns>A ViewModel representing the settings configuration.</returns>
    private object CreateSettingsConfigViewModel()
    {
        // TODO: Create a proper SettingsConfigViewModel
        // For now, we'll need to get this from the parent or create a placeholder
        return "Settings configuration - needs dedicated ViewModel";
    }

    /// <summary>
    /// Updates the log statistics message.
    /// </summary>
    private void UpdateLogStats()
    {
        if (_logDataStore != null)
        {
            LogStatsMessage = $"Logs: {_logDataStore.Count}";
        }
        else
        {
            LogStatsMessage = "Log statistics unavailable";
        }
    }

    /// <summary>
    /// Navigates to the specified view model type.
    /// </summary>
    /// <param name="viewModelType">The view model type.</param>
    public void NavigateTo(Type viewModelType)
    {
        if (viewModelType == null)
        {
            return;
        }

        try
        {
            // Create ViewModel using the factory
            var viewModel = (ViewModelBase)_viewModelFactory.Create(viewModelType);
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

            _logger.LogDebug("Navigated to ViewModel type: {ViewModelType}", viewModelType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to ViewModel type: {ViewModelType}", viewModelType.Name);
        }
    }
}
