using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace S7Tools.Converters
{
    public class GridLengthToDoubleConverter : IValueConverter
    {
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
