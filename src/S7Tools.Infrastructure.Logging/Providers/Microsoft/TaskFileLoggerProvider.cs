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
    private readonly BlockingCollection<S7Tools.Infrastructure.Logging.Models.LogItem> _logQueue = new(10000);
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
        _processingTask = Task.Run(() => S7Tools.Infrastructure.Logging.Services.LogProcessingService.ProcessLogQueue(_logQueue, _pathService.LogsDirectory));
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
                _logQueue.TryAdd(new S7Tools.Infrastructure.Logging.Models.LogItem(fullPath, message));
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
