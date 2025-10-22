using S7Tools.Core.Constants;

namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Represents the configuration for application paths and directories
    /// </summary>
    public sealed class PathConfiguration
    {
        /// <summary>
        /// Root directory for the application (executable location)
        /// </summary>
        public required string BaseDirectory { get; init; }

        /// <summary>
        /// Path to Resources folder (base for all resources)
        /// </summary>
        public string ResourcesDirectory => Path.Combine(BaseDirectory, ResourcePaths.ResourcesFolder);

        /// <summary>
        /// Path to Resources/AppSettings/AppSettings.json
        /// </summary>
        public string AppSettingsPath => Path.Combine(ResourcesDirectory, ResourcePaths.AppSettingsFolder, ResourcePaths.AppSettingsFile);

        /// <summary>
        /// Path to Resources/Profiles/ (contains Serial, Socat, PowerSupply subdirs)
        /// </summary>
        public string ProfilesDirectory => Path.Combine(ResourcesDirectory, ResourcePaths.ProfilesFolder);

        /// <summary>
        /// Path to Resources/Logs/ (contains Main and Exported subdirs)
        /// </summary>
        public string LogsDirectory => Path.Combine(ResourcesDirectory, ResourcePaths.LogsFolder);

        /// <summary>
        /// Path to Resources/Jobs/Jobs.json
        /// </summary>
        public string JobsPath => Path.Combine(ResourcesDirectory, ResourcePaths.JobsFolder, ResourcePaths.JobsFile);

        /// <summary>
        /// Path to Resources/Tasks/Tasks.json
        /// </summary>
        public string TasksPath => Path.Combine(ResourcesDirectory, ResourcePaths.TasksFolder, ResourcePaths.TasksFile);

        /// <summary>
        /// Path to Resources/Payloads/
        /// </summary>
        public string PayloadsDirectory => Path.Combine(ResourcesDirectory, ResourcePaths.PayloadsFolder);

        /// <summary>
        /// Path to Resources/Dumps/
        /// </summary>
        public string DumpsDirectory => Path.Combine(ResourcesDirectory, ResourcePaths.DumpsFolder);

        /// <summary>
        /// Path to Resources/Profiles/MemoryRegions/
        /// </summary>
        public string MemoryRegionsDirectory => Path.Combine(ProfilesDirectory, ResourcePaths.MemoryRegionsFolder);

        /// <summary>
        /// Whether paths have been resolved and validated
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Gets the profile directory path for a specific profile type
        /// </summary>
        /// <param name="profileType">The type of profile (Serial, Socat, PowerSupply, MemoryRegions)</param>
        /// <returns>Full path to the profile directory</returns>
        public string GetProfileDirectory(string profileType)
        {
            return profileType switch
            {
                "Serial" => Path.Combine(ProfilesDirectory, ResourcePaths.SerialFolder),
                "Socat" => Path.Combine(ProfilesDirectory, ResourcePaths.SocatFolder),
                "PowerSupply" => Path.Combine(ProfilesDirectory, ResourcePaths.PowerSupplyFolder),
                "MemoryRegions" => MemoryRegionsDirectory,
                _ => throw new ArgumentException($"Unknown profile type: {profileType}", nameof(profileType))
            };
        }

        /// <summary>
        /// Gets the profile file path for a specific profile type
        /// </summary>
        /// <param name="profileType">The type of profile (Serial, Socat, PowerSupply)</param>
        /// <returns>Full path to the profile JSON file</returns>
        public string GetProfileFilePath(string profileType)
        {
            return profileType switch
            {
                "Serial" => Path.Combine(GetProfileDirectory(profileType), ResourcePaths.SerialProfilesFile),
                "Socat" => Path.Combine(GetProfileDirectory(profileType), ResourcePaths.SocatProfilesFile),
                "PowerSupply" => Path.Combine(GetProfileDirectory(profileType), ResourcePaths.PowerSupplyProfilesFile),
                _ => throw new ArgumentException($"Profile type {profileType} does not have a single file", nameof(profileType))
            };
        }

        /// <summary>
        /// Gets the exported logs directory path for a specific format
        /// </summary>
        /// <param name="format">The export format (CSV, TXT, JSON)</param>
        /// <returns>Full path to the format-specific export directory</returns>
        public string GetExportedLogsDirectory(string format)
        {
            string exportedLogsDir = Path.Combine(LogsDirectory, ResourcePaths.ExportedLogsFolder);
            return format.ToUpperInvariant() switch
            {
                "CSV" => Path.Combine(exportedLogsDir, ResourcePaths.CsvLogsFolder),
                "TXT" => Path.Combine(exportedLogsDir, ResourcePaths.TxtLogsFolder),
                "JSON" => Path.Combine(exportedLogsDir, ResourcePaths.JsonLogsFolder),
                _ => throw new ArgumentException($"Unknown export format: {format}", nameof(format))
            };
        }

        /// <summary>
        /// Gets the main logs directory path
        /// </summary>
        public string GetMainLogsDirectory()
        {
            return Path.Combine(LogsDirectory, ResourcePaths.MainLogsFolder);
        }

        /// <summary>
        /// Validates that all required paths are properly formed and accessible
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool Validate()
        {
            try
            {
                // Check that base directory exists and is accessible
                if (!Directory.Exists(BaseDirectory))
                {
                    return false;
                }

                // Check that all computed paths are valid
                string[] paths = new[]
                {
                    ResourcesDirectory,
                    AppSettingsPath,
                    ProfilesDirectory,
                    LogsDirectory,
                    JobsPath,
                    TasksPath,
                    PayloadsDirectory,
                    DumpsDirectory,
                    MemoryRegionsDirectory
                };

                foreach (string? path in paths)
                {
                    // Basic path validation - check that path is well-formed
                    Path.GetFullPath(path);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
