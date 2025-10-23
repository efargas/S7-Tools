using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Converters;
using S7Tools.ViewModels.Profiles;

namespace S7Tools.Converters;

/// <summary>
/// Converts an object to a collection of PropertyDisplayItem instances
/// by reflecting over its public properties.
/// Uses attributes to control display:
/// - [Browsable(false)] to hide properties
/// - [Display(Name = "...", Order = N)] to customize label and order
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
            // Filter using [Browsable(false)] attribute
            .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
            // Order using [Display(Order = ...)] attribute, then by name
            .OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? int.MaxValue)
            .ThenBy(p => p.Name);

        foreach (var prop in publicProperties)
        {
            try
            {
                var propValue = prop.GetValue(value);
                var displayValue = FormatValue(propValue);
                var label = GetDisplayLabel(prop);

                properties.Add(new PropertyDisplayItem
                {
                    Label = label,
                    Value = displayValue,
                    ValidationState = PropertyValidationState.Valid
                });
            }
            catch (Exception ex)
            {
                // Skip properties that throw exceptions, but log it for debugging.
                System.Diagnostics.Debug.WriteLine($"Error getting property '{prop.Name}': {ex.Message}");
            }
        }

        return properties;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the display label for a property.
    /// Uses [Display(Name = "...")] attribute if present, otherwise formats the property name.
    /// </summary>
    private static string GetDisplayLabel(PropertyInfo property)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute?.Name != null)
        {
            return displayAttribute.Name;
        }

        return FormatLabel(property.Name);
    }

    private static readonly System.Text.RegularExpressions.Regex PascalCaseRegex = new("([a-z])([A-Z])", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex AcronymRegex = new("([A-Z]+)([A-Z][a-z])", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string FormatLabel(string propertyName)
    {
        // Insert spaces before capital letters (PascalCase to Title Case)
        var result = PascalCaseRegex.Replace(propertyName, "$1 $2");

        // Handle acronyms (e.g., "TCPPort" -> "TCP Port")
        result = AcronymRegex.Replace(result, "$1 $2");

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
