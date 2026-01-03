using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// A provider for creating <see cref="TaskFileLogger"/> instances.
/// </summary>
[ProviderAlias("TaskFile")]
public sealed class TaskFileLoggerProvider : ILoggerProvider
{
    internal readonly IOptions<TaskFileLoggerConfiguration> _config;
    private readonly BlockingCollection<(string LogType, string Message)> _logQueue = new(10000);
    private readonly Task _processingTask;
    private readonly S7Tools.Core.Interfaces.Services.IPathService _pathService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskFileLoggerProvider"/> class.
    /// </summary>
    /// <param name="config">The configuration for the file logger.</param>
    /// <param name="pathService">The path service.</param>
    public TaskFileLoggerProvider(IOptions<TaskFileLoggerConfiguration> config, S7Tools.Core.Interfaces.Services.IPathService pathService)
    {
        _config = config;
        _pathService = pathService;
        _processingTask = Task.Run(ProcessLogQueue);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new TaskFileLogger(categoryName, this);
    }

    internal void AddLogMessage(string message, string logType)
    {
        if (!_logQueue.IsAddingCompleted)
        {
            _logQueue.Add((logType, message));
        }
    }

    private async Task ProcessLogQueue()
    {
        var writers = new Dictionary<string, StreamWriter>();
        try
        {
            foreach (var (logType, message) in _logQueue.GetConsumingEnumerable())
            {
                try
                {
                    if (!writers.TryGetValue(logType, out var writer))
                    {
                        var filePath = logType switch
                        {
                            "Main" => _config.Value.MainLogFilePath,
                            "Process" => _config.Value.ProcessLogFilePath,
                            "Protocol" => _config.Value.ProtocolLogFilePath,
                            _ => null
                        };

                        if (filePath != null)
                        {
                            var fullPath = Path.Combine(_pathService.LogsDirectory, filePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            writer = new StreamWriter(fullPath, append: true, System.Text.Encoding.UTF8, 65536);
                            writers[logType] = writer;
                        }
                    }

                    if (writer != null)
                    {
                        await writer.WriteLineAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }
        }
        finally
        {
            foreach (var writer in writers.Values)
            {
                await writer.FlushAsync();
                writer.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _logQueue.CompleteAdding();
        try
        {
            if (!_processingTask.Wait(5000))
            {
                Console.WriteLine("Log processing task did not complete within the timeout period.");
            }
        }
        catch (AggregateException ex)
        {
            ex.Handle(e => e is TaskCanceledException);
        }
        _logQueue.Dispose();
    }
}
