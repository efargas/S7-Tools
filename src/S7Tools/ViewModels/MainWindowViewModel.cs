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

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the main window.
/// </summary>
public class MainWindowViewModel : ReactiveObject
{
    private readonly IGreetingService _greetingService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly ITagRepository _tagRepository;

    /// <summary>
/// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
/// </summary>
/// <remarks>
/// This constructor is used by the designer.
/// A null-forgiving operator is used for services that are not essential for the designer view.
/// </remarks>
    public MainWindowViewModel() : this(new GreetingService(), new ClipboardService(), new DialogService(), new PlcDataService())
    {
    }

    private bool _isLeftPanelOpen = true;
    /// <summary>
/// Gets or sets a value indicating whether the left panel is open.
/// </summary>
    public bool IsLeftPanelOpen
    {
        get => _isLeftPanelOpen;
        set => this.RaiseAndSetIfChanged(ref _isLeftPanelOpen, value);
    }

    private SplitViewDisplayMode _leftPanelDisplayMode = SplitViewDisplayMode.Inline;
    /// <summary>
/// Gets or sets the display mode for the left panel.
/// </summary>
    public SplitViewDisplayMode LeftPanelDisplayMode
    {
        get => _leftPanelDisplayMode;
        set => this.RaiseAndSetIfChanged(ref _leftPanelDisplayMode, value);
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

        private string _testInputText = "This is some text to test clipboard operations.";
        /// <summary>
        /// Gets or sets the text for clipboard testing.
        /// </summary>
        public string TestInputText
        {
            get => _testInputText;
            set => this.RaiseAndSetIfChanged(ref _testInputText, value);
        }
    
        private string _greeting = string.Empty;
        /// <summary>
        /// Gets or sets the greeting message.
        /// </summary>
        public string Greeting
        {
            get => _greeting;
            set => this.RaiseAndSetIfChanged(ref _greeting, value);
        }
    
        private Tag? _lastReadTag;
        /// <summary>
        /// Gets or sets the last tag that was read from the PLC.
        /// </summary>
        public Tag? LastReadTag
        {
            get => _lastReadTag;
            set => this.RaiseAndSetIfChanged(ref _lastReadTag, value);
        }
    
        /// <summary>
            /// Gets the collection of tabs to be displayed.
            /// </summary>
            public ObservableCollection<TabViewModel> Tabs { get; }
        
            private TabViewModel? _selectedTab;
            /// <summary>
            /// Gets or sets the currently selected tab.
            /// </summary>
            public TabViewModel? SelectedTab
            {
                get => _selectedTab;
                set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
            }
        
            /// <summary>
            /// Gets the collection of navigation items for the left panel.
            /// </summary>
            public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

            private NavigationItemViewModel? _selectedNavigationItem;
            /// <summary>
            /// Gets or sets the currently selected navigation item.
            /// </summary>
            public NavigationItemViewModel? SelectedNavigationItem
            {
                get => _selectedNavigationItem;
                set
                {
                    this.RaiseAndSetIfChanged(ref _selectedNavigationItem, value);
                    if (value != null)
                    {
                        // Update CurrentView based on selected item's Tag
                        switch (value.Text)
                        {
                            case "Home":
                                CurrentView = new HomeViewModel(); // Assuming you have a HomeViewModel
                                break;
                            case "Connections":
                                CurrentView = new ConnectionsViewModel(); // Assuming you have a ConnectionsViewModel
                                break;
                            case "Settings":
                                CurrentView = new SettingsViewModel(); // Assuming you have a SettingsViewModel
                                break;
                            case "About":
                                CurrentView = new AboutViewModel(); // Assuming you have an AboutViewModel
                                break;
                            default:
                                CurrentView = null;
                                break;
                        }
                    }
                }
            }

            private object? _currentView;
            /// <summary>
            /// Gets or sets the currently displayed view model.
            /// </summary>
            public object? CurrentView
            {
                get => _currentView;
                set => this.RaiseAndSetIfChanged(ref _currentView, value);
            }
    
        /// <summary>
        /// Toggles the visibility of the left panel.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ToggleLeftPanelCommand { get; }
        /// <summary>
        /// Toggles the visibility of the bottom panel.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ToggleBottomPanelCommand { get; }
        /// <summary>
        /// Exits the application.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
        /// <summary>
        /// Cuts the selected text.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CutCommand { get; }
        /// <summary>
        /// Copies the selected text.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CopyCommand { get; }
        /// <summary>
        /// Pastes text from the clipboard.
        /// </summary>
        public ReactiveCommand<Unit, Unit> PasteCommand { get; }
        /// <summary>
        /// Reads a tag from the PLC.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ReadTagCommand { get; }
    
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
        public MainWindowViewModel(IGreetingService greetingService, IClipboardService clipboardService, IDialogService dialogService, ITagRepository tagRepository)
        {
            _greetingService = greetingService;
            _clipboardService = clipboardService;
            _dialogService = dialogService;
            _tagRepository = tagRepository;
            Greeting = _greetingService.Greet("S7Tools");
    
                    // Initialize the tabs collection
                    Tabs = new ObservableCollection<TabViewModel>
                    {
                        new TabViewModel { Header = "Home" },
                        new TabViewModel { Header = "Connections" }
                    };
                    SelectedTab = Tabs.FirstOrDefault();
            
                    // Initialize navigation items
                    NavigationItems = new ObservableCollection<NavigationItemViewModel>
                    {
                        new NavigationItemViewModel { Icon = "fa-home", Text = "Home" },                new NavigationItemViewModel { Icon = "fa-plug", Text = "Connections" },
                new NavigationItemViewModel { Icon = "fa-cog", Text = "Settings" },
                new NavigationItemViewModel { Icon = "fa-info-circle", Text = "About" }
            };

            // Initialize CurrentView based on the first navigation item
            SelectedNavigationItem = NavigationItems.FirstOrDefault();
            if (SelectedNavigationItem != null)
            {
                // Manually trigger the setter logic to set CurrentView
                SelectedNavigationItem = SelectedNavigationItem; 
            }

            ToggleLeftPanelCommand = ReactiveCommand.Create(() =>
            {
                IsLeftPanelOpen = !IsLeftPanelOpen;
                LeftPanelDisplayMode = IsLeftPanelOpen ? SplitViewDisplayMode.Inline : SplitViewDisplayMode.CompactInline;
            });
    
            ToggleBottomPanelCommand = ReactiveCommand.Create(() =>
            {
                BottomPanelGridLength = (BottomPanelGridLength.Value == 0) ? new GridLength(200, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel);
            });
    
            CloseApplicationInteraction = new Interaction<Unit, Unit>();
    
            ExitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await _dialogService.ShowConfirmationAsync("Exit Application", "Are you sure you want to exit?");
                if (result)
                {
                    await CloseApplicationInteraction.Handle(Unit.Default);
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
    
            ReadTagCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                LastReadTag = await _tagRepository.ReadTagAsync("DB1.DBD0");
            });
        }
}
