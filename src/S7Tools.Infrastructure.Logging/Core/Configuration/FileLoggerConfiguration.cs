using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

/// <summary>
/// Configuration for the File logger provider.
/// </summary>
public class FileLoggerConfiguration : IFileLogConfiguration
{
    /// <summary>
    /// Gets or sets the minimum log level to capture.
    /// Default is LogLevel.Information.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the file path for the log file.
    /// Default is "s7tools.log".
    /// </summary>
    public string FilePath { get; set; } = "s7tools.log";

    public string GetFilePathForCategory(string category) => FilePath;
}
