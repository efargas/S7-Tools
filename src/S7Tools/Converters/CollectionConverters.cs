using System.Globalization;
using Avalonia.Data.Converters;
using S7Tools.Core.Models;

namespace S7Tools.Converters;

/// <summary>
/// Collection of converters for working with collections and memory segments.
/// </summary>
public static class CollectionConverters
{
    /// <summary>
    /// Converter that counts selected memory segments in a collection.
    /// </summary>
    public static readonly IValueConverter CountSelectedSegments = new SelectedSegmentCountConverter();

    /// <summary>
    /// Converter that counts total items in a collection.
    /// </summary>
    public static readonly IValueConverter Count = new CollectionCountConverter();

    /// <summary>
    /// Converter that checks if a collection is empty.
    /// </summary>
    public static readonly IValueConverter IsEmpty = new CollectionIsEmptyConverter();

    /// <summary>
    /// Converter that checks if a collection has items.
    /// </summary>
    public static readonly IValueConverter HasItems = new CollectionHasItemsConverter();

    /// <summary>
    /// Implementation for counting selected memory segments.
    /// </summary>
    private class SelectedSegmentCountConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                IEnumerable<MemorySegment> segments => segments.Count(s => s.IsSelected),
                _ => 0
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Return DoNothing instead of throwing to prevent binding errors
            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Implementation for counting collection items.
    /// </summary>
    private class CollectionCountConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                IEnumerable<object> collection => collection.Count(),
                System.Collections.ICollection collection => collection.Count,
                _ => 0
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Return DoNothing instead of throwing to prevent binding errors
            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Implementation for checking if collection is empty.
    /// </summary>
    private class CollectionIsEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                IEnumerable<object> collection => !collection.Any(),
                System.Collections.ICollection collection => collection.Count == 0,
                null => true,
                _ => false
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Return DoNothing instead of throwing to prevent binding errors
            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Implementation for checking if collection has items.
    /// </summary>
    private class CollectionHasItemsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                IEnumerable<object> collection => collection.Any(),
                System.Collections.ICollection collection => collection.Count > 0,
                null => false,
                _ => false
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Return DoNothing instead of throwing to prevent binding errors
            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }
}
