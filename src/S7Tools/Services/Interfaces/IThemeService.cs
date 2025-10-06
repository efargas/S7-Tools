using System.ComponentModel;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Service for managing application themes and visual appearance.
/// </summary>
public interface IThemeService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the current theme mode.
    /// </summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets all available themes.
    /// </summary>
    IReadOnlyList<ThemeInfo> AvailableThemes { get; }

    /// <summary>
    /// Gets a value indicating whether the current theme is dark.
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Gets a value indicating whether the current theme is light.
    /// </summary>
    bool IsLightTheme { get; }

    /// <summary>
    /// Gets a value indicating whether the theme follows the system setting.
    /// </summary>
    bool IsAutoTheme { get; }

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="theme">The theme to set.</param>
    /// <returns>True if the theme was successfully set; otherwise, false.</returns>
    bool SetTheme(ThemeMode theme);

    /// <summary>
    /// Sets the application theme by name.
    /// </summary>
    /// <param name="themeName">The name of the theme to set.</param>
    /// <returns>True if the theme was successfully set; otherwise, false.</returns>
    bool SetTheme(string themeName);

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    void ToggleTheme();

    /// <summary>
    /// Gets the current theme information.
    /// </summary>
    /// <returns>The current theme information.</returns>
    ThemeInfo GetCurrentThemeInfo();

    /// <summary>
    /// Gets a theme color by its key.
    /// </summary>
    /// <param name="colorKey">The color key to look up.</param>
    /// <returns>The color value if found; otherwise, null.</returns>
    string? GetThemeColor(string colorKey);

    /// <summary>
    /// Gets all theme colors for the current theme.
    /// </summary>
    /// <returns>A dictionary of color keys and values.</returns>
    IReadOnlyDictionary<string, string> GetThemeColors();

    /// <summary>
    /// Applies custom theme colors.
    /// </summary>
    /// <param name="customColors">The custom colors to apply.</param>
    void ApplyCustomColors(Dictionary<string, string> customColors);

    /// <summary>
    /// Resets theme colors to defaults.
    /// </summary>
    void ResetToDefaultColors();

    /// <summary>
    /// Saves the current theme configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveThemeConfigurationAsync();

    /// <summary>
    /// Loads the theme configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous load operation.</returns>
    Task LoadThemeConfigurationAsync();

    /// <summary>
    /// Detects the system theme preference.
    /// </summary>
    /// <returns>The detected system theme mode.</returns>
    ThemeMode DetectSystemTheme();
}

/// <summary>
/// Represents the available theme modes.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark,

    /// <summary>
    /// Automatically follow system theme.
    /// </summary>
    Auto
}

/// <summary>
/// Contains information about a theme.
/// </summary>
public sealed class ThemeInfo
{
    /// <summary>
    /// Initializes a new instance of the ThemeInfo class.
    /// </summary>
    /// <param name="mode">The theme mode.</param>
    /// <param name="name">The theme name.</param>
    /// <param name="displayName">The display name for the theme.</param>
    /// <param name="colors">The theme colors.</param>
    public ThemeInfo(ThemeMode mode, string name, string displayName, Dictionary<string, string> colors)
    {
        Mode = mode;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Colors = colors ?? throw new ArgumentNullException(nameof(colors));
    }

    /// <summary>
    /// Gets the theme mode.
    /// </summary>
    public ThemeMode Mode { get; }

    /// <summary>
    /// Gets the theme name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the display name for the theme.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the theme colors.
    /// </summary>
    public Dictionary<string, string> Colors { get; }

    /// <summary>
    /// Gets a value indicating whether this is a dark theme.
    /// </summary>
    public bool IsDark => Mode == ThemeMode.Dark;

    /// <summary>
    /// Gets a value indicating whether this is a light theme.
    /// </summary>
    public bool IsLight => Mode == ThemeMode.Light;
}

/// <summary>
/// Event arguments for theme change events.
/// </summary>
public sealed class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the ThemeChangedEventArgs class.
    /// </summary>
    /// <param name="previousTheme">The previous theme mode.</param>
    /// <param name="currentTheme">The current theme mode.</param>
    /// <param name="previousThemeInfo">The previous theme information.</param>
    /// <param name="currentThemeInfo">The current theme information.</param>
    public ThemeChangedEventArgs(ThemeMode previousTheme, ThemeMode currentTheme, ThemeInfo? previousThemeInfo, ThemeInfo currentThemeInfo)
    {
        PreviousTheme = previousTheme;
        CurrentTheme = currentTheme;
        PreviousThemeInfo = previousThemeInfo;
        CurrentThemeInfo = currentThemeInfo ?? throw new ArgumentNullException(nameof(currentThemeInfo));
    }

    /// <summary>
    /// Gets the previous theme mode.
    /// </summary>
    public ThemeMode PreviousTheme { get; }

    /// <summary>
    /// Gets the current theme mode.
    /// </summary>
    public ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets the previous theme information.
    /// </summary>
    public ThemeInfo? PreviousThemeInfo { get; }

    /// <summary>
    /// Gets the current theme information.
    /// </summary>
    public ThemeInfo CurrentThemeInfo { get; }
}