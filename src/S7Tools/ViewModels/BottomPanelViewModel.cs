using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Linq;
using S7Tools.Models;
using S7Tools.Views;
using S7Tools.Services.Interfaces;
using S7Tools.Services;
using S7Tools.Infrastructure.Logging.Core.Storage;
using Microsoft.Extensions.Logging;
using System;
using Avalonia.Media;

namespace S7Tools.ViewModels;

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

    private GridLength _panelHeight = new GridLength(200, GridUnitType.Pixel);
    private GridLength _lastPanelHeight = new GridLength(200, GridUnitType.Pixel);
    private PanelTabItem? _selectedTab;

    /// <summary>
    /// Initializes a new instance of the <see cref="BottomPanelViewModel"/> class for design-time.
    /// </summary>
    public BottomPanelViewModel() : this(
        CreateDesignTimeLogger(),
        new ClipboardService(),
        new DialogService())
    {
    }

    /// <summary>
    /// Creates a design-time logger for the designer.
    /// </summary>
    /// <returns>A logger instance for design-time use.</returns>
    private static ILogger<BottomPanelViewModel> CreateDesignTimeLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });
        return loggerFactory.CreateLogger<BottomPanelViewModel>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BottomPanelViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="logDataStore">The log data store (optional).</param>
    /// <param name="uiThreadService">The UI thread service (optional).</param>
    /// <param name="logExportService">The log export service (optional).</param>
    public BottomPanelViewModel(
        ILogger<BottomPanelViewModel> logger,
        IClipboardService clipboardService,
        IDialogService dialogService,
        ILogDataStore? logDataStore = null,
        IUIThreadService? uiThreadService = null,
        ILogExportService? logExportService = null)
    {
        _logger = logger;
        _clipboardService = clipboardService;
        _dialogService = dialogService;
        _logDataStore = logDataStore;
        _uiThreadService = uiThreadService;
        _logExportService = logExportService;

        // Initialize bottom panel tabs
        Tabs = new ObservableCollection<PanelTabItem>
        {
            new PanelTabItem("problems", "PROBLEMS", "No problems detected.", "fa-solid fa-exclamation-triangle"),
            new PanelTabItem("output", "OUTPUT", "Output console ready...", "fa-solid fa-terminal"),
            new PanelTabItem("debug", "DEBUG CONSOLE", "Debug console ready...", "fa-solid fa-bug"),
            new PanelTabItem("logviewer", "LOG VIEWER", CreateLogViewerContent(), "fa-solid fa-file-text")
        };

        // Set the first tab as selected
        SelectedTab = Tabs.FirstOrDefault();
        if (SelectedTab != null)
        {
            SelectedTab.IsSelected = true;
        }

        // Initialize commands
        TogglePanelCommand = ReactiveCommand.Create(TogglePanel);
        SelectTabCommand = ReactiveCommand.Create<PanelTabItem>(SelectTab);

        _logger.LogDebug("BottomPanelViewModel initialized with {TabCount} tabs", Tabs.Count);
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
    public ReactiveCommand<PanelTabItem, Unit> SelectTabCommand { get; }

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
    private void SelectTab(PanelTabItem tab)
    {
        if (tab == null) return;

        var currentSelectedTab = SelectedTab;

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
            foreach (var tabItem in Tabs)
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
                Text = "LogViewer initialization failed. Check logs for details.",
                Foreground = Brushes.Red,
                Margin = new Avalonia.Thickness(10),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
        }
    }
}