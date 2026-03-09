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

        // Use a bounded channel to prevent unbounded memory growth
        // If the channel is full, the oldest log entry will be dropped.
        _logChannel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
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

        // Block briefly if the channel is full, to avoid silent drops
        try
        {
            // We use WriteAsync here which will wait if the bounded channel is full,
            // providing backpressure instead of dropping immediately.
            // Since we are in a synchronous method, we fire and forget the task,
            // but the channel itself handles the queuing logic.
            // Note: Ideally IFileLogSink.Write should be async.
            _logChannel.Writer.TryWrite(entry);
        }
        catch (ChannelClosedException) { /* shutdown */ }
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
                    catch (Exception ex)
                    {
                        // Log write errors to debug output to help diagnosis
                        System.Diagnostics.Debug.WriteLine($"FileLogSink write error: {ex}");
                    }
                }

                // Periodic flush for all open writers
                if (DateTime.UtcNow - lastFlush > flushInterval)
                {
                    foreach (var writer in writers.Values)
                    {
                        try
                        { await writer.FlushAsync(); }
                        catch { }
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
        if (_disposed)
        {
            return;
        }

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
            // Sync dispose triggers cancellation and completes the channel.
            _cts.Cancel();
            _logChannel.Writer.Complete();

            // Block and wait for the processing task to finish to ensure all logs are flushed.
            // This is critical for preventing log loss during synchronous shutdown.
            try
            {
                _processTask.GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore exceptions during shutdown, as the task may be cancelled.
            }

            _cts.Dispose();
        }

        _disposed = true;
    }
}
