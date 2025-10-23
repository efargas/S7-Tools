using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;

namespace S7Tools.Services
{
    /// <summary>
    /// Service for managing application resources and their validation
    /// </summary>
    public sealed class ResourceManagerService : IResourceManagerService
    {
        private readonly ILogger<ResourceManagerService> _logger;
        private readonly IPathService _pathService;
        private readonly ResourceManifest _resourceManifest;

        /// <summary>
        /// Initializes a new instance of the ResourceManagerService class
        /// </summary>
        /// <param name="logger">Logger for structured logging</param>
        /// <param name="pathService">Path service for resolving paths</param>
        public ResourceManagerService(ILogger<ResourceManagerService> logger, IPathService pathService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _resourceManifest = ResourceManifest.CreateDefault();

            _logger.LogInformation("ResourceManagerService initialized with {DirectoryCount} required directories and {FileCount} required files",
                _resourceManifest.RequiredDirectories.Count, _resourceManifest.RequiredFiles.Count);
        }

        /// <summary>
        /// Initializes all required application resources
        /// </summary>
        /// <returns>Resource initialization result</returns>
        public async Task<ResourceInitializationResult> InitializeResourcesAsync()
        {
            _logger.LogInformation("Starting resource initialization");
            var stopwatch = Stopwatch.StartNew();
            var result = new ResourceInitializationResult();

            try
            {
                // First, resolve all absolute paths in the manifest
                ResolveManifestPaths();

                // Create all required directories
                await CreateRequiredDirectoriesAsync(result).ConfigureAwait(false);

                // Create all required files with default content
                await CreateRequiredFilesAsync(result).ConfigureAwait(false);

                // Validate everything was created correctly
                ResourceValidationResult validationResult = await ValidateResourcesAsync().ConfigureAwait(false);

                result.Success = validationResult.IsValid && result.Errors.Count == 0;

                if (!validationResult.IsValid)
                {
                    result.Errors.AddRange(validationResult.MissingResources.Select(r => $"Missing resource: {r}"));
                    result.Errors.AddRange(validationResult.InaccessibleResources.Select(r => $"Inaccessible resource: {r}"));
                    result.Errors.AddRange(validationResult.CorruptedResources.Select(r => $"Corrupted resource: {r}"));
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                _logger.LogInformation("Resource initialization completed in {Duration}ms. Success: {Success}, Created: {CreatedCount}, Errors: {ErrorCount}",
                    result.Duration.TotalMilliseconds, result.Success, result.CreatedResources.Count, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = false;
                result.Errors.Add($"Resource initialization failed: {ex.Message}");

                _logger.LogError(ex, "Resource initialization failed after {Duration}ms", result.Duration.TotalMilliseconds);
                return result;
            }
        }

        /// <summary>
        /// Validates that all required resources exist and are accessible
        /// </summary>
        /// <returns>Resource validation result</returns>
        public async Task<ResourceValidationResult> ValidateResourcesAsync()
        {
            _logger.LogInformation("Starting resource validation");
            var result = new ResourceValidationResult { IsValid = true };

            try
            {
                // Validate required directories
                foreach (Core.Models.Configuration.DirectoryInfo dirInfo in _resourceManifest.RequiredDirectories)
                {
                    if (string.IsNullOrEmpty(dirInfo.AbsolutePath))
                    {
                        dirInfo.AbsolutePath = _pathService.ResolvePath(dirInfo.RelativePath);
                    }

                    if (!Directory.Exists(dirInfo.AbsolutePath))
                    {
                        result.MissingResources.Add($"Directory: {dirInfo.AbsolutePath} ({dirInfo.Purpose})");
                        result.IsValid = false;
                    }
                    else
                    {
                        // Check write access if required
                        if (dirInfo.IsWritable && !await CanWriteToDirectoryAsync(dirInfo.AbsolutePath).ConfigureAwait(false))
                        {
                            result.InaccessibleResources.Add($"Directory (no write access): {dirInfo.AbsolutePath}");
                            result.IsValid = false;
                        }
                    }
                }

                // Validate required files
                foreach (Core.Models.Configuration.FileInfo fileInfo in _resourceManifest.RequiredFiles)
                {
                    if (string.IsNullOrEmpty(fileInfo.AbsolutePath))
                    {
                        fileInfo.AbsolutePath = _pathService.ResolvePath(fileInfo.RelativePath);
                    }

                    if (!File.Exists(fileInfo.AbsolutePath))
                    {
                        result.MissingResources.Add($"File: {fileInfo.AbsolutePath} ({fileInfo.Purpose})");
                        result.IsValid = false;
                    }
                    else
                    {
                        // Validate file content if it's a JSON file
                        if (fileInfo.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!await IsValidJsonFileAsync(fileInfo.AbsolutePath).ConfigureAwait(false))
                            {
                                result.CorruptedResources.Add($"Invalid JSON file: {fileInfo.AbsolutePath}");
                                result.Warnings.Add($"JSON file may be corrupted: {fileInfo.AbsolutePath}");
                            }
                        }
                    }
                }

                _logger.LogInformation("Resource validation completed. Valid: {IsValid}, Missing: {MissingCount}, Inaccessible: {InaccessibleCount}, Corrupted: {CorruptedCount}",
                    result.IsValid, result.MissingResources.Count, result.InaccessibleResources.Count, result.CorruptedResources.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resource validation failed");
                result.IsValid = false;
                result.Warnings.Add($"Validation failed with error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Creates missing resources based on the resource manifest
        /// </summary>
        /// <returns>Resource creation result</returns>
        public async Task<ResourceCreationResult> CreateMissingResourcesAsync()
        {
            _logger.LogInformation("Creating missing resources");
            var stopwatch = Stopwatch.StartNew();
            var result = new ResourceCreationResult { Success = true };

            try
            {
                // Create missing directories
                foreach (Core.Models.Configuration.DirectoryInfo dirInfo in _resourceManifest.RequiredDirectories)
                {
                    if (string.IsNullOrEmpty(dirInfo.AbsolutePath))
                    {
                        dirInfo.AbsolutePath = _pathService.ResolvePath(dirInfo.RelativePath);
                    }

                    if (!Directory.Exists(dirInfo.AbsolutePath) && dirInfo.CreateIfMissing)
                    {
                        try
                        {
                            if (await _pathService.EnsureDirectoryExistsAsync(dirInfo.AbsolutePath).ConfigureAwait(false))
                            {
                                result.CreatedResources.Add($"Directory: {dirInfo.AbsolutePath}");
                                _logger.LogInformation("Created missing directory: {DirectoryPath}", dirInfo.AbsolutePath);
                            }
                            else
                            {
                                result.FailedResources.Add($"Directory: {dirInfo.AbsolutePath}");
                                result.Errors.Add($"Failed to create directory: {dirInfo.AbsolutePath}");
                                result.Success = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailedResources.Add($"Directory: {dirInfo.AbsolutePath}");
                            result.Errors.Add($"Error creating directory {dirInfo.AbsolutePath}: {ex.Message}");
                            result.Success = false;
                        }
                    }
                }

                // Create missing files
                foreach (Core.Models.Configuration.FileInfo fileInfo in _resourceManifest.RequiredFiles)
                {
                    if (string.IsNullOrEmpty(fileInfo.AbsolutePath))
                    {
                        fileInfo.AbsolutePath = _pathService.ResolvePath(fileInfo.RelativePath);
                    }

                    if (!File.Exists(fileInfo.AbsolutePath))
                    {
                        try
                        {
                            // Ensure parent directory exists
                            string? parentDir = Path.GetDirectoryName(fileInfo.AbsolutePath);
                            if (!string.IsNullOrEmpty(parentDir))
                            {
                                await _pathService.EnsureDirectoryExistsAsync(parentDir).ConfigureAwait(false);
                            }

                            // Create file with default content
                            await File.WriteAllTextAsync(fileInfo.AbsolutePath, fileInfo.DefaultContent, Encoding.UTF8).ConfigureAwait(false);

                            result.CreatedResources.Add($"File: {fileInfo.AbsolutePath}");
                            _logger.LogInformation("Created missing file: {FilePath} with default content", fileInfo.AbsolutePath);
                        }
                        catch (Exception ex)
                        {
                            result.FailedResources.Add($"File: {fileInfo.AbsolutePath}");
                            result.Errors.Add($"Error creating file {fileInfo.AbsolutePath}: {ex.Message}");
                            result.Success = false;
                        }
                    }
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                _logger.LogInformation("Resource creation completed in {Duration}ms. Success: {Success}, Created: {CreatedCount}, Failed: {FailedCount}",
                    result.Duration.TotalMilliseconds, result.Success, result.CreatedResources.Count, result.FailedResources.Count);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                result.Success = false;
                result.Errors.Add($"Resource creation failed: {ex.Message}");

                _logger.LogError(ex, "Resource creation failed after {Duration}ms", result.Duration.TotalMilliseconds);
                return result;
            }
        }

        /// <summary>
        /// Gets the resource manifest defining required resources
        /// </summary>
        /// <returns>Current resource manifest</returns>
        public ResourceManifest GetResourceManifest()
        {
            return _resourceManifest;
        }

        /// <summary>
        /// Creates default profile files if they don't exist
        /// </summary>
        /// <returns>True if profiles were created or already existed</returns>
        public async Task<bool> EnsureDefaultProfilesExistAsync()
        {
            _logger.LogInformation("Ensuring default profile files exist");

            try
            {
                string[] profileFiles = new[]
                {
                    _pathService.SerialProfilesPath,
                    _pathService.SocatProfilesPath,
                    _pathService.PowerSupplyProfilesPath,
                    _pathService.JobsPath,
                    _pathService.TasksPath
                };

                bool allExist = true;
                foreach (string? profilePath in profileFiles)
                {
                    if (!File.Exists(profilePath))
                    {
                        try
                        {
                            // Ensure parent directory exists
                            string? parentDir = Path.GetDirectoryName(profilePath);
                            if (!string.IsNullOrEmpty(parentDir))
                            {
                                await _pathService.EnsureDirectoryExistsAsync(parentDir).ConfigureAwait(false);
                            }

                            // Create empty JSON array as default content
                            await File.WriteAllTextAsync(profilePath, "[]", Encoding.UTF8).ConfigureAwait(false);
                            _logger.LogInformation("Created default profile file: {ProfilePath}", profilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create default profile file: {ProfilePath}", profilePath);
                            allExist = false;
                        }
                    }
                }

                return allExist;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure default profiles exist");
                return false;
            }
        }

        /// <summary>
        /// Validates that a specific resource exists
        /// </summary>
        /// <param name="resourcePath">Path to resource</param>
        /// <returns>True if resource exists and is accessible</returns>
        public Task<bool> ResourceExistsAsync(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return Task.FromResult(false);
            }

            try
            {
                // Check if it's a file or directory
                if (File.Exists(resourcePath))
                {
                    // For files, try to access them to ensure they're not locked
                    using FileStream stream = File.OpenRead(resourcePath);
                    return Task.FromResult(true);
                }

                if (Directory.Exists(resourcePath))
                {
                    // For directories, check if we can list contents
                    Directory.GetFiles(resourcePath, "*", SearchOption.TopDirectoryOnly);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resource exists but is not accessible: {ResourcePath}", resourcePath);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Creates a resource from template if it doesn't exist
        /// </summary>
        /// <param name="resourceInfo">Resource information</param>
        /// <returns>True if resource was created or already existed</returns>
        public async Task<bool> EnsureResourceExistsAsync(ResourceInfo resourceInfo)
        {
            if (resourceInfo == null)
            {
                throw new ArgumentNullException(nameof(resourceInfo));
            }

            try
            {
                string fullPath = _pathService.ResolvePath(resourceInfo.Path);

                if (resourceInfo.Type == ResourceType.Directory)
                {
                    return await _pathService.EnsureDirectoryExistsAsync(fullPath).ConfigureAwait(false);
                }

                if (resourceInfo.Type == ResourceType.File && !File.Exists(fullPath))
                {
                    // Ensure parent directory exists
                    string? parentDir = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(parentDir))
                    {
                        await _pathService.EnsureDirectoryExistsAsync(parentDir).ConfigureAwait(false);
                    }

                    // Create empty file or use template content
                    await File.WriteAllTextAsync(fullPath, string.Empty, Encoding.UTF8).ConfigureAwait(false);
                    _logger.LogInformation("Created resource from template: {ResourcePath}", fullPath);
                }

                return await ResourceExistsAsync(fullPath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure resource exists: {ResourceName} at {ResourcePath}",
                    resourceInfo.Name, resourceInfo.Path);
                return false;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Resolves absolute paths for all items in the resource manifest
        /// </summary>
        private void ResolveManifestPaths()
        {
            // Resolve directory paths
            foreach (Core.Models.Configuration.DirectoryInfo dirInfo in _resourceManifest.RequiredDirectories)
            {
                if (string.IsNullOrEmpty(dirInfo.AbsolutePath))
                {
                    dirInfo.AbsolutePath = _pathService.ResolvePath(dirInfo.RelativePath);
                }
            }

            // Resolve file paths
            foreach (Core.Models.Configuration.FileInfo fileInfo in _resourceManifest.RequiredFiles)
            {
                if (string.IsNullOrEmpty(fileInfo.AbsolutePath))
                {
                    fileInfo.AbsolutePath = _pathService.ResolvePath(fileInfo.RelativePath);
                }
            }
        }

        /// <summary>
        /// Creates all required directories from the manifest
        /// </summary>
        private async Task CreateRequiredDirectoriesAsync(ResourceInitializationResult result)
        {
            foreach (Core.Models.Configuration.DirectoryInfo dirInfo in _resourceManifest.RequiredDirectories)
            {
                if (!Directory.Exists(dirInfo.AbsolutePath) && dirInfo.CreateIfMissing)
                {
                    try
                    {
                        if (await _pathService.EnsureDirectoryExistsAsync(dirInfo.AbsolutePath).ConfigureAwait(false))
                        {
                            result.CreatedResources.Add($"Directory: {dirInfo.AbsolutePath}");
                        }
                        else
                        {
                            result.Errors.Add($"Failed to create directory: {dirInfo.AbsolutePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error creating directory {dirInfo.AbsolutePath}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Creates all required files from the manifest
        /// </summary>
        private async Task CreateRequiredFilesAsync(ResourceInitializationResult result)
        {
            foreach (Core.Models.Configuration.FileInfo fileInfo in _resourceManifest.RequiredFiles)
            {
                if (!File.Exists(fileInfo.AbsolutePath))
                {
                    try
                    {
                        // Ensure parent directory exists
                        string? parentDir = Path.GetDirectoryName(fileInfo.AbsolutePath);
                        if (!string.IsNullOrEmpty(parentDir))
                        {
                            await _pathService.EnsureDirectoryExistsAsync(parentDir).ConfigureAwait(false);
                        }

                        await File.WriteAllTextAsync(fileInfo.AbsolutePath, fileInfo.DefaultContent, Encoding.UTF8).ConfigureAwait(false);
                        result.CreatedResources.Add($"File: {fileInfo.AbsolutePath}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error creating file {fileInfo.AbsolutePath}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the current process can write to a directory
        /// </summary>
        private static async Task<bool> CanWriteToDirectoryAsync(string directoryPath)
        {
            string? testFile = null;
            try
            {
                testFile = Path.Combine(directoryPath, $"test_write_{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, "test").ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(testFile) && File.Exists(testFile))
                {
                    try
                    {
                        File.Delete(testFile);
                    }
                    catch
                    {
                        // Best-effort cleanup; swallow exceptions
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Validates that a JSON file has valid syntax
        /// </summary>
        private static async Task<bool> IsValidJsonFileAsync(string filePath)
        {
            try
            {
                string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return true; // Empty files are considered valid
                }

                System.Text.Json.JsonDocument.Parse(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
