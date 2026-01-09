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
        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
        LoadSettingsCommand = ReactiveCommand.CreateFromTask(LoadSettingsAsync);
        ResetSettingsCommand = ReactiveCommand.CreateFromTask(ResetSettingsAsync);
        OpenSettingsFolderCommand = ReactiveCommand.CreateFromTask(OpenSettingsFolderAsync);

        // Initialize properties from current settings
        RefreshFromSettings();

        // Subscribe to settings changes
        _settingsService.SettingsChanged += (_, _) => RefreshFromSettings();
    }

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

    private string _settingsStatusMessage = UIStrings.Status_SettingsReady;
    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _settingsStatusMessage, value);
    }

    private string _currentSettingsFilePath = string.Empty;
    public string CurrentSettingsFilePath
    {
        get => _currentSettingsFilePath;
        set => this.RaiseAndSetIfChanged(ref _currentSettingsFilePath, value);
    }

    private DateTime _settingsLastModified = DateTime.Now;
    public DateTime SettingsLastModified
    {
        get => _settingsLastModified;
        set => this.RaiseAndSetIfChanged(ref _settingsLastModified, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> BrowseDefaultLogPathCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseExportPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenDefaultLogPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenExportPathCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsFolderCommand { get; }

    #endregion

    #region Private Methods

    private void RefreshFromSettings()
    {
        // Load settings using the new structured approach
        DefaultLogPath = _settingsService.GetSetting<string>("logging.logDirectory", "Resources/Logs/Main");
        ExportPath = _settingsService.GetSetting<string>("logging.exportDirectory", "Resources/Logs/Exported");
        MinimumLogLevel = _settingsService.GetSetting<string>("logging.level", "Information");
        AutoScrollLogs = _settingsService.GetSetting<bool>("ui.autoScrollLogs", true);
        EnableRollingLogs = _settingsService.GetSetting<bool>("logging.enableFileLogging", true);
        ShowTimestampInLogs = _settingsService.GetSetting<bool>("ui.showTimestampInLogs", true);
        ShowCategoryInLogs = _settingsService.GetSetting<bool>("ui.showCategoryInLogs", true);
        ShowLogLevelInLogs = _settingsService.GetSetting<bool>("ui.showLogLevelInLogs", true);

        // Use the AppSettingsPath from the path service
        CurrentSettingsFilePath = _pathService.AppSettingsPath;

        try
        {
            var fileInfo = new System.IO.FileInfo(CurrentSettingsFilePath);
            SettingsLastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now;
        }
        catch
        {
            SettingsLastModified = DateTime.Now;
        }
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
            SettingsStatusMessage = UIStrings.Status_ErrorSelectingDirectory;
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
            SettingsStatusMessage = UIStrings.Status_ErrorSelectingDirectory;
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = UIStrings.Status_SavingSettings;

            // Create dictionary of settings to save
            var userSettings = new Dictionary<string, object>
            {
                ["logging.logDirectory"] = DefaultLogPath,
                ["logging.exportDirectory"] = ExportPath,
                ["logging.level"] = MinimumLogLevel,
                ["ui.autoScrollLogs"] = AutoScrollLogs,
                ["logging.enableFileLogging"] = EnableRollingLogs,
                ["ui.showTimestampInLogs"] = ShowTimestampInLogs,
                ["ui.showCategoryInLogs"] = ShowCategoryInLogs,
                ["ui.showLogLevelInLogs"] = ShowLogLevelInLogs
            };

            await _settingsService.SaveUserSettingsAsync(userSettings);
            SettingsStatusMessage = UIStrings.Status_SettingsSavedSuccessfully;
            _logger.LogInformation("Logging settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving logging settings");
            SettingsStatusMessage = UIStrings.Status_ErrorSavingSettings;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = UIStrings.Status_LoadingSettings;
            await _settingsService.LoadSettingsAsync();
            RefreshFromSettings();
            SettingsStatusMessage = UIStrings.Status_SettingsLoadedSuccessfully;
            _logger.LogInformation("Logging settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading logging settings");
            SettingsStatusMessage = UIStrings.Status_ErrorLoadingSettings;
        }
    }

    private async Task ResetSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = UIStrings.Status_ResettingToDefaults;
            await _settingsService.RestoreDefaultsAsync();
            RefreshFromSettings();
            SettingsStatusMessage = UIStrings.Status_SettingsResetToDefaults;
            _logger.LogInformation("Logging settings reset to defaults successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting logging settings to defaults");
            SettingsStatusMessage = UIStrings.Status_ErrorResettingSettings;
        }
    }

    private async Task OpenSettingsFolderAsync()
    {
        try
        {
            string settingsDir = Path.GetDirectoryName(CurrentSettingsFilePath) ?? _pathService.AppSettingsPath;

            // Resolve the path through the path service instead of using it directly
            string resolvedPath = _pathService.ResolvePath(settingsDir);

            if (!Directory.Exists(resolvedPath))
            {
                Directory.CreateDirectory(resolvedPath);
            }

            await PlatformHelper.OpenDirectoryInExplorerAsync(resolvedPath);
            _logger.LogInformation("Opened settings directory in explorer: {Path}", resolvedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings directory");
            SettingsStatusMessage = UIStrings.Status_ErrorOpeningSettingsDirectory;
        }
    }

    private async Task OpenDefaultLogPathAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(DefaultLogPath))
            {
                SettingsStatusMessage = UIStrings.Status_DefaultLogPathNotSet;
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
            SettingsStatusMessage = UIStrings.Status_ErrorOpeningDefaultLogPath;
        }
    }

    private async Task OpenExportPathAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ExportPath))
            {
                SettingsStatusMessage = UIStrings.Status_ExportPathNotSet;
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
            SettingsStatusMessage = UIStrings.Status_ErrorOpeningExportPath;
        }
    }

    #endregion
}
