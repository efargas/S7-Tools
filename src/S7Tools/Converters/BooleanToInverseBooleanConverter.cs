using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Converts a boolean value to its inverse.
/// </summary>
public class BooleanToInverseBooleanConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to its inverse.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">An optional parameter (not used).</param>
    /// <param name="culture">The culture information (not used).</param>
    /// <returns>The inverse of the input boolean, or the original value if not a boolean.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // If parameter is provided and target expects a string/brush/color, use the parameter to choose a value.
        if (value is bool boolValue)
        {
            var inverted = !boolValue;

            if (parameter is string param && (targetType == typeof(string) || targetType == typeof(object)))
            {
                var parts = param.Split('|', 2);
                if (parts.Length == 2)
                {
                    return inverted ? parts[0] : parts[1];
                }
                return param;
            }

            if (parameter is string colorParam && (targetType == typeof(Avalonia.Media.IBrush) || targetType == typeof(Avalonia.Media.Brush)))
            {
                var parts = colorParam.Split('|', 2);
                var colorString = parts.Length == 2 ? (inverted ? parts[0] : parts[1]) : colorParam;
                if (Avalonia.Media.Color.TryParse(colorString, out var color))
                {
                    return new Avalonia.Media.SolidColorBrush(color);
                }

                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Transparent);
            }

            // Default behavior: return inverted boolean
            return inverted;
        }
        return value; // Return original value if not a boolean
    }

    /// <summary>
    /// Converts back a boolean value to its inverse.
    /// </summary>
    /// <param name="value">The boolean value to convert back.</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">An optional parameter (not used).</param>
    /// <param name="culture">The culture information (not used).</param>
    /// <returns>The inverse of the input boolean, or the original value if not a boolean.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value; // Return original value if not a boolean
    }
}
