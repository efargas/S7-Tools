using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Models;
using S7Tools.Services;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for managing application settings and configuration.
/// Handles all settings-related properties, commands, and persistence.
/// </summary>
public class SettingsManagementViewModel : ReactiveObject
{
    private readonly ILogger<SettingsManagementViewModel> _logger;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ISettingsService _settingsService;

    // Settings Properties
    private string _defaultLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "S7Tools", "Logs");
    private string _exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "S7Tools", "Exports");
    private string _minimumLogLevel = "Information";
    private bool _autoScrollLogs = true;
    private bool _enableRollingLogs = true;
    private bool _showTimestampInLogs = true;
    private bool _showCategoryInLogs = true;
    private bool _showLogLevelInLogs = true;
    private string _settingsStatusMessage = "Settings ready";
    private string _currentSettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "S7Tools", "settings.json");
    private DateTime _settingsLastModified = DateTime.Now;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsManagementViewModel"/> class for design-time.
    /// </summary>
    public SettingsManagementViewModel() : this(CreateDesignTimeLogger(), CreateDesignTimeSettingsService())
    {
    }

    /// <summary>
    /// Creates a design-time logger for the designer.
    /// </summary>
    /// <returns>A logger instance for design-time use.</returns>
    private static ILogger<SettingsManagementViewModel> CreateDesignTimeLogger()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { });
        return loggerFactory.CreateLogger<SettingsManagementViewModel>();
    }

    /// <summary>
    /// Creates a design-time settings service for the designer.
    /// </summary>
    /// <returns>A settings service instance for design-time use.</returns>
    private static ISettingsService CreateDesignTimeSettingsService()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { });
        ILogger<SettingsService> logger = loggerFactory.CreateLogger<Services.SettingsService>();
        return new Services.SettingsService(logger);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsManagementViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="fileDialogService">The file dialog service (optional).</param>
    public SettingsManagementViewModel(
        ILogger<SettingsManagementViewModel> logger,
        ISettingsService settingsService,
        IFileDialogService? fileDialogService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _fileDialogService = fileDialogService;

        // Initialize commands
        BrowseDefaultLogPathCommand = ReactiveCommand.CreateFromTask(BrowseDefaultLogPathAsync);
        BrowseExportPathCommand = ReactiveCommand.CreateFromTask(BrowseExportPathAsync);
        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
        LoadSettingsCommand = ReactiveCommand.CreateFromTask(LoadSettingsAsync);
        ResetSettingsCommand = ReactiveCommand.CreateFromTask(ResetSettingsAsync);
        OpenSettingsFolderCommand = ReactiveCommand.CreateFromTask(OpenSettingsFolderAsync);

        // Load current settings from service
        LoadSettingsFromService();

        _logger.LogDebug("SettingsManagementViewModel initialized");
    }

    #region Properties

    /// <summary>
    /// Gets or sets the default log path.
    /// </summary>
    public string DefaultLogPath
    {
        get => _defaultLogPath;
        set => this.RaiseAndSetIfChanged(ref _defaultLogPath, value);
    }

    /// <summary>
    /// Gets or sets the export path.
    /// </summary>
    public string ExportPath
    {
        get => _exportPath;
        set => this.RaiseAndSetIfChanged(ref _exportPath, value);
    }

    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public string MinimumLogLevel
    {
        get => _minimumLogLevel;
        set => this.RaiseAndSetIfChanged(ref _minimumLogLevel, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-scroll logs.
    /// </summary>
    public bool AutoScrollLogs
    {
        get => _autoScrollLogs;
        set => this.RaiseAndSetIfChanged(ref _autoScrollLogs, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to enable rolling log files.
    /// </summary>
    public bool EnableRollingLogs
    {
        get => _enableRollingLogs;
        set => this.RaiseAndSetIfChanged(ref _enableRollingLogs, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show timestamp in logs.
    /// </summary>
    public bool ShowTimestampInLogs
    {
        get => _showTimestampInLogs;
        set => this.RaiseAndSetIfChanged(ref _showTimestampInLogs, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show category in logs.
    /// </summary>
    public bool ShowCategoryInLogs
    {
        get => _showCategoryInLogs;
        set => this.RaiseAndSetIfChanged(ref _showCategoryInLogs, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show log level in logs.
    /// </summary>
    public bool ShowLogLevelInLogs
    {
        get => _showLogLevelInLogs;
        set => this.RaiseAndSetIfChanged(ref _showLogLevelInLogs, value);
    }

    /// <summary>
    /// Gets or sets the settings status message.
    /// </summary>
    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _settingsStatusMessage, value);
    }

    /// <summary>
    /// Gets or sets the current settings file path.
    /// </summary>
    public string CurrentSettingsFilePath
    {
        get => _currentSettingsFilePath;
        set => this.RaiseAndSetIfChanged(ref _currentSettingsFilePath, value);
    }

    /// <summary>
    /// Gets or sets the settings last modified date.
    /// </summary>
    public DateTime SettingsLastModified
    {
        get => _settingsLastModified;
        set => this.RaiseAndSetIfChanged(ref _settingsLastModified, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to browse for default log path.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseDefaultLogPathCommand { get; }

    /// <summary>
    /// Gets the command to browse for export path.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseExportPathCommand { get; }

    /// <summary>
    /// Gets the command to save settings.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

    /// <summary>
    /// Gets the command to load settings.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadSettingsCommand { get; }

    /// <summary>
    /// Gets the command to reset settings to defaults.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand { get; }

    /// <summary>
    /// Gets the command to open settings folder.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSettingsFolderCommand { get; }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Browses for the default log path.
    /// </summary>
    private async Task BrowseDefaultLogPathAsync()
    {
        try
        {
            if (_fileDialogService != null)
            {
                string? selectedPath = await _fileDialogService.ShowFolderBrowserDialogAsync(
                    "Select Default Log Path",
                    DefaultLogPath);

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    DefaultLogPath = selectedPath;
                    SettingsStatusMessage = "Default log path updated successfully";
                    _logger.LogInformation("Default log path updated to: {Path}", selectedPath);
                }
                else
                {
                    SettingsStatusMessage = "Folder selection cancelled";
                }
            }
            else
            {
                SettingsStatusMessage = "File dialog service not available";
                _logger.LogWarning("File dialog service not available for default log path selection");
            }
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Failed to browse for default log path: {ex.Message}";
            _logger.LogError(ex, "Failed to browse for default log path");
        }
    }

    /// <summary>
    /// Browses for the export path.
    /// </summary>
    private async Task BrowseExportPathAsync()
    {
        try
        {
            if (_fileDialogService != null)
            {
                string? selectedPath = await _fileDialogService.ShowFolderBrowserDialogAsync(
                    "Select Export Path",
                    ExportPath);

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    ExportPath = selectedPath;
                    SettingsStatusMessage = "Export path updated successfully";
                    _logger.LogInformation("Export path updated to: {Path}", selectedPath);
                }
                else
                {
                    SettingsStatusMessage = "Folder selection cancelled";
                }
            }
            else
            {
                SettingsStatusMessage = "File dialog service not available";
                _logger.LogWarning("File dialog service not available for export path selection");
            }
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Failed to browse for export path: {ex.Message}";
            _logger.LogError(ex, "Failed to browse for export path");
        }
    }

    /// <summary>
    /// Loads settings from the service into the ViewModel properties.
    /// </summary>
    private void LoadSettingsFromService()
    {
        try
        {
            ApplicationSettings settings = _settingsService.Settings;

            // Map settings to ViewModel properties
            DefaultLogPath = settings.Logging.DefaultLogPath;
            ExportPath = settings.Logging.ExportPath;
            MinimumLogLevel = settings.Logging.MinimumLogLevel.ToString();
            AutoScrollLogs = settings.Logging.AutoScroll;
            EnableRollingLogs = settings.Logging.EnableFileLogging;
            ShowTimestampInLogs = settings.Logging.ShowTimestamp;
            ShowCategoryInLogs = settings.Logging.ShowCategory;
            ShowLogLevelInLogs = settings.Logging.ShowLevel;

            CurrentSettingsFilePath = _settingsService.GetDefaultSettingsPath();

            _logger.LogDebug("Settings loaded from service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from service");
        }
    }

    /// <summary>
    /// Saves the current ViewModel properties to settings.
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            // Get current settings from service
            ApplicationSettings settings = _settingsService.Settings.Clone();

            // Update settings from ViewModel properties
            settings.Logging.DefaultLogPath = DefaultLogPath;
            settings.Logging.ExportPath = ExportPath;
            settings.Logging.MinimumLogLevel = Enum.TryParse<LogLevel>(MinimumLogLevel, out LogLevel level)
                ? level
                : LogLevel.Information;
            settings.Logging.AutoScroll = AutoScrollLogs;
            settings.Logging.EnableFileLogging = EnableRollingLogs;
            settings.Logging.ShowTimestamp = ShowTimestampInLogs;
            settings.Logging.ShowCategory = ShowCategoryInLogs;
            settings.Logging.ShowLevel = ShowLogLevelInLogs;

            // Save settings using the service
            await _settingsService.UpdateSettingsAsync(settings);

            SettingsStatusMessage = "Settings saved successfully";
            SettingsLastModified = DateTime.Now;
            _logger.LogInformation("Settings saved to {Path}", CurrentSettingsFilePath);
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Failed to save settings: {ex.Message}";
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    /// <summary>
    /// Loads settings from file.
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            // Load settings from file using the service
            await _settingsService.LoadSettingsAsync();

            // Update ViewModel properties from loaded settings
            LoadSettingsFromService();

            SettingsStatusMessage = "Settings loaded successfully";
            SettingsLastModified = DateTime.Now;
            _logger.LogInformation("Settings loaded from {Path}", CurrentSettingsFilePath);
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Failed to load settings: {ex.Message}";
            _logger.LogError(ex, "Failed to load settings");
        }
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    private async Task ResetSettingsAsync()
    {
        try
        {
            // Reset to defaults using the service
            await _settingsService.ResetToDefaultsAsync();

            // Update ViewModel properties from reset settings
            LoadSettingsFromService();

            SettingsStatusMessage = "Settings reset to defaults";
            _logger.LogInformation("Settings reset to default values");
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Failed to reset settings: {ex.Message}";
            _logger.LogError(ex, "Failed to reset settings");
        }
    }

    /// <summary>
    /// Opens the settings folder in the file explorer.
    /// </summary>
    private async Task OpenSettingsFolderAsync()
    {
        try
        {
            await _settingsService.OpenSettingsDirectoryAsync();

            string? settingsDir = Path.GetDirectoryName(CurrentSettingsFilePath);
            SettingsStatusMessage = $"Opened settings folder: {settingsDir}";
            _logger.LogInformation("Opened settings folder: {Path}", settingsDir);
        }
        catch (Exception ex)
        {
            SettingsStatusMessage = $"Failed to open settings folder: {ex.Message}";
            _logger.LogError(ex, "Failed to open settings folder");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Validates the current settings configuration.
    /// </summary>
    /// <returns>True if settings are valid, false otherwise.</returns>
    public bool ValidateSettings()
    {
        try
        {
            // Validate paths exist or can be created
            if (!string.IsNullOrEmpty(DefaultLogPath))
            {
                var logDir = new DirectoryInfo(DefaultLogPath);
                if (!logDir.Exists)
                {
                    // Try to create the directory
                    logDir.Create();
                }
            }

            if (!string.IsNullOrEmpty(ExportPath))
            {
                var exportDir = new DirectoryInfo(ExportPath);
                if (!exportDir.Exists)
                {
                    // Try to create the directory
                    exportDir.Create();
                }
            }

            // Validate log level
            string[] validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
            if (!validLogLevels.Contains(MinimumLogLevel))
            {
                _logger.LogWarning("Invalid log level: {LogLevel}. Resetting to Information.", MinimumLogLevel);
                MinimumLogLevel = "Information";
            }

            _logger.LogDebug("Settings validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings validation failed");
            SettingsStatusMessage = $"Settings validation failed: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Exports the current settings to a JSON string.
    /// </summary>
    /// <returns>JSON representation of the current settings.</returns>
    public string ExportSettingsToJson()
    {
        try
        {
            // Get the complete settings from the service
            ApplicationSettings settings = _settingsService.Settings;

            // Serialize to JSON with pretty formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = JsonSerializer.Serialize(settings, options);
            _logger.LogInformation("Settings exported to JSON ({Length} characters)", json.Length);

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings to JSON");
            return string.Empty;
        }
    }

    /// <summary>
    /// Imports settings from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing settings.</param>
    /// <returns>True if import was successful, false otherwise.</returns>
    public bool ImportSettingsFromJson(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning("Cannot import settings from empty JSON");
                SettingsStatusMessage = "Cannot import empty settings";
                return false;
            }

            // Deserialize JSON to ApplicationSettings
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            ApplicationSettings? importedSettings = JsonSerializer.Deserialize<ApplicationSettings>(json, options);

            if (importedSettings == null)
            {
                _logger.LogWarning("Failed to deserialize settings from JSON");
                SettingsStatusMessage = "Invalid settings format";
                return false;
            }

            // Update settings using the service (which will trigger save)
            _ = _settingsService.UpdateSettingsAsync(importedSettings);

            // Update ViewModel properties from imported settings
            LoadSettingsFromService();

            _logger.LogInformation("Settings imported from JSON successfully");
            SettingsStatusMessage = "Settings imported successfully";
            SettingsLastModified = DateTime.Now;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import settings from JSON");
            SettingsStatusMessage = $"Failed to import settings: {ex.Message}";
            return false;
        }
    }

    #endregion
}
