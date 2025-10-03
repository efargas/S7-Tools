using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Controls;

namespace S7Tools.Converters
{
    /// <summary>
    /// Converts a <see cref="GridLength"/> to an icon path.
    /// </summary>
    public class GridLengthToIconConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="GridLength"/> to an icon path.
        /// </summary>
        /// <param name="value">The <see cref="GridLength"/> to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">An optional parameter.</param>
        /// <param name="culture">The culture to use for the conversion.</param>
        /// <returns>An icon path string.</returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is GridLength gridLength)
            {
                // Down arrow SVG path if collapsed, Up arrow SVG path if expanded
                return gridLength.Value == 0 ? "M7 14l5 5 5-5H7z" : "M7 10l5-5 5 5H7z";
            }
            return "";
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}