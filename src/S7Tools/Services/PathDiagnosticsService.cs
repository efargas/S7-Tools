using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using System.Text;

namespace S7Tools.Services
{
    /// <summary>
    /// Service for diagnosing path-related issues and providing detailed error information
    /// </summary>
    public sealed class PathDiagnosticsService : IPathDiagnosticsService
    {
        private readonly ILogger<PathDiagnosticsService> _logger;
        private readonly IPathService _pathService;

        /// <summary>
        /// Initializes a new instance of the PathDiagnosticsService class
        /// </summary>
        /// <param name="logger">Logger for structured logging</param>
        /// <param name="pathService">Path service for resolving paths</param>
        public PathDiagnosticsService(ILogger<PathDiagnosticsService> logger, IPathService pathService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        /// <summary>
        /// Performs comprehensive path validation and reports any issues
        /// </summary>
        /// <returns>Diagnostic report with findings</returns>
        public async Task<PathDiagnosticReport> RunDiagnosticsAsync()
        {
            _logger.LogInformation("Starting path diagnostics");

            var report = new PathDiagnosticReport
            {
                StartTime = DateTime.UtcNow,
                BaseDirectory = _pathService.BaseDirectory
            };

            try
            {
                // Test base directory access
                await ValidateBaseDirectoryAsync(report).ConfigureAwait(false);

                // Test resource paths
                await ValidateResourcePathsAsync(report).ConfigureAwait(false);

                // Test path resolution
                await ValidatePathResolutionAsync(report).ConfigureAwait(false);

                // Test directory creation permissions
                await ValidateDirectoryCreationAsync(report).ConfigureAwait(false);

                // Test file access permissions
                await ValidateFileAccessAsync(report).ConfigureAwait(false);

                report.EndTime = DateTime.UtcNow;
                report.Success = report.Errors.Count == 0;

                _logger.LogInformation("Path diagnostics completed. Success: {Success}, Warnings: {WarningCount}, Errors: {ErrorCount}",
                    report.Success, report.Warnings.Count, report.Errors.Count);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Path diagnostics failed unexpectedly");
                report.EndTime = DateTime.UtcNow;
                report.Success = false;
                report.Errors.Add($"Diagnostics failed: {ex.Message}");
                return report;
            }
        }

        /// <summary>
        /// Validates access to the base directory
        /// </summary>
        /// <param name="report">Diagnostic report to update</param>
        private async Task ValidateBaseDirectoryAsync(PathDiagnosticReport report)
        {
            try
            {
                string baseDir = _pathService.BaseDirectory;

                if (!Directory.Exists(baseDir))
                {
                    report.Errors.Add($"Base directory does not exist: {baseDir}");
                    return;
                }

                var dirInfo = new System.IO.DirectoryInfo(baseDir);

                // Check read permissions
                try
                {
                    _ = dirInfo.GetDirectories();
                    report.Validations.Add("Base directory read access: OK");
                }
                catch (UnauthorizedAccessException)
                {
                    report.Errors.Add($"No read access to base directory: {baseDir}");
                }

                // Check write permissions
                string testFile = Path.Combine(baseDir, $"pathtest_{Guid.NewGuid():N}.tmp");
                try
                {
                    await File.WriteAllTextAsync(testFile, "test").ConfigureAwait(false);
                    File.Delete(testFile);
                    report.Validations.Add("Base directory write access: OK");
                }
                catch (UnauthorizedAccessException)
                {
                    report.Warnings.Add($"No write access to base directory: {baseDir}");
                }
                catch (Exception ex)
                {
                    report.Warnings.Add($"Base directory write test failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                report.Errors.Add($"Base directory validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates all resource paths
        /// </summary>
        /// <param name="report">Diagnostic report to update</param>
        private async Task ValidateResourcePathsAsync(PathDiagnosticReport report)
        {
            var resourcePaths = new Dictionary<string, string>
            {
                ["Resources Directory"] = _pathService.ResourcesDirectory,
                ["App Settings Path"] = _pathService.AppSettingsPath,
                ["Profiles Directory"] = _pathService.ProfilesDirectory,
                ["Logs Directory"] = _pathService.LogsDirectory,
                ["Jobs Path"] = _pathService.JobsPath,
                ["Tasks Path"] = _pathService.TasksPath,
                ["Payloads Directory"] = _pathService.PayloadsDirectory,
                ["Dumps Directory"] = _pathService.DumpsDirectory
            };

            foreach (KeyValuePair<string, string> kvp in resourcePaths)
            {
                try
                {
                    string path = kvp.Value;
                    bool isFile = Path.HasExtension(path);

                    if (isFile)
                    {
                        string? directory = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            await _pathService.EnsureDirectoryExistsAsync(directory).ConfigureAwait(false);
                            report.Validations.Add($"{kvp.Key} directory structure: OK");
                        }
                    }
                    else
                    {
                        await _pathService.EnsureDirectoryExistsAsync(path).ConfigureAwait(false);
                        report.Validations.Add($"{kvp.Key}: OK");
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"{kvp.Key} validation failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates path resolution functionality
        /// </summary>
        /// <param name="report">Diagnostic report to update</param>
        private Task ValidatePathResolutionAsync(PathDiagnosticReport report)
        {
            string[] testPaths = new[]
            {
                "test",
                "test/subfolder",
                "test/subfolder/file.txt",
                Path.Combine("Resources", "test.json"),
                Path.Combine("Resources", "AppSettings", "test.json")
            };

            foreach (string? testPath in testPaths)
            {
                try
                {
                    string resolved = _pathService.ResolvePath(testPath);

                    if (string.IsNullOrEmpty(resolved))
                    {
                        report.Warnings.Add($"Path resolution returned empty result for: {testPath}");
                    }
                    else if (!Path.IsPathRooted(resolved))
                    {
                        report.Warnings.Add($"Path resolution returned non-absolute path for: {testPath} -> {resolved}");
                    }
                    else
                    {
                        report.Validations.Add($"Path resolution for '{testPath}': OK");
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"Path resolution failed for '{testPath}': {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates directory creation permissions
        /// </summary>
        /// <param name="report">Diagnostic report to update</param>
        private async Task ValidateDirectoryCreationAsync(PathDiagnosticReport report)
        {
            string testDirName = $"pathtest_{Guid.NewGuid():N}";
            string testDir = _pathService.ResolvePath(testDirName);

            try
            {
                bool success = await _pathService.EnsureDirectoryExistsAsync(testDir).ConfigureAwait(false);

                if (success && Directory.Exists(testDir))
                {
                    Directory.Delete(testDir);
                    report.Validations.Add("Directory creation: OK");
                }
                else
                {
                    report.Warnings.Add("Directory creation returned success but directory was not created");
                }
            }
            catch (Exception ex)
            {
                report.Errors.Add($"Directory creation test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates file access permissions
        /// </summary>
        /// <param name="report">Diagnostic report to update</param>
        private async Task ValidateFileAccessAsync(PathDiagnosticReport report)
        {
            string testFileName = $"pathtest_{Guid.NewGuid():N}.tmp";
            string testFile = _pathService.ResolvePath(testFileName);

            try
            {
                // Test file creation
                await File.WriteAllTextAsync(testFile, "test content").ConfigureAwait(false);
                report.Validations.Add("File creation: OK");

                // Test file reading
                string content = await File.ReadAllTextAsync(testFile).ConfigureAwait(false);
                if (content == "test content")
                {
                    report.Validations.Add("File reading: OK");
                }
                else
                {
                    report.Warnings.Add("File reading returned unexpected content");
                }

                // Test file modification
                await File.WriteAllTextAsync(testFile, "modified content").ConfigureAwait(false);
                report.Validations.Add("File modification: OK");

                // Test file deletion
                File.Delete(testFile);
                if (!File.Exists(testFile))
                {
                    report.Validations.Add("File deletion: OK");
                }
                else
                {
                    report.Warnings.Add("File deletion did not remove the file");
                }
            }
            catch (Exception ex)
            {
                report.Errors.Add($"File access test failed: {ex.Message}");

                // Clean up test file if it exists
                try
                {
                    if (File.Exists(testFile))
                    {
                        File.Delete(testFile);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Generates a detailed diagnostic report as formatted text
        /// </summary>
        /// <param name="report">Diagnostic report to format</param>
        /// <returns>Formatted diagnostic report</returns>
        public string GenerateDetailedReport(PathDiagnosticReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Path Diagnostics Report ===");
            sb.AppendLine($"Report Time: {report.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Duration: {(report.EndTime - report.StartTime).TotalMilliseconds:F0}ms");
            sb.AppendLine($"Base Directory: {report.BaseDirectory}");
            sb.AppendLine($"Overall Status: {(report.Success ? "SUCCESS" : "FAILURE")}");
            sb.AppendLine();

            if (report.Validations.Count > 0)
            {
                sb.AppendLine("✓ Successful Validations:");
                foreach (string validation in report.Validations)
                {
                    sb.AppendLine($"  • {validation}");
                }
                sb.AppendLine();
            }

            if (report.Warnings.Count > 0)
            {
                sb.AppendLine("⚠ Warnings:");
                foreach (string warning in report.Warnings)
                {
                    sb.AppendLine($"  • {warning}");
                }
                sb.AppendLine();
            }

            if (report.Errors.Count > 0)
            {
                sb.AppendLine("✗ Errors:");
                foreach (string error in report.Errors)
                {
                    sb.AppendLine($"  • {error}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== End Report ===");

            return sb.ToString();
        }
    }
}
