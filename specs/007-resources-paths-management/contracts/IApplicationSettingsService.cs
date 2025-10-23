using S7Tools.Core.Models.Configuration;

namespace S7Tools.Core.Interfaces.Services
{
    /// <summary>
    /// Service for managing application settings with user override support
    /// </summary>
    public interface IApplicationSettingsService
    {
        /// <summary>
        /// Loads settings from default and user configuration sources
        /// </summary>
        /// <returns>Merged application settings</returns>
        Task<ApplicationSettings> LoadSettingsAsync();

        /// <summary>
        /// Saves user settings to the user configuration file
        /// </summary>
        /// <param name="userSettings">Settings to save</param>
        Task SaveUserSettingsAsync(Dictionary<string, object> userSettings);

        /// <summary>
        /// Gets a specific setting value with type conversion
        /// </summary>
        /// <typeparam name="T">Type to convert value to</typeparam>
        /// <param name="key">Setting key</param>
        /// <returns>Setting value or default if not found</returns>
        T GetSetting<T>(string key);

        /// <summary>
        /// Gets a specific setting value with type conversion and fallback
        /// </summary>
        /// <typeparam name="T">Type to convert value to</typeparam>
        /// <param name="key">Setting key</param>
        /// <param name="defaultValue">Value to return if setting not found</param>
        /// <returns>Setting value or provided default</returns>
        T GetSetting<T>(string key, T defaultValue);

        /// <summary>
        /// Sets a user setting value
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value</param>
        Task SetSettingAsync(string key, object value);

        /// <summary>
        /// Resets a user setting to its default value
        /// </summary>
        /// <param name="key">Setting key to reset</param>
        Task ResetSettingAsync(string key);

        /// <summary>
        /// Resets all user settings to defaults
        /// </summary>
        Task ResetAllSettingsAsync();

        /// <summary>
        /// Event fired when settings are reloaded
        /// </summary>
        event EventHandler<SettingsChangedEventArgs> SettingsChanged;
    }

    /// <summary>
    /// Event arguments for settings change notifications
    /// </summary>
    public class SettingsChangedEventArgs : EventArgs
    {
        public string Key { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public bool IsUserSetting { get; set; }
    }
}
