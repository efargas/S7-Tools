using System.Globalization;
using S7Tools.Core.Resources;

namespace S7Tools.Resources;

/// <summary>
/// Implementación de ResourceManager para inyección en DI (puede delegar a InMemoryResourceManager).
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly InMemoryResourceManager _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class with the invariant culture.
    /// </summary>
    public ResourceManager() : this(CultureInfo.InvariantCulture) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class with a specific default culture.
    /// </summary>
    /// <param name="defaultCulture">The default culture to use.</param>
    public ResourceManager(CultureInfo defaultCulture)
    {
        _inner = new InMemoryResourceManager(defaultCulture);
    }

    /// <inheritdoc />
    public CultureInfo CurrentCulture => _inner.CurrentCulture;
    /// <inheritdoc />
    public string GetString(string key) => _inner.GetString(key);
    /// <inheritdoc />
    public string GetString(string key, params object[] args) => _inner.GetString(key, args);
    /// <inheritdoc />
    public string GetString(string key, CultureInfo culture) => _inner.GetString(key, culture);
    /// <inheritdoc />
    public string GetString(string key, CultureInfo culture, params object[] args) => _inner.GetString(key, culture, args);
    /// <inheritdoc />
    public bool HasResource(string key) => _inner.HasResource(key);
    /// <inheritdoc />
    public bool HasResource(string key, CultureInfo culture) => _inner.HasResource(key, culture);
    /// <inheritdoc />
    public IEnumerable<string> GetAvailableKeys() => _inner.GetAvailableKeys();
    /// <inheritdoc />
    public IEnumerable<CultureInfo> GetSupportedCultures() => _inner.GetSupportedCultures();
    /// <inheritdoc />
    public void SetCurrentCulture(CultureInfo culture) => _inner.SetCurrentCulture(culture);
    /// <inheritdoc />
    public void AddOrUpdate(string key, string value, CultureInfo? culture = null) => _inner.AddOrUpdate(key, value, culture);
}
