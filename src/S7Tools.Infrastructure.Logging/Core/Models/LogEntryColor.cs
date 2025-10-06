using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Defines color configuration for different log levels.
/// </summary>
public sealed class LogEntryColor
{
    /// <summary>
    /// Gets or sets the foreground color for the log entry.
    /// </summary>
    public string Foreground { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the background color for the log entry.
    /// </summary>
    public string Background { get; set; } = "Transparent";

    /// <summary>
    /// Gets or sets the border color for the log entry.
    /// </summary>
    public string Border { get; set; } = "Transparent";

    /// <summary>
    /// Gets the default color configuration for different log levels.
    /// </summary>
    public static Dictionary<LogLevel, LogEntryColor> DefaultColors => new()
    {
        [LogLevel.Trace] = new() { Foreground = "#808080", Background = "Transparent" },
        [LogLevel.Debug] = new() { Foreground = "#A0A0A0", Background = "Transparent" },
        [LogLevel.Information] = new() { Foreground = "#FFFFFF", Background = "Transparent" },
        [LogLevel.Warning] = new() { Foreground = "#FFA500", Background = "Transparent" },
        [LogLevel.Error] = new() { Foreground = "#FF4444", Background = "Transparent" },
        [LogLevel.Critical] = new() { Foreground = "#FFFFFF", Background = "#FF0000" },
        [LogLevel.None] = new() { Foreground = "#FFFFFF", Background = "Transparent" }
    };

    /// <summary>
    /// Gets the VSCode dark theme color configuration for different log levels.
    /// </summary>
    public static Dictionary<LogLevel, LogEntryColor> VSCodeDarkColors => new()
    {
        [LogLevel.Trace] = new() { Foreground = "#6A9955", Background = "Transparent" },
        [LogLevel.Debug] = new() { Foreground = "#569CD6", Background = "Transparent" },
        [LogLevel.Information] = new() { Foreground = "#D4D4D4", Background = "Transparent" },
        [LogLevel.Warning] = new() { Foreground = "#DCDCAA", Background = "Transparent" },
        [LogLevel.Error] = new() { Foreground = "#F44747", Background = "Transparent" },
        [LogLevel.Critical] = new() { Foreground = "#FFFFFF", Background = "#F44747" },
        [LogLevel.None] = new() { Foreground = "#D4D4D4", Background = "Transparent" }
    };

    /// <summary>
    /// Gets the VSCode light theme color configuration for different log levels.
    /// </summary>
    public static Dictionary<LogLevel, LogEntryColor> VSCodeLightColors => new()
    {
        [LogLevel.Trace] = new() { Foreground = "#008000", Background = "Transparent" },
        [LogLevel.Debug] = new() { Foreground = "#0000FF", Background = "Transparent" },
        [LogLevel.Information] = new() { Foreground = "#000000", Background = "Transparent" },
        [LogLevel.Warning] = new() { Foreground = "#795E26", Background = "Transparent" },
        [LogLevel.Error] = new() { Foreground = "#A31515", Background = "Transparent" },
        [LogLevel.Critical] = new() { Foreground = "#FFFFFF", Background = "#A31515" },
        [LogLevel.None] = new() { Foreground = "#000000", Background = "Transparent" }
    };
}