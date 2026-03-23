using FluentAssertions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using S7Tools.Converters;
using S7Tools.ViewModels.Controls;
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
        object? result = _converter.Convert(null, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        collection.Should().BeEmpty();
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
        object? result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        collection.Count.Should().Be(2);

        PropertyDisplayItem nameItem = collection.First(p => p.Label.Contains("Name"));
        nameItem.Value.Should().Be("Test");

        PropertyDisplayItem valueItem = collection.First(p => p.Label.Contains("Value"));
        valueItem.Value.Should().Be("42");
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
        object? result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        collection.Should().ContainSingle();
        collection[0].Label.Should().Be("Visible Property");
        collection[0].Value.Should().Be("Visible");
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
        object? result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        collection.Count.Should().Be(2);

        PropertyDisplayItem portItem = collection.First(p => p.Label == "TCP Port");
        portItem.Value.Should().Be("1234");

        PropertyDisplayItem hostItem = collection.First(p => p.Label == "Host Address");
        hostItem.Value.Should().Be("192.168.1.1");
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
        object? result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        collection.Count.Should().Be(3);

        collection[0].Label.Should().Be("First Property");
        collection[0].Value.Should().Be("A");

        collection[1].Label.Should().Be("Second Property");
        collection[1].Value.Should().Be("B");

        collection[2].Label.Should().Be("Third Property");
        collection[2].Value.Should().Be("C");
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
        object? result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        collection.Count.Should().Be(2);

        PropertyDisplayItem enabledItem = collection.First(p => p.Label.Contains("Enabled"));
        enabledItem.Value.Should().Be("True");

        PropertyDisplayItem disabledItem = collection.First(p => p.Label.Contains("Disabled"));
        disabledItem.Value.Should().Be("False");
    }

    [Fact(DisplayName = "ConvertBack returns AvaloniaProperty.UnsetValue")]
    public void ConvertBack_ReturnsUnsetValue()
    {
        // Arrange & Act
        object? result = _converter.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(Avalonia.AvaloniaProperty.UnsetValue);
    }

    [Fact(DisplayName = "Convert with same type twice uses cached reflection results")]
    public void Convert_WithSameTypeTwice_UsesCachedResults()
    {
        // Arrange
        var firstObject = new SimpleTestObject { Name = "First", Value = 1 };
        var secondObject = new SimpleTestObject { Name = "Second", Value = 2 };

        // Act - Convert first object (will populate cache using ConditionalWeakTable)
        object? firstResult = _converter.Convert(firstObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        ObservableCollection<PropertyDisplayItem> firstCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(firstResult);

        // Act - Convert second object (should use cache)
        object? secondResult = _converter.Convert(secondObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        ObservableCollection<PropertyDisplayItem> secondCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(secondResult);

        // Assert - Both conversions produce correct results with same structure
        firstCollection.Count.Should().Be(2);
        secondCollection.Count.Should().Be(2);

        // Verify first object values
        Assert.Contains(firstCollection, p => p.Label.Contains("Name") && p.Value == "First");
        Assert.Contains(firstCollection, p => p.Label.Contains("Value") && p.Value == "1");

        // Verify second object values
        Assert.Contains(secondCollection, p => p.Label.Contains("Name") && p.Value == "Second");
        Assert.Contains(secondCollection, p => p.Label.Contains("Value") && p.Value == "2");

        // Verify labels are consistent (proving cache is being used with ConditionalWeakTable)
        var firstLabels = firstCollection.Select(p => p.Label).ToList();
        var secondLabels = secondCollection.Select(p => p.Label).ToList();
        secondLabels.Should().Equal(firstLabels); // Ensures order and content are the same

        // For a stronger cache proof, assert that the string instances are the same.
        // This is a good indicator that they came from the same cached PropertyMetadata.
        // ConditionalWeakTable allows types to be GC'd if assemblies are unloaded,
        // but during normal operation, it maintains the cache for active types.
        for (int i = 0; i < firstCollection.Count; i++)
        {
            secondCollection[i].Label.Should().BeSameAs(firstCollection[i].Label);
        }
    }

    [Fact(DisplayName = "Convert caches property order correctly")]
    public void Convert_CachesPropertyOrderCorrectly()
    {
        // Arrange
        var firstObject = new TestObjectWithOrder { Third = "C1", First = "A1", Second = "B1" };
        var secondObject = new TestObjectWithOrder { Third = "C2", First = "A2", Second = "B2" };

        // Act
        object? firstResult = _converter.Convert(firstObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        ObservableCollection<PropertyDisplayItem> firstCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(firstResult);

        object? secondResult = _converter.Convert(secondObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);
        ObservableCollection<PropertyDisplayItem> secondCollection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(secondResult);

        // Assert - Order is consistent across both conversions
        firstCollection.Count.Should().Be(3);
        secondCollection.Count.Should().Be(3);

        // First conversion order
        firstCollection[0].Label.Should().Be("First Property");
        firstCollection[1].Label.Should().Be("Second Property");
        firstCollection[2].Label.Should().Be("Third Property");

        // Second conversion order (should match first due to cache)
        secondCollection[0].Label.Should().Be("First Property");
        secondCollection[1].Label.Should().Be("Second Property");
        secondCollection[2].Label.Should().Be("Third Property");
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
