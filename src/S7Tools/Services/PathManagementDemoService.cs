using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;

namespace S7Tools.Services
{
    /// <summary>
    /// Demo service to showcase the complete path management functionality
    /// </summary>
    public sealed class PathManagementDemoService
    {
        private readonly ILogger<PathManagementDemoService> _logger;
        private readonly IPathService _pathService;
        private readonly IResourceManagerService _resourceManager;
        private readonly IApplicationSettingsService _settingsService;
        private readonly IPathDiagnosticsService _diagnosticsService;

        /// <summary>
        /// Initializes a new instance of the PathManagementDemoService class
        /// </summary>
        public PathManagementDemoService(
            ILogger<PathManagementDemoService> logger,
            IPathService pathService,
            IResourceManagerService resourceManager,
            IApplicationSettingsService settingsService,
            IPathDiagnosticsService diagnosticsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
        }

        /// <summary>
        /// Demonstrates all path management features
        /// </summary>
        public async Task DemonstratePathManagementAsync()
        {
            _logger.LogInformation("=== Path Management Demo ===");

            try
            {
                // Demo 1: Dynamic Path Resolution
                await DemoDynamicPathResolutionAsync().ConfigureAwait(false);

                // Demo 2: Settings Hierarchy
                await DemoSettingsHierarchyAsync().ConfigureAwait(false);

                // Demo 3: Error Handling and Diagnostics
                await DemoErrorHandlingAsync().ConfigureAwait(false);

                _logger.LogInformation("=== Path Management Demo Complete ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Path management demo failed");
            }
        }

        /// <summary>
        /// Demonstrates dynamic path resolution features
        /// </summary>
        private async Task DemoDynamicPathResolutionAsync()
        {
            _logger.LogInformation("--- Demo 1: Dynamic Path Resolution ---");

            // Show base directory
            _logger.LogInformation("Base Directory: {BaseDir}", _pathService.BaseDirectory);

            // Show all resource paths
            _logger.LogInformation("Resource Paths:");
            _logger.LogInformation("  Resources: {Path}", _pathService.ResourcesDirectory);
            _logger.LogInformation("  App Settings: {Path}", _pathService.AppSettingsPath);
            _logger.LogInformation("  Profiles: {Path}", _pathService.ProfilesDirectory);
            _logger.LogInformation("  Logs: {Path}", _pathService.LogsDirectory);
            _logger.LogInformation("  Jobs: {Path}", _pathService.JobsPath);
            _logger.LogInformation("  Tasks: {Path}", _pathService.TasksPath);
            _logger.LogInformation("  Payloads: {Path}", _pathService.PayloadsDirectory);
            _logger.LogInformation("  Dumps: {Path}", _pathService.DumpsDirectory);

            // Demonstrate resource initialization
            ResourceInitializationResult result = await _resourceManager.InitializeResourcesAsync().ConfigureAwait(false);
            _logger.LogInformation("Resource Initialization: {Success} ({CreatedCount} created, {ErrorCount} errors)",
                result.Success, result.CreatedResources.Count, result.Errors.Count);

            foreach (string created in result.CreatedResources)
            {
                _logger.LogInformation("  Created: {Resource}", created);
            }

            foreach (string error in result.Errors)
            {
                _logger.LogWarning("  Error: {Error}", error);
            }
        }

        /// <summary>
        /// Demonstrates settings hierarchy features
        /// </summary>
        private async Task DemoSettingsHierarchyAsync()
        {
            _logger.LogInformation("--- Demo 2: Settings Hierarchy ---");

            // Load settings
            ApplicationSettings settings = await _settingsService.LoadSettingsAsync().ConfigureAwait(false);
            _logger.LogInformation("Settings loaded - Defaults: {DefaultCount}, User: {UserCount}, Effective: {EffectiveCount}",
                settings.DefaultSettings.Count, settings.UserSettings.Count, settings.EffectiveSettings.Count);

            // Show some default settings
            _logger.LogInformation("Default Settings Examples:");
            _logger.LogInformation("  logging.level: {Value}", _settingsService.GetSetting<string>("logging.level"));
            _logger.LogInformation("  ui.theme: {Value}", _settingsService.GetSetting<string>("ui.theme"));
            _logger.LogInformation("  paths.autoCreateDirectories: {Value}", _settingsService.GetSetting<bool>("paths.autoCreateDirectories"));

            // Demonstrate user override
            await _settingsService.SetSettingAsync("demo.customSetting", "Custom Value").ConfigureAwait(false);
            _logger.LogInformation("Set user setting: demo.customSetting = 'Custom Value'");

            string customValue = _settingsService.GetSetting<string>("demo.customSetting");
            _logger.LogInformation("Retrieved user setting: demo.customSetting = '{Value}'", customValue);

            // Reset to default
            await _settingsService.ResetSettingAsync("demo.customSetting").ConfigureAwait(false);
            string resetValue = _settingsService.GetSetting<string>("demo.customSetting", "DEFAULT");
            _logger.LogInformation("After reset: demo.customSetting = '{Value}'", resetValue);
        }

        /// <summary>
        /// Demonstrates error handling and diagnostics features
        /// </summary>
        private async Task DemoErrorHandlingAsync()
        {
            _logger.LogInformation("--- Demo 3: Error Handling and Diagnostics ---");

            // Run path diagnostics
            PathDiagnosticReport report = await _diagnosticsService.RunDiagnosticsAsync().ConfigureAwait(false);

            _logger.LogInformation("Diagnostics Summary: {Summary}", report.Summary);
            _logger.LogInformation("Base Directory: {BaseDir}", report.BaseDirectory);
            _logger.LogInformation("Duration: {Duration}ms", report.Duration.TotalMilliseconds);

            // Show detailed results
            _logger.LogInformation("Successful Validations: {Count}", report.Validations.Count);
            foreach (string? validation in report.Validations.Take(5)) // Show first 5
            {
                _logger.LogInformation("  ✓ {Validation}", validation);
            }

            if (report.Warnings.Count > 0)
            {
                _logger.LogInformation("Warnings: {Count}", report.Warnings.Count);
                foreach (string warning in report.Warnings)
                {
                    _logger.LogWarning("  ⚠ {Warning}", warning);
                }
            }

            if (report.Errors.Count > 0)
            {
                _logger.LogInformation("Errors: {Count}", report.Errors.Count);
                foreach (string error in report.Errors)
                {
                    _logger.LogError("  ✗ {Error}", error);
                }
            }

            // Generate and log detailed report
            string detailedReport = _diagnosticsService.GenerateDetailedReport(report);
            _logger.LogInformation("Detailed Diagnostics Report:\n{Report}", detailedReport);
        }
    }
}
