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
    private readonly BlockingCollection<string> _mainLogMessages = new(10000);
    private readonly BlockingCollection<string> _processLogMessages = new(10000);
    private readonly BlockingCollection<string> _protocolLogMessages = new(10000);
    private readonly Task _mainProcessTask;
    private readonly Task _processProcessTask;
    private readonly Task _protocolProcessTask;
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
        _mainProcessTask = Task.Run(() => LogProcessingService.ProcessLogQueue(_mainLogMessages, _config.Value, _pathService.LogsDirectory, _config.Value.MainLogFilePath));
        _processProcessTask = Task.Run(() => LogProcessingService.ProcessLogQueue(_processLogMessages, _config.Value, _pathService.LogsDirectory, _config.Value.ProcessLogFilePath));
        _protocolProcessTask = Task.Run(() => LogProcessingService.ProcessLogQueue(_protocolLogMessages, _config.Value, _pathService.LogsDirectory, _config.Value.ProtocolLogFilePath));
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new TaskFileLogger(categoryName, this);
    }

    internal void AddLogMessage(string message, string logType)
    {
        var collection = logType switch
        {
            "Main" => _mainLogMessages,
            "Process" => _processLogMessages,
            "Protocol" => _protocolLogMessages,
            _ => null
        };

        if (collection != null && !collection.IsAddingCompleted)
        {
            collection.Add(message);
        }
    }

    private Task ProcessLogQueue(BlockingCollection<string> messages, string filePath)
    {
        return S7Tools.Infrastructure.Logging.Services.LogProcessingService.ProcessLogQueue(messages, filePath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mainLogMessages.CompleteAdding();
        _processLogMessages.CompleteAdding();
        _protocolLogMessages.CompleteAdding();
        try
        {
            if (!Task.WaitAll(new[] { _mainProcessTask, _processProcessTask, _protocolProcessTask }, 5000))
            {
                Console.WriteLine("Log processing tasks did not complete within the timeout period.");
            }
        }
        catch (AggregateException ex)
        {
            ex.Handle(e => e is TaskCanceledException);
        }
        _mainLogMessages.Dispose();
        _processLogMessages.Dispose();
        _protocolLogMessages.Dispose();
    }
}
