using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Sinks;

/// <summary>
/// A log sink that writes log entries to a file.
/// </summary>
public class FileLogSink : IFileLogSink, IDisposable
{
    private readonly CombinedFileLoggerConfiguration _configuration;
    private readonly IPathService _pathService;
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogSink"/> class.
    /// </summary>
    /// <param name="options">Configuration options for file logging.</param>
    /// <param name="pathService">Service for path resolution.</param>
    public FileLogSink(IOptions<CombinedFileLoggerConfiguration> options, IPathService pathService)
    {
        _configuration = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _processTask = Task.Run(ProcessQueueAsync);
    }

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        if (entry.LogLevel < _configuration.LogLevel)
        {
            return;
        }

        _logQueue.Enqueue(entry);
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (_logQueue.TryDequeue(out var entry))
                {
                    await WriteToFileAsync(entry);
                }
                else
                {
                    await Task.Delay(100, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Ignore write errors to avoid crashing app
            }
        }
    }

    private async Task WriteToFileAsync(LogEntry entry)
    {
        try
        {
            // Determine file path based on category (Main, Process, Protocol, etc.)
            var relativePath = _configuration.GetFilePathForCategory(entry.Category);
            var fullPath = _pathService.ResolvePath(relativePath);

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Format: [Timestamp] [Level] [Category] Message
            var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.LogLevel}] [{entry.Category}] {entry.Message}";
            if (entry.Exception != null)
            {
                line += Environment.NewLine + entry.Exception;
            }

            await File.AppendAllTextAsync(fullPath, line + Environment.NewLine);
        }
        catch
        {
            // fallback
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the sink.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources; otherwise false.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _disposed = true;
    }
}
