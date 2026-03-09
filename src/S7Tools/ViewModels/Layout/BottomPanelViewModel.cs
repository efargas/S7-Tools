using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Models;
using S7Tools.Resources;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Pages;
using S7Tools.ViewModels.Tasks;
using S7Tools.Views;
using S7Tools.Views.Pages;

namespace S7Tools.ViewModels.Layout;

/// <summary>
/// ViewModel for managing the bottom panel with tabs and visibility.
/// Handles VSCode-style bottom panel behavior with collapsible tabs.
/// </summary>
public class BottomPanelViewModel : ReactiveObject
{
    private readonly ILogger<BottomPanelViewModel> _logger;
    private readonly ILogDataStore? _logDataStore;
    private readonly IUIThreadService? _uiThreadService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly ILogExportService? _logExportService;
    private readonly ICentralizedTaskLogService _centralizedTaskLogService;

    private readonly TaskManagerViewModel? _taskManager;
    private PanelTabItem? _selectedTab;
    private GridLength _panelHeight = new GridLength(200, GridUnitType.Pixel);
    private GridLength _lastPanelHeight = new GridLength(200, GridUnitType.Pixel);

    /// <summary>
    /// Initializes a new instance of the <see cref="BottomPanelViewModel"/> class for design-time.
    /// </summary>
    public BottomPanelViewModel() : this(
        CreateDesignTimeLogger(),
        null, // taskManager
        new ClipboardService(),
        new DialogService(),
        null, // logDataStore
        null, // uiThreadService
        null, // logExportService
        null) // centralizedTaskLogService
    {
    }

    private static ILogger<BottomPanelViewModel> CreateDesignTimeLogger()
    {
        return Microsoft.Extensions.Logging.Abstractions.NullLogger<BottomPanelViewModel>.Instance;
    }

    // ...

