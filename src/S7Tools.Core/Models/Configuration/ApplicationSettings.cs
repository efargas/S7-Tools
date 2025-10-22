using System.Text.Json;

namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Represents the hierarchical settings structure with default and user overrides
    /// </summary>
    public sealed class ApplicationSettings
    {
        /// <summary>
        /// Built-in default values that ship with the application
        /// </summary>
        public Dictionary<string, object> DefaultSettings { get; init; } = new();

        /// <summary>
        /// User-customized values that override defaults
        /// </summary>
        public Dictionary<string, object> UserSettings { get; set; } = new();

        /// <summary>
        /// Computed merged settings (defaults + user overrides)
        /// </summary>
        public Dictionary<string, object> EffectiveSettings { get; private set; } = new();

        /// <summary>
        /// Path to user settings file
        /// </summary>
        public string SettingsFilePath { get; init; } = string.Empty;

        /// <summary>
        /// When settings were last updated
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Merges default and user settings to compute effective settings
        /// </summary>
        public void ComputeEffectiveSettings()
        {
            EffectiveSettings.Clear();

            // Start with defaults
            foreach (KeyValuePair<string, object> kvp in DefaultSettings)
            {
                EffectiveSettings[kvp.Key] = kvp.Value;
            }

            // Override with user settings
            foreach (KeyValuePair<string, object> kvp in UserSettings)
            {
                EffectiveSettings[kvp.Key] = kvp.Value;
            }

            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a setting value by key from effective settings
        /// </summary>
        /// <typeparam name="T">The expected type of the setting value</typeparam>
        /// <param name="key">Setting key using dot notation (e.g., "logging.level")</param>
        /// <param name="defaultValue">Default value if setting is not found</param>
        /// <returns>The setting value or default</returns>
        public T GetSetting<T>(string key, T defaultValue = default!)
        {
            if (!EffectiveSettings.TryGetValue(key, out object? value))
            {
                return defaultValue;
            }

            try
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.Deserialize<T>() ?? defaultValue;
                }

                if (value is T directValue)
                {
                    return directValue;
                }

                // Try to convert
                return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets a user setting value, overriding any default
        /// </summary>
        /// <param name="key">Setting key using dot notation</param>
        /// <param name="value">Setting value</param>
        public void SetUserSetting(string key, object value)
        {
            UserSettings[key] = value;
            ComputeEffectiveSettings();
        }

        /// <summary>
        /// Removes a user setting, reverting to default if available
        /// </summary>
        /// <param name="key">Setting key to remove</param>
        /// <returns>True if setting was removed, false if it didn't exist</returns>
        public bool RemoveUserSetting(string key)
        {
            if (UserSettings.Remove(key))
            {
                ComputeEffectiveSettings();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a setting key exists in effective settings
        /// </summary>
        /// <param name="key">Setting key to check</param>
        /// <returns>True if setting exists, false otherwise</returns>
        public bool HasSetting(string key)
        {
            return EffectiveSettings.ContainsKey(key);
        }

        /// <summary>
        /// Gets all setting keys that start with a prefix
        /// </summary>
        /// <param name="prefix">Key prefix to search for</param>
        /// <returns>List of matching keys</returns>
        public List<string> GetSettingKeys(string prefix)
        {
            return EffectiveSettings.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Creates default application settings
        /// </summary>
        /// <param name="settingsFilePath">Path to the settings file</param>
        /// <returns>ApplicationSettings with default values</returns>
        public static ApplicationSettings CreateDefault(string settingsFilePath = "")
        {
            var settings = new ApplicationSettings
            {
                SettingsFilePath = settingsFilePath
            };

            // Add default settings
            settings.DefaultSettings.Add("logging.level", "Information");
            settings.DefaultSettings.Add("logging.enableFileLogging", true);
            settings.DefaultSettings.Add("logging.maxFileSize", 10485760); // 10MB
            settings.DefaultSettings.Add("logging.maxFiles", 5);
            settings.DefaultSettings.Add("ui.theme", "System");
            settings.DefaultSettings.Add("ui.startMinimized", false);
            settings.DefaultSettings.Add("paths.autoCreateDirectories", true);
            settings.DefaultSettings.Add("profiles.autoSave", true);
            settings.DefaultSettings.Add("profiles.backupOnSave", true);
            settings.DefaultSettings.Add("export.defaultFormat", "JSON");
            settings.DefaultSettings.Add("plc.connectionTimeout", 5000);
            settings.DefaultSettings.Add("plc.readTimeout", 2000);
            settings.DefaultSettings.Add("plc.retryAttempts", 3);

            settings.ComputeEffectiveSettings();
            return settings;
        }
    }
}
