using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Models;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly IPathService _pathService;
    private ApplicationSettings _settings = new();
    private readonly string _defaultSettingsPath;
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Initializes a new instance of the SettingsService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pathService">The path service for resolving settings file paths.</param>
    public SettingsService(ILogger<SettingsService> logger, IPathService pathService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

        // Use the dynamic path service instead of hardcoded paths
        _defaultSettingsPath = _pathService.AppSettingsPath;

        _logger.LogDebug("SettingsService initialized with dynamic settings path: {SettingsPath}", _defaultSettingsPath);
    }

    /// <inheritdoc />
    public ApplicationSettings Settings => _settings;

    /// <inheritdoc />
    public event EventHandler<ApplicationSettings>? SettingsChanged;

    /// <inheritdoc />
    public async Task LoadSettingsAsync()
    {
        await LoadSettingsAsync(_defaultSettingsPath).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task LoadSettingsAsync(string filePath)
    {
        try
        {
            _logger.LogDebug("Loading settings from {FilePath}", filePath);

            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                ApplicationSettings? settings = JsonSerializer.Deserialize<ApplicationSettings>(json, ReadOptions);
                if (settings != null)
                {
                    _settings = settings;
                    PopulateSettingsWithDynamicPaths();
                    EnsureDirectoriesExist();
                    SettingsChanged?.Invoke(this, _settings);
                    _logger.LogInformation("Settings successfully loaded from {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Deserialized settings object was null, using default settings");
                    await CreateDefaultSettings(filePath).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogInformation("Settings file does not exist at {FilePath}, creating default settings", filePath);
                // Create default settings file
                await SaveSettingsAsync(filePath).ConfigureAwait(false);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Settings file at {FilePath} contains invalid JSON, reverting to defaults", filePath);
            await CreateDefaultSettings(filePath).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when reading settings file at {FilePath}", filePath);
            // Use default settings but don't overwrite the file
            _settings = new ApplicationSettings();
            PopulateSettingsWithDynamicPaths();
            EnsureDirectoriesExist();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading settings from {FilePath}, reverting to defaults", filePath);
            await CreateDefaultSettings(filePath).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates default settings and notifies of the change.
    /// </summary>
    /// <param name="filePath">The file path where settings should be saved.</param>
    private async Task CreateDefaultSettings(string filePath)
    {
        _settings = new ApplicationSettings();
        PopulateSettingsWithDynamicPaths();
        EnsureDirectoriesExist();

        try
        {
            await SaveSettingsAsync(filePath).ConfigureAwait(false);
            _logger.LogInformation("Default settings created and saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default settings to {FilePath}", filePath);
        }

        SettingsChanged?.Invoke(this, _settings);
    }

    /// <summary>
    /// Populates the settings with dynamic paths from the PathService.
    /// </summary>
    private void PopulateSettingsWithDynamicPaths()
    {
        // Populate resource paths using PathService
        _settings.ResourcesRoot = _pathService.ResourcesDirectory;
        _settings.PayloadsPath = _pathService.PayloadsDirectory;
        _settings.DumpsPath = _pathService.DumpsDirectory;

        // For now, we'll use the old pattern for firmware and extractions
        // These can be updated when those features are properly integrated
        _settings.FirmwarePath = _pathService.GetResourcePath("firmware");
        _settings.ExtractionsPath = _pathService.GetResourcePath("extractions");

        // Populate logging paths
        _settings.Logging.DefaultLogPath = _pathService.LogsDirectory;
        _settings.Logging.ExportPath = _pathService.ExportedLogsDirectory;

        _logger.LogDebug("Settings populated with dynamic paths from PathService");
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync()
    {
        await SaveSettingsAsync(_defaultSettingsPath).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(string filePath)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(_settings, WriteOptions);
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw new ConfigurationException(
                "SettingsSave",
                $"Failed to save settings to {filePath}");
        }
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(ApplicationSettings settings)
    {
        _settings = settings.Clone();
        PopulateSettingsWithDynamicPaths();
        EnsureDirectoriesExist();
        await SaveSettingsAsync().ConfigureAwait(false);
        SettingsChanged?.Invoke(this, _settings);
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync()
    {
        _settings = new ApplicationSettings();
        PopulateSettingsWithDynamicPaths();
        EnsureDirectoriesExist();
        await SaveSettingsAsync().ConfigureAwait(false);
        SettingsChanged?.Invoke(this, _settings);
    }

    /// <inheritdoc />
    public string GetDefaultSettingsPath()
    {
        return _defaultSettingsPath;
    }

    /// <inheritdoc />
    public async Task OpenSettingsDirectoryAsync()
    {
        string? directory = Path.GetDirectoryName(_defaultSettingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            await OpenDirectoryAsync(directory).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task OpenLogDirectoryAsync()
    {
        await OpenDirectoryAsync(_pathService.LogsDirectory).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task OpenExportDirectoryAsync()
    {
        await OpenDirectoryAsync(_pathService.ExportedLogsDirectory).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures that all required directories exist.
    /// </summary>
    private async void EnsureDirectoriesExist()
    {
        try
        {
            // Use the path service to ensure all application directories exist
            await _pathService.EnsureDirectoryExistsAsync(_pathService.ResourcesDirectory).ConfigureAwait(false);
            await _pathService.EnsureDirectoryExistsAsync(_pathService.LogsDirectory).ConfigureAwait(false);
            await _pathService.EnsureDirectoryExistsAsync(_pathService.ExportedLogsDirectory).ConfigureAwait(false);
            await _pathService.EnsureDirectoryExistsAsync(_pathService.PayloadsDirectory).ConfigureAwait(false);
            await _pathService.EnsureDirectoryExistsAsync(_pathService.DumpsDirectory).ConfigureAwait(false);
            await _pathService.EnsureDirectoryExistsAsync(_pathService.ProfilesDirectory).ConfigureAwait(false);

            _logger.LogDebug("All required directories ensured to exist using PathService");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure some directories exist");
        }
    }

    /// <summary>
    /// Opens a directory in the system file explorer.
    /// </summary>
    /// <param name="directoryPath">The directory path to open.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task OpenDirectoryAsync(string directoryPath)
    {
        try
        {
            _logger.LogDebug("Opening directory: {DirectoryPath}", directoryPath);
            Directory.CreateDirectory(directoryPath);

            await Task.Run(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("explorer.exe", directoryPath);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", directoryPath);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", directoryPath);
                }
                else
                {
                    _logger.LogWarning("Unsupported operating system for opening directory: {DirectoryPath}", directoryPath);
                }
            }).ConfigureAwait(false);

            _logger.LogInformation("Successfully opened directory: {DirectoryPath}", directoryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open directory: {DirectoryPath}", directoryPath);
            throw; // Re-throw so caller can handle appropriately
        }
    }
}
