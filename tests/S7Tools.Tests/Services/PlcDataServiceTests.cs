using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Models;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using Xunit;

namespace S7Tools.Tests.Services;

/// <summary>
/// Contains unit tests for the <see cref="SimulatedPlcDataService"/>.
/// </summary>
public class PlcDataServiceTests : IDisposable
{
    private readonly Mock<ILogger<SimulatedPlcDataService>> _mockLogger;
    private readonly SimulatedPlcDataService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcDataServiceTests"/> class.
    /// </summary>
    public PlcDataServiceTests()
    {
        _mockLogger = new Mock<ILogger<SimulatedPlcDataService>>();
        _service = new SimulatedPlcDataService(_mockLogger.Object);
    }

    /// <summary>
    /// Verifies that the PlcDataService can be instantiated successfully.
    /// </summary>
    [Fact]
    public void PlcDataService_CanBeInstantiated()
    {
        // Arrange & Act
        var service = new SimulatedPlcDataService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(ConnectionState.Disconnected, service.State);
    }

    /// <summary>
    /// Verifies that reading a tag when disconnected returns a failure result.
    /// </summary>
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

    /// <summary>
    /// Verifies that reading a tag when connected returns a success result.
    /// </summary>
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

    /// <summary>
    /// Verifies that connecting with a valid configuration returns a success result.
    /// </summary>
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

    /// <summary>
    /// Verifies that disconnecting when connected returns a success result.
    /// </summary>
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

    /// <summary>
    /// Verifies that testing a connection with a valid configuration returns a success result.
    /// </summary>
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

    /// <summary>
    /// Verifies that getting PLC info when connected returns a success result with PLC information.
    /// </summary>
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

    /// <summary>
    /// Verifies that adding a tag successfully adds it to the managed tags collection.
    /// </summary>
    [Fact]
    public async Task AddTagAsync_AddsTagToManagedTags()
    {
        // Arrange
        var tagResult = Tag.Create("TestTag", "DB1.DBX0.0", true);
        Assert.True(tagResult.IsSuccess);
        var tag = tagResult.Value;
        Assert.NotNull(tag);

        // Act
        var result = await _service.AddTagAsync(tag);

        // Assert
        Assert.True(result.IsSuccess);
        
        var allTagsResult = await _service.GetAllTagsAsync();
        Assert.True(allTagsResult.IsSuccess);
        Assert.NotNull(allTagsResult.Value);
        Assert.Contains(allTagsResult.Value, t => t.Name == "TestTag");
    }

    /// <summary>
    /// Verifies that writing a tag when disconnected returns a failure result.
    /// </summary>
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

    /// <summary>
    /// Verifies that writing a tag when connected returns a success result.
    /// </summary>
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

    /// <summary>
    /// Verifies that disposing the service does not throw an exception.
    /// </summary>
    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var service = new SimulatedPlcDataService(_mockLogger.Object);

        // Act & Assert
        service.Dispose();
    }

    public void Dispose()
    {
        _service.Dispose();
        GC.SuppressFinalize(this);
    }
}