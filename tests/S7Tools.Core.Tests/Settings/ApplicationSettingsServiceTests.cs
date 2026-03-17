using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration;
using S7Tools.Core.Models.Configuration.StrongSettings;
using S7Tools.Services;
using Xunit;

namespace S7Tools.Core.Tests.Settings
{
    public class ApplicationSettingsServiceTests
    {
        private class MockWritableOptions<T> : IWritableOptions<T> where T : class, new()
        {
            public T CurrentValue { get; private set; } = new T();

            public T Value => CurrentValue;

            public T Get(string? name) => CurrentValue;

            public IDisposable? OnChange(Action<T, string?> listener)
            {
                return null;
            }

            public void Update(Action<T> applyChanges)
            {
                applyChanges(CurrentValue);
            }

            public Task UpdateAsync(Func<T, Task> applyChanges)
            {
                applyChanges(CurrentValue).GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
        }

        private ApplicationSettingsService CreateTestService()
        {
            var loggerMock = new Mock<ILogger<ApplicationSettingsService>>();
            var optionsMock = new MockWritableOptions<AppSettings>();
            return new ApplicationSettingsService(loggerMock.Object, optionsMock);
        }

        [Fact]
        public void GetSetting_WithNoUserSettings_ReturnsDefaultSettings()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();

            // Act & Assert
            // These defaults match the AppSettings default constructor values
            Assert.Equal("Information", service.GetSetting<string>("logging.level"));
            Assert.True(service.GetSetting<bool>("logging.enableFileLogging"));
            Assert.Equal("System", service.GetSetting<string>("ui.theme"));
        }

        [Fact]
        public async Task SetSettingAsync_UserSettingOverridesDefault_CorrectHierarchy()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();

            // Act - Set user setting to override default
            await service.SetSettingAsync("logging.level", "Debug");
            await service.SetSettingAsync("ui.theme", "Dark");

            // Assert - User settings override defaults
            Assert.Equal("Debug", service.GetSetting<string>("logging.level"));
            Assert.Equal("Dark", service.GetSetting<string>("ui.theme"));
        }

        [Fact]
        public async Task ResetSettingAsync_UserSettingReset_RevertsToDefault()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();
            await service.SetSettingAsync("logging.level", "Debug");

            // Verify user override is active
            Assert.Equal("Debug", service.GetSetting<string>("logging.level"));

            // Act - Reset to default
            await service.ResetSettingAsync("logging.level");

            // Assert - Reverted to default
            Assert.Equal("Information", service.GetSetting<string>("logging.level"));
        }

        [Fact]
        public async Task SettingsChanged_EventFired_WhenSettingChanged()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();

            S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs? eventArgs = null;
            service.SettingsChanged += (sender, args) => eventArgs = args;

            // Act
            await service.SetSettingAsync("ui.theme", "Dark");

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal("ui.theme", eventArgs.Key);
            Assert.Equal("Dark", eventArgs.NewValue);
            Assert.True(eventArgs.IsUserSetting);
        }
    }
}
