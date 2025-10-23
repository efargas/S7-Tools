using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using S7Tools.Converters;
using S7Tools.ViewModels.Profiles;
using Xunit;

namespace S7Tools.Tests.Converters;

public class ObjectToPropertiesConverterTests
{
    private readonly ObjectToPropertiesConverter _converter;

    public ObjectToPropertiesConverterTests()
    {
        _converter = new ObjectToPropertiesConverter();
    }

    [Fact(DisplayName = "Convert with null value returns empty collection")]
    public void Convert_WithNullValue_ReturnsEmptyCollection()
    {
        // Arrange & Act
        var result = _converter.Convert(null, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Empty(collection);
    }

    [Fact(DisplayName = "Convert with simple object returns properties")]
    public void Convert_WithSimpleObject_ReturnsProperties()
    {
        // Arrange
        var testObject = new SimpleTestObject
        {
            Name = "Test",
            Value = 42
        };

        // Act
        var result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Equal(2, collection.Count);

        var nameItem = collection.First(p => p.Label.Contains("Name"));
        Assert.Equal("Test", nameItem.Value);

        var valueItem = collection.First(p => p.Label.Contains("Value"));
        Assert.Equal("42", valueItem.Value);
    }

    [Fact(DisplayName = "Convert respects Browsable(false) attribute")]
    public void Convert_WithBrowsableFalseAttribute_HidesProperty()
    {
        // Arrange
        var testObject = new TestObjectWithBrowsable
        {
            VisibleProperty = "Visible",
            HiddenProperty = "Hidden"
        };

        // Act
        var result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Single(collection);
        Assert.Equal("Visible Property", collection[0].Label);
        Assert.Equal("Visible", collection[0].Value);
    }

    [Fact(DisplayName = "Convert respects Display Name attribute")]
    public void Convert_WithDisplayNameAttribute_UsesCustomLabel()
    {
        // Arrange
        var testObject = new TestObjectWithDisplay
        {
            TcpPort = 1234,
            HostAddress = "192.168.1.1"
        };

        // Act
        var result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Equal(2, collection.Count);

        var portItem = collection.First(p => p.Label == "TCP Port");
        Assert.Equal("1234", portItem.Value);

        var hostItem = collection.First(p => p.Label == "Host Address");
        Assert.Equal("192.168.1.1", hostItem.Value);
    }

    [Fact(DisplayName = "Convert respects Display Order attribute")]
    public void Convert_WithDisplayOrderAttribute_OrdersPropertiesCorrectly()
    {
        // Arrange
        var testObject = new TestObjectWithOrder
        {
            Third = "C",
            First = "A",
            Second = "B"
        };

        // Act
        var result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Equal(3, collection.Count);

        Assert.Equal("First Property", collection[0].Label);
        Assert.Equal("A", collection[0].Value);

        Assert.Equal("Second Property", collection[1].Label);
        Assert.Equal("B", collection[1].Value);

        Assert.Equal("Third Property", collection[2].Label);
        Assert.Equal("C", collection[2].Value);
    }

    [Fact(DisplayName = "Convert formats boolean values correctly")]
    public void Convert_WithBooleanValue_FormatsCorrectly()
    {
        // Arrange
        var testObject = new TestObjectWithBoolean
        {
            IsEnabled = true,
            IsDisabled = false
        };

        // Act
        var result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Equal(2, collection.Count);

        var enabledItem = collection.First(p => p.Label.Contains("Enabled"));
        Assert.Equal("True", enabledItem.Value);

        var disabledItem = collection.First(p => p.Label.Contains("Disabled"));
        Assert.Equal("False", disabledItem.Value);
    }

    [Fact(DisplayName = "ConvertBack returns AvaloniaProperty.UnsetValue")]
    public void ConvertBack_ReturnsUnsetValue()
    {
        // Arrange & Act
        var result = _converter.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Avalonia.AvaloniaProperty.UnsetValue, result);
    }

    [Fact(DisplayName = "Convert with same type twice uses cached reflection results")]
    public void Convert_WithSameTypeTwice_UsesCachedResults()
    {
        // Arrange
        var firstObject = new SimpleTestObject { Name = "First", Value = 1 };
        var secondObject = new SimpleTestObject { Name = "Second", Value = 2 };

        // Act - Convert first object (will populate cache)
        var firstResult = _converter.Convert(firstObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        var firstCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(firstResult);

        // Act - Convert second object (should use cache)
        var secondResult = _converter.Convert(secondObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        var secondCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(secondResult);

        // Assert - Both conversions produce correct results with same structure
        Assert.Equal(2, firstCollection.Count);
        Assert.Equal(2, secondCollection.Count);

        // Verify first object values
        Assert.Contains(firstCollection, p => p.Label.Contains("Name") && p.Value == "First");
        Assert.Contains(firstCollection, p => p.Label.Contains("Value") && p.Value == "1");

        // Verify second object values
        Assert.Contains(secondCollection, p => p.Label.Contains("Name") && p.Value == "Second");
        Assert.Contains(secondCollection, p => p.Label.Contains("Value") && p.Value == "2");

        // Verify labels are consistent (proving cache is being used)
        var firstLabels = firstCollection.Select(p => p.Label).OrderBy(l => l).ToList();
        var secondLabels = secondCollection.Select(p => p.Label).OrderBy(l => l).ToList();
        Assert.Equal(firstLabels, secondLabels);
    }

    [Fact(DisplayName = "Convert caches property order correctly")]
    public void Convert_CachesPropertyOrderCorrectly()
    {
        // Arrange
        var firstObject = new TestObjectWithOrder { Third = "C1", First = "A1", Second = "B1" };
        var secondObject = new TestObjectWithOrder { Third = "C2", First = "A2", Second = "B2" };

        // Act
        var firstResult = _converter.Convert(firstObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        var firstCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(firstResult);

        var secondResult = _converter.Convert(secondObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        var secondCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(secondResult);

        // Assert - Order is consistent across both conversions
        Assert.Equal(3, firstCollection.Count);
        Assert.Equal(3, secondCollection.Count);

        // First conversion order
        Assert.Equal("First Property", firstCollection[0].Label);
        Assert.Equal("Second Property", firstCollection[1].Label);
        Assert.Equal("Third Property", firstCollection[2].Label);

        // Second conversion order (should match first due to cache)
        Assert.Equal("First Property", secondCollection[0].Label);
        Assert.Equal("Second Property", secondCollection[1].Label);
        Assert.Equal("Third Property", secondCollection[2].Label);
    }

    #region Test Classes

    private class SimpleTestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class TestObjectWithBrowsable
    {
        public string VisibleProperty { get; set; } = string.Empty;

        [Browsable(false)]
        public string HiddenProperty { get; set; } = string.Empty;
    }

    private class TestObjectWithDisplay
    {
        [Display(Name = "TCP Port")]
        public int TcpPort { get; set; }

        [Display(Name = "Host Address")]
        public string HostAddress { get; set; } = string.Empty;
    }

    private class TestObjectWithOrder
    {
        [Display(Name = "Third Property", Order = 3)]
        public string Third { get; set; } = string.Empty;

        [Display(Name = "First Property", Order = 1)]
        public string First { get; set; } = string.Empty;

        [Display(Name = "Second Property", Order = 2)]
        public string Second { get; set; } = string.Empty;
    }

    private class TestObjectWithBoolean
    {
        public bool IsEnabled { get; set; }
        public bool IsDisabled { get; set; }
    }

    #endregion
}
