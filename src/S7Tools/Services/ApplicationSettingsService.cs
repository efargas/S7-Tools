using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Core.Models.Configuration.StrongSettings;

namespace S7Tools.Services
{
    public sealed class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly ILogger<ApplicationSettingsService> _logger;
        private readonly IWritableOptions<AppSettings> _options;

        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        public ApplicationSettingsService(ILogger<ApplicationSettingsService> logger, IWritableOptions<AppSettings> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger.LogInformation("ApplicationSettingsService initialized as strongly-typed proxy");
        }

        public Task<ApplicationSettings> LoadSettingsAsync()
        {
            // Dummy return to fulfill interface contract for unused return values
            return Task.FromResult(new ApplicationSettings());
        }

        public async Task SaveUserSettingsAsync(Dictionary<string, object> userSettings)
        {
            foreach (var kvp in userSettings)
            {
                await SetSettingAsync(kvp.Key, kvp.Value);
            }
        }

        public T GetSetting<T>(string key) => GetSetting(key, default(T)!);

        public T GetSetting<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            var settings = _options.CurrentValue;
            
            object? val = key switch
            {
                "logging.logDirectory" => settings.Logging.LogDirectory,
                "logging.exportDirectory" => settings.Logging.ExportDirectory,
                "logging.level" => settings.Logging.Level.ToString(),
                "logging.enableFileLogging" => settings.Logging.EnableFileLogging,
                "ui.autoScrollLogs" => settings.Ui.AutoScrollLogs,
                "ui.showTimestampInLogs" => settings.Ui.ShowTimestampInLogs,
                "ui.showCategoryInLogs" => settings.Ui.ShowCategoryInLogs,
                "ui.showLogLevelInLogs" => settings.Ui.ShowLogLevelInLogs,
                "ui.theme" => settings.Ui.Theme,
                "paths.autoCreateDirectories" => settings.Paths.AutoCreateDirectories,
                "profiles.serialPath" => settings.Profiles.SerialPath,
                "profiles.socatPath" => settings.Profiles.SocatPath,
                "profiles.powerSupplyPath" => settings.Profiles.PowerSupplyPath,
                "profiles.memoryRegionPath" => settings.Profiles.MemoryRegionPath,
                "profiles.socatPath" => settings.Profiles.SocatPath,
                "profiles.powerSupplyPath" => settings.Profiles.PowerSupplyPath,
                "profiles.memoryRegionPath" => settings.Profiles.MemoryRegionPath,
                "powerSupply.powerStateChangeDelayMs" => settings.PowerSupply.PowerStateChangeDelayMs,
                "memoryDump.defaultFolder" => settings.MemoryDump.DefaultFolder,
                _ => null
            };

