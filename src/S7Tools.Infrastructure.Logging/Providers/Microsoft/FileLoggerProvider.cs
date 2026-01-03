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
    private readonly BlockingCollection<string> _logMessages = new BlockingCollection<string>(new ConcurrentQueue<string>());
    private readonly Task _processTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
    /// </summary>
    /// <param name="config">The configuration for the file logger.</param>
    public FileLoggerProvider(IOptions<FileLoggerConfiguration> config)
    {
        _config = config;
        _processTask = Task.Run(ProcessLogQueue);
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
            _processTask.Wait();
        }
        catch (TaskCanceledException)
        {
        }
        _logMessages.Dispose();
    }
}
