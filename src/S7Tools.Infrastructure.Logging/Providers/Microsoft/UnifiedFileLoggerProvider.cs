using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Models;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

[ProviderAlias("UnifiedFile")]
public sealed class UnifiedFileLoggerProvider : ILoggerProvider
{
    internal readonly IFileLogConfiguration _config;
    private readonly BlockingCollection<LogItem> _logQueue = new(10000);
    private readonly Task _processingTask;
    private readonly S7Tools.Core.Interfaces.Services.IPathService _pathService;

    public UnifiedFileLoggerProvider(IOptions<IFileLogConfiguration> config, S7Tools.Core.Interfaces.Services.IPathService pathService)
    {
        _config = config.Value;
        _pathService = pathService;
        _processingTask = Task.Run(() => S7Tools.Infrastructure.Logging.Services.LogProcessingService.ProcessLogQueue(_logQueue, _pathService.LogsDirectory));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    internal void AddLogMessage(string message, string categoryName)
    {
        if (!_logQueue.IsAddingCompleted)
        {
            var filePath = _config.GetFilePathForCategory(categoryName);
            var fullPath = Path.Combine(_pathService.LogsDirectory, filePath);
            if (!_logQueue.TryAdd(new LogItem(fullPath, message)))
            {
                Console.WriteLine("Warning: File logger queue is full, log message discarded.");
            }
        }
    }

    public void Dispose()
    {
        _logQueue.CompleteAdding();
        try
        {
            // Wait for the processing task to drain remaining items
            _processingTask.Wait();
        }
        catch (AggregateException) { /* swallow to avoid throwing from Dispose */ }
        _logQueue.Dispose();
    }
}
