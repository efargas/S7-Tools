using System;
using System.Globalization;
using S7Tools.Converters;
using Xunit;

namespace S7Tools.Tests.Converters;

public class DateTimeToStringConverterTests
{
    [Fact]
    public void Convert_WithDateTime_ReturnsFormattedString()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var dateTime = new DateTime(2025, 10, 22, 14, 30, 45);
        var format = "yyyy-MM-dd HH:mm";

        // Act
        var result = converter.Convert(dateTime, typeof(string), format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("2025-10-22 14:30", result);
    }

    [Fact]
    public void Convert_WithDateTimeOffset_ReturnsFormattedString()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var dateTimeOffset = new DateTimeOffset(2025, 10, 22, 14, 30, 45, TimeSpan.Zero);
        var format = "yyyy-MM-dd HH:mm";

        // Act
        var result = converter.Convert(dateTimeOffset, typeof(string), format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("2025-10-22 14:30", result);
    }

    [Fact]
    public void Convert_WithNull_ReturnsEmptyString()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();

        // Act
        var result = converter.Convert(null, typeof(string), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Convert_WithoutParameter_UsesDefaultFormat()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var dateTime = new DateTime(2025, 10, 22, 14, 30, 45);

        // Act
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("2025-10-22 14:30", result);
    }

    [Fact]
    public void Convert_WithCustomFormat_ReturnsFormattedString()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var dateTime = new DateTime(2025, 10, 22, 14, 30, 45);
        var format = "MMM dd, yyyy";

        // Act
        var result = converter.Convert(dateTime, typeof(string), format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Oct 22, 2025", result);
    }

    [Fact]
    public void Convert_WithNonDateTimeValue_ReturnsStringRepresentation()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var value = "Some string value";

        // Act
        var result = converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Some string value", result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("2025-10-22", typeof(DateTime), null, CultureInfo.InvariantCulture));
    }
}
