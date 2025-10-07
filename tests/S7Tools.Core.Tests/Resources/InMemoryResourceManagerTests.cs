using System.Globalization;
using S7Tools.Core.Resources;
using Xunit;

namespace S7Tools.Core.Tests.Resources;

public class InMemoryResourceManagerTests
{
    [Fact]
    public void AddOrUpdate_And_GetString_Works_For_DefaultCulture()
    {
        var manager = new InMemoryResourceManager();
        manager.AddOrUpdate("Hello", "Hola");
        Assert.Equal("Hola", manager.GetString("Hello"));
    }

    [Fact]
    public void GetString_ReturnsKey_IfNotFound()
    {
        var manager = new InMemoryResourceManager();
        Assert.Equal("MissingKey", manager.GetString("MissingKey"));
    }

    [Fact]
    public void AddOrUpdate_And_GetString_Works_For_SpecificCulture()
    {
        var es = new CultureInfo("es-ES");
        var en = new CultureInfo("en-US");
        var manager = new InMemoryResourceManager(en);
        manager.AddOrUpdate("Hello", "Hello", en);
        manager.AddOrUpdate("Hello", "Hola", es);
        Assert.Equal("Hello", manager.GetString("Hello", en));
        Assert.Equal("Hola", manager.GetString("Hello", es));
    }

    [Fact]
    public void HasResource_ReturnsTrue_IfExists()
    {
        var manager = new InMemoryResourceManager();
        manager.AddOrUpdate("Key", "Valor");
        Assert.True(manager.HasResource("Key"));
    }

    [Fact]
    public void GetAvailableKeys_Returns_All_Keys()
    {
        var manager = new InMemoryResourceManager();
        manager.AddOrUpdate("A", "1");
        manager.AddOrUpdate("B", "2");
        var keys = manager.GetAvailableKeys();
        Assert.Contains("A", keys);
        Assert.Contains("B", keys);
    }

    [Fact]
    public void GetSupportedCultures_Returns_All_Cultures()
    {
        var manager = new InMemoryResourceManager();
        manager.AddOrUpdate("A", "1", new CultureInfo("es-ES"));
        manager.AddOrUpdate("B", "2", new CultureInfo("en-US"));
        var cultures = manager.GetSupportedCultures();
        Assert.Contains(cultures, c => c.Name == "es-ES");
        Assert.Contains(cultures, c => c.Name == "en-US");
    }

    [Fact]
    public void SetCurrentCulture_Changes_Culture()
    {
        var es = new CultureInfo("es-ES");
        var en = new CultureInfo("en-US");
        var manager = new InMemoryResourceManager(en);
        manager.AddOrUpdate("Hello", "Hello", en);
        manager.AddOrUpdate("Hello", "Hola", es);
        manager.SetCurrentCulture(es);
        Assert.Equal("Hola", manager.GetString("Hello"));
    }
}
