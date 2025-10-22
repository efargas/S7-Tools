using Microsoft.Extensions.Logging;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using System.Text.Json;

namespace S7Tools.Services
{
    /// <summary>
    /// Service for managing application settings with user override support and layered configuration
    /// </summary>
    public sealed class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly ILogger<ApplicationSettingsService> _logger;
        private readonly IPathService _pathService;
        private ApplicationSettings? _currentSettings;
        private readonly object _settingsLock = new();

        /// <summary>
        /// Event fired when settings are reloaded or changed
        /// </summary>
        public event EventHandler<S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs>? SettingsChanged;

        /// <summary>
        /// Initializes a new instance of the ApplicationSettingsService class
        /// </summary>
        /// <param name="logger">Logger for structured logging</param>
        /// <param name="pathService">Path service for resolving settings file paths</param>
        public ApplicationSettingsService(ILogger<ApplicationSettingsService> logger, IPathService pathService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

            _logger.LogInformation("ApplicationSettingsService initialized");
        }

        /// <summary>
        /// Loads settings from default and user configuration sources
        /// </summary>
        /// <returns>Merged application settings</returns>
        public async Task<ApplicationSettings> LoadSettingsAsync()
        {
            _logger.LogInformation("Loading application settings");

            try
            {
                lock (_settingsLock)
                {
                    // Create default settings with settings file path
                    _currentSettings = ApplicationSettings.CreateDefault(_pathService.AppSettingsPath);

                    _logger.LogDebug("Default settings created with {DefaultCount} default values",
                        _currentSettings.DefaultSettings.Count);
                }

                // Load user settings from file
                await LoadUserSettingsFromFileAsync().ConfigureAwait(false);

                lock (_settingsLock)
                {
                    // Compute effective settings by merging defaults with user settings
                    _currentSettings.ComputeEffectiveSettings();

                    _logger.LogInformation("Settings loaded successfully. Effective settings: {EffectiveCount}, User overrides: {UserCount}",
                        _currentSettings.EffectiveSettings.Count, _currentSettings.UserSettings.Count);

                    return _currentSettings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load application settings");

                // Return default settings as fallback
                lock (_settingsLock)
                {
                    _currentSettings = ApplicationSettings.CreateDefault(_pathService.AppSettingsPath);
                    _currentSettings.ComputeEffectiveSettings();
                    return _currentSettings;
                }
            }
        }

        /// <summary>
        /// Saves user settings to the user configuration file
        /// </summary>
        /// <param name="userSettings">Settings to save</param>
        public async Task SaveUserSettingsAsync(Dictionary<string, object> userSettings)
        {
            if (userSettings == null)
            {
                throw new ArgumentNullException(nameof(userSettings));
            }

            _logger.LogInformation("Saving user settings with {SettingCount} entries", userSettings.Count);

            try
            {
                lock (_settingsLock)
                {
                    if (_currentSettings == null)
                    {
                        throw new InvalidOperationException("Settings not loaded. Call LoadSettingsAsync first.");
                    }

                    // Update user settings
                    foreach (KeyValuePair<string, object> kvp in userSettings)
                    {
                        object? oldValue = _currentSettings.UserSettings.TryGetValue(kvp.Key, out object? existing) ? existing : null;
                        _currentSettings.UserSettings[kvp.Key] = kvp.Value;

                        // Fire change event
                        SettingsChanged?.Invoke(this, new S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs
                        {
                            Key = kvp.Key,
                            OldValue = oldValue,
                            NewValue = kvp.Value,
                            IsUserSetting = true
                        });
                    }

                    // Recompute effective settings
                    _currentSettings.ComputeEffectiveSettings();
                }

                // Save to file
                await SaveUserSettingsToFileAsync().ConfigureAwait(false);

                _logger.LogInformation("User settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user settings");
                throw new SettingsLoadException("Failed to save user settings", _pathService.AppSettingsPath, "Save", ex);
            }
        }

        /// <summary>
        /// Gets a specific setting value with type conversion
        /// </summary>
        /// <typeparam name="T">Type to convert value to</typeparam>
        /// <param name="key">Setting key</param>
        /// <returns>Setting value or default if not found</returns>
        public T GetSetting<T>(string key)
        {
            return GetSetting(key, default(T)!);
        }

        /// <summary>
        /// Gets a specific setting value with type conversion and fallback
        /// </summary>
        /// <typeparam name="T">Type to convert value to</typeparam>
        /// <param name="key">Setting key</param>
        /// <param name="defaultValue">Value to return if setting not found</param>
        /// <returns>Setting value or provided default</returns>
        public T GetSetting<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Setting key cannot be null or empty", nameof(key));
            }

