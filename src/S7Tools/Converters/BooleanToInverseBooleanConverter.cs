using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace S7Tools.Converters;

public class BooleanToInverseBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value; // Return original value if not a boolean
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value; // Return original value if not a boolean
    }
}