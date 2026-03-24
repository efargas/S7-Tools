using S7Tools.ViewModels.Base;
using System;
using System.Globalization;
using System.Reactive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Resources;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Jobs;
using S7Tools.ViewModels.Pages;
using S7Tools.ViewModels.Profiles;
using S7Tools.ViewModels.Settings;
using S7Tools.ViewModels.Tasks;
using DesignTimeFactory = S7Tools.Services.DesignTimeViewModelFactory;

namespace S7Tools.ViewModels.Layout;

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

    private string _sidebarTitle = UIStrings.Navigation_Explorer;
    private bool _isSidebarVisible = true;
    private bool _showLogStats;
    private string _logStatsMessage = "";

    // Default constructor for design-time data
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewModel"/> class.
    /// </summary>
    public NavigationViewModel() : this(
        new ActivityBarService(),
        new DesignTimeFactory(),
        NullLogger<NavigationViewModel>.Instance,
        null)
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
    /// Action callback set by MainWindowViewModel to open a ViewModel as a docked document tab.
    /// </summary>
    public Action<IDockableViewModel>? OpenDocumentAction { get; set; }

    /// <summary>
    /// Action to open a tool window in the bottom dock panel.
    /// </summary>
    public Action<IDockableViewModel>? OpenToolAction { get; set; }

    /// <summary>
    /// Creates the welcome/initial ViewModel for the dock's default document.
    /// </summary>
    public object? CreateWelcomeViewModel()
    {
        return CreateViewModel<LoggingTestViewModel>();
    }

    /// <summary>
    /// Routes a ViewModel to the docking system if it implements IDockableViewModel,
    /// otherwise falls back to setting MainContent directly.
    /// Also subscribes to property changes so that sidebar interaction
    /// will reopen the dock tab if it was closed.
    /// </summary>
    private void OpenDockableContent(object? content)
    {
        if (content is IDockableViewModel dockable && OpenDocumentAction != null)
        {
            // If the content is the generic memory dump shell, open its currently selected doc instead
            if (dockable is MemoryDumpViewerViewModel memDumpVm && memDumpVm.GetDockableForOpen() is IDockableViewModel subDockable)
            {
                OpenDocumentAction(subDockable);
            }
            else if (dockable is ProfilesViewModel profVm && profVm.GetDockableForOpen() is IDockableViewModel profDockable)
            {
                OpenDocumentAction(profDockable);
            }
            else
            {
                OpenDocumentAction(dockable);
            }

            // Subscribe to property changes so that sidebar interaction
            // will reopen the dock tab if it was closed.
            // Clean up any previous subscription first.
            UnsubscribeSidebarWatcher();

            if (content is System.ComponentModel.INotifyPropertyChanged notifiable)
            {
                _currentSidebarDockable = dockable;
                _currentSidebarNotifiable = notifiable;
                _currentSidebarNotifiable.PropertyChanged += OnSidebarPropertyChanged;
            }
        }
        else
        {
            _logger.LogWarning("Attempted to open non-dockable content: {ContentType}", content?.GetType().Name);
        }
    }

    /// <summary>
    /// Ensures the current dockable tab is open. Called when the user clicks
    /// the same activity bar item or sidebar category that is already selected.
    /// </summary>
    public void EnsureDockTabOpen()
    {
        if (_currentSidebarDockable != null && OpenDocumentAction != null)
        {
            if (_currentSidebarDockable is MemoryDumpViewerViewModel memDumpVm && memDumpVm.GetDockableForOpen() is IDockableViewModel subDockable)
            {
                OpenDocumentAction(subDockable);
            }
            else if (_currentSidebarDockable is ProfilesViewModel profVm && profVm.GetDockableForOpen() is IDockableViewModel profDockable)
            {
                OpenDocumentAction(profDockable);
            }
            else
            {
                OpenDocumentAction(_currentSidebarDockable);
            }
        }
    }

    private IDockableViewModel? _currentSidebarDockable;
    private System.ComponentModel.INotifyPropertyChanged? _currentSidebarNotifiable;

    /// <summary>
    /// Called when a property changes on the current sidebar ViewModel.
    /// Re-invokes OpenDocumentAction to ensure the dock tab is open/active.
    /// </summary>
    private void OnSidebarPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_currentSidebarDockable != null && OpenDocumentAction != null)
        {
            if (e.PropertyName == "SidebarItemTapped")
            {
                if (_currentSidebarDockable is MemoryDumpViewerViewModel memDumpVm && memDumpVm.GetDockableForOpen() is IDockableViewModel subDockable)
                {
                    OpenDocumentAction(subDockable);
                }
                else if (_currentSidebarDockable is ProfilesViewModel profVm && profVm.GetDockableForOpen() is IDockableViewModel profDockable)
                {
                    OpenDocumentAction(profDockable);
                }
                else
                {
                    OpenDocumentAction(_currentSidebarDockable);
                }
            }
        }
    }

    /// <summary>
    /// Removes the PropertyChanged subscription from the previous sidebar ViewModel.
    /// </summary>
    private void UnsubscribeSidebarWatcher()
    {
        if (_currentSidebarNotifiable != null)
        {
            _currentSidebarNotifiable.PropertyChanged -= OnSidebarPropertyChanged;
            _currentSidebarNotifiable = null;
            _currentSidebarDockable = null;
        }
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
    /// Gets or sets the title displayed in the sidebar header.
    /// </summary>
    public string SidebarTitle
    {
        get => _sidebarTitle;
        set => this.RaiseAndSetIfChanged(ref _sidebarTitle, value);
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

        if (currentSelectedItem != null && currentSelectedItem.Id == itemId)
        {
            if (IsSidebarVisible)
            {
                // Sidebar is expanded → just collapse it, do NOT retrigger dock open
                IsSidebarVisible = false;
                _logger.LogDebug("Collapsed sidebar for item {ItemId}", itemId);
            }
            else
            {
                // Sidebar is collapsed → expand it and ensure dock tab is open
                IsSidebarVisible = true;
                EnsureDockTabOpen();
                _logger.LogDebug("Expanded sidebar and ensured dock tab open for item {ItemId}", itemId);
            }
        }
        else
        {
            // Different item selected → switch sidebar content and expand
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
                    SidebarTitle = UIStrings.Navigation_Explorer;
                    CurrentContent = CreateViewModel<HomeViewModel>();
                    ShowLogStats = false;
                    // Open the Welcome/LoggingTest view as a dock tab
                    OpenDockableContent(CreateLoggingTestViewModel());
                    _logger.LogDebug("Navigated to Explorer");
                    break;

                case "connections":
                    SidebarTitle = UIStrings.Navigation_Connections;
                    ConnectionsViewModel? connectionsViewModel = CreateViewModel<ConnectionsViewModel>();
                    CurrentContent = connectionsViewModel;
                    ShowLogStats = false;
                    // Open connections view as a dock tab
                    OpenDockableContent(connectionsViewModel);
                    _logger.LogDebug("Navigated to Connections");
                    break;

                case "logviewer":
                    SidebarTitle = UIStrings.Navigation_LogViewer;
                    CurrentContent = CreateViewModel<HomeViewModel>();
                    ShowLogStats = true;
                    UpdateLogStats();
                    _logger.LogDebug("Navigated to Log Viewer");
                    break;

                case "settings":
                    SidebarTitle = UIStrings.Navigation_Settings;
                    SettingsViewModel? settingsViewModel = CreateViewModel<SettingsViewModel>();
                    CurrentContent = settingsViewModel; // Categories in sidebar
                    ShowLogStats = false;
                    // Open settings as a dock tab
                    OpenDockableContent(settingsViewModel);
                    _logger.LogDebug("Navigated to Settings");
                    break;

                case "taskmanager":
                    SidebarTitle = UIStrings.Navigation_TaskManager;
                    TaskManagerShellViewModel? taskManagerShell = CreateViewModel<TaskManagerShellViewModel>();
                    if (taskManagerShell != null)
                    {
                        taskManagerShell.OpenDocumentAction = OpenDocumentAction;
                        taskManagerShell.OpenToolAction = OpenToolAction;
                        CurrentContent = taskManagerShell; // Sidebar categories
                        ShowLogStats = false;
                        // Open task manager as a dock tab
                        OpenDockableContent(taskManagerShell);
                    }
                    else
                    {
                        CurrentContent = null;
                        ShowLogStats = false;
                    }
                    _logger.LogDebug("Navigated to Task Manager");
                    break;

                case "jobs":
                    SidebarTitle = UIStrings.Navigation_JobsManagement;
                    JobsManagementViewModel? jobsViewModel = CreateViewModel<JobsManagementViewModel>();
                    CurrentContent = jobsViewModel; // Sidebar will use JobsSidebarView DataTemplate
                    ShowLogStats = false;
                    // Open jobs as a dock tab
                    OpenDockableContent(jobsViewModel);
                    _logger.LogDebug("Navigated to Jobs Management");
                    break;

                case "profiles":
                    SidebarTitle = "Profile Management";
                    ProfilesViewModel? profilesViewModel = CreateViewModel<ProfilesViewModel>();
                    CurrentContent = profilesViewModel; // Sidebar categories
                    ShowLogStats = false;
                    OpenDockableContent(profilesViewModel);
                    _logger.LogDebug("Navigated to Profiles");
                    break;

                case "memorydump":
                    SidebarTitle = "Memory Dump Viewer";
                    MemoryDumpViewerViewModel? memoryDumpViewModel = CreateViewModel<MemoryDumpViewerViewModel>();
                    if (memoryDumpViewModel != null)
                    {
                        memoryDumpViewModel.OpenDocumentAction = OpenDocumentAction;
                        CurrentContent = memoryDumpViewModel; // Enable sidebar content for memory dump
                        OpenDockableContent(memoryDumpViewModel);
                    }
                    else
                    {
                        CurrentContent = null;
                    }
                    ShowLogStats = false;
                    _logger.LogDebug("Navigated to Memory Dump Viewer");
                    break;

                default:
                    SidebarTitle = UIStrings.Navigation_Explorer;
                    CurrentContent = null;
                    ShowLogStats = false;
                    _logger.LogWarning("Unknown activity bar item: {ItemId}", itemId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to activity bar item: {ItemId}", itemId);
            // Set fallback content
            SidebarTitle = UIStrings.Navigation_ErrorTitle;
            CurrentContent = UIStrings.Navigation_NavigationFailed(ex.Message);
            _logger.LogError(ex, "Failed to navigate to {ItemId}", itemId);
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
    /// </summary>
    /// <returns>A ViewModel representing the logging test functionality.</returns>
    private object? CreateLoggingTestViewModel()
    {
        return CreateViewModel<LoggingTestViewModel>();
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

            if (viewModel is HomeViewModel)
            {
                // CurrentContent logic is fine
            }
            else if (viewModel is ConnectionsViewModel)
            {
                // CurrentContent logic is fine
            }
            else
            {
                // Refresh if needed
            }
            _logger.LogDebug("Navigated to ViewModel type: {ViewModelType}", viewModelType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to ViewModel type: {ViewModelType}", viewModelType.Name);
        }
    }
}