using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Converts a pair of [current, total] byte values into a human-readable progress string.
/// Format: "1.5 / 3.0 MB" or "500 / 1024 B"
/// </summary>
public class ProgressByteSizeConverter : IMultiValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the converter.
    /// </summary>
    public static readonly ProgressByteSizeConverter Instance = new();

    public object? Convert(IList<object?>? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2)
        {
            return "0 / 0 B";
        }

        long current = ParseLong(values[0]);
        long total = ParseLong(values[1]);

        // Prevent total from being 0 to avoid division errors in logic if needed, though we just formatting here.
        if (total == 0)
        {
            total = 1;
        }

        return FormatProgress(current, total);
    }

    private static long ParseLong(object? value)
    {
        if (value == null)
        {
            return 0;
        }

        if (value is long l)
        {
            return l;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is double d)
        {
            return (long)d;
        }

        if (long.TryParse(value.ToString(), out var result))
        {
            return result;
        }

        return 0;
    }

    private static string FormatProgress(long current, long total)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        // Decide unit based on TOTAL size
        string unit;
        double divisor;
        string format;

        if (total >= GB)
        {
            unit = "GB";
            divisor = GB;
            format = "F2";
        }
        else if (total >= MB)
        {
            unit = "MB";
            divisor = MB;
            format = "F2";
        }
        else if (total >= KB)
        {
            unit = "KB";
            divisor = KB;
            format = "F2";
        }
        else
        {
            unit = "B";
            divisor = 1;
            format = "N0";
        }

        double currentScaled = current / divisor;
        double totalScaled = total / divisor;

        return $"{currentScaled.ToString(format, CultureInfo.InvariantCulture)} / {totalScaled.ToString(format, CultureInfo.InvariantCulture)} {unit}";
    }
}
