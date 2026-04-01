using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Data.Converters;
using S7Tools.Core.Constants;
using S7Tools.ViewModels.Controls;
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

    /// <summary>
    /// Cache for property metadata per type to avoid repeated reflection.
    /// Uses ConditionalWeakTable to allow types to be garbage collected if assemblies are unloaded.
    /// </summary>
    private static readonly ConditionalWeakTable<Type, IReadOnlyList<PropertyMetadata>> PropertyCache = new();

    /// <summary>
    /// Cached metadata for a property including its PropertyInfo, display name, and order.
    /// </summary>
    private sealed class PropertyMetadata
    {
        public PropertyInfo PropertyInfo { get; init; } = null!;
        public string DisplayLabel { get; init; } = string.Empty;
        public int Order { get; init; }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return new ObservableCollection<PropertyDisplayItem>();
        }

        var properties = new ObservableCollection<PropertyDisplayItem>();
        Type type = value.GetType();

        // Get cached property metadata or compute and cache it
        IReadOnlyList<PropertyMetadata> propertyMetadata = PropertyCache.GetValue(type, BuildPropertyMetadata);

        foreach (PropertyMetadata metadata in propertyMetadata)
        {
            try
            {
                object? propValue = metadata.PropertyInfo.GetValue(value);
                string displayValue = FormatValue(propValue);

                properties.Add(new PropertyDisplayItem
                {
                    Label = metadata.DisplayLabel,
                    Value = displayValue,
                    ValidationState = PropertyValidationState.Valid
                });
            }
            catch (Exception ex)
            {
                // Skip properties that throw exceptions, but log it for debugging.
                System.Diagnostics.Debug.WriteLine($"Error getting property '{metadata.PropertyInfo.Name}': {ex.Message}");
            }
        }

        return properties;
    }

    /// <summary>
    /// Builds and caches property metadata for a given type using reflection.
    /// This method is called once per type and the results are cached.
    /// </summary>
    private static IReadOnlyList<PropertyMetadata> BuildPropertyMetadata(Type type)
    {
        var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !ExcludedProperties.Contains(p.Name))
            // Filter using [Browsable(false)] attribute
            .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
            .Select(p => new PropertyMetadata
            {
                PropertyInfo = p,
                DisplayLabel = GetDisplayLabel(p),
                Order = p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? int.MaxValue
            })
            // Order using cached order value, then by property name
            .OrderBy(m => m.Order)
            .ThenBy(m => m.PropertyInfo.Name)
            .ToList();

        return publicProperties;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Gets the display label for a property.
    /// Uses [Display(Name = "...")] attribute if present, otherwise formats the property name.
    /// </summary>
    private static string GetDisplayLabel(PropertyInfo property)
    {
        DisplayAttribute? displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
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
        string result = PascalCaseRegex.Replace(propertyName, "$1 $2");

        // Handle acronyms (e.g., "TCPPort" -> "TCP Port")
        result = AcronymRegex.Replace(result, "$1 $2");

        return result;
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "N/A";
        }

        if (value is bool boolValue)
        {
            return boolValue ? "True" : "False";
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString(DateTimeFormats.ShortDateTime);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToString(DateTimeFormats.ShortDateTime);
        }

        return value.ToString() ?? "N/A";
    }
}
