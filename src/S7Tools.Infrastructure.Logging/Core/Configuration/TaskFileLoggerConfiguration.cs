using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

/// <summary>
/// Configuration for the Task File logger provider.
/// </summary>
public class TaskFileLoggerConfiguration : IFileLogConfiguration
{
    /// <summary>
    /// Gets or sets the minimum log level to capture.
    /// Default is LogLevel.Information.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the file path for the main log file.
    /// </summary>
    public string MainLogFilePath { get; set; } = "task-main.log";

    /// <summary>
    /// Gets or sets the file path for the process log file.
    /// </summary>
    public string ProcessLogFilePath { get; set; } = "task-process.log";

    /// <summary>
    /// Gets or sets the file path for the protocol log file.
    /// </summary>
    public string ProtocolLogFilePath { get; set; } = "task-protocol.log";

    public string GetFilePathForCategory(string category)
    {
        var parts = category.Split('.');
        var lastPart = parts.LastOrDefault();
        return lastPart switch
        {
            "Process" => ProcessLogFilePath,
            "Protocol" => ProtocolLogFilePath,
            _ => MainLogFilePath
        };
    }
}
