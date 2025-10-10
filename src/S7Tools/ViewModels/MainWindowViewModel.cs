using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Resources;
using S7Tools.Services;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// Refactored MainWindowViewModel following Single Responsibility Principle.
/// Delegates specific responsibilities to specialized ViewModels.
/// This is the new, clean implementation that replaces the God Object pattern.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<MainWindowViewModel> _logger;

    private string _testInputText = "This is some text to test clipboard operations.";
    private string _statusMessage = "Ready";
    private string _lastButtonPressed = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class for design-time.
    /// </summary>
    public MainWindowViewModel() : this(
        new NavigationViewModel(),
        new BottomPanelViewModel(),
        new SettingsManagementViewModel(),
        new DialogService(),
        new ClipboardService(),
        new Services.SettingsService(),
        null,
        CreateDesignTimeLogger())
    {
    }

    /// <summary>
    /// Creates a design-time logger for the designer.
    /// </summary>
    /// <returns>A logger instance for design-time use.</returns>
    private static ILogger<MainWindowViewModel> CreateDesignTimeLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });
        return loggerFactory.CreateLogger<MainWindowViewModel>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="navigation">The navigation ViewModel.</param>
    /// <param name="bottomPanel">The bottom panel ViewModel.</param>
    /// <param name="settings">The settings management ViewModel.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="logger">The logger instance.</param>
    public MainWindowViewModel(
        NavigationViewModel navigation,
        BottomPanelViewModel bottomPanel,
        SettingsManagementViewModel settings,
        IDialogService dialogService,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        IFileDialogService? fileDialogService,
        ILogger<MainWindowViewModel> logger)
    {
        Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        BottomPanel = bottomPanel ?? throw new ArgumentNullException(nameof(bottomPanel));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _fileDialogService = fileDialogService; // optional in design-time
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize commands
        ExitCommand = ReactiveCommand.CreateFromTask(ExitAsync);
        CutCommand = ReactiveCommand.CreateFromTask(CutAsync);
        CopyCommand = ReactiveCommand.CreateFromTask(CopyAsync);
        PasteCommand = ReactiveCommand.CreateFromTask(PasteAsync);

        // Initialize logging test commands
        TestTraceLogCommand = ReactiveCommand.Create(() => TestLog(LogLevel.Trace, "TRACE"));
        TestDebugLogCommand = ReactiveCommand.Create(() => TestLog(LogLevel.Debug, "DEBUG"));
        TestInfoLogCommand = ReactiveCommand.Create(() => TestLog(LogLevel.Information, "INFO"));
        TestWarningLogCommand = ReactiveCommand.Create(() => TestLog(LogLevel.Warning, "WARNING"));
        TestErrorLogCommand = ReactiveCommand.Create(() => TestLog(LogLevel.Error, "ERROR"));
        TestCriticalLogCommand = ReactiveCommand.Create(() => TestLog(LogLevel.Critical, "CRITICAL"));

        ExportLogsCommand = ReactiveCommand.CreateFromTask(ExportLogsAsync);

        LoadConfigurationCommand = ReactiveCommand.CreateFromTask(LoadConfigurationAsync);
        SaveConfigurationCommand = ReactiveCommand.CreateFromTask(SaveConfigurationAsync);

        CloseApplicationInteraction = new Interaction<Unit, Unit>();

        _logger.LogDebug("MainWindowViewModel initialized with specialized ViewModels");
    }

    #region Specialized ViewModels

    /// <summary>
    /// Gets the navigation ViewModel that handles sidebar and content management.
    /// </summary>
    public NavigationViewModel Navigation { get; }

    /// <summary>
    /// Gets the bottom panel ViewModel that handles tab management and panel visibility.
    /// </summary>
    public BottomPanelViewModel BottomPanel { get; }

    /// <summary>
    /// Gets the settings management ViewModel that handles all settings-related functionality.
    /// </summary>
    public SettingsManagementViewModel Settings { get; }

    #endregion

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
    /// Gets the command to export logs.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportLogsCommand { get; }

    /// <summary>
    /// Command to load configuration from a file via file picker.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadConfigurationCommand { get; }

    /// <summary>
    /// Command to save configuration to a file via file picker.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveConfigurationCommand { get; }

    /// <summary>
    /// Interaction to signal the view to close the application.
    /// </summary>
    public Interaction<Unit, Unit> CloseApplicationInteraction { get; }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Handles application exit with confirmation dialog.
    /// </summary>
    private async Task ExitAsync()
    {
        try
        {
            var result = await _dialogService.ShowConfirmationAsync(
                UIStrings.Dialog_ExitTitle,
                UIStrings.Confirm_Exit);
            if (result)
            {
                await CloseApplicationInteraction.Handle(Unit.Default).FirstAsync();
                _logger.LogInformation("Application exit confirmed by user");
            }
            else
            {
                _logger.LogDebug("Application exit cancelled by user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application exit");
        }
    }

    /// <summary>
    /// Cuts text to clipboard and clears the input.
    /// </summary>
    private async Task CutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(TestInputText))
            {
                await _clipboardService.SetTextAsync(TestInputText);
                TestInputText = string.Empty;
                StatusMessage = "Text cut to clipboard";
                _logger.LogDebug("Text cut to clipboard");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cut text to clipboard");
            StatusMessage = "Failed to cut text";
        }
    }

    /// <summary>
    /// Copies text to clipboard.
    /// </summary>
    private async Task CopyAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(TestInputText))
            {
                await _clipboardService.SetTextAsync(TestInputText);
                StatusMessage = "Text copied to clipboard";
                _logger.LogDebug("Text copied to clipboard");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard");
            StatusMessage = "Failed to copy text";
        }
    }

    /// <summary>
    /// Pastes text from clipboard.
    /// </summary>
    private async Task PasteAsync()
    {
        try
        {
            var text = await _clipboardService.GetTextAsync();
            if (!string.IsNullOrEmpty(text))
            {
                TestInputText += text;
                StatusMessage = "Text pasted from clipboard";
                _logger.LogDebug("Text pasted from clipboard");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to paste text from clipboard");
            StatusMessage = "Failed to paste text";
        }
    }

    /// <summary>
    /// Tests logging at the specified level.
    /// </summary>
    /// <param name="level">The log level to test.</param>
    /// <param name="levelName">The display name of the log level.</param>
    private void TestLog(LogLevel level, string levelName)
    {
        try
        {
            var message = $"This is a {levelName} level log message generated at {DateTime.Now}";

            switch (level)
            {
                case LogLevel.Trace:
                    _logger.LogTrace(message);
                    break;
                case LogLevel.Debug:
                    _logger.LogDebug("{Message} with debug info: {@DebugData}", message, new { UserId = 123, Action = "ButtonClick" });
                    break;
                case LogLevel.Information:
                    _logger.LogInformation("{Message}. User performed action: {Action}", message, $"Test {levelName} Log");
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning("{Message}. Something might need attention: {Warning}", message, "Test warning condition");
                    break;
                case LogLevel.Error:
                    _logger.LogError("{Message}. An error occurred: {Error}", message, "Simulated error for testing");
                    break;
                case LogLevel.Critical:
                    _logger.LogCritical("{Message}. System is in critical state: {CriticalIssue}", message, "Simulated critical issue");
                    break;
            }

            LastButtonPressed = $"{levelName} Log Generated";
            StatusMessage = $"Generated {levelName} log message";
            ClearButtonPressedAfterDelay();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {LogLevel} log message", levelName);
            StatusMessage = $"Failed to generate {levelName} log";
        }
    }

    /// <summary>
    /// Exports logs to clipboard.
    /// </summary>
    private async Task ExportLogsAsync()
    {
        try
        {
            var exportText = "Log Export - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await _clipboardService.SetTextAsync(exportText);
            StatusMessage = "Log export copied to clipboard";
            _logger.LogInformation("Log export copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export logs to clipboard");
            StatusMessage = "Failed to export logs";
        }
    }

    /// <summary>
    /// Opens a file picker and loads application settings from the selected file.
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
        try
        {
            var path = await (_fileDialogService?.ShowOpenFileDialogAsync("Load Configuration", "JSON (*.json)|*.json|All files (*.*)|*.*") ?? Task.FromResult<string?>(null));
            if (!string.IsNullOrWhiteSpace(path))
            {
                await _settingsService.LoadSettingsAsync(path);
                StatusMessage = $"Loaded configuration: {System.IO.Path.GetFileName(path)}";
                _logger.LogInformation("Configuration loaded from {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            StatusMessage = "Failed to load configuration";
        }
    }

    /// <summary>
    /// Opens a file picker and saves application settings to the selected file.
    /// </summary>
    private async Task SaveConfigurationAsync()
    {
        try
        {
            var defaultName = "settings.json";
            var path = await (_fileDialogService?.ShowSaveFileDialogAsync("Save Configuration", "JSON (*.json)|*.json|All files (*.*)|*.*", defaultFileName: defaultName) ?? Task.FromResult<string?>(null));
            if (!string.IsNullOrWhiteSpace(path))
            {
                await _settingsService.SaveSettingsAsync(path);
                StatusMessage = $"Saved configuration: {System.IO.Path.GetFileName(path)}";
                _logger.LogInformation("Configuration saved to {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            StatusMessage = "Failed to save configuration";
        }
    }

    /// <summary>
    /// Clears the button pressed message after a delay.
    /// </summary>
    private async void ClearButtonPressedAfterDelay()
    {
        await Task.Delay(3000); // Clear after 3 seconds
        LastButtonPressed = "";
        StatusMessage = "Ready";
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Navigates to the specified view model type.
    /// Delegates to the Navigation ViewModel.
    /// </summary>
    /// <param name="viewModelType">The view model type.</param>
    public void NavigateTo(Type viewModelType)
    {
        Navigation.NavigateTo(viewModelType);
    }

    #endregion
}
