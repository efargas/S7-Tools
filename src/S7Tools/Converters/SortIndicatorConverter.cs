using Avalonia.Data.Converters;
using System.Globalization;

namespace S7Tools.Converters;

/// <summary>
/// Converter that shows sort direction indicators (↑ for ascending, ↓ for descending).
/// </summary>
public class SortIndicatorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not string sortColumn || parameter is not string columnName)
            return "";

        return sortColumn == columnName ? "↑" : "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotSupportedException();
    }
}
