using System.Linq;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

public class CombinedFileLoggerConfiguration : IFileLogConfiguration
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public string DefaultLogPath { get; set; } = "s7tools.log";
    public string TaskMainLogPath { get; set; } = "task-main.log";
    public string TaskProcessLogPath { get; set; } = "task-process.log";
    public string TaskProtocolLogPath { get; set; } = "task-protocol.log";

    public string GetFilePathForCategory(string category)
    {
        if (!string.IsNullOrEmpty(category) && category.StartsWith("Task", StringComparison.OrdinalIgnoreCase))
        {
            var parts = category.Split('.');
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
