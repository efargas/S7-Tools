using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using System;
using System.Reactive.Linq;
using S7_Tools.Services;
using S7_Tools.Services.Interfaces;

namespace S7_Tools.ViewModels;

/// <summary>
/// ViewModel for the main window.
/// </summary>
public partial class MainWindowViewModel : ReactiveObject
{
    private readonly IGreetingService _greetingService;
    private readonly IClipboardService _clipboardService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is used by the designer.
    /// </remarks>
    public MainWindowViewModel() : this(new GreetingService(), new ClipboardService())
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

    private object? _activeWorkspaceContent;
    /// <summary>
    /// Gets or sets the content of the active workspace.
    /// </summary>
    public object? ActiveWorkspaceContent
    {
        get => _activeWorkspaceContent;
        set => this.RaiseAndSetIfChanged(ref _activeWorkspaceContent, value);
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
    /// Interaction to signal the view to close the application.
    /// </summary>
    public Interaction<Unit, Unit> CloseApplicationInteraction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="greetingService">The greeting service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    public MainWindowViewModel(IGreetingService greetingService, IClipboardService clipboardService)
    {
        _greetingService = greetingService;
        _clipboardService = clipboardService;
        Greeting = _greetingService.Greet("Dependency Injection");

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
            await CloseApplicationInteraction.Handle(Unit.Default).FirstAsync();
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

        // Set initial content for the workspace (optional)
        ActiveWorkspaceContent = null;
    }
}