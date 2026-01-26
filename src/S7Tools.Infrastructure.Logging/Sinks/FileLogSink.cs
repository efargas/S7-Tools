using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Sinks;

/// <summary>
/// A log sink that writes log entries to a file efficiently using a background writer task.
/// </summary>
public class FileLogSink : IFileLogSink, IAsyncDisposable, IDisposable
{
    private readonly CombinedFileLoggerConfiguration _configuration;
    private readonly IPathService _pathService;

    // Use an unbounded channel for non-blocking writes
    private readonly Channel<LogEntry> _logChannel;
    private readonly Task _processTask;
    private readonly CancellationTokenSource _cts = new();
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

        // Create unbounded channel to ensure producers (loggers) are never blocked
        _logChannel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _processTask = Task.Run(ProcessQueueAsync);
    }

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        if (entry.LogLevel < _configuration.LogLevel || _disposed)
        {
            return;
        }

        // Fire and forget write to channel
        _logChannel.Writer.TryWrite(entry);
    }

    private async Task ProcessQueueAsync()
    {
        // Group writers by category to keep file streams open
        // Key: File Path, Value: StreamWriter
        var writers = new System.Collections.Generic.Dictionary<string, StreamWriter>();
        var lastFlush = DateTime.UtcNow;
        var flushInterval = TimeSpan.FromSeconds(1); // Batch writes and flush every 1 second

        try
        {
            while (await _logChannel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_logChannel.Reader.TryRead(out var entry))
                {
                    try
                    {
                        var relativePath = _configuration.GetFilePathForCategory(entry.Category);
                        var fullPath = _pathService.ResolvePath(relativePath);

                        if (!writers.TryGetValue(fullPath, out var writer))
                        {
                            var dir = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            // Keep stream open, append mode, share read access
                            var fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                            writer = new StreamWriter(fs, System.Text.Encoding.UTF8) { AutoFlush = false };
                            writers[fullPath] = writer;
                        }

                        // Format: [Timestamp] [Level] [Category] Message
                        var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.LogLevel}] [{entry.Category}] {entry.Message}";
                        if (entry.Exception != null)
                        {
                            line += Environment.NewLine + entry.Exception;
                        }

                        await writer.WriteLineAsync(line);

                        // Force flush on errors to ensure they are persisted immediately
                        if (entry.LogLevel >= LogLevel.Error)
                        {
                            await writer.FlushAsync();
                        }
                    }
                    catch
                    {
                        // Ignore individual write errors
                    }
                }

                // Periodic flush for all open writers
                if (DateTime.UtcNow - lastFlush > flushInterval)
                {
                    foreach (var writer in writers.Values)
                    {
                        try { await writer.FlushAsync(); } catch { }
                    }
                    lastFlush = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        finally
        {
            // Clean up all writers
            foreach (var writer in writers.Values)
            {
                try
                {
                    await writer.FlushAsync();
                    writer.Dispose();
                }
                catch { }
            }
            writers.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously releases the resources used by the sink.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;
        _cts.Cancel();
        _logChannel.Writer.Complete();

        try
        {
            await _processTask;
        }
        catch
        {
            // Ignore task cancellation errors
        }

        _cts.Dispose();
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
            // Sync dispose triggers cancellation
            _cts.Cancel();

            // We can't await the task here in synchronous Dispose,
            // so we rely on the background task responding to cancellation
            // and cleaning up its own resources (StreamWriters) in the finally block.
            _logChannel.Writer.Complete();

            // Do not dispose _cts here, as the background task might still be using its token.
            // This prevents an ObjectDisposedException. The CancellationTokenSource
            // will be garbage collected. The DisposeAsync path handles this correctly.
        }

        _disposed = true;
    }
}
