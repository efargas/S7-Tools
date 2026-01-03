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
        // Ensure the expected directory ends with a separator
        var root = Path.GetFullPath(expectedDirectory)
                      .TrimEnd(Path.DirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
        var relative = Path.GetRelativePath(root, fullPath);
        // If it starts with ".." then it's outside the directory
        return !relative.StartsWith("..", StringComparison.Ordinal);
    }
}
