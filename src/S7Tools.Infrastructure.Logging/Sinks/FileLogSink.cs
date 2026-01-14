using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    // Batch processing settings
    private const int MaxBatchSize = 100;
    private const int FlushIntervalMs = 500;

    // Cache for resolved paths to avoid resolving on every write
    // Using a limited cache to prevent memory leaks from dynamic categories (e.g. Task.{Guid})
    // In practice, file paths are determined by the log configuration, not the dynamic category parts usually.
    // However, if configuration maps specific categories to files, we need to be careful.
    // _configuration.GetFilePathForCategory usually maps predefined categories.
    // We will use a bounded cache with LRU-like behavior if needed, or just rely on the fact that
    // GetFilePathForCategory collapses categories.
    //
    // Let's inspect `GetFilePathForCategory`. It likely maps "Task.*" to a specific file.
    // If so, the returned relativePath is stable. We should key off the *result* of GetFilePathForCategory?
    // No, we key off the category string because that's the input.
    // To prevent leaks, we will only cache the first 50 unique categories encountered.
    // Most apps have a finite set of high-volume categories. Dynamic ones usually map to a default or wildcard.
    private readonly ConcurrentDictionary<string, string> _pathCache = new();
    private const int MaxCacheSize = 50;

    // Track directories we've already ensured exist
    private readonly ConcurrentDictionary<string, bool> _ensuredDirectories = new();

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
        var batch = new List<LogEntry>(MaxBatchSize);
        var flushTimer = Task.Delay(FlushIntervalMs, _cts.Token);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                // Drain the queue up to MaxBatchSize
                while (batch.Count < MaxBatchSize && _logQueue.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }

                if (batch.Count > 0)
                {
                    await WriteBatchAsync(batch);
                    batch.Clear();

                    // Reset timer since we just flushed
                    flushTimer = Task.Delay(FlushIntervalMs, _cts.Token);
                }
                else
                {
                    // No logs, wait for timer or cancellation
                    await flushTimer;
                    flushTimer = Task.Delay(FlushIntervalMs, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Ignore processing errors to keep loop alive
            }
        }

        // Flush remaining logs on exit
        if (!_logQueue.IsEmpty)
        {
            try
            {
                batch.Clear();
                while (_logQueue.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }

                if (batch.Count > 0)
                {
                    await WriteBatchAsync(batch);
                }
            }
            catch
            {
                // Best effort
            }
        }
    }

    private async Task WriteBatchAsync(List<LogEntry> entries)
    {
        // Group by destination file path to minimize file opens
        var groups = entries
            .GroupBy(e => GetFullPath(e.Category))
            .ToList();

        foreach (var group in groups)
        {
            string fullPath = group.Key;
            if (string.IsNullOrEmpty(fullPath)) continue;

            try
            {
                EnsureDirectoryExists(fullPath);

                var sb = new StringBuilder();
                foreach (var entry in group)
                {
                    sb.Append('[').Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] [")
                      .Append(entry.LogLevel).Append("] [")
                      .Append(entry.Category).Append("] ")
                      .Append(entry.Message);

                    if (entry.Exception != null)
                    {
                        sb.AppendLine();
                        sb.Append(entry.Exception);
                    }
                    sb.AppendLine();
                }

                await File.AppendAllTextAsync(fullPath, sb.ToString(), _cts.Token);
            }
            catch
            {
                // Fallback or ignore
            }
        }
    }

    private string GetFullPath(string category)
    {
        // Use TryGetValue to avoid closure allocation in GetOrAdd if key exists
        if (_pathCache.TryGetValue(category, out var path))
        {
            return path;
        }

        // Bounded cache: if full, resolve directly without caching to prevent leak
        if (_pathCache.Count >= MaxCacheSize)
        {
             var relativePath = _configuration.GetFilePathForCategory(category);
             return _pathService.ResolvePath(relativePath);
        }

        // Add to cache
        return _pathCache.GetOrAdd(category, cat =>
        {
            var relativePath = _configuration.GetFilePathForCategory(cat);
            return _pathService.ResolvePath(relativePath);
        });
    }

    private void EnsureDirectoryExists(string fullPath)
    {
        string? dir = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(dir)) return;

        if (!_ensuredDirectories.ContainsKey(dir))
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            // Use TryAdd to be safe, value doesn't strictly matter
            _ensuredDirectories.TryAdd(dir, true);
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
            try
            {
                _processTask.Wait(1000);
            }
            catch
            {
                // Ignore task wait errors
            }

            _cts.Dispose();
        }

        _disposed = true;
    }
}
