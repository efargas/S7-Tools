using System;
using System.Globalization;
using Avalonia.Data.Converters;
using S7Tools.Resources;

namespace S7Tools.Converters;

/// <summary>
/// Converts a double value to a scaled double based on a percentage parameter.
/// </summary>
public class GridLengthToDoubleConverter : IValueConverter
{
    /// <summary>
    /// Converts a double height value by multiplying it with a percentage parameter.
    /// </summary>
    /// <param name="value">The double height value to convert.</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">A string percentage to scale the value.</param>
    /// <param name="culture">The culture information for parsing.</param>
    /// <returns>The scaled height value, or 0 if conversion fails.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double height && parameter is string percentage)
        {
            if (double.TryParse(percentage, NumberStyles.Any, CultureInfo.InvariantCulture, out double percent))
            {
                return height * percent;
            }
        }
        return 0d;
    }

    /// <summary>
    /// Converts back from a scaled double to the original value.
    /// This converter does not support two-way binding.
    /// </summary>
    /// <param name="value">The value to convert back.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">An optional parameter.</param>
    /// <param name="culture">The culture information.</param>
    /// <returns>This method is not supported.</returns>
    /// <exception cref="NotImplementedException">This converter does not support ConvertBack.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException(UIStrings.Exception_GridLengthToDoubleConverterNoConvertBack);
    }
}
