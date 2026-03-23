using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Models;
using S7Tools.Core.Models.ValueObjects;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using Xunit;

namespace S7Tools.Tests.Services;

public sealed class PlcDataServiceTests : IDisposable
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
        service.Should().NotBeNull();
        service.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task ReadTagAsync_WhenNotConnected_ReturnsFailure()
    {
        // Arrange
        var address = new PlcAddress("DB1.DBX0.0");

        // Act
        Result<Tag> result = await _service.ReadTagAsync(address);

        // Assert
        result.IsFailure.Should().BeTrue();
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
        Result<Tag> result = await _service.ReadTagAsync(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        Assert.Contains("Tag_", result.Value.Name);
    }

    [Fact]
    public async Task ConnectAsync_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");

        // Act
        Result result = await _service.ConnectAsync(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _service.State.Should().Be(ConnectionState.Connected);
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");
        await _service.ConnectAsync(config);

        // Act
        Result result = await _service.DisconnectAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _service.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");

        // Act
        Result result = await _service.TestConnectionAsync(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlcInfoAsync_WhenConnected_ReturnsPlcInfo()
    {
        // Arrange
        var config = new S7ConnectionConfig("192.168.1.100");
        await _service.ConnectAsync(config);

        // Act
        Result<PlcInfo> result = await _service.GetPlcInfoAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        Assert.Contains("CPU", result.Value.CpuType);
    }

    [Fact]
    public async Task AddTagAsync_AddsTagToManagedTags()
    {
        // Arrange
        Result<Tag> tagResult = Tag.Create("TestTag", "DB1.DBX0.0", true);
        tagResult.IsSuccess.Should().BeTrue();
        Tag? tag = tagResult.Value;

        // Act
        Result result = await _service.AddTagAsync(tag!);

        // Assert
        result.IsSuccess.Should().BeTrue();

        Result<IReadOnlyCollection<Tag>> allTagsResult = await _service.GetAllTagsAsync();
        allTagsResult.IsSuccess.Should().BeTrue();
        Assert.Contains(allTagsResult.Value!, t => t.Name == "TestTag");
    }

    [Fact]
    public async Task WriteTagAsync_WhenNotConnected_ReturnsFailure()
    {
        // Arrange
        var address = new PlcAddress("DB1.DBX0.0");

        // Act
        Result result = await _service.WriteTagAsync(address, true);

        // Assert
        result.IsFailure.Should().BeTrue();
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
        Result result = await _service.WriteTagAsync(address, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
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
