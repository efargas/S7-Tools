using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double gridLengthValue)
        {
            return gridLengthValue > 0;
        }
        return true; // Default to true if not a double
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
