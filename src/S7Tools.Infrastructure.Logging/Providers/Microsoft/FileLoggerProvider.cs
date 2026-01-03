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

    private async Task ProcessLogQueue()
    {
        var messages = new List<string>();
        while (!_logMessages.IsCompleted)
        {
            messages.Clear();
            try
            {
                // Block until a message is available
                messages.Add(_logMessages.Take());
                // Add any other messages that are immediately available
                while (_logMessages.TryTake(out var message))
                {
                    messages.Add(message);
                }

                if (messages.Any())
                {
                    await File.AppendAllLinesAsync(_config.Value.FilePath, messages);
                }
            }
            catch (InvalidOperationException)
            {
                // The collection was completed while waiting, which is expected.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
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
