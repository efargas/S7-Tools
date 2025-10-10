using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters
{
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height && parameter is string percentage)
            {
                if (double.TryParse(percentage, NumberStyles.Any, CultureInfo.InvariantCulture, out var percent))
                {
                    return height * percent;
                }
            }
            return 0;
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
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
