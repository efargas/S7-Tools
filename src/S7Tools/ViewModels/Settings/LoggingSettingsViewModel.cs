using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Helpers;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Settings;

/// <summary>
/// ViewModel for logging settings configuration.
/// </summary>
public class LoggingSettingsViewModel : ViewModelBase
{
    private readonly IApplicationSettingsService _settingsService;
    private readonly IPathService _pathService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<LoggingSettingsViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the LoggingSettingsViewModel class.
    /// </summary>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="pathService">The path service for resolving paths.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="logger">The logger.</param>
    public LoggingSettingsViewModel(
        IApplicationSettingsService settingsService,
        IPathService pathService,
        IFileDialogService? fileDialogService,
        ILogger<LoggingSettingsViewModel> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _fileDialogService = fileDialogService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize commands
        BrowseDefaultLogPathCommand = ReactiveCommand.CreateFromTask(BrowseDefaultLogPathAsync);
        BrowseExportPathCommand = ReactiveCommand.CreateFromTask(BrowseExportPathAsync);
        OpenDefaultLogPathCommand = ReactiveCommand.CreateFromTask(OpenDefaultLogPathAsync);
        OpenExportPathCommand = ReactiveCommand.CreateFromTask(OpenExportPathAsync);

        // Initialize properties from current settings
        _isInitializing = true;
        RefreshFromSettings();
        _isInitializing = false;

        // Subscribe to settings changes
        _settingsService.SettingsChanged += (_, _) =>
        {
            _isInitializing = true;
            RefreshFromSettings();
            _isInitializing = false;
        };

        // Auto-save when properties change
        this.PropertyChanged += (s, e) =>
        {
            if (_isInitializing)
            {
                return;
            }

            if (e.PropertyName is nameof(DefaultLogPath) or nameof(ExportPath) or nameof(MinimumLogLevel) or
                nameof(AutoScrollLogs) or nameof(EnableRollingLogs) or nameof(ShowTimestampInLogs) or
                nameof(ShowCategoryInLogs) or nameof(ShowLogLevelInLogs))
            {
                _ = SaveLoggingSettingsAsync();
            }
        };
    }

    private bool _isInitializing;

    #region Properties

    private string _defaultLogPath = string.Empty;
    public string DefaultLogPath
    {
        get => _defaultLogPath;
        set => this.RaiseAndSetIfChanged(ref _defaultLogPath, value);
    }

    private string _exportPath = string.Empty;
    public string ExportPath
    {
        get => _exportPath;
        set => this.RaiseAndSetIfChanged(ref _exportPath, value);
    }

    private string _minimumLogLevel = "Information";
    public string MinimumLogLevel
    {
        get => _minimumLogLevel;
        set => this.RaiseAndSetIfChanged(ref _minimumLogLevel, value);
    }

    /// <summary>
    /// Gets the available log levels for the ComboBox.
    /// </summary>
    public List<string> LogLevels { get; } = new()
    {
        "Trace",
        "Debug",
        "Information",
        "Warning",
        "Error",
        "Critical"
    };

    private bool _autoScrollLogs = true;
    public bool AutoScrollLogs
    {
        get => _autoScrollLogs;
        set => this.RaiseAndSetIfChanged(ref _autoScrollLogs, value);
    }

    private bool _enableRollingLogs = true;
    public bool EnableRollingLogs
    {
        get => _enableRollingLogs;
        set => this.RaiseAndSetIfChanged(ref _enableRollingLogs, value);
    }

    private bool _showTimestampInLogs = true;
    public bool ShowTimestampInLogs
    {
        get => _showTimestampInLogs;
        set => this.RaiseAndSetIfChanged(ref _showTimestampInLogs, value);
    }

    private bool _showCategoryInLogs = true;
    public bool ShowCategoryInLogs
    {
        get => _showCategoryInLogs;
        set => this.RaiseAndSetIfChanged(ref _showCategoryInLogs, value);
    }

