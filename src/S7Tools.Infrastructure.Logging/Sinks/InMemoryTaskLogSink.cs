using System;
using System.Linq;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Sinks;

/// <summary>
/// A log sink that writes log entries to an in-memory centralized service.
/// </summary>
public class InMemoryTaskLogSink : IInMemoryTaskLogSink
{
    private readonly ICentralizedTaskLogService _logService;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTaskLogSink"/> class.
    /// </summary>
    /// <param name="logService">The centralized task log service.</param>
    public InMemoryTaskLogSink(ICentralizedTaskLogService logService)
    {
        _logService = logService;
    }

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        if (entry.Category.StartsWith("Task"))
        {
            var parts = entry.Category.Split('.');
            if (parts.Length > 1 && Guid.TryParse(parts[1], out var taskId))
            {
                var (main, process, protocol) = _logService.GetOrCreateStoresForTask(taskId);
                var logModel = new LogModel
                {
                    Timestamp = entry.Timestamp,
                    Level = entry.LogLevel,
                    Category = entry.Category,
                    Message = entry.Message
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

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the sink.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources; otherwise false.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // No managed resources to dispose currently.
        }

        _disposed = true;
    }
}
