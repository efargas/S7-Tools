using S7Tools.Core.Models.Configuration;

namespace S7Tools.Core.Interfaces.Services
{
    /// <summary>
    /// Service for resolving and managing application paths dynamically
    /// </summary>
    public interface IPathService
    {
        /// <summary>
        /// Gets the base directory where the application executable is located
        /// </summary>
        string BaseDirectory { get; }

        /// <summary>
        /// Gets the path to the Resources directory
        /// </summary>
        string ResourcesDirectory { get; }

        /// <summary>
        /// Gets the path to Resources/AppSettings/AppSettings.json
        /// </summary>
        string AppSettingsPath { get; }

        /// <summary>
        /// Gets the path to Resources/Profiles directory
        /// </summary>
        string ProfilesDirectory { get; }

        /// <summary>
        /// Gets the path to Resources/Profiles/Serial/SerialProfiles.json
        /// </summary>
        string SerialProfilesPath { get; }

        /// <summary>
        /// Gets the path to Resources/Profiles/Socat/SocatProfiles.json
        /// </summary>
        string SocatProfilesPath { get; }

        /// <summary>
        /// Gets the path to Resources/Profiles/PowerSupply/PowerSupplyProfiles.json
        /// </summary>
        string PowerSupplyProfilesPath { get; }

        /// <summary>
        /// Gets the path to Resources/Profiles/MemoryRegions directory
        /// </summary>
        string MemoryRegionsDirectory { get; }

        /// <summary>
        /// Gets the path to Resources/Logs directory
        /// </summary>
        string LogsDirectory { get; }

        /// <summary>
        /// Gets the path to Resources/Logs/Main directory
        /// </summary>
        string MainLogsDirectory { get; }

        /// <summary>
        /// Gets the path to Resources/Logs/Exported directory
        /// </summary>
        string ExportedLogsDirectory { get; }

        /// <summary>
        /// Gets the path to Resources/Jobs/Jobs.json
        /// </summary>
        string JobsPath { get; }

        /// <summary>
        /// Gets the path to Resources/Tasks/Tasks.json
        /// </summary>
        string TasksPath { get; }

        /// <summary>
        /// Gets the path to Resources/Payloads directory
        /// </summary>
        string PayloadsDirectory { get; }

        /// <summary>
        /// Gets the path to Resources/Dumps directory
        /// </summary>
        string DumpsDirectory { get; }

        /// <summary>
        /// Initializes all required directories and validates paths
        /// </summary>
        /// <returns>PathConfiguration with resolved paths</returns>
        Task<PathConfiguration> InitializeAsync();

        /// <summary>
        /// Resolves a path relative to the base directory
        /// </summary>
        /// <param name="relativePath">Path relative to base directory</param>
        /// <returns>Absolute path</returns>
        string ResolvePath(string relativePath);

        /// <summary>
        /// Ensures a directory exists, creating it if necessary
        /// </summary>
        /// <param name="directoryPath">Path to directory</param>
        /// <returns>True if directory exists or was created successfully</returns>
        Task<bool> EnsureDirectoryExistsAsync(string directoryPath);

        /// <summary>
        /// Validates that all required paths are accessible
        /// </summary>
        /// <returns>Validation result with any issues found</returns>
        Task<PathValidationResult> ValidatePathsAsync();

        /// <summary>
        /// Generates a timestamped main log file path
        /// </summary>
        /// <param name="rollingNumber">Rolling number for the log file</param>
        /// <returns>Full path to main log file</returns>
        string GetMainLogPath(int rollingNumber = 0);

        /// <summary>
        /// Generates a timestamped exported log file path
        /// </summary>
        /// <param name="format">Export format (CSV, TXT, JSON)</param>
        /// <returns>Full path to exported log file</returns>
        string GetExportedLogPath(string format);

        /// <summary>
        /// Gets a path within the Resources directory using string constants
        /// </summary>
        /// <param name="pathComponents">Path components to combine</param>
        /// <returns>Full path within Resources directory</returns>
        string GetResourcePath(params string[] pathComponents);
    }

    /// <summary>
    /// Result of path validation operations
    /// </summary>
    public class PathValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> CreatedDirectories { get; set; } = new();
    }
}
