using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// ViewModel for the Logging Test page, handling logging test commands and clipboard operations.
/// Follows Single Responsibility Principle by focusing only on logging and clipboard testing.
/// </summary>
public sealed class LoggingTestViewModel : ViewModelBase, IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;
    private readonly ILogger<LoggingTestViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private string _testInputText = UIStrings.TestClipboardText;
    private string _statusMessage = UIStrings.StatusReady;
    private string _lastButtonPressed = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingTestViewModel"/> class for design-time.
    /// </summary>
    public LoggingTestViewModel() : this(
        new DesignTimeDialogService(),
        new DesignTimeClipboardService(),
        CreateDesignTimeLogger())
    {
    }

    /// <summary>
    /// Creates a design-time logger for the designer.
    /// </summary>
    /// <returns>A logger instance for design-time use.</returns>
    private static ILogger<LoggingTestViewModel> CreateDesignTimeLogger()
    {
        return Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingTestViewModel>.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingTestViewModel"/> class.
    /// </summary>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="logger">The logger instance.</param>
    public LoggingTestViewModel(
        IDialogService dialogService,
        IClipboardService clipboardService,
        ILogger<LoggingTestViewModel> logger)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize commands
        CutCommand = ReactiveCommand.CreateFromTask(CutAsync);
        CopyCommand = ReactiveCommand.CreateFromTask(CopyAsync);
        PasteCommand = ReactiveCommand.CreateFromTask(PasteAsync);

        // Initialize the unified logging test command
        TestLogCommand = ReactiveCommand.Create<LogLevel>(TestLogWithLevel);

        // Initialize individual logging test commands for backward compatibility
        // These now delegate to the unified command, eliminating code duplication
        TestTraceLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Trace));
        TestDebugLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Debug));
        TestInfoLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Information));
        TestWarningLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Warning));
        TestErrorLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Error));
        TestCriticalLogCommand = ReactiveCommand.Create(() => TestLogWithLevel(LogLevel.Critical));

        ExportLogsCommand = ReactiveCommand.CreateFromTask(ExportLogsAsync);

        // Set up reactive pattern for button pressed message clearing
        this.WhenAnyValue(x => x.LastButtonPressed)
            .Where(name => !string.IsNullOrEmpty(name))
            .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(3)))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                LastButtonPressed = "";
                StatusMessage = UIStrings.StatusReady;
            })
            .DisposeWith(_disposables);

        _logger.LogDebug("LoggingTestViewModel initialized");
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

    #endregion

    #region Commands

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
    /// Gets the command to test logging with a specific log level.
    /// </summary>
    public ReactiveCommand<LogLevel, Unit> TestLogCommand { get; }

    /// <summary>
    /// Gets the command to test trace logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestTraceLogCommand { get; }

    /// <summary>
    /// Gets the command to test debug logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestDebugLogCommand { get; }

    /// <summary>
    /// Gets the command to test information logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestInfoLogCommand { get; }

    /// <summary>
    /// Gets the command to test warning logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestWarningLogCommand { get; }

    /// <summary>
    /// Gets the command to test error logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestErrorLogCommand { get; }

    /// <summary>
    /// Gets the command to test critical logging.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestCriticalLogCommand { get; }

    /// <summary>
    /// Gets the command to export logs to clipboard.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportLogsCommand { get; }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Cuts the test input text to the clipboard.
    /// </summary>
    private async Task CutAsync()
    {
        try
        {
            await _clipboardService.SetTextAsync(TestInputText);
            TestInputText = string.Empty;
            StatusMessage = UIStrings.ClipboardTextCut;
            LastButtonPressed = "Cut";
            _logger.LogInformation("Text cut to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cut text to clipboard");
            await _dialogService.ShowErrorAsync("Clipboard Error", $"Failed to cut text: {ex.Message}");
        }
    }

    /// <summary>
    /// Copies the test input text to the clipboard.
    /// </summary>
    private async Task CopyAsync()
    {
        try
        {
            await _clipboardService.SetTextAsync(TestInputText);
            StatusMessage = UIStrings.ClipboardTextCopied;
            LastButtonPressed = "Copy";
            _logger.LogInformation("Text copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard");
            await _dialogService.ShowErrorAsync("Clipboard Error", $"Failed to copy text: {ex.Message}");
        }
    }

    /// <summary>
    /// Pastes text from the clipboard to the test input.
    /// </summary>
    private async Task PasteAsync()
    {
        try
        {
            string? clipboardText = await _clipboardService.GetTextAsync();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                TestInputText = clipboardText;
                StatusMessage = UIStrings.ClipboardTextPasted;
                LastButtonPressed = "Paste";
                _logger.LogInformation("Text pasted from clipboard");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to paste text from clipboard");
            await _dialogService.ShowErrorAsync("Clipboard Error", $"Failed to paste text: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests logging with the specified log level.
    /// This unified method eliminates code duplication across different log level commands.
    /// </summary>
    /// <param name="logLevel">The log level to test.</param>
    private void TestLogWithLevel(LogLevel logLevel)
    {
        string message = $"Test {logLevel} log message at {DateTime.Now:HH:mm:ss}";


        _logger.Log(logLevel, message);

        LastButtonPressed = logLevel.ToString();
    }

    /// <summary>
    /// Exports logs to the clipboard.
    /// </summary>
    private async Task ExportLogsAsync()
    {
        try
        {
            // For now, just show a message. Full implementation would export actual logs.
            string exportedLogs = $"Log export requested at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            await _clipboardService.SetTextAsync(exportedLogs);
            StatusMessage = "Logs exported to clipboard";
            LastButtonPressed = "Export";
            _logger.LogInformation("Logs exported to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export logs");
            await _dialogService.ShowErrorAsync("Export Error", $"Failed to export logs: {ex.Message}");
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the ViewModel and releases resources.
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
        _logger.LogDebug("LoggingTestViewModel disposed");
    }

    #endregion
}
