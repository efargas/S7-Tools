using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// A provider for creating <see cref="FileLogger"/> instances.
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider
{
    internal readonly IOptions<FileLoggerConfiguration> _config;
    private readonly BlockingCollection<string> _logMessages = new BlockingCollection<string>(new ConcurrentQueue<string>(), 10000);
    private readonly Task _processTask;
    private readonly S7Tools.Core.Interfaces.Services.IPathService _pathService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
    /// </summary>
    /// <param name="config">The configuration for the file logger.</param>
    /// <param name="pathService">The path service.</param>
    public FileLoggerProvider(IOptions<FileLoggerConfiguration> config, S7Tools.Core.Interfaces.Services.IPathService pathService)
    {
        _config = config;
        _pathService = pathService;
        _processTask = Task.Run(() => LogProcessingService.ProcessLogQueue(
            _logMessages,
            System.IO.Path.Combine(_pathService.LogsDirectory, _config.Value.FilePath),
            _pathService.LogsDirectory));
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    internal void AddLogMessage(string message)
    {
        if (!_logMessages.IsAddingCompleted)
        {
            _logMessages.Add(message);
        }
    }

    private Task ProcessLogQueue()
    {
        return S7Tools.Infrastructure.Logging.Services.LogProcessingService.ProcessLogQueue(_logMessages, _config.Value.FilePath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _logMessages.CompleteAdding();
        try
        {
            // Wait for the processing task to complete, with a timeout.
            if (!_processTask.Wait(5000)) // 5 second timeout
            {
                // Log a warning if the task did not complete in time.
                // This would ideally use a logger, but Console is a fallback.
                Console.WriteLine("Log processing task did not complete within the timeout period.");
            }
        }
        catch (AggregateException ex)
        {
            // It's good practice to log exceptions that occur during disposal.
            ex.Handle(e => e is TaskCanceledException); // Suppress TaskCanceledException, but let others propagate or be logged.
        }
        _logMessages.Dispose();
    }
}
