using System.Globalization;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Service for managing localized strings and resource files.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string by key with format arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">The format arguments.</param>
    /// <returns>The formatted localized string, or the key if not found.</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets the current culture being used for localization.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Sets the current culture for localization.
    /// </summary>
    /// <param name="culture">The culture to set.</param>
    void SetCulture(CultureInfo culture);

    /// <summary>
    /// Gets all available cultures supported by the application.
    /// </summary>
    /// <returns>An array of supported cultures.</returns>
    CultureInfo[] GetSupportedCultures();
}
