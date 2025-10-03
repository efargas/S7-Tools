using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Controls;

namespace S7_Tools.Converters
{
    public class BooleanToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is GridLength gridLength)
            {
                // Down arrow SVG path if collapsed, Up arrow SVG path if expanded
                return gridLength.Value == 0 ? "M7 14l5 5 5-5H7z" : "M7 10l5-5 5 5H7z";
            }
            return "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}