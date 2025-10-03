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
using FluentAvalonia.UI.Controls;
using Avalonia.Media;

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

    public ObservableCollection<NavigationItemViewModel> MenuItems { get; }
    public ObservableCollection<NavigationItemViewModel> FooterMenuItems { get; }

    private object? _currentContent;
    public object? CurrentContent
    {
        get => _currentContent;
        set => this.RaiseAndSetIfChanged(ref _currentContent, value);
    }

    private NavigationItemViewModel? _selectedMenuItem;
    public NavigationItemViewModel? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => this.RaiseAndSetIfChanged(ref _selectedMenuItem, value);
    }

    private string _testInputText = "This is some text to test clipboard operations.";
    public string TestInputText
    {
        get => _testInputText;
        set => this.RaiseAndSetIfChanged(ref _testInputText, value);
    }

    private GridLength _bottomPanelGridLength = new GridLength(200, GridUnitType.Pixel);
    public GridLength BottomPanelGridLength
    {
        get => _bottomPanelGridLength;
        set => this.RaiseAndSetIfChanged(ref _bottomPanelGridLength, value);
    }

    public ObservableCollection<TabViewModel> Tabs { get; }
    private TabViewModel? _selectedTab;
    public TabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    public ReactiveCommand<Unit, Unit> ToggleBottomPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> CutCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> PasteCommand { get; }
    
    
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

            MenuItems = new ObservableCollection<NavigationItemViewModel>
            {
                new NavigationItemViewModel("Home", new FontIconSource { Glyph = "&#xf015;", FontFamily = new FontFamily("avares://Projektanker.Icons.Avalonia.FontAwesome/Assets/FontAwesome.Solid.otf#Font Awesome 6 Free Solid") }, typeof(HomeViewModel)),
                new NavigationItemViewModel("Connections", new FontIconSource { Glyph = "&#xf1e6;", FontFamily = new FontFamily("avares://Projektanker.Icons.Avalonia.FontAwesome/Assets/FontAwesome.Solid.otf#Font Awesome 6 Free Solid") }, typeof(ConnectionsViewModel)),
            };

            FooterMenuItems = new ObservableCollection<NavigationItemViewModel>
            {
                new NavigationItemViewModel("Settings", new SymbolIconSource { Symbol = Symbol.Settings }, typeof(SettingsViewModel))
            };

            // Set initial content
            if (MenuItems.Count > 0)
            {
                NavigateTo(MenuItems[0].ContentViewModelType);
            }

            Tabs = new ObservableCollection<TabViewModel>
            {
                new TabViewModel { Header = "Output" },
                new TabViewModel { Header = "Problems" },
            };
            SelectedTab = Tabs.FirstOrDefault();

            ToggleBottomPanelCommand = ReactiveCommand.Create(() =>
            {
                BottomPanelGridLength = (BottomPanelGridLength.Value == 0) ? new GridLength(200, GridUnitType.Pixel) : new GridLength(0, GridUnitType.Pixel);
            });

            ExitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_dialogService != null)
                {
                    var result = await _dialogService.ShowConfirmationAsync("Exit Application", "Are you sure you want to exit?");
                    if (result)
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
        }

        public void NavigateTo(Type viewModelType)
        {
            if (viewModelType == null) return;

            // Using Activator.CreateInstance for simplicity. In a real app, you would use a DI container.
            var viewModel = (ViewModelBase)Activator.CreateInstance(viewModelType)!;
            CurrentContent = viewModel;
        }
}
