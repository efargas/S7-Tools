using Avalonia.Media;
using Microsoft.Extensions.Logging;
using S7Tools.Converters;
using System.Globalization;
using Xunit;

namespace S7Tools.Tests.Converters;

/// <summary>
/// Contains unit tests for the value converters in <see cref="ObjectConverters"/>.
/// </summary>
public class ObjectConvertersTests
{
    /// <summary>
    /// Verifies that the IsNotNull converter returns true for a non-null value.
    /// </summary>
    [Fact]
    public void IsNotNull_WithNonNullValue_ReturnsTrue()
    {
        // Arrange
        var converter = ObjectConverters.IsNotNull;
        var value = "test";

        // Act
        var result = converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result!);
    }

    /// <summary>
    /// Verifies that the IsNotNull converter returns false for a null value.
    /// </summary>
    [Fact]
    public void IsNotNull_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var converter = ObjectConverters.IsNotNull;

        // Act
        var result = converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result!);
    }

    /// <summary>
    /// Verifies that the LogLevelToColor converter returns the correct color for the Information level.
    /// </summary>
    [Fact]
    public void LogLevelToColor_WithInformation_ReturnsGreen()
    {
        // Arrange
        var converter = ObjectConverters.LogLevelToColor;

        // Act
        var result = converter.Convert(LogLevel.Information, typeof(Color), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Color.FromRgb(0, 150, 0), result);
    }

    /// <summary>
    /// Verifies that the LogLevelToColor converter returns the correct color for the Error level.
    /// </summary>
    [Fact]
    public void LogLevelToColor_WithError_ReturnsCrimson()
    {
        // Arrange
        var converter = ObjectConverters.LogLevelToColor;

        // Act
        var result = converter.Convert(LogLevel.Error, typeof(Color), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Color.FromRgb(220, 20, 60), result);
    }
}