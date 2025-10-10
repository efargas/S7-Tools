using System.Globalization;
using System.Resources;
using S7Tools.Resources.Strings;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for managing application localization and resource strings.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly List<CultureInfo> _availableCultures;
    private CultureInfo _currentCulture;
    private CultureInfo _currentUICulture;

    /// <summary>
    /// Initializes a new instance of the LocalizationService class.
    /// </summary>
    public LocalizationService()
    {
        _resourceManager = UIStrings.ResourceManager;
        _availableCultures = GetAvailableCultures();
        _currentCulture = CultureInfo.CurrentCulture;
        _currentUICulture = CultureInfo.CurrentUICulture;
    }

    /// <inheritdoc />
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <inheritdoc />
    public CultureInfo CurrentCulture => _currentCulture;

    /// <inheritdoc />
    public CultureInfo CurrentUICulture => _currentUICulture;

    /// <inheritdoc />
    public IReadOnlyList<CultureInfo> AvailableCultures => _availableCultures.AsReadOnly();

    /// <inheritdoc />
    public string GetString(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        try
        {
            var value = _resourceManager.GetString(key, _currentUICulture);
            return value ?? key;
        }
        catch
        {
            return key;
        }
    }

    /// <inheritdoc />
    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);

        if (args == null || args.Length == 0)
        {
            return format;
        }

        try
        {
            return string.Format(_currentUICulture, format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <inheritdoc />
    public bool TryGetString(string key, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        try
        {
            var result = _resourceManager.GetString(key, _currentUICulture);
            if (result != null)
            {
                value = result;
                return true;
            }
        }
        catch
        {
            // Ignore exceptions and return false
        }

        return false;
    }

    /// <inheritdoc />
    public bool SetCulture(CultureInfo culture)
    {
        if (culture == null)
        {
            return false;
        }

        var bestMatch = GetBestMatchingCulture(culture);
        var oldCulture = _currentUICulture;

        if (bestMatch.Equals(_currentUICulture))
        {
            return true;
        }

        try
        {
            _currentCulture = bestMatch;
            _currentUICulture = bestMatch;

            // Update thread cultures
            Thread.CurrentThread.CurrentCulture = bestMatch;
            Thread.CurrentThread.CurrentUICulture = bestMatch;

            // Update default cultures for new threads
            CultureInfo.DefaultThreadCurrentCulture = bestMatch;
            CultureInfo.DefaultThreadCurrentUICulture = bestMatch;

            // Update resource manager culture
            UIStrings.Culture = bestMatch;

            // Raise culture changed event
            CultureChanged?.Invoke(this, new CultureChangedEventArgs(oldCulture, bestMatch));

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetCulture(string cultureName)
    {
        if (string.IsNullOrEmpty(cultureName))
        {
            return false;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            return SetCulture(culture);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void ResetToSystemCulture()
    {
        var systemCulture = CultureInfo.InstalledUICulture;
        SetCulture(systemCulture);
    }

    /// <inheritdoc />
    public bool IsCultureSupported(CultureInfo culture)
    {
        if (culture == null)
        {
            return false;
        }

        return _availableCultures.Any(c =>
            c.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase) ||
            c.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public CultureInfo GetBestMatchingCulture(CultureInfo culture)
    {
        if (culture == null)
        {
            return _availableCultures.First();
        }

        // Exact match
        var exactMatch = _availableCultures.FirstOrDefault(c =>
            c.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return exactMatch;
        }

        // Language match (e.g., "en" for "en-US")
        var languageMatch = _availableCultures.FirstOrDefault(c =>
            c.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
        if (languageMatch != null)
        {
            return languageMatch;
        }

        // Parent culture match
        if (!culture.IsNeutralCulture)
        {
            var parentMatch = _availableCultures.FirstOrDefault(c =>
                c.Name.Equals(culture.Parent.Name, StringComparison.OrdinalIgnoreCase));
            if (parentMatch != null)
            {
                return parentMatch;
            }
        }

        // Default to first available culture (usually English)
        return _availableCultures.First();
    }

    private List<CultureInfo> GetAvailableCultures()
    {
        var cultures = new List<CultureInfo>();

        // Add default culture (English)
        cultures.Add(CultureInfo.GetCultureInfo("en-US"));

        // Add other supported cultures
        // Note: In a real implementation, you would scan for available resource files
        // or maintain a configuration of supported cultures
        var supportedCultureNames = new[]
        {
            "en-US", // English (United States)
            "en-GB", // English (United Kingdom)
            "de-DE", // German (Germany)
            "fr-FR", // French (France)
            "es-ES", // Spanish (Spain)
            "it-IT", // Italian (Italy)
            "ja-JP", // Japanese (Japan)
            "ko-KR", // Korean (Korea)
            "zh-CN", // Chinese (Simplified)
            "zh-TW", // Chinese (Traditional)
            "ru-RU", // Russian (Russia)
            "pt-BR", // Portuguese (Brazil)
            "nl-NL", // Dutch (Netherlands)
            "sv-SE", // Swedish (Sweden)
            "da-DK", // Danish (Denmark)
            "no-NO", // Norwegian (Norway)
            "fi-FI", // Finnish (Finland)
            "pl-PL", // Polish (Poland)
            "cs-CZ", // Czech (Czech Republic)
            "hu-HU"  // Hungarian (Hungary)
        };

        foreach (var cultureName in supportedCultureNames)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                if (!cultures.Any(c => c.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    cultures.Add(culture);
                }
            }
            catch
            {
                // Ignore invalid culture names
            }
        }

        return cultures;
    }
}
