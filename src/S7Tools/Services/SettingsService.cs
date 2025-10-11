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
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDirectory = Path.Combine(appDataPath, "S7Tools");
        Directory.CreateDirectory(appDirectory);
        _defaultSettingsPath = Path.Combine(appDirectory, "settings.json");
    }

    /// <inheritdoc />
    public ApplicationSettings GetSettings() => _settings.Clone();

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
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(json);
                if (settings != null)
                {
                    _settings = settings;
                    EnsureDirectoriesExist();
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
            else
            {
                // Create default settings file
                await SaveSettingsAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {FilePath}. Using default settings.", filePath);
            _settings = new ApplicationSettings();
            EnsureDirectoriesExist();
        }
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
        await OpenDirectoryAsync(GetSettings().Logging.DefaultLogPath);
    }

    /// <inheritdoc />
    public async Task OpenExportDirectoryAsync()
    {
        await OpenDirectoryAsync(GetSettings().Logging.ExportPath);
    }

    /// <summary>
    /// Ensures that all required directories exist.
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        try
        {
            var currentSettings = GetSettings();
            // Root resources
            var resourcesRoot = currentSettings.ResourcesRoot;
            if (!string.IsNullOrWhiteSpace(resourcesRoot))
            {
                Directory.CreateDirectory(resourcesRoot);
            }

            // Standard application resource folders
            Directory.CreateDirectory(currentSettings.Logging.DefaultLogPath);
            Directory.CreateDirectory(currentSettings.Logging.ExportPath);
            Directory.CreateDirectory(currentSettings.PayloadsPath);
            Directory.CreateDirectory(currentSettings.FirmwarePath);
            Directory.CreateDirectory(currentSettings.ExtractionsPath);
            Directory.CreateDirectory(currentSettings.DumpsPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create one or more required directories. This may be due to permissions issues.");
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open directory '{DirectoryPath}' in file explorer.", directoryPath);
        }
    }
}
