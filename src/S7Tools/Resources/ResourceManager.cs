using Microsoft.Extensions.Logging;
using S7Tools.Core.Resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace S7Tools.Resources;

/// <summary>
/// Implementation of resource manager for handling localized resources.
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly ILogger<ResourceManager> _logger;
    private readonly ConcurrentDictionary<string, System.Resources.ResourceManager> _resourceManagers;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _resourceCache;
    private CultureInfo _currentCulture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ResourceManager(ILogger<ResourceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceManagers = new ConcurrentDictionary<string, System.Resources.ResourceManager>();
        _resourceCache = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        _currentCulture = CultureInfo.CurrentUICulture;
        
        InitializeDefaultResources();
    }

    /// <inheritdoc/>
    public CultureInfo CurrentCulture => _currentCulture;

    /// <inheritdoc/>
    public string GetString(string key)
    {
        return GetString(key, _currentCulture);
    }

    /// <inheritdoc/>
    public string GetString(string key, params object[] args)
    {
        return GetString(key, _currentCulture, args);
    }

    /// <inheritdoc/>
    public string GetString(string key, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Resource key is null or empty");
            return key ?? string.Empty;
        }

        culture ??= _currentCulture;
        var cacheKey = $"{culture.Name}:{key}";

        // Try to get from cache first
        if (_resourceCache.TryGetValue(cacheKey, out var cultureCache) && 
            cultureCache.TryGetValue(key, out var cachedValue))
        {
            return cachedValue;
        }

        // Try to get from resource managers
        foreach (var resourceManager in _resourceManagers.Values)
        {
            try
            {
                var value = resourceManager.GetString(key, culture);
                if (!string.IsNullOrEmpty(value))
                {
                    // Cache the result
                    var cache = _resourceCache.GetOrAdd(cacheKey, _ => new ConcurrentDictionary<string, string>());
                    cache.TryAdd(key, value);
                    
                    _logger.LogDebug("Found resource for key: {Key}, Culture: {Culture}", key, culture.Name);
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving resource for key: {Key}, Culture: {Culture}", key, culture.Name);
            }
        }

        _logger.LogWarning("Resource not found for key: {Key}, Culture: {Culture}", key, culture.Name);
        return key; // Return the key as fallback
    }

    /// <inheritdoc/>
    public string GetString(string key, CultureInfo culture, params object[] args)
    {
        var format = GetString(key, culture);
        
        if (args == null || args.Length == 0)
        {
            return format;
        }

        try
        {
            return string.Format(culture, format, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Error formatting resource string for key: {Key}, Culture: {Culture}", key, culture.Name);
            return format; // Return unformatted string as fallback
        }
    }

    /// <inheritdoc/>
    public bool HasResource(string key)
    {
        return HasResource(key, _currentCulture);
    }

    /// <inheritdoc/>
    public bool HasResource(string key, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        culture ??= _currentCulture;

        foreach (var resourceManager in _resourceManagers.Values)
        {
            try
            {
                var value = resourceManager.GetString(key, culture);
                if (!string.IsNullOrEmpty(value))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking resource existence for key: {Key}, Culture: {Culture}", key, culture.Name);
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableKeys()
    {
        var keys = new HashSet<string>();

        foreach (var resourceManager in _resourceManagers.Values)
        {
            try
            {
                var resourceSet = resourceManager.GetResourceSet(_currentCulture, true, false);
                if (resourceSet != null)
                {
                    foreach (System.Collections.DictionaryEntry entry in resourceSet)
                    {
                        if (entry.Key is string key)
                        {
                            keys.Add(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving available keys from resource manager");
            }
        }

        return keys.ToList();
    }

    /// <inheritdoc/>
    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        var cultures = new HashSet<CultureInfo> { CultureInfo.InvariantCulture };

        // Add commonly supported cultures
        var commonCultures = new[]
        {
            "en-US", "en-GB", "es-ES", "fr-FR", "de-DE", "it-IT", "pt-BR", "ru-RU", "zh-CN", "ja-JP"
        };

        foreach (var cultureName in commonCultures)
        {
            try
            {
                cultures.Add(new CultureInfo(cultureName));
            }
            catch (CultureNotFoundException ex)
            {
                _logger.LogDebug(ex, "Culture not available: {CultureName}", cultureName);
            }
        }

        return cultures.ToList();
    }

    /// <inheritdoc/>
    public void SetCurrentCulture(CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException(nameof(culture));
        }

        var previousCulture = _currentCulture;
        _currentCulture = culture;
        
        _logger.LogInformation("Culture changed from {PreviousCulture} to {NewCulture}", 
            previousCulture.Name, culture.Name);

        // Clear cache when culture changes
        _resourceCache.Clear();
    }

    /// <summary>
    /// Registers a resource manager for a specific resource type.
    /// </summary>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceManager">The resource manager to register.</param>
    public void RegisterResourceManager(Type resourceType, System.Resources.ResourceManager resourceManager)
    {
        if (resourceType == null)
        {
            throw new ArgumentNullException(nameof(resourceType));
        }

        if (resourceManager == null)
        {
            throw new ArgumentNullException(nameof(resourceManager));
        }

        var key = resourceType.FullName ?? resourceType.Name;
        _resourceManagers.AddOrUpdate(key, resourceManager, (_, _) => resourceManager);
        
        _logger.LogDebug("Registered resource manager for type: {ResourceType}", resourceType.Name);
    }

    /// <summary>
    /// Unregisters a resource manager for a specific resource type.
    /// </summary>
    /// <param name="resourceType">The type of resource.</param>
    /// <returns>true if the resource manager was removed; otherwise, false.</returns>
    public bool UnregisterResourceManager(Type resourceType)
    {
        if (resourceType == null)
        {
            return false;
        }

        var key = resourceType.FullName ?? resourceType.Name;
        var removed = _resourceManagers.TryRemove(key, out _);
        
        if (removed)
        {
            _logger.LogDebug("Unregistered resource manager for type: {ResourceType}", resourceType.Name);
            // Clear related cache entries
            var keysToRemove = _resourceCache.Keys.Where(k => k.Contains(key)).ToList();
            foreach (var cacheKey in keysToRemove)
            {
                _resourceCache.TryRemove(cacheKey, out _);
            }
        }

        return removed;
    }

    /// <summary>
    /// Initializes default resource managers.
    /// </summary>
    private void InitializeDefaultResources()
    {
        try
        {
            // Register the main UI strings resource manager
            var uiStringsResourceManager = new System.Resources.ResourceManager(
                "S7Tools.Resources.Strings.UIStrings", 
                typeof(ResourceManager).Assembly);
            
            RegisterResourceManager(typeof(UIStrings), uiStringsResourceManager);
            
            _logger.LogDebug("Initialized default resource managers");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error initializing default resource managers");
        }
    }
}

/// <summary>
/// Typed resource manager for strongly-typed resource access.
/// </summary>
/// <typeparam name="T">The type representing the resource category.</typeparam>
public class ResourceManager<T> : IResourceManager<T>
{
    private readonly IResourceManager _baseResourceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceManager{T}"/> class.
    /// </summary>
    /// <param name="baseResourceManager">The base resource manager.</param>
    public ResourceManager(IResourceManager baseResourceManager)
    {
        _baseResourceManager = baseResourceManager ?? throw new ArgumentNullException(nameof(baseResourceManager));
    }

    /// <inheritdoc/>
    public Type ResourceType => typeof(T);

    /// <inheritdoc/>
    public CultureInfo CurrentCulture => _baseResourceManager.CurrentCulture;

    /// <inheritdoc/>
    public string GetString(string key) => _baseResourceManager.GetString(key);

    /// <inheritdoc/>
    public string GetString(string key, params object[] args) => _baseResourceManager.GetString(key, args);

    /// <inheritdoc/>
    public string GetString(string key, CultureInfo culture) => _baseResourceManager.GetString(key, culture);

    /// <inheritdoc/>
    public string GetString(string key, CultureInfo culture, params object[] args) => 
        _baseResourceManager.GetString(key, culture, args);

    /// <inheritdoc/>
    public bool HasResource(string key) => _baseResourceManager.HasResource(key);

    /// <inheritdoc/>
    public bool HasResource(string key, CultureInfo culture) => _baseResourceManager.HasResource(key, culture);

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableKeys() => _baseResourceManager.GetAvailableKeys();

    /// <inheritdoc/>
    public IEnumerable<CultureInfo> GetSupportedCultures() => _baseResourceManager.GetSupportedCultures();

    /// <inheritdoc/>
    public void SetCurrentCulture(CultureInfo culture) => _baseResourceManager.SetCurrentCulture(culture);
}