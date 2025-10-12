using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace S7Tools.Converters;

using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Controls;

namespace S7Tools.Converters;

/// <summary>
/// A converter that converts a boolean or a non-empty string to a visibility state and vice-versa.
/// True or a non-empty string is converted to Visible, and False or an empty/null string is converted to Collapsed.
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isVisible = value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            _ => false
        };
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isVisible = value is Visibility v && v == Visibility.Visible;
        return isVisible;
    }
}