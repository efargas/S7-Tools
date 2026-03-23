using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration.StrongSettings;
using S7Tools.Services.Interfaces;
using System.Collections.Generic;
using System.Text.Json;

namespace S7Tools.Services
{
    public sealed class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly ILogger<ApplicationSettingsService> _logger;
        private readonly IWritableOptions<AppSettings> _options;
        private readonly IConfigurationRoot? _configurationRoot;
        private readonly IUIThreadService? _uiThreadService;

        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        public ApplicationSettingsService(
            ILogger<ApplicationSettingsService> logger,
            IWritableOptions<AppSettings> options,
            IConfiguration? configuration = null,
            IUIThreadService? uiThreadService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _configurationRoot = configuration as IConfigurationRoot;
            _uiThreadService = uiThreadService;
            _logger.LogInformation("ApplicationSettingsService initialized as strongly-typed proxy");
        }

        public AppSettings Current => _options.CurrentValue;

        public async Task LoadSettingsAsync()
        {
            if (_configurationRoot is null)
            {
                _logger.LogWarning("Cannot reload application settings because configuration root is not available.");
                return;
            }

            _logger.LogInformation("Reloading application settings from current configuration source.");
            try
            {
                await Task.Run(() => _configurationRoot.Reload()).ConfigureAwait(false);
                RaiseSettingsChanged(new SettingsChangedEventArgs { IsUserSetting = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload application settings from configuration source.");
                throw;
            }
        }

        public async Task UpdateSettingsAsync(Action<AppSettings> updateAction)
        {
            await _options.UpdateAsync(settings =>
            {
                updateAction(settings);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            RaiseSettingsChanged(new SettingsChangedEventArgs { IsUserSetting = true });
        }

        public async Task ResetAllSettingsAsync()
        {
            await _options.UpdateAsync(s =>
            {
                var def = new AppSettings();
                s.Logging = def.Logging;
                s.Ui = def.Ui;
                s.Paths = def.Paths;
                s.Profiles = def.Profiles;
                s.PowerSupply = def.PowerSupply;
                s.MemoryDump = def.MemoryDump;
                s.MemoryRegion = def.MemoryRegion;
                s.Export = def.Export;
                s.Plc = def.Plc;
                s.Jobs = def.Jobs;
                s.Tasks = def.Tasks;
                s.Serial = def.Serial;
                s.Network = def.Network;
                s.Socat = def.Socat;
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            RaiseSettingsChanged(new SettingsChangedEventArgs { IsUserSetting = false });
        }

        public Task RestoreDefaultsAsync() => ResetAllSettingsAsync();

        public string ExportSettingsToJson()
        {
            try
            {
                var settings = Current;
                var exportDict = new Dictionary<string, object>
                {
                    ["logging.logDirectory"] = settings.Logging.LogDirectory,
                    ["logging.exportDirectory"] = settings.Logging.ExportDirectory,
                    ["logging.level"] = settings.Logging.Level,
                    ["ui.autoScrollLogs"] = settings.Ui.AutoScrollLogs,
                    ["logging.enableFileLogging"] = settings.Logging.EnableFileLogging,
                    ["ui.showTimestampInLogs"] = settings.Ui.ShowTimestampInLogs,
                    ["ui.showCategoryInLogs"] = settings.Ui.ShowCategoryInLogs,
                    ["ui.showLogLevelInLogs"] = settings.Ui.ShowLogLevelInLogs
                };

                var optionsFormatter = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(exportDict, optionsFormatter);
                _logger.LogInformation("Settings exported to JSON ({Length} characters)", json.Length);
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export settings to JSON");
                return string.Empty;
            }
        }

        public async Task<bool> ImportSettingsFromJsonAsync(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }

                var optionsFormatter = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var importedSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json, optionsFormatter);
                if (importedSettings == null)
                {
                    return false;
                }

                await UpdateSettingsAsync(s =>
                {
                    if (importedSettings.TryGetValue("logging.logDirectory", out object? logDir) && logDir is JsonElement logDirElem && logDirElem.ValueKind == JsonValueKind.String)
                    {
                        s.Logging.LogDirectory = logDirElem.GetString() ?? s.Logging.LogDirectory;
                    }

                    if (importedSettings.TryGetValue("logging.exportDirectory", out object? expDir) && expDir is JsonElement expDirElem && expDirElem.ValueKind == JsonValueKind.String)
                    {
                        s.Logging.ExportDirectory = expDirElem.GetString() ?? s.Logging.ExportDirectory;
                    }

                    if (importedSettings.TryGetValue("logging.level", out object? level) && level is JsonElement levelElem && levelElem.ValueKind == JsonValueKind.String)
                    {
                        s.Logging.Level = levelElem.GetString() ?? s.Logging.Level;
                    }

                    if (importedSettings.TryGetValue("ui.autoScrollLogs", out object? autoScroll) && autoScroll is JsonElement autoScrollElem && (autoScrollElem.ValueKind == JsonValueKind.True || autoScrollElem.ValueKind == JsonValueKind.False))
                    {
                        s.Ui.AutoScrollLogs = autoScrollElem.GetBoolean();
                    }

                    if (importedSettings.TryGetValue("logging.enableFileLogging", out object? enableFileLog) && enableFileLog is JsonElement enableFileLogElem && (enableFileLogElem.ValueKind == JsonValueKind.True || enableFileLogElem.ValueKind == JsonValueKind.False))
                    {
                        s.Logging.EnableFileLogging = enableFileLogElem.GetBoolean();
                    }

                    if (importedSettings.TryGetValue("ui.showTimestampInLogs", out object? showTimestamp) && showTimestamp is JsonElement showTimestampElem && (showTimestampElem.ValueKind == JsonValueKind.True || showTimestampElem.ValueKind == JsonValueKind.False))
                    {
                        s.Ui.ShowTimestampInLogs = showTimestampElem.GetBoolean();
                    }

                    if (importedSettings.TryGetValue("ui.showCategoryInLogs", out object? showCat) && showCat is JsonElement showCatElem && (showCatElem.ValueKind == JsonValueKind.True || showCatElem.ValueKind == JsonValueKind.False))
                    {
                        s.Ui.ShowCategoryInLogs = showCatElem.GetBoolean();
                    }

                    if (importedSettings.TryGetValue("ui.showLogLevelInLogs", out object? showLogLevel) && showLogLevel is JsonElement showLogLevelElem && (showLogLevelElem.ValueKind == JsonValueKind.True || showLogLevelElem.ValueKind == JsonValueKind.False))
                    {
                        s.Ui.ShowLogLevelInLogs = showLogLevelElem.GetBoolean();
                    }
                }).ConfigureAwait(false);

                _logger.LogInformation("Settings imported successfully via service");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import settings from JSON in service");
                return false;
            }
        }

        private void RaiseSettingsChanged(SettingsChangedEventArgs args)
        {
            var handler = SettingsChanged;
            if (handler is null) return;

            if (_uiThreadService is not null)
            {
                _uiThreadService.PostToUIThread(() => handler(this, args));
            }
            else
            {
                handler(this, args);
            }
        }
    }
}
