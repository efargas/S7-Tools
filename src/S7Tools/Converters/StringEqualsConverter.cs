using System;
using Avalonia.Data.Converters;

namespace S7Tools.Converters;

/// <summary>
/// Compares two strings for equality using case-insensitive comparison.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    /// <summary>
    /// Compares the value with the parameter string using case-insensitive comparison.
    /// </summary>
    /// <param name="value">The first string to compare.</param>
    /// <param name="targetType">The target type (not used).</param>
    /// <param name="parameter">The second string to compare.</param>
    /// <param name="culture">The culture information (not used).</param>
    /// <returns>True if the strings are equal (case-insensitive); otherwise, false.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        string? s1 = value as string;
        string? s2 = parameter as string;
        return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Not supported - this converter does not support two-way binding.
    /// </summary>
    /// <param name="value">The value to convert back.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">An optional parameter.</param>
    /// <param name="culture">The culture information.</param>
    /// <returns>Not supported.</returns>
    /// <exception cref="NotSupportedException">This method is not supported.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
