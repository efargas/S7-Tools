using System.Globalization;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using S7Tools.Converters;
using Xunit;

namespace S7Tools.Tests.Converters;

public class ObjectConvertersTests
{
    [Fact]
    public void IsNotNull_WithNonNullValue_ReturnsTrue()
    {
        // Arrange
        var converter = ObjectConverters.IsNotNull;
        string value = "test";

        // Act
        object? result = converter.Convert(value, typeof(bool), (object?)null, CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result!);
    }

    [Fact]
    public void IsNotNull_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var converter = ObjectConverters.IsNotNull;

        // Act
        object? result = converter.Convert(null, typeof(bool), (object?)null, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result!);
    }

    [Fact]
    public void LogLevelToColor_WithInformation_ReturnsGreen()
    {
        // Arrange
        var converter = ObjectConverters.LogLevelToColor;

        // Act
        object? result = converter.Convert(LogLevel.Information, typeof(Color), (object?)null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Color.FromRgb(0, 150, 0), result);
    }

    [Fact]
    public void LogLevelToColor_WithError_ReturnsCrimson()
    {
        // Arrange
        var converter = ObjectConverters.LogLevelToColor;

        // Act
        object? result = converter.Convert(LogLevel.Error, typeof(Color), (object?)null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Color.FromRgb(220, 20, 60), result);
    }
}
