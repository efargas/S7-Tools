using S7Tools.Core.Models.Configuration.StrongSettings;

namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for managing application settings with user override support.
/// </summary>
/// <remarks>
/// Implementations load settings from a layered configuration (default + user overrides),
/// support in-process mutation via <see cref="UpdateSettingsAsync"/>, and raise
/// <see cref="SettingsChanged"/> when the configuration changes so that dependent
/// services can react without polling.
/// </remarks>
public interface IApplicationSettingsService
{
    /// <summary>
    /// Gets the current strongly-typed application settings snapshot.
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Loads settings from the default and user configuration sources asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous load operation.</returns>
    Task LoadSettingsAsync();

    /// <summary>
    /// Applies the specified mutation to the current settings and persists the result.
    /// </summary>
    /// <param name="updateAction">An action that receives the mutable <see cref="AppSettings"/> instance and applies the desired changes.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task UpdateSettingsAsync(Action<AppSettings> updateAction);

    /// <summary>
    /// Resets all user settings to their factory defaults and persists the change.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    Task ResetAllSettingsAsync();

    /// <summary>
    /// Restores all user-overridden values to their defaults while preserving the base default settings section.
    /// </summary>
    /// <returns>A task representing the asynchronous restore operation.</returns>
    Task RestoreDefaultsAsync();

    /// <summary>
    /// Exports the current settings to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the current <see cref="AppSettings"/>.</returns>
    string ExportSettingsToJson();

    /// <summary>
    /// Imports settings from the provided JSON string and applies them to the current configuration.
    /// </summary>
    /// <param name="json">The JSON string containing the settings to import.</param>
    /// <returns><see langword="true"/> if the import was successful; otherwise, <see langword="false"/>.</returns>
    Task<bool> ImportSettingsFromJsonAsync(string json);

    /// <summary>
    /// Occurs when settings are reloaded or updated via <see cref="UpdateSettingsAsync"/> or <see cref="ImportSettingsFromJsonAsync"/>.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

/// <summary>
/// Provides event data for the <see cref="IApplicationSettingsService.SettingsChanged"/> event.
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether the change originated from a user override.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if a user setting was changed; <see langword="false"/> if a default setting was changed.
    /// </value>
    public bool IsUserSetting { get; set; }
}
