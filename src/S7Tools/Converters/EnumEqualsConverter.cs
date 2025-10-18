using System;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Compares an enum value to a string parameter representing the enum name.
/// Returns true when they match.
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        // Compare by enum name
        string enumName = value.ToString() ?? string.Empty;
        string targetName = parameter.ToString() ?? string.Empty;
        return string.Equals(enumName, targetName, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
