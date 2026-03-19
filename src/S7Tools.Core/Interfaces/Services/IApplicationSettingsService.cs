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
