using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Models;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using Xunit;

namespace S7Tools.Tests.Services;

public class PlcDataServiceTests : IDisposable
{
    private readonly Mock<ILogger<PlcDataService>> _mockLogger;
    private readonly PlcDataService _service;

    public PlcDataServiceTests()
    {
        _mockLogger = new Mock<ILogger<PlcDataService>>();
        _service = new PlcDataService(_mockLogger.Object);
    }

    [Fact]
    public void PlcDataService_CanBeInstantiated()
    {
        // Arrange & Act
        var service = new PlcDataService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(ConnectionState.Disconnected, service.State);
    }

    [Fact]
    public async Task ReadTagAsync_WhenNotConnected_ReturnsFailure()
    {
        // Arrange
        var address = new PlcAddress("DB1.DBX0.0");

        // Act
        var result = await _service.ReadTagAsync(address);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Not connected", result.Error);
    }

    [Fact]
    public async Task ReadTagAsync_WhenConnected_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");
        var address = new PlcAddress("DB1.DBX0.0");

        await _service.ConnectAsync(config);

        // Act
        var result = await _service.ReadTagAsync(address);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("Tag_", result.Value.Name);
    }

    [Fact]
    public async Task ConnectAsync_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");

        // Act
        var result = await _service.ConnectAsync(config);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ConnectionState.Connected, _service.State);
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");
        await _service.ConnectAsync(config);

        // Act
        var result = await _service.DisconnectAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ConnectionState.Disconnected, _service.State);
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");

        // Act
        var result = await _service.TestConnectionAsync(config);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetPlcInfoAsync_WhenConnected_ReturnsPlcInfo()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");
        await _service.ConnectAsync(config);

        // Act
        var result = await _service.GetPlcInfoAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("CPU", result.Value.CpuType);
    }

    [Fact]
    public async Task AddTagAsync_AddsTagToManagedTags()
    {
        // Arrange
        var tagResult = Tag.Create("TestTag", "DB1.DBX0.0", true);
        Assert.True(tagResult.IsSuccess);
        var tag = tagResult.Value;

        // Act
        var result = await _service.AddTagAsync(tag);

        // Assert
        Assert.True(result.IsSuccess);

        var allTagsResult = await _service.GetAllTagsAsync();
        Assert.True(allTagsResult.IsSuccess);
        Assert.Contains(allTagsResult.Value, t => t.Name == "TestTag");
    }

    [Fact]
    public async Task WriteTagAsync_WhenNotConnected_ReturnsFailure()
    {
        // Arrange
        var address = new PlcAddress("DB1.DBX0.0");

        // Act
        var result = await _service.WriteTagAsync(address, true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Not connected", result.Error);
    }

    [Fact]
    public async Task WriteTagAsync_WhenConnected_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");
        var address = new PlcAddress("DB1.DBX0.0");

        await _service.ConnectAsync(config);

        // Act
        var result = await _service.WriteTagAsync(address, true);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var service = new PlcDataService(_mockLogger.Object);

        // Act & Assert
        service.Dispose();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }
}
