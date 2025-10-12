using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace S7Tools.Converters
{
    /// <summary>
    /// Converts a GridLength to a double.
    /// </summary>
    public sealed class GridLengthToDoubleConverter : IValueConverter
    {
        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                var multiplier = 1.0;
                if (parameter != null)
                {
                    if (parameter is double pDouble)
                    {
                        multiplier = pDouble;
                    }
                    else if (parameter is string pString && double.TryParse(pString, NumberStyles.Any, CultureInfo.InvariantCulture, out var pDoubleParsed))
                    {
                        multiplier = pDoubleParsed;
                    }
                }
                return height * multiplier;
            }

            if (value is GridLength gridLength)
            {
                return gridLength.Value;
            }

            return 0.0;
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return new GridLength(d);
            }
            return AvaloniaProperty.UnsetValue;
        }
    }
}