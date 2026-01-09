using System;
using System.Globalization;
using Avalonia.Data.Converters;
using S7Tools.Resources;

namespace S7Tools.Converters;

/// <summary>
/// Converts DateTime or DateTimeOffset values to formatted string representations.
/// </summary>
public class DateTimeToStringConverter : IValueConverter
{
    /// <summary>
    /// Converts a DateTime or DateTimeOffset value to a formatted string.
    /// </summary>
    /// <param name="value">The DateTime or DateTimeOffset value to convert.</param>
    /// <param name="targetType">The target type (string).</param>
    /// <param name="parameter">The format string (e.g., 'yyyy-MM-dd HH:mm').</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>Formatted string representation of the date/time value.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        string format = parameter as string ?? S7Tools.Constants.AppConstants.StandardDateFormat;

        try
        {
            return value switch
            {
                DateTime dateTime => dateTime.ToLocalTime().ToString(format, culture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.LocalDateTime.ToString(format, culture),
                _ => value?.ToString() ?? string.Empty
            };
        }
        catch (FormatException)
        {
            // Return empty string for formatting errors to prevent crashes
            return string.Empty;
        }
    }

    /// <summary>
    /// ConvertBack is not supported for this converter.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException(UIStrings.Exception_DateTimeToStringConverterNoConvertBack);
    }
}
