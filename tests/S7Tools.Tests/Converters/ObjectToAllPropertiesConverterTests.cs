using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using S7Tools.Converters;
using S7Tools.ViewModels.Controls;
using S7Tools.ViewModels.Profiles;
using Xunit;

namespace S7Tools.Tests.Converters;

/// <summary>
/// Tests for ObjectToAllPropertiesConverter to verify it shows all properties
/// including those marked with [Browsable(false)]
/// </summary>
public class ObjectToAllPropertiesConverterTests
{
    private readonly ObjectToAllPropertiesConverter _converter;

    public ObjectToAllPropertiesConverterTests()
    {
        _converter = new ObjectToAllPropertiesConverter();
    }

    [Fact(DisplayName = "Convert with null value returns empty collection")]
    public void Convert_WithNullValue_ReturnsEmptyCollection()
    {
        // Act
        object? result = _converter.Convert(null, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Empty(collection);
    }

    [Fact(DisplayName = "Convert shows all properties including Browsable(false)")]
    public void Convert_WithBrowsableFalseProperties_ShowsAllProperties()
    {
        // Arrange
        var testObject = new TestObjectWithBrowsableFalse
        {
            VisibleProperty = "Visible",
            HiddenProperty = "Hidden"
        };

        // Act
        object? result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);

        // Should show both properties (unlike ObjectToPropertiesConverter which would hide HiddenProperty)
        Assert.Equal(2, collection.Count);
        Assert.Contains(collection, p => p.Label.Contains("Visible Property"));
        Assert.Contains(collection, p => p.Label.Contains("Hidden Property"));
    }

    [Fact(DisplayName = "Convert respects Display Name attribute")]
    public void Convert_WithDisplayNameAttribute_UsesCustomLabel()
    {
        // Arrange
        var testObject = new TestObjectWithDisplayName
        {
            CustomLabelProperty = "Test Value"
        };

        // Act
        object? result = _converter.Convert(testObject, typeof(ObservableCollection<PropertyDisplayItem>), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        ObservableCollection<PropertyDisplayItem> collection = Assert.IsType<ObservableCollection<PropertyDisplayItem>>(result);
        Assert.Single(collection);
        Assert.Equal("Custom Display Name", collection[0].Label);
        Assert.Equal("Test Value", collection[0].Value);
    }

    private class TestObjectWithBrowsableFalse
    {
        public string VisibleProperty { get; set; } = string.Empty;

        [Browsable(false)]
        public string HiddenProperty { get; set; } = string.Empty;
    }

    private class TestObjectWithDisplayName
    {
        [Display(Name = "Custom Display Name")]
        public string CustomLabelProperty { get; set; } = string.Empty;
    }
}
