using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Services;

namespace S7Tools.Core.Tests.Settings
{
    /// <summary>
    /// Tests for ApplicationSettingsService to verify settings hierarchy functionality
    /// </summary>
    public class ApplicationSettingsServiceTests
    {
        /// <summary>
        /// Creates a test service instance with dependencies
        /// </summary>
        private ApplicationSettingsService CreateTestService()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());

            // Mock path service for testing
            var mockPathService = new MockPathService();
            services.AddSingleton<IPathService>(mockPathService);

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            ILogger<ApplicationSettingsService> logger = serviceProvider.GetRequiredService<ILogger<ApplicationSettingsService>>();
            IPathService pathService = serviceProvider.GetRequiredService<IPathService>();

            return new ApplicationSettingsService(logger, pathService);
        }

        /// <summary>
        /// Tests that default settings are loaded correctly
        /// </summary>
        [Fact]
        public async Task LoadSettingsAsync_WithNoUserSettings_ReturnsDefaultSettings()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();

            // Act
            ApplicationSettings settings = await service.LoadSettingsAsync();

            // Assert
            Assert.NotNull(settings);
            Assert.True(settings.DefaultSettings.Count > 0, "Default settings should be populated");
            Assert.Empty(settings.UserSettings); // No user settings file exists
            Assert.True(settings.EffectiveSettings.Count > 0, "Effective settings should contain defaults");

            // Verify specific default values
            Assert.Equal("Information", settings.GetSetting<string>("logging.level"));
            Assert.True(settings.GetSetting<bool>("logging.enableFileLogging"));
            Assert.Equal("System", settings.GetSetting<string>("ui.theme"));
        }

        /// <summary>
        /// Tests that user settings override defaults correctly
        /// </summary>
        [Fact]
        public async Task SetSettingAsync_UserSettingOverridesDefault_CorrectHierarchy()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();
            await service.LoadSettingsAsync();

            // Act - Set user setting to override default
            await service.SetSettingAsync("logging.level", "Debug");
            await service.SetSettingAsync("ui.theme", "Dark");

            // Assert - User settings override defaults
            Assert.Equal("Debug", service.GetSetting<string>("logging.level"));
            Assert.Equal("Dark", service.GetSetting<string>("ui.theme"));

            // Verify defaults still exist but are overridden
            ApplicationSettings settings = await service.LoadSettingsAsync();
            Assert.Equal("Information", settings.DefaultSettings["logging.level"]); // Default unchanged
            Assert.Equal("Debug", settings.GetSetting<string>("logging.level")); // Effective value is user override
            Assert.Equal("Debug", service.GetSetting<string>("logging.level")); // Service returns effective value

            // Verify user settings were saved
            Assert.True(settings.UserSettings.ContainsKey("logging.level"));
            Assert.True(settings.UserSettings.ContainsKey("ui.theme"));
        }

        /// <summary>
        /// Tests that resetting user settings reverts to defaults
        /// </summary>
        [Fact]
        public async Task ResetSettingAsync_UserSettingReset_RevertsToDefault()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();
            await service.LoadSettingsAsync();
            await service.SetSettingAsync("logging.level", "Debug");

            // Verify user override is active
            Assert.Equal("Debug", service.GetSetting<string>("logging.level"));

            // Act - Reset to default
            await service.ResetSettingAsync("logging.level");

            // Assert - Reverted to default
            Assert.Equal("Information", service.GetSetting<string>("logging.level"));
        }

        /// <summary>
        /// Tests settings change events are fired correctly
        /// </summary>
        [Fact]
        public async Task SettingsChanged_EventFired_WhenSettingChanged()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();
            await service.LoadSettingsAsync();

            S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs? eventArgs = null;
            service.SettingsChanged += (sender, args) => eventArgs = args;

            // Act
            await service.SetSettingAsync("test.setting", "test.value");

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal("test.setting", eventArgs.Key);
            Assert.Equal("test.value", eventArgs.NewValue);
            Assert.True(eventArgs.IsUserSetting);
        }

        /// <summary>
        /// Mock path service for testing
        /// </summary>
        private class MockPathService : IPathService
        {
            private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            public string BaseDirectory => _tempDir;
            public string ResourcesDirectory => Path.Combine(_tempDir, "Resources");
            public string AppSettingsPath => Path.Combine(_tempDir, "AppSettings", "AppSettings.json");
            public string ProfilesDirectory => Path.Combine(ResourcesDirectory, "Profiles");
            public string SerialProfilesPath => Path.Combine(ProfilesDirectory, "Serial", "SerialProfiles.json");
            public string SocatProfilesPath => Path.Combine(ProfilesDirectory, "Socat", "SocatProfiles.json");
            public string PowerSupplyProfilesPath => Path.Combine(ProfilesDirectory, "PowerSupply", "PowerSupplyProfiles.json");
            public string MemoryRegionProfilesPath => Path.Combine(ProfilesDirectory, "MemoryRegions", "MemoryRegionProfiles.json");
            public string PayloadSetProfilesPath => Path.Combine(ProfilesDirectory, "PayloadSets", "PayloadSetProfiles.json");
            public string MemoryRegionsDirectory => Path.Combine(ResourcesDirectory, "MemoryRegions");
            public string LogsDirectory => Path.Combine(ResourcesDirectory, "Logs");
            public string MainLogsDirectory => Path.Combine(LogsDirectory, "Main");
            public string ExportedLogsDirectory => Path.Combine(LogsDirectory, "Exported");
            public string JobsPath => Path.Combine(ResourcesDirectory, "Jobs", "Jobs.json");
            public string TasksPath => Path.Combine(ResourcesDirectory, "Tasks", "Tasks.json");
            public string PayloadsDirectory => Path.Combine(ResourcesDirectory, "Payloads");
            public string DumpsDirectory => Path.Combine(ResourcesDirectory, "Dumps");

            public Task<PathConfiguration> InitializeAsync() => Task.FromResult(new PathConfiguration { BaseDirectory = _tempDir });

            public Task<bool> EnsureDirectoryExistsAsync(string directoryPath)
            {
                Directory.CreateDirectory(directoryPath);
                return Task.FromResult(true);
            }

            public string ResolvePath(string relativePath) => Path.Combine(_tempDir, relativePath);

            public Task<PathValidationResult> ValidatePathsAsync() => Task.FromResult(new PathValidationResult { IsValid = true });

            public string GetMainLogPath(int rollingNumber = 0) => Path.Combine(MainLogsDirectory, $"main_{rollingNumber}.log");

            public string GetExportedLogPath(string format) => Path.Combine(ExportedLogsDirectory, $"export.{format.ToLowerInvariant()}");

            public string GetResourcePath(params string[] pathComponents) => Path.Combine(new[] { ResourcesDirectory }.Concat(pathComponents).ToArray());
        }
    }
}
