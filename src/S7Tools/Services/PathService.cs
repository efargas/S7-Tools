using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Resources;

namespace S7Tools.Services
{
    /// <summary>
    /// Service for resolving and managing application paths dynamically based on executable location
    /// </summary>
    public sealed class PathService : IPathService
    {
        private readonly IServiceProvider _serviceProvider;
        private ILogger<PathService>? _loggerInstance;

        private ILogger<PathService>? Logger => _loggerInstance ??= _serviceProvider.GetService<ILogger<PathService>>();

        private PathConfiguration? _pathConfiguration;

        /// <summary>
        /// Initializes a new instance of the PathService class
        /// </summary>
        /// <param name="serviceProvider">Service provider for lazy dependency resolution</param>
        public PathService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Resolve base directory from executable location
            string executablePath = Assembly.GetExecutingAssembly().Location;
            BaseDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;

            // Log manually via lazy logger if available, but avoid strictly requiring it during ctor
            // Logger?.LogInformation("PathService initialized with base directory: {BaseDirectory}", BaseDirectory);
        }

        /// <summary>
        /// Gets the base directory where the application executable is located
        /// </summary>
        public string BaseDirectory { get; }

        /// <summary>
        /// Gets the path to the Resources directory
        /// </summary>
        public string ResourcesDirectory => ResolvePath(ResourcePaths.ResourcesFolder);

