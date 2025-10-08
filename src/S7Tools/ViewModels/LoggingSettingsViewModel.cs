using ReactiveUI;
using S7Tools.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Threading.Tasks;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for logging settings configuration.
/// </summary>
public class LoggingSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<LoggingSettingsViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the LoggingSettingsViewModel class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="logger">The logger.</param>
    public LoggingSettingsViewModel(
        ISettingsService settingsService,
        IFileDialogService? fileDialogService,
        ILogger<LoggingSettingsViewModel> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
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

    private string _settingsStatusMessage = "Ready";
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
        var settings = _settingsService.Settings;
        DefaultLogPath = settings.Logging.DefaultLogPath;
        ExportPath = settings.Logging.ExportPath;
        MinimumLogLevel = settings.Logging.MinimumLogLevel.ToString();
        AutoScrollLogs = settings.Logging.AutoScroll;
        EnableRollingLogs = settings.Logging.EnableFileLogging;
        ShowTimestampInLogs = settings.Logging.ShowTimestamp;
        ShowCategoryInLogs = settings.Logging.ShowCategory;
        ShowLogLevelInLogs = settings.Logging.ShowLevel;
        CurrentSettingsFilePath = _settingsService.GetDefaultSettingsPath();
        
        try
        {
            var fileInfo = new FileInfo(CurrentSettingsFilePath);
            SettingsLastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now;
        }
        catch
        {
            SettingsLastModified = DateTime.Now;
        }
    }

    private async Task BrowseDefaultLogPathAsync()
    {
        if (_fileDialogService == null) return;

        try
        {
            var result = await _fileDialogService.ShowFolderBrowserDialogAsync("Select Default Log Directory", DefaultLogPath);
            if (!string.IsNullOrEmpty(result))
            {
                DefaultLogPath = result;
                await UpdateSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for default log path");
            SettingsStatusMessage = "Error selecting directory";
        }
    }

    private async Task BrowseExportPathAsync()
    {
        if (_fileDialogService == null) return;

        try
        {
            var result = await _fileDialogService.ShowFolderBrowserDialogAsync("Select Export Directory", ExportPath);
            if (!string.IsNullOrEmpty(result))
            {
                ExportPath = result;
                await UpdateSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for export path");
            SettingsStatusMessage = "Error selecting directory";
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = "Saving settings...";
            await UpdateSettingsAsync();
            await _settingsService.SaveSettingsAsync();
            SettingsStatusMessage = "Settings saved successfully";
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            SettingsStatusMessage = "Error saving settings";
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = "Loading settings...";
            await _settingsService.LoadSettingsAsync();
            RefreshFromSettings();
            SettingsStatusMessage = "Settings loaded successfully";
            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
            SettingsStatusMessage = "Error loading settings";
        }
    }

    private async Task ResetSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = "Resetting to defaults...";
            await _settingsService.ResetToDefaultsAsync();
            RefreshFromSettings();
            SettingsStatusMessage = "Settings reset to defaults";
            _logger.LogInformation("Settings reset to defaults");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings");
            SettingsStatusMessage = "Error resetting settings";
        }
    }

    private async Task OpenSettingsFolderAsync()
    {
        try
        {
            await _settingsService.OpenSettingsDirectoryAsync();
            _logger.LogInformation("Opened settings directory");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings directory");
            SettingsStatusMessage = "Error opening settings directory";
        }
    }

    private async Task OpenDefaultLogPathAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(DefaultLogPath))
            {
                SettingsStatusMessage = "Default log path is not set";
                return;
            }

            if (!Directory.Exists(DefaultLogPath))
            {
                // Try to create the directory if it doesn't exist
                Directory.CreateDirectory(DefaultLogPath);
            }

            await OpenDirectoryInExplorerAsync(DefaultLogPath);
            _logger.LogInformation("Opened default log path in explorer: {Path}", DefaultLogPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening default log path in explorer");
            SettingsStatusMessage = "Error opening default log path";
        }
    }

    private async Task OpenExportPathAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ExportPath))
            {
                SettingsStatusMessage = "Export path is not set";
                return;
            }

            if (!Directory.Exists(ExportPath))
            {
                // Try to create the directory if it doesn't exist
                Directory.CreateDirectory(ExportPath);
            }

            await OpenDirectoryInExplorerAsync(ExportPath);
            _logger.LogInformation("Opened export path in explorer: {Path}", ExportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening export path in explorer");
            SettingsStatusMessage = "Error opening export path";
        }
    }

    private static async Task OpenDirectoryInExplorerAsync(string path)
    {
        await Task.Run(() =>
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", path);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("open", path);
                }
                else
                {
                    throw new PlatformNotSupportedException("Opening directories in explorer is not supported on this platform");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open directory in explorer: {path}", ex);
            }
        });
    }

    private async Task UpdateSettingsAsync()
    {
        var settings = _settingsService.Settings.Clone();
        settings.Logging.DefaultLogPath = DefaultLogPath;
        settings.Logging.ExportPath = ExportPath;
        
        if (Enum.TryParse<LogLevel>(MinimumLogLevel, out var logLevel))
        {
            settings.Logging.MinimumLogLevel = logLevel;
        }
        
        settings.Logging.AutoScroll = AutoScrollLogs;
        settings.Logging.EnableFileLogging = EnableRollingLogs;
        settings.Logging.ShowTimestamp = ShowTimestampInLogs;
        settings.Logging.ShowCategory = ShowCategoryInLogs;
        settings.Logging.ShowLevel = ShowLogLevelInLogs;

        await _settingsService.UpdateSettingsAsync(settings);
    }

    #endregion
}