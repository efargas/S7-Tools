using S7Tools.Core.Models;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Infrastructure.Logging.Sinks;

public class InMemoryTaskLogSink : IInMemoryTaskLogSink
{
    private readonly ICentralizedTaskLogService _centralizedTaskLogService;

    public InMemoryTaskLogSink(ICentralizedTaskLogService centralizedTaskLogService)
    {
        _centralizedTaskLogService = centralizedTaskLogService;
    }

    public void Write(LogEntry logEntry)
    {
        if (logEntry.Category.StartsWith("Task"))
        {
            var parts = logEntry.Category.Split('.');
            if (parts.Length > 1 && Guid.TryParse(parts[1], out var taskId))
            {
                var (main, process, protocol) = _centralizedTaskLogService.GetOrCreateStoresForTask(taskId);
                var logModel = new LogModel
                {
                    Timestamp = logEntry.Timestamp,
                    Level = logEntry.LogLevel,
                    Category = logEntry.Category,
                    Message = logEntry.Message
                };

                var lastPart = parts.Last();
                if (lastPart.Equals("Process", StringComparison.OrdinalIgnoreCase))
                {
                    process.AddEntry(logModel);
                }
                else if (lastPart.Equals("Protocol", StringComparison.OrdinalIgnoreCase))
                {
                    protocol.AddEntry(logModel);
                }
                else
                {
                    main.AddEntry(logModel);
                }
            }
        }
    }
}
