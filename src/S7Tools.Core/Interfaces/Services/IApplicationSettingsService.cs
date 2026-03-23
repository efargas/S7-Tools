using System;
using System.Threading.Tasks;
using S7Tools.Core.Models.Configuration.StrongSettings;

namespace S7Tools.Core.Interfaces.Services
{
    /// <summary>
    /// Service for managing application settings with user override support
    /// </summary>
    public interface IApplicationSettingsService
    {
        /// <summary>
        /// Gets the current strongly-typed application settings
        /// </summary>
        AppSettings Current { get; }

        /// <summary>
        /// Loads settings from default and user configuration sources
        /// </summary>
        Task LoadSettingsAsync();

        /// <summary>
        /// Updates settings safely and saves them
        /// </summary>
        /// <param name="updateAction">Action to apply changes to the settings</param>
        Task UpdateSettingsAsync(Action<AppSettings> updateAction);

        /// <summary>
        /// Resets all user settings to defaults
        /// </summary>
        Task ResetAllSettingsAsync();

        /// <summary>
        /// Restores all default values to user settings, preserving the default settings section
        /// </summary>
        Task RestoreDefaultsAsync();

        /// <summary>
        /// Exports the current settings to a JSON string
        /// </summary>
        /// <returns>JSON representation of the current settings.</returns>
        string ExportSettingsToJson();

        /// <summary>
        /// Imports settings from a JSON string
        /// </summary>
        /// <param name="json">The JSON string containing settings.</param>
        /// <returns>True if import was successful, false otherwise.</returns>
        Task<bool> ImportSettingsFromJsonAsync(string json);

        /// <summary>
        /// Event fired when settings are reloaded or updated
        /// </summary>
        event EventHandler<SettingsChangedEventArgs> SettingsChanged;
    }

    /// <summary>
    /// Event arguments for settings change notifications
    /// </summary>
    public class SettingsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether this is a user setting (true) or default setting (false)
        /// </summary>
        public bool IsUserSetting { get; set; }
    }
}