            try
            {
                lock (_settingsLock)
                {
                    if (_currentSettings == null)
                    {
                        _logger.LogWarning("Settings not loaded when getting setting {Key}, returning default", key);
                        return defaultValue;
                    }

                    return _currentSettings.GetSetting(key, defaultValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting setting {Key}, returning default value", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets a user setting value
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value</param>
        public async Task SetSettingAsync(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Setting key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Setting user setting {Key} to {Value}", key, value);

            try
            {
                object? oldValue;
                lock (_settingsLock)
                {
                    if (_currentSettings == null)
                    {
                        throw new InvalidOperationException("Settings not loaded. Call LoadSettingsAsync first.");
                    }

                    oldValue = _currentSettings.UserSettings.TryGetValue(key, out object? existing) ? existing : null;
                    _currentSettings.SetUserSetting(key, value);
                }

                // Fire change event
                SettingsChanged?.Invoke(this, new S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = value,
                    IsUserSetting = true
                });

                // Save to file
                await SaveUserSettingsToFileAsync().ConfigureAwait(false);

                _logger.LogDebug("User setting {Key} updated successfully", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set user setting {Key}", key);
                throw new SettingsLoadException($"Failed to set setting: {key}", _pathService.AppSettingsPath, "SetSetting", ex);
            }
        }

        /// <summary>
        /// Resets a user setting to its default value
        /// </summary>
        /// <param name="key">Setting key to reset</param>
        public async Task ResetSettingAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Setting key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Resetting user setting {Key} to default", key);

            try
            {
                object? oldValue;
                object? newValue;
                bool wasRemoved;

                lock (_settingsLock)
                {
                    if (_currentSettings == null)
                    {
                        throw new InvalidOperationException("Settings not loaded. Call LoadSettingsAsync first.");
                    }

                    oldValue = _currentSettings.UserSettings.TryGetValue(key, out object? existing) ? existing : null;
                    wasRemoved = _currentSettings.RemoveUserSetting(key);
                    newValue = _currentSettings.DefaultSettings.TryGetValue(key, out object? defaultVal) ? defaultVal : null;
                }

                if (wasRemoved)
                {
                    // Fire change event
                    SettingsChanged?.Invoke(this, new S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs
                    {
                        Key = key,
                        OldValue = oldValue,
                        NewValue = newValue,
                        IsUserSetting = false
                    });

                    // Save to file
                    await SaveUserSettingsToFileAsync().ConfigureAwait(false);

                    _logger.LogDebug("User setting {Key} reset to default successfully", key);
                }
                else
                {
                    _logger.LogDebug("User setting {Key} was not set, no reset needed", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset user setting {Key}", key);
                throw new SettingsLoadException($"Failed to reset setting: {key}", _pathService.AppSettingsPath, "ResetSetting", ex);
            }
        }

        /// <summary>
        /// Resets all user settings to defaults
        /// </summary>
        public async Task ResetAllSettingsAsync()
        {
            _logger.LogInformation("Resetting all user settings to defaults");

            try
            {
                List<string> resetKeys;
                lock (_settingsLock)
                {
                    if (_currentSettings == null)
                    {
                        throw new InvalidOperationException("Settings not loaded. Call LoadSettingsAsync first.");
                    }

                    resetKeys = _currentSettings.UserSettings.Keys.ToList();
                    _currentSettings.UserSettings.Clear();
                    _currentSettings.ComputeEffectiveSettings();
                }

                // Fire change events for all reset keys
                foreach (string key in resetKeys)
                {
                    object? defaultValue = _currentSettings?.DefaultSettings.TryGetValue(key, out object? val) == true ? val : null;
                    SettingsChanged?.Invoke(this, new S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs
                    {
                        Key = key,
                        OldValue = null, // We don't track the old values during bulk reset
                        NewValue = defaultValue,
                        IsUserSetting = false
                    });
                }

                // Save to file
                await SaveUserSettingsToFileAsync().ConfigureAwait(false);

                _logger.LogInformation("All user settings reset to defaults successfully. Reset {KeyCount} settings", resetKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset all user settings");
                throw new SettingsLoadException("Failed to reset all settings", _pathService.AppSettingsPath, "ResetAll", ex);
            }
        }

        /// <summary>
        /// Restores all default values to user settings, preserving the default settings section
        /// </summary>
        public async Task RestoreDefaultsAsync()
        {
            _logger.LogInformation("Restoring all user settings to default values");

            try
            {
                List<string> restoredKeys = new();

                lock (_settingsLock)
                {
                    if (_currentSettings == null)
                    {
                        throw new InvalidOperationException("Settings not loaded. Call LoadSettingsAsync first.");
                    }

                    // Copy all default settings to user settings
                    foreach (KeyValuePair<string, object> kvp in _currentSettings.DefaultSettings)
                    {
                        object? oldValue = _currentSettings.UserSettings.TryGetValue(kvp.Key, out object? existing) ? existing : null;
                        _currentSettings.UserSettings[kvp.Key] = kvp.Value;
                        restoredKeys.Add(kvp.Key);

                        // Fire change event
                        SettingsChanged?.Invoke(this, new S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs
                        {
                            Key = kvp.Key,
                            OldValue = oldValue,
                            NewValue = kvp.Value,
                            IsUserSetting = true
                        });
                    }

                    // Recompute effective settings
                    _currentSettings.ComputeEffectiveSettings();
                }

                // Save to file
                await SaveUserSettingsToFileAsync().ConfigureAwait(false);

                _logger.LogInformation("All user settings restored to defaults successfully. Restored {KeyCount} settings", restoredKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore user settings to defaults");
                throw new SettingsLoadException("Failed to restore default settings", _pathService.AppSettingsPath, "RestoreDefaults", ex);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Loads user settings from the settings file
        /// </summary>
        private async Task LoadUserSettingsFromFileAsync()
        {
            string settingsFilePath = _pathService.AppSettingsPath;

            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    _logger.LogInformation("User settings file does not exist at {FilePath}, using defaults only", settingsFilePath);
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(settingsFilePath).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
                {
                    _logger.LogInformation("User settings file is empty or contains only empty object, using defaults only");
                    return;
                }

                // Try to parse as new structured format first
                try
                {
                    Dictionary<string, JsonElement>? structuredSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);
                    if (structuredSettings != null && structuredSettings.ContainsKey("userSettings"))
                    {
                        // New format with structured sections
                        JsonElement userSettingsElement = structuredSettings["userSettings"];
                        Dictionary<string, JsonElement>? userSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userSettingsElement.GetRawText());

                        if (userSettings != null)
                        {
                            lock (_settingsLock)
                            {
                                if (_currentSettings != null)
                                {
                                    // Load user settings section
                                    foreach (KeyValuePair<string, JsonElement> kvp in userSettings)
                                    {
                                        _currentSettings.UserSettings[kvp.Key] = kvp.Value;
                                    }

                                    _logger.LogDebug("Loaded {UserSettingCount} user settings from structured file format", userSettings.Count);
                                }
                            }
                        }
                    }
                    else if (structuredSettings != null)
                    {
                        // Legacy format - treat entire file as user settings
                        lock (_settingsLock)
                        {
                            if (_currentSettings != null)
                            {
                                foreach (KeyValuePair<string, JsonElement> kvp in structuredSettings)
                                {
                                    _currentSettings.UserSettings[kvp.Key] = kvp.Value;
                                }

                                _logger.LogDebug("Loaded {UserSettingCount} user settings from legacy file format", structuredSettings.Count);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // If structured parsing fails, fall back to treating entire content as user settings
                    Dictionary<string, JsonElement>? userSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);
                    if (userSettings != null)
                    {
                        lock (_settingsLock)
                        {
                            if (_currentSettings != null)
                            {
                                // Convert JsonElement values to objects
                                foreach (KeyValuePair<string, JsonElement> kvp in userSettings)
                                {
                                    _currentSettings.UserSettings[kvp.Key] = kvp.Value;
                                }

                                _logger.LogDebug("Loaded {UserSettingCount} user settings from file (fallback parsing)", userSettings.Count);
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON in user settings file {FilePath}, using defaults only", settingsFilePath);
                throw new SettingsLoadException("Invalid JSON in settings file", settingsFilePath, "Parse", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user settings from {FilePath}", settingsFilePath);
                throw new SettingsLoadException("Failed to load user settings file", settingsFilePath, "FileAccess", ex);
            }
        }

        /// <summary>
        /// Saves user settings to the settings file
        /// </summary>
        private async Task SaveUserSettingsToFileAsync()
        {
            string settingsFilePath = _pathService.AppSettingsPath;

            try
            {
                Dictionary<string, object> userSettingsToSave;
                Dictionary<string, object> defaultSettingsToSave;

                lock (_settingsLock)
                {
                    if (_currentSettings == null)
                    {
                        return;
                    }

                    userSettingsToSave = new Dictionary<string, object>(_currentSettings.UserSettings);
                    defaultSettingsToSave = new Dictionary<string, object>(_currentSettings.DefaultSettings);
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(settingsFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    await _pathService.EnsureDirectoryExistsAsync(directory).ConfigureAwait(false);
                }

                // Create the complete structured content
                var appSettingsFileContent = new
                {
                    DefaultSettings = defaultSettingsToSave,
                    UserSettings = userSettingsToSave,
                    SettingsFilePath = settingsFilePath,
                    LastModified = DateTime.UtcNow
                };

                // Serialize with proper formatting
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonContent = JsonSerializer.Serialize(appSettingsFileContent, options);

                // Atomic write: write to temp file first, then rename
                string tempFilePath = settingsFilePath + ".tmp";
                await File.WriteAllTextAsync(tempFilePath, jsonContent).ConfigureAwait(false);

                // Replace original file atomically
                if (File.Exists(settingsFilePath))
                {
                    File.Delete(settingsFilePath);
                }
                File.Move(tempFilePath, settingsFilePath);

                _logger.LogDebug("Structured settings saved to {FilePath} with {UserSettingCount} user settings and {DefaultSettingCount} default settings",
                    settingsFilePath, userSettingsToSave.Count, defaultSettingsToSave.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save user settings to {FilePath}", settingsFilePath);
                throw new SettingsLoadException("Failed to save user settings file", settingsFilePath, "FileSave", ex);
            }
        }

        #endregion
    }
}
