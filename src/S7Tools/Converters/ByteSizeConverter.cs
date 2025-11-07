using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Converter to format byte sizes into human-readable strings.
/// </summary>
public class ByteSizeConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the converter.
    /// </summary>
    public static readonly ByteSizeConverter Instance = new();

    /// <summary>
    /// Converts a byte size value to a human-readable string.
    /// </summary>
    /// <param name="value">The byte size value.</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">Optional parameter (not used).</param>
    /// <param name="culture">The culture info (not used).</param>
    /// <returns>A formatted string representing the size in appropriate units (B, KB, MB, GB).</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long sizeInBytes)
        {
            return value?.ToString() ?? "0 B";
        }

        return FormatByteSize(sizeInBytes);
    }

    /// <summary>
    /// Not implemented - this converter is one-way only.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ByteSizeConverter is a one-way converter.");
    }

    /// <summary>
    /// Formats a byte size into a human-readable string.
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <returns>A formatted string with appropriate unit suffix.</returns>
    private static string FormatByteSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;
        const long TB = GB * 1024;

        return bytes switch
        {
            >= TB => $"{bytes / (double)TB:F2} TB",
            >= GB => $"{bytes / (double)GB:F2} GB",
            >= MB => $"{bytes / (double)MB:F2} MB",
            >= KB => $"{bytes / (double)KB:F2} KB",
            _ => $"{bytes} B"
        };
    }
}
