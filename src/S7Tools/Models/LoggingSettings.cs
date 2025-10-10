using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace S7Tools.Models;

/// <summary>
/// Settings for logging configuration.
/// </summary>
public class LoggingSettings
{
    private static string ResourcesPath(string subfolder)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", subfolder);

    /// <summary>
    /// Gets or sets the default path for log files (under application resources/logs).
    /// </summary>
    public string DefaultLogPath { get; set; } = ResourcesPath("logs");

    /// <summary>
    /// Gets or sets the export path for log files (under application resources/exports).
    /// </summary>
    public string ExportPath { get; set; } = ResourcesPath("exports");

    /// <summary>
    /// Gets or sets the minimum log level to display.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether auto-scroll is enabled.
    /// </summary>
    public bool AutoScroll { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of log entries to keep in memory.
    /// </summary>
    public int MaxLogEntries { get; set; } = 10000;

    /// <summary>
    /// Gets or sets whether to enable file logging.
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the log file name pattern.
    /// </summary>
    public string LogFileNamePattern { get; set; } = "MainLog_{timestamp}.log";

    /// <summary>
    /// Gets or sets the maximum log file size in MB before rolling.
    /// </summary>
    public int MaxLogFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of log files to retain.
    /// </summary>
    public int RetainedLogFiles { get; set; } = 5;

    /// <summary>
    /// Gets or sets the color settings for different log levels.
    /// </summary>
    public Dictionary<LogLevel, string> LogLevelColors { get; set; } = new()
    {
        { LogLevel.Trace, "#808080" },      // Gray
        { LogLevel.Debug, "#007ACC" },      // Blue
        { LogLevel.Information, "#00AA00" }, // Green
        { LogLevel.Warning, "#FFA500" },    // Orange
        { LogLevel.Error, "#DC143C" },      // Crimson
        { LogLevel.Critical, "#8B0000" }    // Dark Red
    };

    /// <summary>
    /// Gets or sets whether to show timestamps in the log viewer.
    /// </summary>
    public bool ShowTimestamp { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show categories in the log viewer.
    /// </summary>
    public bool ShowCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show log levels in the log viewer.
    /// </summary>
    public bool ShowLevel { get; set; } = true;

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    /// <returns>A new LoggingSettings instance with the same values.</returns>
    public LoggingSettings Clone()
    {
        return new LoggingSettings
        {
            DefaultLogPath = DefaultLogPath,
            ExportPath = ExportPath,
            MinimumLogLevel = MinimumLogLevel,
            AutoScroll = AutoScroll,
            MaxLogEntries = MaxLogEntries,
            EnableFileLogging = EnableFileLogging,
            LogFileNamePattern = LogFileNamePattern,
            MaxLogFileSizeMB = MaxLogFileSizeMB,
            RetainedLogFiles = RetainedLogFiles,
            LogLevelColors = new Dictionary<LogLevel, string>(LogLevelColors),
            ShowTimestamp = ShowTimestamp,
            ShowCategory = ShowCategory,
            ShowLevel = ShowLevel
        };
    }
}
