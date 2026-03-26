using S7Tools.ViewModels.Base;
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
                nameof(ShowCategoryInLogs) or nameof(ShowLogLevelInLogs) or nameof(LogViewerFontSize) or
                nameof(MaxLogFileSizeMb) or nameof(MaxRetainedLogFiles) or
                nameof(LogProfileOperations) or nameof(LogModbusOperations) or
                nameof(LogConnectionStateChanges) or nameof(LogPowerStateChanges))
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

    private double _logViewerFontSize = 12.0;
    public double LogViewerFontSize
    {
        get => _logViewerFontSize;
        set => this.RaiseAndSetIfChanged(ref _logViewerFontSize, value);
    }

    /// <summary>
    /// Gets available font sizes for the log viewer.
    /// </summary>
    public List<double> AvailableFontSizes { get; } = new()
    {
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        18,
        20,
        22,
        24
    };

    private int _maxLogFileSizeMb = 10;
    /// <summary>
    /// Gets the maximum log file size in MB before rotation.
    /// </summary>
    public int MaxLogFileSizeMb
    {
        get => _maxLogFileSizeMb;
        set => this.RaiseAndSetIfChanged(ref _maxLogFileSizeMb, value);
    }

    private int _maxRetainedLogFiles = 5;
    /// <summary>
    /// Gets the maximum number of rotated log files to retain.
    /// </summary>
    public int MaxRetainedLogFiles
    {
        get => _maxRetainedLogFiles;
        set => this.RaiseAndSetIfChanged(ref _maxRetainedLogFiles, value);
    }

    private bool _logProfileOperations = true;
    public bool LogProfileOperations
    {
        get => _logProfileOperations;
        set => this.RaiseAndSetIfChanged(ref _logProfileOperations, value);
    }

    private bool _logModbusOperations;
    public bool LogModbusOperations
    {
        get => _logModbusOperations;
        set => this.RaiseAndSetIfChanged(ref _logModbusOperations, value);
    }

    private bool _logConnectionStateChanges = true;
    public bool LogConnectionStateChanges
    {
        get => _logConnectionStateChanges;
        set => this.RaiseAndSetIfChanged(ref _logConnectionStateChanges, value);
    }

    private bool _logPowerStateChanges = true;
    public bool LogPowerStateChanges
    {
        get => _logPowerStateChanges;
        set => this.RaiseAndSetIfChanged(ref _logPowerStateChanges, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets or sets the BrowseDefaultLogPathCommand.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseDefaultLogPathCommand { get; }
    /// <summary>
    /// Gets or sets the BrowseExportPathCommand.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseExportPathCommand { get; }
    /// <summary>
    /// Gets or sets the OpenDefaultLogPathCommand.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenDefaultLogPathCommand { get; }
    /// <summary>
    /// Gets or sets the OpenExportPathCommand.
    /// </summary>
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
        LogViewerFontSize = current.Ui.LogViewerFontSize;
        MaxLogFileSizeMb = (int)(current.Logging.MaxFileSize / 1024 / 1024);
        MaxRetainedLogFiles = current.Logging.MaxFiles;
        
        // Detailed Operation Logging
        LogProfileOperations = current.MemoryRegion.LogProfileOperations;
        LogModbusOperations = current.PowerSupply.LogModbusOperations;
        LogConnectionStateChanges = current.PowerSupply.LogConnectionStateChanges;
        LogPowerStateChanges = current.PowerSupply.LogPowerStateChanges;
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
                settings.Ui.LogViewerFontSize = LogViewerFontSize;
                
                // Serilog/File Logging
                settings.Logging.MaxFileSize = (long)MaxLogFileSizeMb * 1024 * 1024;
                settings.Logging.MaxFiles = MaxRetainedLogFiles;
                
                // Detailed Operation Logging
                settings.MemoryRegion.LogProfileOperations = LogProfileOperations;
                settings.PowerSupply.LogModbusOperations = LogModbusOperations;
                settings.PowerSupply.LogConnectionStateChanges = LogConnectionStateChanges;
                settings.PowerSupply.LogPowerStateChanges = LogPowerStateChanges;
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