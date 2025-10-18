using System;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Returns true when input int is less than or equal to zero; false otherwise.
/// Useful for validation visibility bindings.
/// </summary>
public sealed class IntLessOrEqualZeroToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int i)
        {
            return i <= 0;
        }
        if (value is uint ui)
        {
            return ui == 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
