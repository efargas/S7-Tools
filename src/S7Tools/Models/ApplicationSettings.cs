using System;
using System.IO;

namespace S7Tools.Models;

/// <summary>
/// Application-wide settings.
/// </summary>
public class ApplicationSettings
{
    private static string ResourcesPath(string subfolder)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", subfolder);

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

    // Resources folder paths (relative to application build directory)

    /// <summary>
    /// Root resources directory inside the application build folder.
    /// Example: bin/Debug/net8.0/resources
    /// </summary>
    public string ResourcesRoot { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");

    /// <summary>
    /// Default path for payload files.
    /// </summary>
    public string PayloadsPath { get; set; } = ResourcesPath("payloads");

    /// <summary>
    /// Default path for firmware files.
    /// </summary>
    public string FirmwarePath { get; set; } = ResourcesPath("firmware");

    /// <summary>
    /// Default path for extractions.
    /// </summary>
    public string ExtractionsPath { get; set; } = ResourcesPath("extractions");

    /// <summary>
    /// Default path for memory dumps.
    /// </summary>
    public string DumpsPath { get; set; } = ResourcesPath("dumps");

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
            BottomPanelVisible = BottomPanelVisible,
            ResourcesRoot = ResourcesRoot,
            PayloadsPath = PayloadsPath,
            FirmwarePath = FirmwarePath,
            ExtractionsPath = ExtractionsPath,
            DumpsPath = DumpsPath
        };
    }
}