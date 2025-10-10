using System.Globalization;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for managing application localization and resource strings.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current culture used for localization.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the current UI culture used for localization.
    /// </summary>
    CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Gets all available cultures supported by the application.
    /// </summary>
    IReadOnlyList<CultureInfo> AvailableCultures { get; }

    /// <summary>
    /// Event raised when the current culture changes.
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <summary>
    /// Gets a localized string by its key.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string by its key with format arguments.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    /// <param name="args">Format arguments for the string.</param>
    /// <returns>The formatted localized string, or the key if not found.</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Tries to get a localized string by its key.
    /// </summary>
    /// <param name="key">The resource key to look up.</param>
    /// <param name="value">The localized string if found.</param>
    /// <returns>True if the string was found; otherwise, false.</returns>
    bool TryGetString(string key, out string value);

    /// <summary>
    /// Sets the current culture for the application.
    /// </summary>
    /// <param name="culture">The culture to set.</param>
    /// <returns>True if the culture was successfully set; otherwise, false.</returns>
    bool SetCulture(CultureInfo culture);

    /// <summary>
    /// Sets the current culture for the application by culture name.
    /// </summary>
    /// <param name="cultureName">The name of the culture to set (e.g., "en-US", "de-DE").</param>
    /// <returns>True if the culture was successfully set; otherwise, false.</returns>
    bool SetCulture(string cultureName);

    /// <summary>
    /// Resets the culture to the system default.
    /// </summary>
    void ResetToSystemCulture();

    /// <summary>
    /// Determines if a culture is supported by the application.
    /// </summary>
    /// <param name="culture">The culture to check.</param>
    /// <returns>True if the culture is supported; otherwise, false.</returns>
    bool IsCultureSupported(CultureInfo culture);

    /// <summary>
    /// Gets the best matching supported culture for the given culture.
    /// </summary>
    /// <param name="culture">The desired culture.</param>
    /// <returns>The best matching supported culture.</returns>
    CultureInfo GetBestMatchingCulture(CultureInfo culture);
}

/// <summary>
/// Event arguments for culture change events.
/// </summary>
public sealed class CultureChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the CultureChangedEventArgs class.
    /// </summary>
    /// <param name="oldCulture">The previous culture.</param>
    /// <param name="newCulture">The new culture.</param>
    public CultureChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
    {
        OldCulture = oldCulture ?? throw new ArgumentNullException(nameof(oldCulture));
        NewCulture = newCulture ?? throw new ArgumentNullException(nameof(newCulture));
    }

    /// <summary>
    /// Gets the previous culture.
    /// </summary>
    public CultureInfo OldCulture { get; }

    /// <summary>
    /// Gets the new culture.
    /// </summary>
    public CultureInfo NewCulture { get; }
}
