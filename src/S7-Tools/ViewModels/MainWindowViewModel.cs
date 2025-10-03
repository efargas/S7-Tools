using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using System;
using System.Reactive.Linq; // Added for FirstAsync()
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;

namespace S7_Tools.ViewModels;

public partial class MainWindowViewModel : ReactiveObject
{
    private bool _isLeftPanelOpen = true;
    public bool IsLeftPanelOpen
    {
        get => _isLeftPanelOpen;
        set => this.RaiseAndSetIfChanged(ref _isLeftPanelOpen, value);
    }

    private SplitViewDisplayMode _leftPanelDisplayMode = SplitViewDisplayMode.Inline;
    public SplitViewDisplayMode LeftPanelDisplayMode
    {
        get => _leftPanelDisplayMode;
        set => this.RaiseAndSetIfChanged(ref _leftPanelDisplayMode, value);
    }

    private GridLength _bottomPanelGridLength = new GridLength(200, GridUnitType.Pixel);
    public GridLength BottomPanelGridLength
    {
        get => _bottomPanelGridLength;
        set => this.RaiseAndSetIfChanged(ref _bottomPanelGridLength, value);
    }

    private string _testInputText = "This is some text to test clipboard operations.";
    public string TestInputText
    {
        get => _testInputText;
        set => this.RaiseAndSetIfChanged(ref _testInputText, value);
    }

    private object? _activeWorkspaceContent;
    public object? ActiveWorkspaceContent
    {
        get => _activeWorkspaceContent;
        set => this.RaiseAndSetIfChanged(ref _activeWorkspaceContent, value);
    }

    public ReactiveCommand<Unit, Unit> ToggleLeftPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleBottomPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> CutCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> PasteCommand { get; }

    public Interaction<Unit, Unit> CloseApplicationInteraction { get; }

    public MainWindowViewModel()
    {
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
                var clipboard = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(TestInputText);
                    TestInputText = string.Empty;
                }
            }
        });

        CopyCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (!string.IsNullOrEmpty(TestInputText))
            {
                var clipboard = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(TestInputText);
                }
            }
        });

        PasteCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var clipboard = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                var text = await clipboard.GetTextAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    TestInputText += text;
                }
            }
        });

        // Set initial content for the workspace (optional)
        ActiveWorkspaceContent = null;
    }
}
