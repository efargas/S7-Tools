using FluentAssertions;
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
        var dateTime = new DateTime(2025, 10, 22, 14, 30, 45, DateTimeKind.Local);
        string format = "yyyy-MM-dd HH:mm";

        // Act
        object? result = converter.Convert(dateTime, typeof(string), format, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("2025-10-22 14:30");
    }

    [Fact]
    public void Convert_WithDateTimeOffset_ReturnsFormattedString()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var date = new DateTime(2025, 10, 22, 14, 30, 45);
        var localOffsetForDate = TimeZoneInfo.Local.GetUtcOffset(date);
        var dateTimeOffset = new DateTimeOffset(date, localOffsetForDate);
        string format = "yyyy-MM-dd HH:mm";

        // Act
        object? result = converter.Convert(dateTimeOffset, typeof(string), format, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("2025-10-22 14:30");
    }

    [Fact]
    public void Convert_WithNull_ReturnsEmptyString()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();

        // Act
        object? result = converter.Convert(null, typeof(string), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_WithoutParameter_UsesDefaultFormat()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var dateTime = new DateTime(2025, 10, 22, 14, 30, 45, DateTimeKind.Local);

        // Act
        object? result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        // Default format is "yyyy-MM-dd HH:mm:ss.fff"
        result.Should().Be("2025-10-22 14:30:45.000");
    }

    [Fact]
    public void Convert_WithCustomFormat_ReturnsFormattedString()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        var dateTime = new DateTime(2025, 10, 22, 14, 30, 45, DateTimeKind.Local);
        string format = "MMM dd, yyyy";

        // Act
        object? result = converter.Convert(dateTime, typeof(string), format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Oct 22, 2025", result);
    }

    [Fact]
    public void Convert_WithNonDateTimeValue_ReturnsStringRepresentation()
    {
        // Arrange
        var converter = new DateTimeToStringConverter();
        string value = "Some string value";

        // Act
        object? result = converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("Some string value");
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
