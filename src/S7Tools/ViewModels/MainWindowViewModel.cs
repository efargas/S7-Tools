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
        new LayoutService())
    {
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
        public MainWindowViewModel(
        IGreetingService greetingService, 
        IClipboardService clipboardService, 
        IDialogService dialogService, 
        ITagRepository tagRepository,
        IActivityBarService activityBarService,
        ILayoutService layoutService)
        {
        _greetingService = greetingService;
        _clipboardService = clipboardService;
        _dialogService = dialogService;
        _tagRepository = tagRepository;
        _activityBarService = activityBarService;
        _layoutService = layoutService;
        
        // Activity bar items are already initialized by the service
        // No need to add them again
        
        // Initialize bottom panel tabs
        Tabs = new ObservableCollection<PanelTabItem>
        {
        new PanelTabItem("problems", "PROBLEMS", "No problems detected.", "fa-solid fa-exclamation-triangle"),
        new PanelTabItem("output", "OUTPUT", "Output console ready...", "fa-solid fa-terminal"),
        new PanelTabItem("debug", "DEBUG CONSOLE", "Debug console ready...", "fa-solid fa-bug"),
        new PanelTabItem("logviewer", "LOG VIEWER", "Application logs will appear here...", "fa-solid fa-file-text")
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
        BottomPanelGridLength = (BottomPanelGridLength.Value == 0) 
        ? new GridLength(200, GridUnitType.Pixel) 
        : new GridLength(0, GridUnitType.Pixel);
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
        // Toggle bottom panel visibility
        BottomPanelGridLength = (BottomPanelGridLength.Value == 0) 
        ? new GridLength(200, GridUnitType.Pixel) 
        : new GridLength(0, GridUnitType.Pixel);
        }
        else
        {
        // Select new tab and ensure bottom panel is visible
        if (BottomPanelGridLength.Value == 0)
        {
        BottomPanelGridLength = new GridLength(200, GridUnitType.Pixel);
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
        CurrentContent = new HomeViewModel();
        DetailContent = ((HomeViewModel)CurrentContent).DetailContent;
        break;
        case "connections":
        SidebarTitle = "CONNECTIONS";
        CurrentContent = new ConnectionsViewModel();
        DetailContent = ((ConnectionsViewModel)CurrentContent).DetailContent;
        break;
        case "logviewer":
        SidebarTitle = "LOG VIEWER";
        CurrentContent = new HomeViewModel(); // TODO: Create LogViewerViewModel
        DetailContent = "Log Viewer functionality coming soon...";
        break;
        case "settings":
        SidebarTitle = "SETTINGS";
        CurrentContent = new SettingsViewModel();
        DetailContent = "Settings panel";
        break;
        default:
        SidebarTitle = "EXPLORER";
        CurrentContent = null;
        DetailContent = null;
        break;
        }
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
}
