using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace S7Tools.Converters;

/// <summary>
/// Converts between DateTimeOffset and DateTime for DatePicker binding compatibility.
/// </summary>
public class DateTimeOffsetToDateTimeConverter : IValueConverter
{
    /// <summary>
    /// Converts DateTimeOffset to DateTime for DatePicker.SelectedDate binding.
    /// </summary>
    /// <param name="value">The DateTimeOffset value to convert.</param>
    /// <param name="targetType">The target type (DateTime?).</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>DateTime? value compatible with DatePicker.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value == null)
                return null;

            if (value is DateTimeOffset dateTimeOffset)
                return dateTimeOffset.DateTime;

            if (value is DateTime dateTime)
                return dateTime;

            // Handle nullable types
            if (value.GetType() == typeof(DateTimeOffset?))
            {
                var nullableDateTimeOffset = (DateTimeOffset?)value;
                return nullableDateTimeOffset?.DateTime;
            }

            if (value.GetType() == typeof(DateTime?))
            {
                return (DateTime?)value;
            }

            return null;
        }
        catch (Exception)
        {
            // Return null for any conversion errors to prevent crashes
            return null;
        }
    }

    /// <summary>
    /// Converts DateTime back to DateTimeOffset for ViewModel binding.
    /// </summary>
    /// <param name="value">The DateTime value from DatePicker.</param>
    /// <param name="targetType">The target type (DateTimeOffset?).</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>DateTimeOffset? value for ViewModel.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value == null)
                return null;

            if (value is DateTime dateTime)
                return new DateTimeOffset(dateTime);

            if (value is DateTimeOffset dateTimeOffset)
                return dateTimeOffset;

            return null;
        }
        catch (Exception)
        {
            // Return null for any conversion errors to prevent crashes
            return null;
        }
    }
}

/// <summary>
/// Converts between DateTime? and DateTime? with proper null handling for DatePicker binding.
/// </summary>
public class NullableDateTimeConverter : IValueConverter
{
    /// <summary>
    /// Converts DateTime? to DateTime? with validation and error handling.
    /// </summary>
    /// <param name="value">The DateTime? value to convert.</param>
    /// <param name="targetType">The target type (DateTime?).</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>DateTime? value with proper validation.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            return value switch
            {
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                null => null,
                string dateString when DateTime.TryParse(dateString, culture, DateTimeStyles.None, out var parsedDate) => parsedDate,
                _ => null
            };
        }
        catch (Exception)
        {
            // Return null for any conversion errors to prevent crashes
            return null;
        }
    }

    /// <summary>
    /// Converts DateTime? back to DateTime? with validation.
    /// </summary>
    /// <param name="value">The DateTime? value from DatePicker.</param>
    /// <param name="targetType">The target type (DateTime?).</param>
    /// <param name="parameter">Optional conversion parameter.</param>
    /// <param name="culture">The culture to use for conversion.</param>
    /// <returns>DateTime? value with proper validation.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            return value switch
            {
                DateTime dateTime => dateTime,
                DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                null => null,
                string dateString when DateTime.TryParse(dateString, culture, DateTimeStyles.None, out var parsedDate) => parsedDate,
                _ => null
            };
        }
        catch (Exception)
        {
            // Return null for any conversion errors to prevent crashes
            return null;
        }
    }
}