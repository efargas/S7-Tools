using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using S7Tools.Models;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private ApplicationSettings _settings = new();
    private readonly string _defaultSettingsPath;

    /// <summary>
    /// Initializes a new instance of the SettingsService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDirectory = Path.Combine(appDataPath, "S7Tools");
        Directory.CreateDirectory(appDirectory);
        _defaultSettingsPath = Path.Combine(appDirectory, "settings.json");

        _logger.LogDebug("SettingsService initialized with settings path: {SettingsPath}", _defaultSettingsPath);
    }

    /// <inheritdoc />
    public ApplicationSettings Settings => _settings;

    /// <inheritdoc />
    public event EventHandler<ApplicationSettings>? SettingsChanged;

    /// <inheritdoc />
    public async Task LoadSettingsAsync()
    {
        await LoadSettingsAsync(_defaultSettingsPath);
    }

    /// <inheritdoc />
    public async Task LoadSettingsAsync(string filePath)
    {
        try
        {
            _logger.LogDebug("Loading settings from {FilePath}", filePath);

            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(json);
                if (settings != null)
                {
                    _settings = settings;
                    EnsureDirectoriesExist();
                    SettingsChanged?.Invoke(this, _settings);
                    _logger.LogInformation("Settings successfully loaded from {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("Deserialized settings object was null, using default settings");
                    await CreateDefaultSettings(filePath);
                }
            }
            else
            {
                _logger.LogInformation("Settings file does not exist at {FilePath}, creating default settings", filePath);
                // Create default settings file
                await SaveSettingsAsync(filePath);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Settings file at {FilePath} contains invalid JSON, reverting to defaults", filePath);
            await CreateDefaultSettings(filePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when reading settings file at {FilePath}", filePath);
            // Use default settings but don't overwrite the file
            _settings = new ApplicationSettings();
            EnsureDirectoriesExist();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading settings from {FilePath}, reverting to defaults", filePath);
            await CreateDefaultSettings(filePath);
        }
    }

    /// <summary>
    /// Creates default settings and notifies of the change.
    /// </summary>
    /// <param name="filePath">The file path where settings should be saved.</param>
    private async Task CreateDefaultSettings(string filePath)
    {
        _settings = new ApplicationSettings();
        EnsureDirectoriesExist();

        try
        {
            await SaveSettingsAsync(filePath);
            _logger.LogInformation("Default settings created and saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default settings to {FilePath}", filePath);
        }

        SettingsChanged?.Invoke(this, _settings);
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync()
    {
        await SaveSettingsAsync(_defaultSettingsPath);
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_settings, options);
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save settings to {filePath}", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateSettingsAsync(ApplicationSettings settings)
    {
        _settings = settings.Clone();
        EnsureDirectoriesExist();
        await SaveSettingsAsync();
        SettingsChanged?.Invoke(this, _settings);
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync()
    {
        _settings = new ApplicationSettings();
        EnsureDirectoriesExist();
        await SaveSettingsAsync();
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
        var directory = Path.GetDirectoryName(_defaultSettingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            await OpenDirectoryAsync(directory);
        }
    }

    /// <inheritdoc />
    public async Task OpenLogDirectoryAsync()
    {
        await OpenDirectoryAsync(_settings.Logging.DefaultLogPath);
    }

    /// <inheritdoc />
    public async Task OpenExportDirectoryAsync()
    {
        await OpenDirectoryAsync(_settings.Logging.ExportPath);
    }

    /// <summary>
    /// Ensures that all required directories exist.
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        var directoriesToCreate = new[]
        {
            ("ResourcesRoot", _settings.ResourcesRoot),
            ("DefaultLogPath", _settings.Logging.DefaultLogPath),
            ("ExportPath", _settings.Logging.ExportPath),
            ("PayloadsPath", _settings.PayloadsPath),
            ("FirmwarePath", _settings.FirmwarePath),
            ("ExtractionsPath", _settings.ExtractionsPath),
            ("DumpsPath", _settings.DumpsPath)
        };

        foreach (var (name, path) in directoriesToCreate)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    _logger.LogDebug("Ensured directory exists: {DirectoryName} = {Path}", name, path);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create directory {DirectoryName} at {Path}", name, path);
                }
            }
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
