using System.Globalization;
using S7Tools.Core.Resources;

namespace S7Tools.Resources;

/// <summary>
/// Implementación de ResourceManager para inyección en DI (puede delegar a InMemoryResourceManager).
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly InMemoryResourceManager _inner;

    public ResourceManager() : this(CultureInfo.InvariantCulture) { }
    public ResourceManager(CultureInfo defaultCulture)
    {
        _inner = new InMemoryResourceManager(defaultCulture);
    }

    public CultureInfo CurrentCulture => _inner.CurrentCulture;
    public string GetString(string key) => _inner.GetString(key);
    public string GetString(string key, params object[] args) => _inner.GetString(key, args);
    public string GetString(string key, CultureInfo culture) => _inner.GetString(key, culture);
    public string GetString(string key, CultureInfo culture, params object[] args) => _inner.GetString(key, culture, args);
    public bool HasResource(string key) => _inner.HasResource(key);
    public bool HasResource(string key, CultureInfo culture) => _inner.HasResource(key, culture);
    public IEnumerable<string> GetAvailableKeys() => _inner.GetAvailableKeys();
    public IEnumerable<CultureInfo> GetSupportedCultures() => _inner.GetSupportedCultures();
    public void SetCurrentCulture(CultureInfo culture) => _inner.SetCurrentCulture(culture);
    public void AddOrUpdate(string key, string value, CultureInfo? culture = null) => _inner.AddOrUpdate(key, value, culture);
}
