using S7Tools.Services;
using Xunit;

namespace S7Tools.Tests.Services;

public class PlcDataServiceTests
{
    [Fact]
    public async Task ReadTagAsync_ShouldReturnTag()
    {
        // Arrange
        var service = new PlcDataService();
        var address = "DB1.DBX0.0";

        // Act
        var result = await service.ReadTagAsync(address);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(address, result.Address);
        Assert.Equal("Test Tag", result.Name);
        Assert.True(result.Value is int);
    }

    [Fact]
    public async Task ConnectAsync_ShouldComplete()
    {
        // Arrange
        var service = new PlcDataService();

        // Act & Assert
        await service.ConnectAsync();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldComplete()
    {
        // Arrange
        var service = new PlcDataService();

        // Act & Assert
        await service.DisconnectAsync();
    }
}