    private bool _showLogLevelInLogs = true;
    public bool ShowLogLevelInLogs
    {
        get => _showLogLevelInLogs;
        set => this.RaiseAndSetIfChanged(ref _showLogLevelInLogs, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> BrowseDefaultLogPathCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseExportPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenDefaultLogPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenExportPathCommand { get; }

    #endregion

    #region Private Methods

    private void RefreshFromSettings()
    {
        // Load settings using the new structured approach
        var current = _settingsService.Current;
        DefaultLogPath = current.Logging.LogDirectory;
        ExportPath = current.Logging.ExportDirectory;
        MinimumLogLevel = current.Logging.Level.ToString();
        AutoScrollLogs = current.Ui.AutoScrollLogs;
        EnableRollingLogs = current.Logging.EnableFileLogging;
        ShowTimestampInLogs = current.Ui.ShowTimestampInLogs;
        ShowCategoryInLogs = current.Ui.ShowCategoryInLogs;
        ShowLogLevelInLogs = current.Ui.ShowLogLevelInLogs;
    }

    private async Task BrowseDefaultLogPathAsync()
    {
        if (_fileDialogService == null)
        {
            return;
        }

        try
        {
            string? result = await _fileDialogService.ShowFolderBrowserDialogAsync(UIStrings.Dialog_SelectDefaultLogDirectory, DefaultLogPath);
            if (!string.IsNullOrEmpty(result))
            {
                DefaultLogPath = result;
                _logger.LogInformation("Default log path updated to: {Path}", DefaultLogPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for default log path");
        }
    }

    private async Task BrowseExportPathAsync()
    {
        if (_fileDialogService == null)
        {
            return;
        }

        try
        {
            string? result = await _fileDialogService.ShowFolderBrowserDialogAsync(UIStrings.Dialog_SelectExportDirectory, ExportPath);
            if (!string.IsNullOrEmpty(result))
            {
                ExportPath = result;
                _logger.LogInformation("Export path updated to: {Path}", ExportPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for export path");
        }
    }

    private async Task SaveLoggingSettingsAsync()
    {
        try
        {
            await _settingsService.UpdateSettingsAsync(settings =>
            {
                settings.Logging.LogDirectory = DefaultLogPath;
                settings.Logging.ExportDirectory = ExportPath;
                settings.Logging.Level = MinimumLogLevel;
                settings.Ui.AutoScrollLogs = AutoScrollLogs;
                settings.Logging.EnableFileLogging = EnableRollingLogs;
                settings.Ui.ShowTimestampInLogs = ShowTimestampInLogs;
                settings.Ui.ShowCategoryInLogs = ShowCategoryInLogs;
                settings.Ui.ShowLogLevelInLogs = ShowLogLevelInLogs;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-saving logging settings");
        }
    }

    private async Task OpenDefaultLogPathAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(DefaultLogPath))
            {
                return;
            }

            // Resolve the path through the path service instead of using it directly
            string resolvedPath = _pathService.ResolvePath(DefaultLogPath);

            if (!Directory.Exists(resolvedPath))
            {
                // Try to create the directory if it doesn't exist
                Directory.CreateDirectory(resolvedPath);
            }

            await PlatformHelper.OpenDirectoryInExplorerAsync(resolvedPath);
            _logger.LogInformation("Opened default log path in explorer: {Path}", resolvedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening default log path in explorer");
        }
    }

    private async Task OpenExportPathAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ExportPath))
            {
                return;
            }

            // Resolve the path through the path service instead of using it directly
            string resolvedPath = _pathService.ResolvePath(ExportPath);

            if (!Directory.Exists(resolvedPath))
            {
                // Try to create the directory if it doesn't exist
                Directory.CreateDirectory(resolvedPath);
            }

            await PlatformHelper.OpenDirectoryInExplorerAsync(resolvedPath);
            _logger.LogInformation("Opened export path in explorer: {Path}", resolvedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening export path in explorer");
        }
    }

    #endregion
}
