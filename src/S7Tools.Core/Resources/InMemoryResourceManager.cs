using System.Collections.Generic;
using System.Globalization;

namespace S7Tools.Core.Resources;

/// <summary>
/// Implementación simple en memoria de IResourceManager para uso de dominio.
/// </summary>
public class InMemoryResourceManager : IResourceManager
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources;

    /// <inheritdoc/>
    public CultureInfo CurrentCulture { get; private set; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="InMemoryResourceManager"/>.
    /// </summary>
    public InMemoryResourceManager(CultureInfo? defaultCulture = null)
    {
        _resources = new();
        CurrentCulture = defaultCulture ?? CultureInfo.InvariantCulture;
    }

    /// <inheritdoc/>
    public string GetString(string key) => GetString(key, CurrentCulture);

    /// <inheritdoc/>
    public string GetString(string key, params object[] args) =>
        string.Format(GetString(key, CurrentCulture), args);

    /// <inheritdoc/>
    public string GetString(string key, CultureInfo culture)
    {
        var cultureKey = culture.Name;
        if (_resources.TryGetValue(cultureKey, out var dict))
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        if (_resources.TryGetValue("", out var fallbackDict))
        {
            if (fallbackDict.TryGetValue(key, out var fallback))
            {
                return fallback;
            }
        }
        return key;
    }

    /// <inheritdoc/>
    public string GetString(string key, CultureInfo culture, params object[] args) =>
        string.Format(GetString(key, culture), args);

    /// <inheritdoc/>
    public bool HasResource(string key) => HasResource(key, CurrentCulture);

    /// <inheritdoc/>
    public bool HasResource(string key, CultureInfo culture)
    {
        var cultureKey = culture.Name;
        if (_resources.TryGetValue(cultureKey, out var dict) && dict.ContainsKey(key))
        {
            return true;
        }
        if (_resources.TryGetValue("", out var fallback) && fallback.ContainsKey(key))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Agrega o actualiza un recurso para una cultura dada.
    /// </summary>
    public void AddOrUpdate(string key, string value, CultureInfo? culture = null)
    {
        var cultureKey = (culture ?? CurrentCulture).Name;
        if (!_resources.ContainsKey(cultureKey))
        {
            _resources[cultureKey] = new();
        }
        _resources[cultureKey][key] = value;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableKeys()
    {
        var keys = new HashSet<string>();
        foreach (var dict in _resources.Values)
        {
            foreach (var key in dict.Keys)
            {
                keys.Add(key);
            }
        }
        return keys;
    }

    /// <inheritdoc/>
    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        var cultures = new List<CultureInfo>();
        foreach (var key in _resources.Keys)
        {
            if (!string.IsNullOrEmpty(key))
            {
                cultures.Add(new CultureInfo(key));
            }
        }
        return cultures;
    }

    /// <inheritdoc/>
    public void SetCurrentCulture(CultureInfo culture)
    {
        CurrentCulture = culture;
    }
}
