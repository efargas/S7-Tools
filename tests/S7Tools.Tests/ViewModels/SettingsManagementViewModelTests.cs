using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Interfaces.Services;
using S7Tools.ViewModels;
using Xunit;

namespace S7Tools.Tests.ViewModels;

/// <summary>
/// Tests for the SettingsManagementViewModel.
/// NOTE: This test file uses the old ISettingsService which has been removed.
/// These tests are disabled pending update to use IApplicationSettingsService.
/// </summary>
public class SettingsManagementViewModelTests
{
    private readonly Mock<ILogger<SettingsManagementViewModel>> _mockLogger;
    private readonly Mock<IApplicationSettingsService> _mockSettingsService;

    public SettingsManagementViewModelTests()
    {
        _mockLogger = new Mock<ILogger<SettingsManagementViewModel>>();
        _mockSettingsService = new Mock<IApplicationSettingsService>();

        // Setup mock to return default values for settings
        _mockSettingsService.Setup(s => s.GetSetting(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string defaultValue) => defaultValue);
        _mockSettingsService.Setup(s => s.GetSetting(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, bool defaultValue) => defaultValue);
        _mockSettingsService.Setup(s => s.GetSetting(It.IsAny<string>(), It.IsAny<int>()))
            .Returns((string key, int defaultValue) => defaultValue);
        _mockSettingsService.Setup(s => s.LoadSettingsAsync())
            .ReturnsAsync(S7Tools.Core.Models.Configuration.ApplicationSettings.CreateDefault());
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var viewModel = new SettingsManagementViewModel(_mockLogger.Object, _mockSettingsService.Object);

        // Assert
        Assert.NotNull(viewModel);
        Assert.NotNull(viewModel.SaveSettingsCommand);
        Assert.NotNull(viewModel.LoadSettingsCommand);
        Assert.NotNull(viewModel.ResetSettingsCommand);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        Assert.Throws<ArgumentNullException>(() =>
            new SettingsManagementViewModel(null, _mockSettingsService.Object));
#pragma warning restore CS8625
    }

    [Fact]
    public void Constructor_WithNullSettingsService_ThrowsArgumentNullException()
    {
        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        Assert.Throws<ArgumentNullException>(() =>
            new SettingsManagementViewModel(_mockLogger.Object, null));
#pragma warning restore CS8625
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var viewModel = new SettingsManagementViewModel(_mockLogger.Object, _mockSettingsService.Object);

        // Act
        viewModel.DefaultLogPath = "/test/path";
        viewModel.ExportPath = "/export/path";
        viewModel.MinimumLogLevel = "Debug";
        viewModel.AutoScrollLogs = false;

        // Assert
        Assert.Equal("/test/path", viewModel.DefaultLogPath);
        Assert.Equal("/export/path", viewModel.ExportPath);
        Assert.Equal("Debug", viewModel.MinimumLogLevel);
        Assert.False(viewModel.AutoScrollLogs);
    }

    // TODO: Re-enable and add more comprehensive tests for IApplicationSettingsService integration
    [Fact(Skip = "Test disabled - needs update for IApplicationSettingsService")]
    public void Placeholder_Test()
    {
        // This test is a placeholder to prevent test discovery errors
        Assert.True(true);
    }
}
