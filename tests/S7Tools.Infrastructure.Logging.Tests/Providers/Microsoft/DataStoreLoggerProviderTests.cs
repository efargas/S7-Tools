using System;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;
using FluentAssertions;
using Xunit;

namespace S7Tools.Infrastructure.Logging.Tests.Providers.Microsoft;

public sealed class DataStoreLoggerProviderTests : IDisposable
{
    private readonly Mock<ILogDataStore> _mockDataStore;
    private readonly Mock<ITimeProvider> _mockTimeProvider;
    private readonly DataStoreLoggerProvider _provider;

    public DataStoreLoggerProviderTests()
    {
        _mockDataStore = new Mock<ILogDataStore>();
        _mockTimeProvider = new Mock<ITimeProvider>();
        _provider = new DataStoreLoggerProvider(_mockDataStore.Object, _mockTimeProvider.Object);
    }

    [Fact]
    public void UpdateConfiguration_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _provider.UpdateConfiguration(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void UpdateConfiguration_ValidConfiguration_UpdatesInternalConfiguration()
    {
        // Arrange
        var newConfig = new DataStoreLoggerConfiguration
        {
            LogLevel = LogLevel.Critical,
            EventId = 123,
            IncludeScopes = false,
            CaptureProperties = false,
            CategoryFilter = "Test.*",
            FormatMessages = false,
            MaxMessageLength = 500,
            CaptureStackTrace = false
        };

        // Act
        _provider.UpdateConfiguration(newConfig);

        // Assert
        _provider.Configuration.LogLevel.Should().Be(newConfig.LogLevel);
        _provider.Configuration.EventId.Should().Be(newConfig.EventId);
        _provider.Configuration.IncludeScopes.Should().Be(newConfig.IncludeScopes);
        _provider.Configuration.CaptureProperties.Should().Be(newConfig.CaptureProperties);
        _provider.Configuration.CategoryFilter.Should().Be(newConfig.CategoryFilter);
        _provider.Configuration.FormatMessages.Should().Be(newConfig.FormatMessages);
        _provider.Configuration.MaxMessageLength.Should().Be(newConfig.MaxMessageLength);
        _provider.Configuration.CaptureStackTrace.Should().Be(newConfig.CaptureStackTrace);
    }

    [Fact]
    public void UpdateConfiguration_WhenDisposed_DoesNotUpdate()
    {
        // Arrange
        var initialLogLevel = _provider.Configuration.LogLevel;
        _provider.Dispose();

        var newConfig = new DataStoreLoggerConfiguration
        {
            LogLevel = LogLevel.Critical
        };

        // Act
        _provider.UpdateConfiguration(newConfig);

        // Assert
        _provider.Configuration.LogLevel.Should().Be(initialLogLevel);
    }

    [Fact]
    public void UpdateConfiguration_ReflectsInExistingLoggers()
    {
        // Arrange
        var logger = (DataStoreLogger)_provider.CreateLogger("TestCategory");

        var newConfig = new DataStoreLoggerConfiguration
        {
            LogLevel = LogLevel.Error
        };

        // Act
        _provider.UpdateConfiguration(newConfig);

        // Assert
        // Since DataStoreLogger uses the same configuration object reference,
        // it should reflect the changes.
        logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
    }

    public void Dispose()
    {
        _provider?.Dispose();
    }
}
