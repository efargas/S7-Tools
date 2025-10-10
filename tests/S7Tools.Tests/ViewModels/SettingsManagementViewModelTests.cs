using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Models;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using Xunit;

namespace S7Tools.Tests.ViewModels;

/// <summary>
/// Tests for the SettingsManagementViewModel.
/// </summary>
public class SettingsManagementViewModelTests
{
    private readonly Mock<ILogger<SettingsManagementViewModel>> _mockLogger;
    private readonly ISettingsService _settingsService;
    private readonly SettingsManagementViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the SettingsManagementViewModelTests class.
    /// </summary>
    public SettingsManagementViewModelTests()
    {
        _mockLogger = new Mock<ILogger<SettingsManagementViewModel>>();
        _settingsService = new SettingsService();
        _viewModel = new SettingsManagementViewModel(_mockLogger.Object, _settingsService);
    }

    /// <summary>
    /// Test that the ViewModel can be instantiated successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Assert
        Assert.NotNull(_viewModel);
        Assert.NotNull(_viewModel.SaveSettingsCommand);
        Assert.NotNull(_viewModel.LoadSettingsCommand);
        Assert.NotNull(_viewModel.ResetSettingsCommand);
    }

    /// <summary>
    /// Test that the ViewModel loads initial settings from the service.
    /// </summary>
    [Fact]
    public void Constructor_LoadsInitialSettings_FromService()
    {
        // Assert
        Assert.NotNull(_viewModel.DefaultLogPath);
        Assert.NotNull(_viewModel.ExportPath);
        Assert.NotNull(_viewModel.MinimumLogLevel);
        Assert.NotEmpty(_viewModel.DefaultLogPath);
        Assert.NotEmpty(_viewModel.ExportPath);
    }

    /// <summary>
    /// Test that settings can be exported to JSON.
    /// </summary>
    [Fact]
    public void ExportSettingsToJson_ReturnsValidJson()
    {
        // Act
        var json = _viewModel.ExportSettingsToJson();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("logging", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("defaultLogPath", json, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test that settings can be imported from JSON.
    /// </summary>
    [Fact]
    public void ImportSettingsFromJson_WithValidJson_ReturnsTrue()
    {
        // Arrange
        var json = _viewModel.ExportSettingsToJson();

        // Act
        var result = _viewModel.ImportSettingsFromJson(json);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test that importing empty JSON returns false.
    /// </summary>
    [Fact]
    public void ImportSettingsFromJson_WithEmptyJson_ReturnsFalse()
    {
        // Act
        var result = _viewModel.ImportSettingsFromJson(string.Empty);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test that importing invalid JSON returns false.
    /// </summary>
    [Fact]
    public void ImportSettingsFromJson_WithInvalidJson_ReturnsFalse()
    {
        // Act
        var result = _viewModel.ImportSettingsFromJson("{ invalid json }");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test that settings validation works correctly.
    /// </summary>
    [Fact]
    public void ValidateSettings_WithValidSettings_ReturnsTrue()
    {
        // Act
        var result = _viewModel.ValidateSettings();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test that property changes are reflected.
    /// </summary>
    [Fact]
    public void PropertyChanged_WhenSettingDefaultLogPath_RaisesEvent()
    {
        // Arrange
        var propertyChanged = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.DefaultLogPath))
                propertyChanged = true;
        };

        // Act
        _viewModel.DefaultLogPath = "/new/path";

        // Assert
        Assert.True(propertyChanged);
        Assert.Equal("/new/path", _viewModel.DefaultLogPath);
    }

    /// <summary>
    /// Test that save command exists and is not null.
    /// </summary>
    [Fact]
    public void SaveSettingsCommand_Exists_IsNotNull()
    {
        // Assert
        Assert.NotNull(_viewModel.SaveSettingsCommand);
    }

    /// <summary>
    /// Test that load command exists and is not null.
    /// </summary>
    [Fact]
    public void LoadSettingsCommand_Exists_IsNotNull()
    {
        // Assert
        Assert.NotNull(_viewModel.LoadSettingsCommand);
    }

    /// <summary>
    /// Test that reset command exists and is not null.
    /// </summary>
    [Fact]
    public void ResetSettingsCommand_Exists_IsNotNull()
    {
        // Assert
        Assert.NotNull(_viewModel.ResetSettingsCommand);
    }
}
