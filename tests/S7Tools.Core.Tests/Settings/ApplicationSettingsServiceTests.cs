using FluentAssertions;
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
using S7Tools.Services;
using S7Tools.Core.Models.Configuration.StrongSettings;
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

        /// <summary>
        /// Tracks which write path was used (Update vs UpdateAsync) for verification tests.
        /// </summary>
        private class TrackingWritableOptions<T> : IWritableOptions<T> where T : class, new()
        {
            public T CurrentValue { get; private set; } = new T();
            public T Value => CurrentValue;
            public bool UpdateSyncCalled { get; set; }
            public bool UpdateAsyncCalled { get; private set; }

            public T Get(string? name) => CurrentValue;
            public IDisposable? OnChange(Action<T, string?> listener) => null;

            public void Update(Action<T> applyChanges)
            {
                UpdateSyncCalled = true;
                applyChanges(CurrentValue);
            }

            public Task UpdateAsync(Func<T, Task> applyChanges)
            {
                UpdateAsyncCalled = true;
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
        public void Current_WithNoUserSettings_ReturnsDefaultSettings()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();

            // Act & Assert - defaults match AppSettings default constructor values
            service.Current.Logging.Level.Should().Be("Information");
            service.Current.Logging.EnableFileLogging.Should().BeTrue();
            service.Current.Ui.Theme.Should().Be("System");
        }

        [Fact]
        public async Task UpdateSettingsAsync_UserSettingOverridesDefault_CorrectHierarchy()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();

            // Act - update settings via strongly-typed action
            await service.UpdateSettingsAsync(s =>
            {
                s.Logging.Level = "Debug";
                s.Ui.Theme = "Dark";
            });

            // Assert - settings reflect the update
            service.Current.Logging.Level.Should().Be("Debug");
            service.Current.Ui.Theme.Should().Be("Dark");
        }

        [Fact]
        public async Task ResetAllSettingsAsync_UserSettingReset_RevertsToDefault()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();
            await service.UpdateSettingsAsync(s => s.Logging.Level = "Debug");

            // Verify update was applied
            service.Current.Logging.Level.Should().Be("Debug");

            // Act - reset to defaults
            await service.ResetAllSettingsAsync();

            // Assert - reverted to default
            service.Current.Logging.Level.Should().Be("Information");
        }

        [Fact]
        public async Task SettingsChanged_EventFired_WhenSettingsUpdated()
        {
            // Arrange
            ApplicationSettingsService service = CreateTestService();

            SettingsChangedEventArgs? eventArgs = null;
            service.SettingsChanged += (sender, args) => eventArgs = args;

            // Act
            await service.UpdateSettingsAsync(s => s.Ui.Theme = "Dark");

            // Assert
            // The strongly-typed API fires SettingsChanged with IsUserSetting=true for any UpdateSettingsAsync call.
            // Individual key/value change info is no longer tracked; instead, callers read Current directly for the new values.
            eventArgs.Should().NotBeNull();
            eventArgs.IsUserSetting.Should().BeTrue();
            // Verify the actual value change is accessible via Current
            service.Current.Ui.Theme.Should().Be("Dark");
        }

        [Fact]
        public async Task UpdateSettingsAsync_DelegatesToUpdateAsync_NotSyncUpdate()
        {
            // Arrange
            var trackingOptions = new TrackingWritableOptions<AppSettings>();
            var loggerMock = new Mock<ILogger<ApplicationSettingsService>>();
            var service = new ApplicationSettingsService(loggerMock.Object, trackingOptions);

            // Act
            await service.UpdateSettingsAsync(s => s.Logging.Level = "Debug");

            // Assert – only the async path must have been called
            Assert.True(trackingOptions.UpdateAsyncCalled, "UpdateSettingsAsync must delegate to UpdateAsync.");
            Assert.False(trackingOptions.UpdateSyncCalled, "UpdateSettingsAsync must not call the synchronous Update method.");
        }

        [Fact]
        public async Task ResetAllSettingsAsync_DelegatesToUpdateAsync_NotSyncUpdate()
        {
            // Arrange
            var trackingOptions = new TrackingWritableOptions<AppSettings>();
            var loggerMock = new Mock<ILogger<ApplicationSettingsService>>();
            var service = new ApplicationSettingsService(loggerMock.Object, trackingOptions);
            await service.UpdateSettingsAsync(s => s.Logging.Level = "Debug");

            // Verify arrange step used UpdateAsync
            Assert.True(trackingOptions.UpdateAsyncCalled, "Arrange: UpdateSettingsAsync must have called UpdateAsync.");
            trackingOptions.UpdateSyncCalled = false;

            // Act
            await service.ResetAllSettingsAsync();

            // Assert
            Assert.False(trackingOptions.UpdateSyncCalled, "ResetAllSettingsAsync must not call the synchronous Update method.");
        }
    }
}