    /// <summary>
    /// Initializes a new instance of the <see cref="BottomPanelViewModel"/> class.
    /// </summary>
    public BottomPanelViewModel(
        ILogger<BottomPanelViewModel> logger,
        TaskManagerViewModel? taskManager,
        IClipboardService clipboardService,
        IDialogService dialogService,
        ILogDataStore? logDataStore = null,
        IUIThreadService? uiThreadService = null,
        ILogExportService? logExportService = null,
        ICentralizedTaskLogService? centralizedTaskLogService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskManager = taskManager;
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logDataStore = logDataStore;
        _uiThreadService = uiThreadService;
        _logExportService = logExportService;
        // Ideally centralized task log service should be required, but for old compatibility or if not registered?
        // We will mock it or handle null if needed, but TaskLogsPanelViewModel needs it.
        // Assuming it is registered. We might need to enforce it.
        // For now, I'll assign it or create a dummy if null?
        // No, DI should provide it.
        // I'll make it nullable in constructor but assume it's provided via DI in real app.
        _centralizedTaskLogService = centralizedTaskLogService!;

        Tabs = new ObservableCollection<PanelTabItem>
        {
            new PanelTabItem("log-viewer", Resources.UIStrings.Panel_LogViewer, CreateLogViewerContent(), "fa-solid fa-list", false)
        };
        TogglePanelCommand = ReactiveCommand.Create(TogglePanel);
        SelectTabCommand = ReactiveCommand.Create<PanelTabItem?>(SelectTab);
        CloseTabCommand = ReactiveCommand.Create<PanelTabItem>(CloseTab);

        if (_taskManager != null)
        {
            _taskManager.WhenAnyValue(tm => tm.ActiveTasks)
                .Subscribe(tasks =>
                {
                    // Add tabs for new tasks
                    if (tasks != null)
                    {
                        foreach (var task in tasks)
                        {
                            AddNewTaskTab(task);
                        }
                    }
                });

            // Also need to handle removed tasks if observable collection changes? 
            // ActiveTasks is ObservableCollection? 
            // We can subscribe to CollectionChanged of ActiveTasks
            if (_taskManager.ActiveTasks is System.Collections.Specialized.INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (Core.Models.Jobs.TaskExecution task in e.NewItems)
                        {
                            AddNewTaskTab(task);
                        }
                    }
                    if (e.OldItems != null)
                    {
                        foreach (Core.Models.Jobs.TaskExecution task in e.OldItems)
                        {
                            RemoveTaskTab(task);
                        }
                    }
                };
            }
        }
    }

    // ...

    private void AddNewTaskTab(Core.Models.Jobs.TaskExecution task)
    {
        // Avoid duplicates
        string tabId = $"task-{task.TaskId}";
        if (Tabs.Any(t => t.Id == tabId))
        {
            return;
        }

        var viewModel = new S7Tools.ViewModels.Components.TaskLogsPanelViewModel(
            task,
            _clipboardService,
            _centralizedTaskLogService,
            _uiThreadService);

        var view = new S7Tools.Views.Components.TaskLogsPanelView { DataContext = viewModel };

        var newTab = new PanelTabItem(tabId, $"Task: {task.JobName}", view, "fa-solid fa-tasks", true);
        Tabs.Add(newTab);

        SelectTab(newTab);
    }

    private void RemoveTaskTab(Core.Models.Jobs.TaskExecution task)
    {
        string tabId = $"task-{task.TaskId}";
        var tabToRemove = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tabToRemove != null)
        {
            Tabs.Remove(tabToRemove);

            // If we removed the selected tab, select the main log viewer
            if (SelectedTab == tabToRemove)
            {
                SelectTab(Tabs.FirstOrDefault());
            }
        }
    }

    public ReactiveCommand<PanelTabItem, Unit> CloseTabCommand { get; }

    private void CloseTab(PanelTabItem tab)
    {
        if (tab != null && tab.IsClosable)
        {
            Tabs.Remove(tab);
            if (SelectedTab == tab)
            {
                SelectTab(Tabs.FirstOrDefault());
            }
        }
    }

    /// <summary>
    /// Gets the bottom panel tabs.
    /// </summary>
    public ObservableCollection<PanelTabItem> Tabs { get; }

    /// <summary>
    /// Gets or sets the selected bottom panel tab.
    /// </summary>
    public PanelTabItem? SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    /// <summary>
    /// Gets or sets the height of the bottom panel.
    /// </summary>
    public GridLength PanelHeight
    {
        get => _panelHeight;
        set => this.RaiseAndSetIfChanged(ref _panelHeight, value);
    }

    /// <summary>
    /// Gets a value indicating whether the bottom panel is expanded.
    /// </summary>
    public bool IsExpanded => PanelHeight.Value > 35;

    /// <summary>
    /// Gets the command to toggle the bottom panel visibility.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TogglePanelCommand { get; }

    /// <summary>
    /// Gets the command to select a bottom panel tab (expands panel if collapsed).
    /// </summary>
    public ReactiveCommand<PanelTabItem?, Unit> SelectTabCommand { get; }

    /// <summary>
    /// Toggles the bottom panel between collapsed and expanded states.
    /// VSCode-like behavior: Toggle between collapsed (35px for tab headers) and expanded (200px default).
    /// </summary>
    private void TogglePanel()
    {
        if (PanelHeight.Value <= 35)
        {
            // Expand to the last known height
            PanelHeight = _lastPanelHeight;
            _logger.LogDebug("Bottom panel expanded to {Height}px", PanelHeight.Value);
        }
        else
        {
            // Store the current height before collapsing
            _lastPanelHeight = PanelHeight;
            // Collapse to show only tab headers (35px)
            PanelHeight = new GridLength(35, GridUnitType.Pixel);
            _logger.LogDebug("Bottom panel collapsed to {Height}px", PanelHeight.Value);
        }

        this.RaisePropertyChanged(nameof(IsExpanded));
    }

    /// <summary>
    /// Selects a tab and manages panel visibility with VSCode-like behavior.
    /// </summary>
    /// <param name="tab">The tab to select.</param>
    private void SelectTab(PanelTabItem? tab)
    {
        if (tab == null)
        {
            return;
        }

        PanelTabItem? currentSelectedTab = SelectedTab;

        // VSCode behavior: clicking on selected tab toggles bottom panel
        if (currentSelectedTab != null && currentSelectedTab.Id == tab.Id)
        {
            TogglePanel();
        }
        else
        {
            // Select new tab and ensure bottom panel is expanded
            if (PanelHeight.Value <= 35)
            {
                PanelHeight = new GridLength(200, GridUnitType.Pixel);
                this.RaisePropertyChanged(nameof(IsExpanded));
                _logger.LogDebug("Bottom panel expanded when selecting new tab: {TabName}", tab.Header);
            }

            // Update IsSelected property on all tabs
            foreach (PanelTabItem tabItem in Tabs)
            {
                tabItem.IsSelected = (tabItem.Id == tab.Id);
            }

            // Select the tab
            SelectedTab = tab;
            _logger.LogDebug("Selected bottom panel tab: {TabName}", tab.Header);
        }
    }

    /// <summary>
    /// Expands the panel if it's currently collapsed.
    /// </summary>
    public void EnsureExpanded()
    {
        if (PanelHeight.Value <= 35)
        {
            PanelHeight = new GridLength(200, GridUnitType.Pixel);
            this.RaisePropertyChanged(nameof(IsExpanded));
            _logger.LogDebug("Bottom panel expanded via EnsureExpanded()");
        }
    }

    /// <summary>
    /// Collapses the panel to show only tab headers.
    /// </summary>
    public void Collapse()
    {
        if (PanelHeight.Value > 35)
        {
            PanelHeight = new GridLength(35, GridUnitType.Pixel);
            this.RaisePropertyChanged(nameof(IsExpanded));
            _logger.LogDebug("Bottom panel collapsed via Collapse()");
        }
    }

    /// <summary>
    /// Creates the LogViewer content for the bottom panel.
    /// </summary>
    /// <returns>The LogViewer view or a placeholder if creation fails.</returns>
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
                    _dialogService,
                    _logExportService
                );
                logViewerView.DataContext = logViewerViewModel;
                _logger.LogDebug("LogViewer created with full services including export service");
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
                Text = UIStrings.Panel_LogViewerInitializationFailed,
                Foreground = Brushes.Red,
                Margin = new Avalonia.Thickness(10),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
        }
    }
}