            if (val == null) return defaultValue;
            try
            {
                if (typeof(T).IsEnum && val is string enumStr)
                    return (T)Enum.Parse(typeof(T), enumStr, true);
                if (typeof(T) == typeof(string) && val.GetType().IsEnum)
                    return (T)(object)val.ToString()!;
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task SetSettingAsync(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            object? oldValue = null;
            await _options.UpdateAsync(settings =>
            {
                switch (key)
                {
                    case "logging.logDirectory": oldValue = settings.Logging.LogDirectory; settings.Logging.LogDirectory = value.ToString() ?? ""; break;
                    case "logging.exportDirectory": oldValue = settings.Logging.ExportDirectory; settings.Logging.ExportDirectory = value.ToString() ?? ""; break;
                    case "logging.level": 
                        oldValue = settings.Logging.Level; 
                        if (Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(value.ToString(), true, out var lvl)) settings.Logging.Level = lvl.ToString(); 
                        break;
                    case "logging.enableFileLogging": oldValue = settings.Logging.EnableFileLogging; settings.Logging.EnableFileLogging = Convert.ToBoolean(value); break;
                    case "ui.autoScrollLogs": oldValue = settings.Ui.AutoScrollLogs; settings.Ui.AutoScrollLogs = Convert.ToBoolean(value); break;
                    case "ui.showTimestampInLogs": oldValue = settings.Ui.ShowTimestampInLogs; settings.Ui.ShowTimestampInLogs = Convert.ToBoolean(value); break;
                    case "ui.showCategoryInLogs": oldValue = settings.Ui.ShowCategoryInLogs; settings.Ui.ShowCategoryInLogs = Convert.ToBoolean(value); break;
                    case "ui.showLogLevelInLogs": oldValue = settings.Ui.ShowLogLevelInLogs; settings.Ui.ShowLogLevelInLogs = Convert.ToBoolean(value); break;
                    case "ui.theme": oldValue = settings.Ui.Theme; settings.Ui.Theme = value.ToString() ?? ""; break;
                    case "paths.autoCreateDirectories": oldValue = settings.Paths.AutoCreateDirectories; settings.Paths.AutoCreateDirectories = Convert.ToBoolean(value); break;
                    case "profiles.serialPath": oldValue = settings.Profiles.SerialPath; settings.Profiles.SerialPath = value.ToString() ?? ""; break;
                    case "profiles.socatPath": oldValue = settings.Profiles.SocatPath; settings.Profiles.SocatPath = value.ToString() ?? ""; break;
                    case "profiles.powerSupplyPath": oldValue = settings.Profiles.PowerSupplyPath; settings.Profiles.PowerSupplyPath = value.ToString() ?? ""; break;
                    case "profiles.memoryRegionPath":
                    case "profiles.memoryMappingPath":
                        oldValue = settings.Profiles.MemoryRegionPath;
                        settings.Profiles.MemoryRegionPath = value.ToString() ?? "";
                        break;
                    case "powerSupply.powerStateChangeDelayMs": oldValue = settings.PowerSupply.PowerStateChangeDelayMs; settings.PowerSupply.PowerStateChangeDelayMs = Convert.ToInt32(value); break;
                    case "memoryDump.defaultFolder": oldValue = settings.MemoryDump.DefaultFolder; settings.MemoryDump.DefaultFolder = value.ToString() ?? ""; break;
                }
                return Task.CompletedTask;
            });
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { Key = key, OldValue = oldValue, NewValue = value, IsUserSetting = true });
        }

        public async Task ResetSettingAsync(string key)
        {
            var def = new AppSettings();
            object? defValue = key switch
            {
                "logging.logDirectory" => def.Logging.LogDirectory,
                "logging.exportDirectory" => def.Logging.ExportDirectory,
                "logging.level" => def.Logging.Level,
                "logging.enableFileLogging" => def.Logging.EnableFileLogging,
                "ui.autoScrollLogs" => def.Ui.AutoScrollLogs,
                "ui.showTimestampInLogs" => def.Ui.ShowTimestampInLogs,
                "ui.showCategoryInLogs" => def.Ui.ShowCategoryInLogs,
                "ui.showLogLevelInLogs" => def.Ui.ShowLogLevelInLogs,
                "ui.theme" => def.Ui.Theme,
                "paths.autoCreateDirectories" => def.Paths.AutoCreateDirectories,
                "profiles.serialPath" => def.Profiles.SerialPath,
                "profiles.socatPath" => def.Profiles.SocatPath,
                "profiles.powerSupplyPath" => def.Profiles.PowerSupplyPath,
                "profiles.memoryRegionPath" => def.Profiles.MemoryRegionPath,
        {
            get => def.Profiles.MemoryRegionPath;
        }

                "powerSupply.powerStateChangeDelayMs" => def.PowerSupply.PowerStateChangeDelayMs,
                "memoryDump.defaultFolder" => def.MemoryDump.DefaultFolder,
                _ => null
            };
            if (defValue != null)
                await SetSettingAsync(key, defValue);
        }

        public async Task ResetAllSettingsAsync()
        {
            await _options.UpdateAsync(s => {
                var def = new AppSettings();
                s.Logging = def.Logging;
                s.Ui = def.Ui;
                s.Paths = def.Paths;
                s.Profiles = def.Profiles;
                s.PowerSupply = def.PowerSupply;
                s.MemoryDump = def.MemoryDump;
                return Task.CompletedTask;
            });
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { IsUserSetting = false });
        }

        public async Task RestoreDefaultsAsync() => await ResetAllSettingsAsync();
    }
}
