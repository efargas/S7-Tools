using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Converts a double value to a boolean visibility indicator.
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a double value to a boolean indicating visibility (true if value > 0).
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">An optional parameter (not used).</param>
    /// <param name="culture">The culture information (not used).</param>
    /// <returns>True if the value is greater than 0; otherwise, true by default.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double gridLengthValue)
        {
            return gridLengthValue > 0;
        }
        return true; // Default to true if not a double
    }

    /// <summary>
    /// Not implemented - this converter does not support two-way binding.
    /// </summary>
    /// <param name="value">The value to convert back.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">An optional parameter.</param>
    /// <param name="culture">The culture information.</param>
    /// <returns>Not implemented.</returns>
    /// <exception cref="NotImplementedException">This method is not implemented.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
