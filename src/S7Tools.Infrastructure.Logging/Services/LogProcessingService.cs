using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

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
        if (!IsPathSafe(filePath, logsDirectory))
        {
            Console.WriteLine($"Error: Log file path '{filePath}' is not in the expected directory '{logsDirectory}'.");
            return;
        }

        var logMessages = new System.Collections.Generic.List<string>();
        while (!messages.IsCompleted)
        {
            logMessages.Clear();
            try
            {
                logMessages.Add(messages.Take());
                while (messages.TryTake(out var message))
                {
                    logMessages.Add(message);
                }

                if (logMessages.Count > 0)
                {
                    await File.AppendAllLinesAsync(filePath, logMessages);
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
