using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services
{
    public class PathService : IPathService
    {
        private readonly ILogger<PathService> _logger;
        private readonly AppSettings _appSettings;
        private readonly string _resourcesPath;

        public PathService(ILogger<PathService> logger)
        {
            _logger = logger;
            _resourcesPath = Path.Combine(AppContext.BaseDirectory, "resources");
            AppSettingsPath = Path.Combine(_resourcesPath, "AppSettings", "AppSettings.json");
            _appSettings = LoadAppSettings();

            SerialProfilesPath = ResolvePath(_appSettings.SerialProfilesPath, "SerialProfiles");
            SocatProfilesPath = ResolvePath(_appSettings.SocatProfilesPath, "SocatProfiles");
            PowerSupplyProfilesPath = ResolvePath(_appSettings.PowerSupplyProfilesPath, "PowerSupplyProfiles");
            JobsProfilesPath = ResolvePath(_appSettings.JobsProfilesPath, "JobsProfiles");
            TasksPath = ResolvePath(_appSettings.TasksPath, "Tasks");
            ExtractedFwPath = ResolvePath(_appSettings.ExtractedFwPath, "ExtractedFw");
            DumpsPath = ResolvePath(_appSettings.DumpsPath, "Dumps");
            LogsPath = ResolvePath(_appSettings.LogsPath, "Logs");
            PayloadsPath = ResolvePath(_appSettings.PayloadsPath, "Payloads");
            ExportLogsPath = ResolvePath(_appSettings.ExportLogsPath, "ExportLogs");
        }

        public string AppSettingsPath { get; }
        public string SerialProfilesPath { get; }
        public string SocatProfilesPath { get; }
        public string PowerSupplyProfilesPath { get; }
        public string JobsProfilesPath { get; }
        public string TasksPath { get; }
        public string ExtractedFwPath { get; }
        public string DumpsPath { get; }
        public string LogsPath { get; }
        public string PayloadsPath { get; }
        public string ExportLogsPath { get; }

        public async Task InitializeDirectoriesAsync()
        {
            try
            {
                var appSettingsDir = Path.GetDirectoryName(AppSettingsPath);
                if (appSettingsDir != null) Directory.CreateDirectory(appSettingsDir);
                Directory.CreateDirectory(SerialProfilesPath);
                Directory.CreateDirectory(SocatProfilesPath);
                Directory.CreateDirectory(PowerSupplyProfilesPath);
                Directory.CreateDirectory(JobsProfilesPath);
                Directory.CreateDirectory(TasksPath);
                Directory.CreateDirectory(ExtractedFwPath);
                Directory.CreateDirectory(DumpsPath);
                Directory.CreateDirectory(LogsPath);
                Directory.CreateDirectory(PayloadsPath);
                Directory.CreateDirectory(ExportLogsPath);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create application directories.");
                throw;
            }
        }

        private AppSettings LoadAppSettings()
        {
            try
            {
                if (File.Exists(AppSettingsPath))
                {
                    var json = File.ReadAllText(AppSettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefaultAppSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load AppSettings.json. Using default settings.");
            }

            var defaultSettings = CreateDefaultAppSettings();
            SaveDefaultAppSettings(defaultSettings);
            return defaultSettings;
        }

        private AppSettings CreateDefaultAppSettings()
        {
            return new AppSettings();
        }

        private void SaveDefaultAppSettings(AppSettings appSettings)
        {
            try
            {
                var appSettingsDir = Path.GetDirectoryName(AppSettingsPath);
                if (appSettingsDir != null) Directory.CreateDirectory(appSettingsDir);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(appSettings, options);
                File.WriteAllText(AppSettingsPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save default AppSettings.json.");
            }
        }

        private string ResolvePath(string customPath, string defaultPath)
        {
            if (!string.IsNullOrEmpty(customPath) && Path.IsPathRooted(customPath))
            {
                return customPath;
            }
            return Path.Combine(_resourcesPath, defaultPath);
        }
    }
}
