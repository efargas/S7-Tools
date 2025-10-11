using System.Globalization;
using S7Tools.Core.Resources;

namespace S7Tools.Resources;

/// <summary>
/// ResourceManager implementation for dependency injection that delegates to InMemoryResourceManager.
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly InMemoryResourceManager _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class with invariant culture.
    /// </summary>
    public ResourceManager() : this(CultureInfo.InvariantCulture) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class with the specified default culture.
    /// </summary>
    /// <param name="defaultCulture">The default culture to use for resource retrieval.</param>
    public ResourceManager(CultureInfo defaultCulture)
    {
        _inner = new InMemoryResourceManager(defaultCulture);
    }

    /// <summary>
    /// Gets the current culture used for resource retrieval.
    /// </summary>
    public CultureInfo CurrentCulture => _inner.CurrentCulture;
    
    /// <summary>
    /// Gets the string resource for the specified key using the current culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string.</returns>
    public string GetString(string key) => _inner.GetString(key);
    
    /// <summary>
    /// Gets the formatted string resource for the specified key using the current culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">The formatting arguments.</param>
    /// <returns>The formatted localized string.</returns>
    public string GetString(string key, params object[] args) => _inner.GetString(key, args);
    
    /// <summary>
    /// Gets the string resource for the specified key using the specified culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to use for resource retrieval.</param>
    /// <returns>The localized string.</returns>
    public string GetString(string key, CultureInfo culture) => _inner.GetString(key, culture);
    
    /// <summary>
    /// Gets the formatted string resource for the specified key using the specified culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to use for resource retrieval.</param>
    /// <param name="args">The formatting arguments.</param>
    /// <returns>The formatted localized string.</returns>
    public string GetString(string key, CultureInfo culture, params object[] args) => _inner.GetString(key, culture, args);
    
    /// <summary>
    /// Determines whether a resource exists for the specified key using the current culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>True if the resource exists; otherwise, false.</returns>
    public bool HasResource(string key) => _inner.HasResource(key);
    
    /// <summary>
    /// Determines whether a resource exists for the specified key using the specified culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to check.</param>
    /// <returns>True if the resource exists; otherwise, false.</returns>
    public bool HasResource(string key, CultureInfo culture) => _inner.HasResource(key, culture);
    
    /// <summary>
    /// Gets all available resource keys.
    /// </summary>
    /// <returns>An enumerable collection of resource keys.</returns>
    public IEnumerable<string> GetAvailableKeys() => _inner.GetAvailableKeys();
    
    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    /// <returns>An enumerable collection of supported cultures.</returns>
    public IEnumerable<CultureInfo> GetSupportedCultures() => _inner.GetSupportedCultures();
    
    /// <summary>
    /// Sets the current culture for resource retrieval.
    /// </summary>
    /// <param name="culture">The culture to set as current.</param>
    public void SetCurrentCulture(CultureInfo culture) => _inner.SetCurrentCulture(culture);
    
    /// <summary>
    /// Adds or updates a resource for the specified key and culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="value">The resource value.</param>
    /// <param name="culture">The culture for the resource, or null for the current culture.</param>
    public void AddOrUpdate(string key, string value, CultureInfo? culture = null) => _inner.AddOrUpdate(key, value, culture);
}
