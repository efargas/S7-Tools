using System;
using System.Linq;
using S7Tools.Core.Models;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Infrastructure.Logging.Sinks;

/// <summary>
/// A log sink that writes log entries to an in-memory centralized service.
/// </summary>
public class InMemoryTaskLogSink : IInMemoryTaskLogSink
{
    private readonly ICentralizedTaskLogService _logService;
    private bool _disposed;

    // Constant for the prefix to avoid string allocations
    private const string TaskCategoryPrefix = "Task.";

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
        // Optimized check using constant
        if (!entry.Category.StartsWith(TaskCategoryPrefix, StringComparison.Ordinal))
        {
            return;
        }

        // Category format: Task.{TaskId}.{SubCategory}
        // e.g. Task.53e1a90c-....Main or Task.53e1a90c-....Process

        ReadOnlySpan<char> categorySpan = entry.Category.AsSpan();

        // Skip prefix "Task."
        int prefixLength = TaskCategoryPrefix.Length;
        if (categorySpan.Length <= prefixLength) return;

        ReadOnlySpan<char> remaining = categorySpan.Slice(prefixLength);

        // Find next dot to isolate TaskId
        int dotIndex = remaining.IndexOf('.');

        // If no dot, it might just be Task.{Guid} which implies Main
        ReadOnlySpan<char> guidSpan = dotIndex == -1 ? remaining : remaining.Slice(0, dotIndex);

        if (Guid.TryParse(guidSpan, out var taskId))
        {
            var (main, process, protocol) = _logService.GetOrCreateStoresForTask(taskId);
            var logModel = new LogModel
            {
                Timestamp = entry.Timestamp,
                Level = entry.LogLevel,
                Category = entry.Category,
                Message = entry.Message
            };

            // Determine subcategory
            if (dotIndex != -1 && dotIndex < remaining.Length - 1)
            {
                ReadOnlySpan<char> subCategory = remaining.Slice(dotIndex + 1);

                if (subCategory.Equals("Process", StringComparison.OrdinalIgnoreCase))
                {
                    process.AddEntry(logModel);
                }
                else if (subCategory.Equals("Protocol", StringComparison.OrdinalIgnoreCase))
                {
                    protocol.AddEntry(logModel);
                }
                else
                {
                    main.AddEntry(logModel);
                }
            }
            else
            {
                // Default to Main if no subcategory specified
                main.AddEntry(logModel);
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
