using S7Tools.Models;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    ApplicationSettings Settings { get; }

    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    event EventHandler<ApplicationSettings>? SettingsChanged;

    /// <summary>
    /// Loads settings from the default location.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadSettingsAsync();

    /// <summary>
    /// Saves the current settings to the default location.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveSettingsAsync();

    /// <summary>
    /// Saves settings to a specific file path.
    /// </summary>
    /// <param name="filePath">The file path to save to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveSettingsAsync(string filePath);

    /// <summary>
    /// Loads settings from a specific file path.
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadSettingsAsync(string filePath);

    /// <summary>
    /// Updates the settings with new values.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateSettingsAsync(ApplicationSettings settings);

    /// <summary>
    /// Resets settings to default values.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Gets the default settings file path.
    /// </summary>
    /// <returns>The default settings file path.</returns>
    string GetDefaultSettingsPath();

    /// <summary>
    /// Opens the settings directory in the file explorer.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OpenSettingsDirectoryAsync();

    /// <summary>
    /// Opens the log directory in the file explorer.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OpenLogDirectoryAsync();

    /// <summary>
    /// Opens the export directory in the file explorer.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OpenExportDirectoryAsync();
}
