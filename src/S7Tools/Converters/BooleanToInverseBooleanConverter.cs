using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace S7Tools.Converters;

/// <summary>
/// Converts a boolean value to its inverse.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public sealed class BooleanToInverseBooleanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolean)
        {
            return !boolean;
        }
        return false;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}