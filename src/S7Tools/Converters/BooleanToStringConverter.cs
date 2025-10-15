using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Converts a boolean value to a string.
/// </summary>
public class BooleanToStringConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to a string.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The converter parameter in format "TrueString|FalseString".</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>A string based on the boolean value.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        if (parameter is string paramString)
        {
            var parts = paramString.Split('|', 2);
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
            return paramString;
        }

        // If no parameter provided, fall back to simple True/False strings
        return boolValue ? "True" : "False";
    }

    /// <summary>
    /// Converts back from a string to a boolean value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The converter parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>Not implemented.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
