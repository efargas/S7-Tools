using S7Tools.Core.Constants;

namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Represents the catalog of required application resources and their expected locations
    /// </summary>
    public sealed class ResourceManifest
    {
        /// <summary>
        /// Directories that must exist for the application to function
        /// </summary>
        public List<DirectoryInfo> RequiredDirectories { get; init; } = new();

        /// <summary>
        /// Files that must be present for the application to function
        /// </summary>
        public List<FileInfo> RequiredFiles { get; init; } = new();

        /// <summary>
        /// Resources that are created on-demand when needed
        /// </summary>
        public List<ResourceInfo> OptionalResources { get; init; } = new();

        /// <summary>
        /// Track what was created vs what already existed during initialization
        /// </summary>
        public Dictionary<string, bool> CreationStatus { get; set; } = new();

        /// <summary>
        /// Creates a default resource manifest for the S7Tools application
        /// </summary>
        public static ResourceManifest CreateDefault()
        {
            var manifest = new ResourceManifest();

            // Required directories
            manifest.RequiredDirectories.AddRange(new[]
            {
                new DirectoryInfo
                {
                    Name = ResourcePaths.ResourcesFolder,
                    RelativePath = ResourcePaths.ResourcesFolder,
                    Purpose = "Root directory for all application resources"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.AppSettingsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.AppSettingsFolder),
                    Purpose = "Application settings and configuration"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.ProfilesFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder),
                    Purpose = "All profile types and configurations"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.SerialFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.SerialFolder),
                    Purpose = "Serial communication profiles"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.SocatFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.SocatFolder),
                    Purpose = "Socat proxy profiles"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.PowerSupplyFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.PowerSupplyFolder),
                    Purpose = "Power supply control profiles"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.MemoryRegionsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.MemoryRegionsFolder),
                    Purpose = "Memory region profiles and definitions"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.LogsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.LogsFolder),
                    Purpose = "Application logs and exported log files"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.MainLogsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.LogsFolder, ResourcePaths.MainLogsFolder),
                    Purpose = "Main application runtime logs"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.ExportedLogsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.LogsFolder, ResourcePaths.ExportedLogsFolder),
                    Purpose = "Exported log files in various formats"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.CsvLogsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.LogsFolder, ResourcePaths.ExportedLogsFolder, ResourcePaths.CsvLogsFolder),
                    Purpose = "Exported logs in CSV format"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.TxtLogsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.LogsFolder, ResourcePaths.ExportedLogsFolder, ResourcePaths.TxtLogsFolder),
                    Purpose = "Exported logs in text format"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.JsonLogsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.LogsFolder, ResourcePaths.ExportedLogsFolder, ResourcePaths.JsonLogsFolder),
                    Purpose = "Exported logs in JSON format"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.JobsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.JobsFolder),
                    Purpose = "Job definitions and configurations"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.TasksFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.TasksFolder),
                    Purpose = "Task definitions and configurations"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.PayloadsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.PayloadsFolder),
                    Purpose = "Binary payloads and firmware files"
                },
                new DirectoryInfo
                {
                    Name = ResourcePaths.DumpsFolder,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.DumpsFolder),
                    Purpose = "Memory dumps and extracted data"
                }
            });

            // Required files with default content
            manifest.RequiredFiles.AddRange(new[]
            {
                new FileInfo
                {
                    Name = ResourcePaths.AppSettingsFile,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.AppSettingsFolder, ResourcePaths.AppSettingsFile),
                    DefaultContent = GetDefaultAppSettingsContent(),
                    Purpose = "Application settings and preferences"
                },
                new FileInfo
                {
                    Name = ResourcePaths.SerialProfilesFile,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.SerialFolder, ResourcePaths.SerialProfilesFile),
                    DefaultContent = "[]",
                    Purpose = "Serial communication profile configurations"
                },
                new FileInfo
                {
                    Name = ResourcePaths.SocatProfilesFile,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.SocatFolder, ResourcePaths.SocatProfilesFile),
                    DefaultContent = "[]",
                    Purpose = "Socat proxy profile configurations"
                },
                new FileInfo
                {
                    Name = ResourcePaths.PowerSupplyProfilesFile,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.PowerSupplyFolder, ResourcePaths.PowerSupplyProfilesFile),
                    DefaultContent = "[]",
                    Purpose = "Power supply control profile configurations"
                },
                new FileInfo
                {
                    Name = ResourcePaths.MemoryRegionProfilesFile,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder, ResourcePaths.MemoryRegionsFolder, ResourcePaths.MemoryRegionProfilesFile),
                    DefaultContent = "[]",
                    Purpose = "Memory region profile configurations"
                },
                new FileInfo
                {
                    Name = ResourcePaths.JobsFile,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.JobsFolder, ResourcePaths.JobsFile),
                    DefaultContent = "[]",
                    Purpose = "Job definitions and scheduling information"
                },
                new FileInfo
                {
                    Name = ResourcePaths.TasksFile,
                    RelativePath = Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.TasksFolder, ResourcePaths.TasksFile),
                    DefaultContent = "[]",
                    Purpose = "Task definitions and execution information"
                }
            });

            return manifest;
        }

        /// <summary>
        /// Generates default content for AppSettings.json file with proper structure
        /// </summary>
        /// <returns>JSON content with both default and user settings sections</returns>
        private static string GetDefaultAppSettingsContent()
        {
            // Create a default ApplicationSettings instance to get ALL the default values
            var defaultSettings = ApplicationSettings.CreateDefault();

            // Create the proper file structure with both sections
            var appSettingsFileContent = new
            {
                DefaultSettings = defaultSettings.DefaultSettings,
                UserSettings = new Dictionary<string, object>(defaultSettings.DefaultSettings), // Copy defaults to user settings initially
                SettingsFilePath = "Resources/AppSettings/AppSettings.json",
                LastModified = DateTime.UtcNow
            };

            // Serialize to JSON with proper formatting
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            return System.Text.Json.JsonSerializer.Serialize(appSettingsFileContent, options);
        }
    }
}
