using System;
using Avalonia;
using Avalonia.Data.Converters;
using System.Globalization;

namespace S7Tools.Converters;

/// <summary>
/// Compares two strings for equality, ignoring case.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s1 = value as string;
        var s2 = parameter as string;
        return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}