using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

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
        // Support being used for both Color and IBrush targets.
        var boolValue = value is bool b && b;
        var colorString = "Gray";

        if (parameter is string paramString)
        {
            var colors = paramString.Split('|', 2);
            if (colors.Length == 2)
            {
                colorString = boolValue ? colors[0] : colors[1];
            }
            else if (!string.IsNullOrWhiteSpace(paramString))
            {
                colorString = paramString;
            }
        }

        // Try parse color name or hex to a Color
        if (!Color.TryParse(colorString, out var parsedColor))
        {
            parsedColor = Colors.Gray;
        }

        // If target expects a Color value, return Color directly
        if (targetType == typeof(Color) || targetType == typeof(Avalonia.Media.Color))
        {
            return parsedColor;
        }

        // If target expects a brush or object, return a SolidColorBrush
        return new SolidColorBrush(parsedColor);
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
