using System.Text.Json.Serialization;

namespace S7Tools.Models;

/// <summary>
/// Application-wide settings.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Gets or sets the logging settings.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the theme settings.
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// Gets or sets the language/culture setting.
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets whether the sidebar is visible by default.
    /// </summary>
    public bool SidebarVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the default sidebar width.
    /// </summary>
    public double SidebarWidth { get; set; } = 300;

    /// <summary>
    /// Gets or sets the default bottom panel height.
    /// </summary>
    public double BottomPanelHeight { get; set; } = 200;

    /// <summary>
    /// Gets or sets whether the bottom panel is visible by default.
    /// </summary>
    public bool BottomPanelVisible { get; set; } = true;

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    /// <returns>A new ApplicationSettings instance with the same values.</returns>
    public ApplicationSettings Clone()
    {
        return new ApplicationSettings
        {
            Logging = Logging.Clone(),
            Theme = Theme,
            Language = Language,
            SidebarVisible = SidebarVisible,
            SidebarWidth = SidebarWidth,
            BottomPanelHeight = BottomPanelHeight,
            BottomPanelVisible = BottomPanelVisible
        };
    }
}