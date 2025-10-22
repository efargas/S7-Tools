using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Converters;
using S7Tools.ViewModels.Profiles;

namespace S7Tools.Converters;

/// <summary>
/// Converts an object to a collection of PropertyDisplayItem instances
/// by reflecting over its public properties.
/// </summary>
public class ObjectToPropertiesConverter : IValueConverter
{
    private static readonly HashSet<string> ExcludedProperties = new()
    {
        "Type", // Usually an enum or type identifier
        "Configuration" // Nested configuration object handled separately
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return new ObservableCollection<PropertyDisplayItem>();

        var properties = new ObservableCollection<PropertyDisplayItem>();
        var type = value.GetType();

        // Get all public properties
        var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !ExcludedProperties.Contains(p.Name))
            .OrderBy(p => p.Name);

        foreach (var prop in publicProperties)
        {
            try
            {
                var propValue = prop.GetValue(value);
                var displayValue = FormatValue(propValue);

                properties.Add(new PropertyDisplayItem
                {
                    Label = FormatLabel(prop.Name),
                    Value = displayValue,
                    ValidationState = PropertyValidationState.Valid
                });
            }
            catch
            {
                // Skip properties that throw exceptions when accessed
            }
        }

        return properties;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatLabel(string propertyName)
    {
        // Insert spaces before capital letters (PascalCase to Title Case)
        var result = System.Text.RegularExpressions.Regex.Replace(
            propertyName,
            "([a-z])([A-Z])",
            "$1 $2"
        );

        // Handle acronyms (e.g., "TCPPort" -> "TCP Port")
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            "([A-Z]+)([A-Z][a-z])",
            "$1 $2"
        );

        return result;
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "N/A";

        if (value is bool boolValue)
            return boolValue ? "True" : "False";

        if (value is DateTime dateTime)
            return dateTime.ToString("yyyy-MM-dd HH:mm");

        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset.ToString("yyyy-MM-dd HH:mm");

        return value.ToString() ?? "N/A";
    }
}
