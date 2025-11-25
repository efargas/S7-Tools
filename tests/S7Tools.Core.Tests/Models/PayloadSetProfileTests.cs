using S7Tools.Core.Models.Jobs;
using Xunit;

namespace S7Tools.Core.Tests.Models;

/// <summary>
/// Unit tests for PayloadSetProfile validation.
/// </summary>
public class PayloadSetProfileTests
{
    [Fact]
    public void PayloadSetProfile_Creation_Should_Accept_Valid_BasePath()
    {
        // Arrange & Act
        var profile = new PayloadSetProfile { BasePath = "/tmp/payloads" };

        // Assert
        Assert.Equal("/tmp/payloads", profile.BasePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PayloadSetProfile_Creation_Should_Accept_Empty_BasePath(string basePath)
    {
        // Arrange & Act
        var profile = new PayloadSetProfile { BasePath = basePath };

        // Assert - Record construction doesn't validate, validation happens in service layer
        Assert.Equal(basePath, profile.BasePath);
    }
}
