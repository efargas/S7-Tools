using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace S7Tools.Converters;

/// <summary>
/// Converts a boolean value to a color brush.
/// </summary>
public class BooleanToColorConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to a color brush.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The converter parameter in format "TrueColor|FalseColor".</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>A color brush based on the boolean value.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
        {
            return new SolidColorBrush(Colors.Gray);
        }

        var colors = paramString.Split('|');
        if (colors.Length != 2)
        {
            return new SolidColorBrush(Colors.Gray);
        }

        var colorString = boolValue ? colors[0] : colors[1];
        
        if (Color.TryParse(colorString, out var color))
        {
            return new SolidColorBrush(color);
        }

        return new SolidColorBrush(Colors.Gray);
    }

    /// <summary>
    /// Converts back from a color brush to a boolean value.
    /// </summary>
    /// <param name="value">The color brush value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The converter parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>Not implemented.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}