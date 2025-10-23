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

    [Fact(DisplayName = "ConvertBack throws NotSupportedException")]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange & Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture));
    }

    #region Test Classes

    private class SimpleTestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class TestObjectWithBrowsable
    {
        [Display(Name = "Visible Property")]
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
