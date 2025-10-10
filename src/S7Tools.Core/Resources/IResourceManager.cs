using System.Collections.Generic;
using System.Globalization;

namespace S7Tools.Core.Resources;

/// <summary>
/// Defines a resource manager for handling localized resources.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Gets the current culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets a localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string for the specified key with formatting arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">The formatting arguments.</param>
    /// <returns>The formatted localized string, or the key if not found.</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string for the specified key and culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to use for localization.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string GetString(string key, CultureInfo culture);

    /// <summary>
    /// Gets a localized string for the specified key and culture with formatting arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to use for localization.</param>
    /// <param name="args">The formatting arguments.</param>
    /// <returns>The formatted localized string, or the key if not found.</returns>
    string GetString(string key, CultureInfo culture, params object[] args);

    /// <summary>
    /// Determines whether a resource exists for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>true if the resource exists; otherwise, false.</returns>
    bool HasResource(string key);

    /// <summary>
    /// Determines whether a resource exists for the specified key and culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to check.</param>
    /// <returns>true if the resource exists; otherwise, false.</returns>
    bool HasResource(string key, CultureInfo culture);

    /// <summary>
    /// Gets all available resource keys.
    /// </summary>
    /// <returns>A collection of available resource keys.</returns>
    IEnumerable<string> GetAvailableKeys();

    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    /// <returns>A collection of supported cultures.</returns>
    IEnumerable<CultureInfo> GetSupportedCultures();

    /// <summary>
    /// Sets the current culture for resource retrieval.
    /// </summary>
    /// <param name="culture">The culture to set as current.</param>
    void SetCurrentCulture(CultureInfo culture);
}

/// <summary>
/// Defines a typed resource manager for strongly-typed resource access.
/// </summary>
/// <typeparam name="T">The type representing the resource category.</typeparam>
public interface IResourceManager<T> : IResourceManager
{
    /// <summary>
    /// Gets the resource category type.
    /// </summary>
    System.Type ResourceType { get; }
}
