using System.Linq;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

/// <summary>
/// Configuration for the combined file logger, specifying paths for different log categories.
/// </summary>
public class CombinedFileLoggerConfiguration : IFileLogConfiguration
{
    /// <inheritdoc />
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Default path for logs that do not match specific categories.
    /// </summary>
    public string DefaultLogPath { get; set; } = "Logs/app.log";

    /// <summary>
    /// Path for main task logs.
    /// </summary>
    public string TaskMainLogPath { get; set; } = "Logs/Main/main.log";

    /// <summary>
    /// Path for process-related logs.
    /// </summary>
    public string TaskProcessLogPath { get; set; } = "Logs/Process/process.log";

    /// <summary>
    /// Path for protocol communication logs.
    /// </summary>
    public string TaskProtocolLogPath { get; set; } = "Logs/Protocol/protocol.log";

    /// <inheritdoc />
    public string GetFilePathForCategory(string categoryName)
    {
        var parts = categoryName?.Split('.') ?? Array.Empty<string>();
        if (parts.Length > 0 && parts[0].Equals("Task", StringComparison.OrdinalIgnoreCase))
        {
            var lastPart = parts.LastOrDefault();
            return lastPart?.Equals("Process", System.StringComparison.OrdinalIgnoreCase) == true
                ? TaskProcessLogPath
                : lastPart?.Equals("Protocol", System.StringComparison.OrdinalIgnoreCase) == true
                    ? TaskProtocolLogPath
                    : TaskMainLogPath;
        }
        return DefaultLogPath;
    }
}
