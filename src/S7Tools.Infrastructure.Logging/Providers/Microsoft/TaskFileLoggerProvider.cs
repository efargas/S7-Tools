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
    private readonly BlockingCollection<string> _mainLogMessages = new();
    private readonly BlockingCollection<string> _processLogMessages = new();
    private readonly BlockingCollection<string> _protocolLogMessages = new();
    private readonly Task _mainProcessTask;
    private readonly Task _processProcessTask;
    private readonly Task _protocolProcessTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskFileLoggerProvider"/> class.
    /// </summary>
    /// <param name="config">The configuration for the file logger.</param>
    public TaskFileLoggerProvider(IOptions<TaskFileLoggerConfiguration> config)
    {
        _config = config;
        _mainProcessTask = Task.Run(() => ProcessLogQueue(_mainLogMessages, _config.Value.MainLogFilePath));
        _processProcessTask = Task.Run(() => ProcessLogQueue(_processLogMessages, _config.Value.ProcessLogFilePath));
        _protocolProcessTask = Task.Run(() => ProcessLogQueue(_protocolLogMessages, _config.Value.ProtocolLogFilePath));
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
        Task.WaitAll(_mainProcessTask, _processProcessTask, _protocolProcessTask);
        _mainLogMessages.Dispose();
        _processLogMessages.Dispose();
        _protocolLogMessages.Dispose();
    }
}
