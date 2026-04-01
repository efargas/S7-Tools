#nullable enable
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using S7Tools.Core.Constants;
using S7Tools.Resources.Strings;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Settings;

/// <summary>
/// ViewModel for the Test Settings category, handling logging test commands and clipboard operations.
/// </summary>
public sealed class TestSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<TestSettingsViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private string _testInputText = UIStrings.TestClipboardText;
    private string _statusMessage = UIStrings.StatusReady;
    private string _lastButtonPressed = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="TestSettingsViewModel"/> class.
    /// </summary>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="logger">The logger instance.</param>
    public TestSettingsViewModel(
        IDialogService dialogService,
        IClipboardService clipboardService,
        ILogger<TestSettingsViewModel> logger)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize commands
        IObservable<bool> canExecuteClipboard = this.WhenAnyValue(
            x => x.TestInputText,
            x => x.SelectionStart,
            x => x.SelectionEnd,
            (text, start, end) =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    return false;
                }
                int minIdx = Math.Min(start, end);
                int maxIdx = Math.Max(start, end);
                int len = maxIdx - minIdx;
                return len > 0 && minIdx >= 0 && maxIdx <= text.Length;
            });

        CutCommand = ReactiveCommand.CreateFromTask(CutAsync, canExecuteClipboard);
        CopyCommand = ReactiveCommand.CreateFromTask(CopyAsync, canExecuteClipboard);
        PasteCommand = ReactiveCommand.CreateFromTask(PasteAsync);

        // Initialize logging test commands
        TestTraceLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Trace));
        TestDebugLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Debug));
        TestInfoLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Information));
        TestWarningLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Warning));
        TestErrorLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Error));
        TestCriticalLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Critical));

        TestStressLogCommand = ReactiveCommand.CreateFromTask(TestStressLogAsync);

        ExportLogsCommand = ReactiveCommand.CreateFromTask(ExportLogsAsync);

        // Set up reactive pattern for button pressed message clearing
        this.WhenAnyValue(x => x.LastButtonPressed)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(_ => Observable.Timer(TimeSpan.FromSeconds(3), RxApp.MainThreadScheduler))
            .Switch()
            .Subscribe(_ =>
            {
                LastButtonPressed = "";
                StatusMessage = UIStrings.StatusReady;
            })
            .DisposeWith(_disposables);

        _logger.LogDebug("TestSettingsViewModel initialized");
    }

    #region Properties

    /// <summary>
    /// Gets or sets the test input text for clipboard operations.
    /// </summary>
    public string TestInputText
    {
        get => _testInputText;
        set => this.RaiseAndSetIfChanged(ref _testInputText, value);
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets the last button pressed message.
    /// </summary>
    public string LastButtonPressed
    {
        get => _lastButtonPressed;
        set => this.RaiseAndSetIfChanged(ref _lastButtonPressed, value);
    }

    /// <summary>
    /// Gets or sets the selection start index.
    /// </summary>
    public int SelectionStart
    {
        get => _selectionStart;
        set => this.RaiseAndSetIfChanged(ref _selectionStart, value);
    }

    /// <summary>
    /// Gets or sets the selection end index.
    /// </summary>
    public int SelectionEnd
    {
        get => _selectionEnd;
        set => this.RaiseAndSetIfChanged(ref _selectionEnd, value);
    }

    /// <summary>
    /// Gets or sets the caret index.
    /// </summary>
    public int CaretIndex
    {
        get => _caretIndex;
        set => this.RaiseAndSetIfChanged(ref _caretIndex, value);
    }

    private int _selectionStart;
    private int _selectionEnd;
    private int _caretIndex;

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> CutCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyCommand { get; }
    public ReactiveCommand<Unit, Unit> PasteCommand { get; }
    public ReactiveCommand<Unit, Unit> TestTraceLogCommand { get; }
    public ReactiveCommand<Unit, Unit> TestDebugLogCommand { get; }
    public ReactiveCommand<Unit, Unit> TestInfoLogCommand { get; }
    public ReactiveCommand<Unit, Unit> TestWarningLogCommand { get; }
    public ReactiveCommand<Unit, Unit> TestErrorLogCommand { get; }
    public ReactiveCommand<Unit, Unit> TestCriticalLogCommand { get; }
    public ReactiveCommand<Unit, Unit> TestStressLogCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportLogsCommand { get; }

    #endregion

    #region Command Implementations

    private async Task CutAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(TestInputText))
            {
                return;
            }

            int start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, TestInputText.Length);
            int length = Math.Clamp(Math.Abs(SelectionStart - SelectionEnd), 0, TestInputText.Length - start);

            if (length == 0)
            {
                _logger.LogWarning("Cut requested but no text is selected");
                StatusMessage = "Please select text to cut";
                return;
            }

            string textToCut = TestInputText.Substring(start, length);
            await _clipboardService.SetTextAsync(textToCut);
            TestInputText = TestInputText.Remove(start, length);
            CaretIndex = start;
            StatusMessage = UIStrings.ClipboardTextCut;

            LastButtonPressed = "Cut";
            _logger.LogInformation("Text cut to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cut text to clipboard");
            await _dialogService.ShowErrorAsync("Clipboard Error", string.Format(UIStrings.Status_ErrorCopyingToClipboard, ex.Message));
        }
    }

    private async Task CopyAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(TestInputText))
            {
                return;
            }

            int start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, TestInputText.Length);
            int length = Math.Clamp(Math.Abs(SelectionStart - SelectionEnd), 0, TestInputText.Length - start);

            if (length == 0)
            {
                _logger.LogWarning("Copy requested but no text is selected");
                StatusMessage = "Please select text to copy";
                return;
            }

            string textToCopy = TestInputText.Substring(start, length);
            await _clipboardService.SetTextAsync(textToCopy);
            StatusMessage = UIStrings.ClipboardTextCopied;

            LastButtonPressed = "Copy";
            _logger.LogInformation("Text copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard");
            await _dialogService.ShowErrorAsync("Clipboard Error", string.Format(UIStrings.Status_ErrorCopyingToClipboard, ex.Message));
        }
    }

    private async Task PasteAsync()
    {
        try
        {
            string? clipboardText = await _clipboardService.GetTextAsync();
            if (string.IsNullOrEmpty(clipboardText))
            {
                return;
            }

            string currentText = TestInputText ?? string.Empty;

            int start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, currentText.Length);
            int length = Math.Clamp(Math.Abs(SelectionStart - SelectionEnd), 0, currentText.Length - start);

            if (length > 0)
            {
                // Replace selection
                TestInputText = currentText.Remove(start, length).Insert(start, clipboardText);
                CaretIndex = start + clipboardText.Length;
            }
            else
            {
                // Insert at caret
                int insertPos = Math.Clamp(CaretIndex, 0, currentText.Length);
                TestInputText = currentText.Insert(insertPos, clipboardText);
                CaretIndex = insertPos + clipboardText.Length;
            }

            StatusMessage = UIStrings.ClipboardTextPasted;
            LastButtonPressed = "Paste";
            _logger.LogInformation("Text pasted from clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to paste text from clipboard");
            await _dialogService.ShowErrorAsync("Clipboard Error", string.Format(UIStrings.Status_ErrorCopyingToClipboard, ex.Message));
        }
    }

    private void TestLogWithLevel(LogLevel logLevel)
    {
        string message = $"Test {logLevel} log message at {DateTime.UtcNow.ToLocalTime():HH:mm:ss}";
        _logger.Log(logLevel, message);
        LastButtonPressed = logLevel.ToString();
    }

    private async Task TestStressLogAsync()
    {
        StatusMessage = "Starting UI Stress Test (5000 logs)...";
        LastButtonPressed = "Stress Test";

        await Task.Run(async () =>
        {
            for (int i = 1; i <= 5000; i++)
            {
                _logger.LogInformation("Stress test message #{Index} at {Time}", i, DateTime.Now.ToString("HH:mm:ss.fff"));
                if (i % 10 == 0)
                {
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
        }).ConfigureAwait(false);

        StatusMessage = "UI Stress Test Complete (5000 logs)";
    }

    private async Task ExportLogsAsync()
    {
        try
        {
            string exportedLogs = $"Log export requested at {DateTime.UtcNow.ToLocalTime().ToString(DateTimeFormats.LongDateTime)}";
            await _clipboardService.SetTextAsync(exportedLogs);
            StatusMessage = UIStrings.Status_LogsExportedToClipboard;
            LastButtonPressed = "Export";
            _logger.LogInformation("Logs exported to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export logs");
            await _dialogService.ShowErrorAsync("Export Error", string.Format(UIStrings.Status_ExportFailed, ex.Message));
        }
    }

    #endregion

    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("TestSettingsViewModel disposed");
    }
}
