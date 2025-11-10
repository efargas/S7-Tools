using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace S7Tools.Converters;

/// <summary>
/// Converter that returns orange for warning messages (containing ⚠️) and red for errors.
/// </summary>
public class WarningToColorConverter : IValueConverter
{
    public static readonly WarningToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string message && !string.IsNullOrEmpty(message))
        {
            // Orange for warnings, Red for errors
            return message.Contains("⚠️") || message.Contains("Warning")
                ? Color.FromRgb(255, 152, 0)  // Orange
                : Color.FromRgb(211, 47, 47);   // Red
        }

        return Color.FromRgb(211, 47, 47); // Default to red
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
