using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Sinks;

public class FileLogSink : IFileLogSink, IDisposable
{
    private readonly CombinedFileLoggerConfiguration _config;
    private readonly S7Tools.Core.Interfaces.Services.IPathService _pathService;
    private readonly BlockingCollection<(string, string)> _logQueue = new(10000);
    private readonly Task _processingTask;

    public FileLogSink(CombinedFileLoggerConfiguration config, S7Tools.Core.Interfaces.Services.IPathService pathService)
    {
        _config = config;
        _pathService = pathService;
        _processingTask = Task.Run(ProcessLogQueue);
    }

    public void Write(LogEntry logEntry)
    {
        var message = System.Text.Json.JsonSerializer.Serialize(logEntry, LogJsonContext.Default.LogEntry);
        var filePath = _config.GetFilePathForCategory(logEntry.Category);
        var fullPath = Path.Combine(_pathService.LogsDirectory, filePath);
        _logQueue.TryAdd((fullPath, message));
    }

    private async Task ProcessLogQueue()
    {
        Directory.CreateDirectory(_pathService.LogsDirectory);
        while (!_logQueue.IsCompleted)
        {
            try
            {
                var batch = new System.Collections.Generic.List<(string, string)> { _logQueue.Take() };
                while (_logQueue.TryTake(out var item))
                {
                    batch.Add(item);
                }

                var groupedMessages = batch.GroupBy(item => item.Item1);

                foreach (var group in groupedMessages)
                {
                    var filePath = group.Key;
                    if (IsPathSafe(filePath, _pathService.LogsDirectory))
                    {
                        await File.AppendAllLinesAsync(filePath, group.Select(item => item.Item2));
                    }
                    else
                    {
                        Console.WriteLine($"Error: Log file path '{filePath}' is not in the expected directory '{_pathService.LogsDirectory}'.");
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

        // Ensure the expected directory path ends with a directory separator to avoid partial name matches.
        if (!fullExpectedDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            fullExpectedDirectory += Path.DirectorySeparatorChar;
        }

        // Check if the file's full path starts with the full directory path.
        return fullPath.StartsWith(fullExpectedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _logQueue.CompleteAdding();
        try
        {
            // Wait up to 5 seconds for the processing task to finish draining
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException) { /* swallow to avoid throwing from Dispose */ }
        _logQueue.Dispose();
    }
}