        /// <summary>
        /// Gets the path to Resources/AppSettings/AppSettings.json
        /// </summary>
        public string AppSettingsPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.AppSettingsFolder,
            ResourcePaths.AppSettingsFile));

        /// <summary>
        /// Gets the path to Resources/Profiles directory
        /// </summary>
        public string ProfilesDirectory => ResolvePath(Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.ProfilesFolder));

        /// <summary>
        /// Gets the path to Resources/Profiles/Serial/SerialProfiles.json
        /// </summary>
        public string SerialProfilesPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.ProfilesFolder,
            ResourcePaths.SerialFolder,
            ResourcePaths.SerialProfilesFile));

        /// <summary>
        /// Gets the path to Resources/Profiles/Socat/SocatProfiles.json
        /// </summary>
        public string SocatProfilesPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.ProfilesFolder,
            ResourcePaths.SocatFolder,
            ResourcePaths.SocatProfilesFile));

        /// <summary>
        /// Gets the path to Resources/Profiles/PowerSupply/PowerSupplyProfiles.json
        /// </summary>
        public string PowerSupplyProfilesPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.ProfilesFolder,
            ResourcePaths.PowerSupplyFolder,
            ResourcePaths.PowerSupplyProfilesFile));

        /// <summary>
        /// Gets the path to Resources/Profiles/MemoryRegions/MemoryRegionProfiles.json
        /// </summary>
        public string MemoryRegionProfilesPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.ProfilesFolder,
            ResourcePaths.MemoryRegionsFolder,
            ResourcePaths.MemoryRegionProfilesFile));

        /// <summary>
        /// Gets the path to Resources/Profiles/MemoryRegions directory
        /// </summary>
        public string MemoryRegionsDirectory => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.ProfilesFolder,
            ResourcePaths.MemoryRegionsFolder));

        /// <summary>
        /// Gets the path to Resources/Profiles/PayloadSets/PayloadSetProfiles.json
        /// </summary>
        public string PayloadSetProfilesPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.ProfilesFolder,
            "PayloadSets",
            "PayloadSetProfiles.json"));

        /// <summary>
        /// Gets the path to Resources/Logs directory
        /// </summary>
        public string LogsDirectory => ResolvePath(Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.LogsFolder));

        /// <summary>
        /// Gets the path to Resources/Logs/Main directory
        /// </summary>
        public string MainLogsDirectory => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.LogsFolder,
            ResourcePaths.MainLogsFolder));

        /// <summary>
        /// Gets the path to Resources/Logs/Exported directory
        /// </summary>
        public string ExportedLogsDirectory => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.LogsFolder,
            ResourcePaths.ExportedLogsFolder));

        /// <summary>
        /// Gets the path to Resources/Jobs/Jobs.json
        /// </summary>
        public string JobsPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.JobsFolder,
            ResourcePaths.JobsFile));

        /// <summary>
        /// Gets the path to Resources/Tasks/Tasks.json
        /// </summary>
        public string TasksPath => ResolvePath(Path.Combine(
            ResourcePaths.ResourcesFolder,
            ResourcePaths.TasksFolder,
            ResourcePaths.TasksFile));

        /// <summary>
        /// Gets the path to Resources/Payloads directory
        /// </summary>
        public string PayloadsDirectory => ResolvePath(Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.PayloadsFolder));

        /// <summary>
        /// Gets the path to Resources/Dumps directory
        /// </summary>
        public string DumpsDirectory => ResolvePath(Path.Combine(ResourcePaths.ResourcesFolder, ResourcePaths.DumpsFolder));

        /// <summary>
        /// Initializes all required directories and validates paths
        /// </summary>
        /// <returns>PathConfiguration with resolved paths</returns>
        public async Task<PathConfiguration> InitializeAsync()
        {
            Logger?.LogInformation("Initializing path configuration and creating required directories");

            try
            {
                _pathConfiguration = new PathConfiguration
                {
                    BaseDirectory = BaseDirectory
                };

                // Validate path configuration
                if (!_pathConfiguration.Validate())
                {
                    throw new PathResolutionException("Path configuration validation failed", BaseDirectory, "Initialize");
                }

                // Create all required directories
                string[] directoriesToCreate = new[]
                {
                    ResourcesDirectory,
                    Path.Combine(ResourcesDirectory, ResourcePaths.AppSettingsFolder),
                    ProfilesDirectory,
                    Path.Combine(ProfilesDirectory, ResourcePaths.SerialFolder),
                    Path.Combine(ProfilesDirectory, ResourcePaths.SocatFolder),
                    Path.Combine(ProfilesDirectory, ResourcePaths.PowerSupplyFolder),
                    MemoryRegionsDirectory,
                    LogsDirectory,
                    MainLogsDirectory,
                    ExportedLogsDirectory,
                    Path.Combine(ExportedLogsDirectory, ResourcePaths.CsvLogsFolder),
                    Path.Combine(ExportedLogsDirectory, ResourcePaths.TxtLogsFolder),
                    Path.Combine(ExportedLogsDirectory, ResourcePaths.JsonLogsFolder),
                    Path.Combine(ResourcesDirectory, ResourcePaths.JobsFolder),
                    Path.Combine(ResourcesDirectory, ResourcePaths.TasksFolder),
                    PayloadsDirectory,
                    DumpsDirectory
                };

                var createdDirectories = new List<string>();
                foreach (string? directory in directoriesToCreate)
                {
                    bool existedBefore = Directory.Exists(directory);
                    if (await EnsureDirectoryExistsAsync(directory).ConfigureAwait(false))
                    {
                        if (!existedBefore)
                        {
                            createdDirectories.Add(directory);
                        }
                    }
                    else
                    {
                        Logger?.LogWarning("Directory could not be ensured: {DirectoryPath}", directory);
                    }
                }

                _pathConfiguration.IsInitialized = true;

                Logger?.LogInformation("Path configuration initialized successfully. Created {DirectoryCount} directories",
                    createdDirectories.Count);

                if (createdDirectories.Count > 0)
                {
                    Logger?.LogDebug("Created directories: {CreatedDirectories}", string.Join(", ", createdDirectories));
                }

                return _pathConfiguration;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to initialize path configuration");
                throw new PathResolutionException("Path configuration initialization failed", BaseDirectory, "Initialize", ex);
            }
        }

        /// <summary>
        /// Resolves a path relative to the base directory
        /// </summary>
        /// <param name="relativePath">Path relative to base directory</param>
        /// <returns>Absolute path</returns>
        public string ResolvePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException(UIStrings.Exception_RelativePathNullOrEmpty, nameof(relativePath));
            }

            try
            {
                string fullPath = Path.Combine(BaseDirectory, relativePath);
                return Path.GetFullPath(fullPath);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to resolve path: {RelativePath} from base: {BaseDirectory}",
                    relativePath, BaseDirectory);
                throw new PathResolutionException($"Failed to resolve path: {relativePath}", relativePath, "ResolvePath", ex);
            }
        }

        /// <summary>
        /// Ensures a directory exists, creating it if necessary
        /// </summary>
        /// <param name="directoryPath">Path to directory</param>
        /// <returns>True if directory exists or was created successfully</returns>
        public async Task<bool> EnsureDirectoryExistsAsync(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentException(UIStrings.Exception_DirectoryPathNullOrEmpty, nameof(directoryPath));
            }

            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Logger?.LogDebug("Directory already exists: {DirectoryPath}", directoryPath);
                    return true;
                }

                Logger?.LogDebug("Creating directory: {DirectoryPath}", directoryPath);
                Directory.CreateDirectory(directoryPath);

                // Verify creation
                await Task.Delay(10).ConfigureAwait(false); // Small delay to ensure filesystem consistency

                if (Directory.Exists(directoryPath))
                {
                    Logger?.LogInformation("Successfully created directory: {DirectoryPath}", directoryPath);
                    return true;
                }

                Logger?.LogWarning("Directory creation reported success but directory does not exist: {DirectoryPath}", directoryPath);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger?.LogError(ex, "Access denied when creating directory: {DirectoryPath}", directoryPath);
                throw new PathResolutionException($"Access denied when creating directory: {directoryPath}",
                    directoryPath, "EnsureDirectoryExists", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger?.LogError(ex, "Parent directory not found when creating: {DirectoryPath}", directoryPath);
                throw new PathResolutionException($"Parent directory not found when creating: {directoryPath}",
                    directoryPath, "EnsureDirectoryExists", ex);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Unexpected error creating directory: {DirectoryPath}", directoryPath);
                throw new PathResolutionException($"Failed to create directory: {directoryPath}",
                    directoryPath, "EnsureDirectoryExists", ex);
            }
        }

        /// <summary>
        /// Validates that all required paths are accessible
        /// </summary>
        /// <returns>Validation result with any issues found</returns>
        public async Task<PathValidationResult> ValidatePathsAsync()
        {
            Logger?.LogInformation("Validating all required paths");

            var result = new PathValidationResult { IsValid = true };

            try
            {
                // Check base directory
                if (!Directory.Exists(BaseDirectory))
                {
                    result.Errors.Add($"Base directory does not exist: {BaseDirectory}");
                    result.IsValid = false;
                }

                // Check all key paths
                var pathsToValidate = new Dictionary<string, string>
                {
                    { "Resources Directory", ResourcesDirectory },
                    { "App Settings Path", Path.GetDirectoryName(AppSettingsPath)! },
                    { "Profiles Directory", ProfilesDirectory },
                    { "Serial Profiles Path", Path.GetDirectoryName(SerialProfilesPath)! },
                    { "Socat Profiles Path", Path.GetDirectoryName(SocatProfilesPath)! },
                    { "PowerSupply Profiles Path", Path.GetDirectoryName(PowerSupplyProfilesPath)! },
                    { "Memory Region Profiles Path", Path.GetDirectoryName(MemoryRegionProfilesPath)! },
                    { "Memory Regions Directory", MemoryRegionsDirectory },
                    { "Logs Directory", LogsDirectory },
                    { "Main Logs Directory", MainLogsDirectory },
                    { "Exported Logs Directory", ExportedLogsDirectory },
                    { "Jobs Path", Path.GetDirectoryName(JobsPath)! },
                    { "Tasks Path", Path.GetDirectoryName(TasksPath)! },
                    { "Payloads Directory", PayloadsDirectory },
                    { "Dumps Directory", DumpsDirectory }
                };

                foreach (KeyValuePair<string, string> kvp in pathsToValidate)
                {
                    string pathName = kvp.Key;
                    string pathValue = kvp.Value;

                    if (!Directory.Exists(pathValue))
                    {
                        try
                        {
                            if (await EnsureDirectoryExistsAsync(pathValue).ConfigureAwait(false))
                            {
                                result.CreatedDirectories.Add(pathValue);
                                Logger?.LogInformation("Created missing directory for {PathName}: {PathValue}", pathName, pathValue);
                            }
                            else
                            {
                                result.Errors.Add($"Failed to create directory for {pathName}: {pathValue}");
                                result.IsValid = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Error validating {pathName}: {ex.Message}");
                            result.IsValid = false;
                        }
                    }
                }

                Logger?.LogInformation("Path validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Created: {CreatedCount}",
                    result.IsValid, result.Errors.Count, result.CreatedDirectories.Count);

                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Unexpected error during path validation");
                result.Errors.Add($"Validation failed with unexpected error: {ex.Message}");
                result.IsValid = false;
                return result;
            }
        }

        /// <summary>
        /// Generates a timestamped main log file path
        /// </summary>
        /// <param name="rollingNumber">Rolling number for the log file</param>
        /// <returns>Full path to main log file</returns>
        public string GetMainLogPath(int rollingNumber = 0)
        {
            string timestamp = DateTime.UtcNow.ToLocalTime().ToString(FileNaming.TimestampFormat);
            string fileName = string.Format(ResourcePaths.MainLogFilePattern, timestamp, rollingNumber);
            return Path.Combine(MainLogsDirectory, fileName);
        }

        /// <summary>
        /// Generates a timestamped exported log file path
        /// </summary>
        /// <param name="format">Export format (CSV, TXT, JSON)</param>
        /// <returns>Full path to exported log file</returns>
        public string GetExportedLogPath(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentException(UIStrings.Exception_FormatNullOrEmpty, nameof(format));
            }

            string timestamp = DateTime.UtcNow.ToLocalTime().ToString(FileNaming.TimestampFormat);
            string extension = format.ToLowerInvariant() switch
            {
                "csv" => FileNaming.CsvExtension,
                "txt" => FileNaming.TxtExtension,
                "json" => FileNaming.JsonExtension,
                _ => format.ToLowerInvariant()
            };

            string fileName = string.Format(ResourcePaths.ExportedLogFilePattern, timestamp, extension);

            string formatDirectory = format.ToUpperInvariant() switch
            {
                "CSV" => Path.Combine(ExportedLogsDirectory, ResourcePaths.CsvLogsFolder),
                "TXT" => Path.Combine(ExportedLogsDirectory, ResourcePaths.TxtLogsFolder),
                "JSON" => Path.Combine(ExportedLogsDirectory, ResourcePaths.JsonLogsFolder),
                _ => ExportedLogsDirectory
            };

            return Path.Combine(formatDirectory, fileName);
        }

        /// <summary>
        /// Gets a path within the Resources directory using string constants
        /// </summary>
        /// <param name="pathComponents">Path components to combine</param>
        /// <returns>Full path within Resources directory</returns>
        public string GetResourcePath(params string[] pathComponents)
        {
            if (pathComponents == null || pathComponents.Length == 0)
            {
                return ResourcesDirectory;
            }

            string[] allComponents = new[] { ResourcePaths.ResourcesFolder }.Concat(pathComponents).ToArray();
            return ResolvePath(Path.Combine(allComponents));
        }
    }
}
