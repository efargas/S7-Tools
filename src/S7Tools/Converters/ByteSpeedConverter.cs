using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Converts a byte per second speed (double) into a human-readable string (e.g., "1.5 MB/s").
/// </summary>
public class ByteSpeedConverter : IValueConverter
{
    private static readonly string[] Suffixes = ["B/s", "KB/s", "MB/s", "GB/s", "TB/s"];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double bytesPerSecond)
        {
            return "0 B/s";
        }

        if (bytesPerSecond < 0)
        {
            return "0 B/s";
        }

        int i = 0;
        double dValue = bytesPerSecond;

        // Use 1024 for binary prefixes (KiB/MiB/GiB)
        while (dValue >= 1024 && i < Suffixes.Length - 1)
        {
            dValue /= 1024;
            i++;
        }

        return $"{dValue:0.##} {Suffixes[i]}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null; // Return null instead of throwing, as this is used in OneWay contexts mostly
    }
}
