using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using S7Tools.Infrastructure.Logging.Models;

namespace S7Tools.Infrastructure.Logging.Services;

/// <summary>
/// Provides a shared service for processing and writing log messages.
/// </summary>
public static class LogProcessingService
{
    /// <summary>
    /// Processes a queue of log messages and writes them to a file.
    /// </summary>
    public static async Task ProcessLogQueue(BlockingCollection<string> messages, string filePath, string logsDirectory)
    {
        var logItems = new BlockingCollection<LogItem>();
        _ = Task.Run(() =>
        {
            foreach (var message in messages.GetConsumingEnumerable())
            {
                logItems.Add(new LogItem(filePath, message));
            }
            logItems.CompleteAdding();
        });
        await ProcessLogQueue(logItems, logsDirectory);
    }

    public static async Task ProcessLogQueue(BlockingCollection<LogItem> messages, string logsDirectory)
    {
        while (!messages.IsCompleted)
        {
            try
            {
                var batch = new System.Collections.Generic.List<LogItem> { messages.Take() };
                while (messages.TryTake(out var message))
                {
                    batch.Add(message);
                }

                var groupedMessages = batch.GroupBy(item => item.FilePath);

                foreach (var group in groupedMessages)
                {
                    var filePath = group.Key;
                    if (IsPathSafe(filePath, logsDirectory))
                    {
                        await File.AppendAllLinesAsync(filePath, group.Select(item => item.Message));
                    }
                    else
                    {
                        Console.WriteLine($"Error: Log file path '{filePath}' is not in the expected directory '{logsDirectory}'.");
                    }
                }
            }
            catch (InvalidOperationException) { } // Collection completed.
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }

    private static bool IsPathSafe(string filePath, string expectedDirectory)
    {
        var fullPath = Path.GetFullPath(filePath);
        var fullExpectedDirectory = Path.GetFullPath(expectedDirectory);
        return fullPath.StartsWith(fullExpectedDirectory, StringComparison.OrdinalIgnoreCase);
    }
}
