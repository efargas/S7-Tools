using Avalonia;
using Avalonia.Styling;
using S7Tools.Services.Interfaces;
using System.ComponentModel;
using System.Text.Json;

namespace S7Tools.Services;

/// <summary>
/// Service for managing application themes and visual appearance.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const string ThemeConfigFileName = "theme.json";
    private readonly List<ThemeInfo> _availableThemes;
    private readonly Dictionary<string, string> _customColors;
    private ThemeMode _currentTheme;
    private ThemeInfo _currentThemeInfo;

    /// <summary>
    /// Initializes a new instance of the ThemeService class.
    /// </summary>
    public ThemeService()
    {
        _availableThemes = CreateAvailableThemes();
        _customColors = new Dictionary<string, string>();
        _currentTheme = ThemeMode.Dark; // Default to dark theme
        _currentThemeInfo = _availableThemes.First(t => t.Mode == _currentTheme);
        
        // Apply the initial theme
        ApplyTheme(_currentThemeInfo);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc />
    public ThemeMode CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public IReadOnlyList<ThemeInfo> AvailableThemes => _availableThemes.AsReadOnly();

    /// <inheritdoc />
    public bool IsDarkTheme => _currentTheme == ThemeMode.Dark || (_currentTheme == ThemeMode.Auto && DetectSystemTheme() == ThemeMode.Dark);

    /// <inheritdoc />
    public bool IsLightTheme => _currentTheme == ThemeMode.Light || (_currentTheme == ThemeMode.Auto && DetectSystemTheme() == ThemeMode.Light);

    /// <inheritdoc />
    public bool IsAutoTheme => _currentTheme == ThemeMode.Auto;

    /// <inheritdoc />
    public bool SetTheme(ThemeMode theme)
    {
        if (_currentTheme == theme)
            return true;

        var previousTheme = _currentTheme;
        var previousThemeInfo = _currentThemeInfo;

        // Resolve the actual theme if auto
        var actualTheme = theme == ThemeMode.Auto ? DetectSystemTheme() : theme;
        var themeInfo = _availableThemes.FirstOrDefault(t => t.Mode == actualTheme);
        
        if (themeInfo == null)
            return false;

        _currentTheme = theme;
        _currentThemeInfo = themeInfo;

        ApplyTheme(_currentThemeInfo);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTheme)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkTheme)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLightTheme)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAutoTheme)));

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(previousTheme, _currentTheme, previousThemeInfo, _currentThemeInfo));

        return true;
    }

    /// <inheritdoc />
    public bool SetTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName))
            return false;

        if (Enum.TryParse<ThemeMode>(themeName, true, out var themeMode))
        {
            return SetTheme(themeMode);
        }

        var themeInfo = _availableThemes.FirstOrDefault(t => 
            t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase) ||
            t.DisplayName.Equals(themeName, StringComparison.OrdinalIgnoreCase));

        return themeInfo != null && SetTheme(themeInfo.Mode);
    }

    /// <inheritdoc />
    public void ToggleTheme()
    {
        var newTheme = _currentTheme switch
        {
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.Light,
            ThemeMode.Auto => IsDarkTheme ? ThemeMode.Light : ThemeMode.Dark,
            _ => ThemeMode.Dark
        };

        SetTheme(newTheme);
    }

    /// <inheritdoc />
    public ThemeInfo GetCurrentThemeInfo()
    {
        return _currentThemeInfo;
    }

    /// <inheritdoc />
    public string? GetThemeColor(string colorKey)
    {
        if (string.IsNullOrEmpty(colorKey))
            return null;

        // Check custom colors first
        if (_customColors.TryGetValue(colorKey, out var customColor))
            return customColor;

        // Check theme colors
        if (_currentThemeInfo.Colors.TryGetValue(colorKey, out var themeColor))
            return themeColor;

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetThemeColors()
    {
        var colors = new Dictionary<string, string>(_currentThemeInfo.Colors);
        
        // Override with custom colors
        foreach (var customColor in _customColors)
        {
            colors[customColor.Key] = customColor.Value;
        }

        return colors;
    }

    /// <inheritdoc />
    public void ApplyCustomColors(Dictionary<string, string> customColors)
    {
        if (customColors == null)
            throw new ArgumentNullException(nameof(customColors));

        _customColors.Clear();
        foreach (var color in customColors)
        {
            _customColors[color.Key] = color.Value;
        }

        ApplyTheme(_currentThemeInfo);
    }

    /// <inheritdoc />
    public void ResetToDefaultColors()
    {
        _customColors.Clear();
        ApplyTheme(_currentThemeInfo);
    }

    /// <inheritdoc />
    public async Task SaveThemeConfigurationAsync()
    {
        try
        {
            var config = new ThemeConfiguration
            {
                CurrentTheme = _currentTheme,
                CustomColors = new Dictionary<string, string>(_customColors)
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var configPath = GetConfigFilePath();
            await File.WriteAllTextAsync(configPath, json).ConfigureAwait(false);
        }
        catch
        {
            // Silently ignore save errors
        }
    }

    /// <inheritdoc />
    public async Task LoadThemeConfigurationAsync()
    {
        try
        {
            var configPath = GetConfigFilePath();
            if (!File.Exists(configPath))
                return;

            var json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            var config = JsonSerializer.Deserialize<ThemeConfiguration>(json);

            if (config != null)
            {
                if (config.CustomColors != null)
                {
                    _customColors.Clear();
                    foreach (var color in config.CustomColors)
                    {
                        _customColors[color.Key] = color.Value;
                    }
                }

                SetTheme(config.CurrentTheme);
            }
        }
        catch
        {
            // Silently ignore load errors and use defaults
        }
    }

    /// <inheritdoc />
    public ThemeMode DetectSystemTheme()
    {
        try
        {
            // Try to detect system theme preference
            // This is a simplified implementation - in a real app you might use platform-specific APIs
            var app = Application.Current;
            if (app?.ActualThemeVariant == ThemeVariant.Dark)
                return ThemeMode.Dark;
            else
                return ThemeMode.Light;
        }
        catch
        {
            // Default to dark theme if detection fails
            return ThemeMode.Dark;
        }
    }

    private void ApplyTheme(ThemeInfo themeInfo)
    {
        try
        {
            var app = Application.Current;
            if (app != null)
            {
                app.RequestedThemeVariant = themeInfo.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }
        catch
        {
            // Silently ignore theme application errors
        }
    }

    private static List<ThemeInfo> CreateAvailableThemes()
    {
        return new List<ThemeInfo>
        {
            new ThemeInfo(ThemeMode.Light, "Light", "Light", CreateLightThemeColors()),
            new ThemeInfo(ThemeMode.Dark, "Dark", "Dark", CreateDarkThemeColors())
        };
    }

    private static Dictionary<string, string> CreateLightThemeColors()
    {
        return new Dictionary<string, string>
        {
            // VSCode Light Theme Colors
            ["Background"] = "#FFFFFF",
            ["Foreground"] = "#000000",
            ["AccentColor"] = "#0078D4",
            ["BorderColor"] = "#E1E1E1",
            
            // Activity Bar
            ["ActivityBarBackground"] = "#F3F3F3",
            ["ActivityBarForeground"] = "#000000",
            ["ActivityBarBorder"] = "#E1E1E1",
            ["ActivityBarActiveBackground"] = "#FFFFFF",
            ["ActivityBarActiveForeground"] = "#0078D4",
            
            // Sidebar
            ["SidebarBackground"] = "#F8F8F8",
            ["SidebarForeground"] = "#000000",
            ["SidebarBorder"] = "#E1E1E1",
            
            // Editor
            ["EditorBackground"] = "#FFFFFF",
            ["EditorForeground"] = "#000000",
            ["EditorLineHighlight"] = "#F0F0F0",
            ["EditorSelection"] = "#ADD6FF",
            
            // Status Bar
            ["StatusBarBackground"] = "#0078D4",
            ["StatusBarForeground"] = "#FFFFFF",
            
            // Menu Bar
            ["MenuBarBackground"] = "#F3F3F3",
            ["MenuBarForeground"] = "#000000",
            ["MenuBarHover"] = "#E1E1E1",
            
            // Bottom Panel
            ["BottomPanelBackground"] = "#F8F8F8",
            ["BottomPanelForeground"] = "#000000",
            ["BottomPanelBorder"] = "#E1E1E1",
            
            // Log Viewer Colors
            ["LogTrace"] = "#008000",
            ["LogDebug"] = "#0000FF",
            ["LogInformation"] = "#000000",
            ["LogWarning"] = "#795E26",
            ["LogError"] = "#A31515",
            ["LogCritical"] = "#FFFFFF",
            ["LogCriticalBackground"] = "#A31515"
        };
    }

    private static Dictionary<string, string> CreateDarkThemeColors()
    {
        return new Dictionary<string, string>
        {
            // VSCode Dark Theme Colors
            ["Background"] = "#1E1E1E",
            ["Foreground"] = "#D4D4D4",
            ["AccentColor"] = "#0078D4",
            ["BorderColor"] = "#3C3C3C",
            
            // Activity Bar
            ["ActivityBarBackground"] = "#333333",
            ["ActivityBarForeground"] = "#CCCCCC",
            ["ActivityBarBorder"] = "#3C3C3C",
            ["ActivityBarActiveBackground"] = "#1E1E1E",
            ["ActivityBarActiveForeground"] = "#FFFFFF",
            
            // Sidebar
            ["SidebarBackground"] = "#252526",
            ["SidebarForeground"] = "#CCCCCC",
            ["SidebarBorder"] = "#3C3C3C",
            
            // Editor
            ["EditorBackground"] = "#1E1E1E",
            ["EditorForeground"] = "#D4D4D4",
            ["EditorLineHighlight"] = "#2A2D2E",
            ["EditorSelection"] = "#264F78",
            
            // Status Bar
            ["StatusBarBackground"] = "#0078D4",
            ["StatusBarForeground"] = "#FFFFFF",
            
            // Menu Bar
            ["MenuBarBackground"] = "#3C3C3C",
            ["MenuBarForeground"] = "#CCCCCC",
            ["MenuBarHover"] = "#505050",
            
            // Bottom Panel
            ["BottomPanelBackground"] = "#252526",
            ["BottomPanelForeground"] = "#CCCCCC",
            ["BottomPanelBorder"] = "#3C3C3C",
            
            // Log Viewer Colors
            ["LogTrace"] = "#6A9955",
            ["LogDebug"] = "#569CD6",
            ["LogInformation"] = "#D4D4D4",
            ["LogWarning"] = "#DCDCAA",
            ["LogError"] = "#F44747",
            ["LogCritical"] = "#FFFFFF",
            ["LogCriticalBackground"] = "#F44747"
        };
    }

    private static string GetConfigFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "S7Tools");
        
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        return Path.Combine(appFolder, ThemeConfigFileName);
    }

    private sealed class ThemeConfiguration
    {
        public ThemeMode CurrentTheme { get; set; }
        public Dictionary<string, string>? CustomColors { get; set; }
    }
}