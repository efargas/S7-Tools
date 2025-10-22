using System;
using System.IO;
using S7Tools.Core.Models;

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
    /// Gets or sets the serial port settings.
    /// </summary>
    public SerialPortSettings SerialPorts { get; set; } = new();

    /// <summary>
    /// Gets or sets the socat (Serial-to-TCP Proxy) settings.
    /// </summary>
    public SocatSettings Socat { get; set; } = new();

    /// <summary>
    /// Gets or sets the power supply control settings.
    /// </summary>
    public PowerSupplySettings PowerSupply { get; set; } = new();

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

    // Resources folder paths - populated dynamically by PathService at runtime

    /// <summary>
    /// Root resources directory.
    /// This will be populated by the PathService at runtime to use dynamic paths.
    /// </summary>
    public string ResourcesRoot { get; set; } = string.Empty;

    /// <summary>
    /// Default path for payload files.
    /// This will be populated by the PathService at runtime to use dynamic paths.
    /// </summary>
    public string PayloadsPath { get; set; } = string.Empty;

    /// <summary>
    /// Default path for firmware files.
    /// This will be populated by the PathService at runtime to use dynamic paths.
    /// </summary>
    public string FirmwarePath { get; set; } = string.Empty;

    /// <summary>
    /// Default path for extractions.
    /// This will be populated by the PathService at runtime to use dynamic paths.
    /// </summary>
    public string ExtractionsPath { get; set; } = string.Empty;

    /// <summary>
    /// Default path for memory dumps.
    /// This will be populated by the PathService at runtime to use dynamic paths.
    /// </summary>
    public string DumpsPath { get; set; } = string.Empty;

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    /// <returns>A new ApplicationSettings instance with the same values.</returns>
    public ApplicationSettings Clone()
    {
        return new ApplicationSettings
        {
            Logging = Logging.Clone(),
            SerialPorts = SerialPorts.Clone(),
            Socat = Socat.Clone(),
            PowerSupply = PowerSupply.Clone(),
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
