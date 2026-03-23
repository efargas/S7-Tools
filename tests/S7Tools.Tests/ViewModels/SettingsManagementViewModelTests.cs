using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using S7Tools.Core.Models.Configuration.StrongSettings;
using S7Tools.ViewModels.Layout;
using Xunit;

namespace S7Tools.Tests.ViewModels;

/// <summary>
/// Tests for the SettingsManagementViewModel using the strongly-typed IApplicationSettingsService.
/// </summary>
public class SettingsManagementViewModelTests
{
    private readonly Mock<ILogger<SettingsManagementViewModel>> _mockLogger;
    private readonly Mock<IApplicationSettingsService> _mockSettingsService;

    public SettingsManagementViewModelTests()
    {
        _mockLogger = new Mock<ILogger<SettingsManagementViewModel>>();
        _mockSettingsService = new Mock<IApplicationSettingsService>();

        // Setup mock to return default AppSettings
        _mockSettingsService.Setup(s => s.Current).Returns(new AppSettings());
        _mockSettingsService.Setup(s => s.LoadSettingsAsync()).Returns(Task.CompletedTask);
        _mockSettingsService.Setup(s => s.UpdateSettingsAsync(It.IsAny<Action<AppSettings>>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var viewModel = new SettingsManagementViewModel(_mockLogger.Object, _mockSettingsService.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.SaveSettingsCommand.Should().NotBeNull();
        viewModel.LoadSettingsCommand.Should().NotBeNull();
        viewModel.ResetSettingsCommand.Should().NotBeNull();
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
        viewModel.DefaultLogPath.Should().Be("/test/path");
        viewModel.ExportPath.Should().Be("/export/path");
        viewModel.MinimumLogLevel.Should().Be("Debug");
        viewModel.AutoScrollLogs.Should().BeFalse();
    }
}
