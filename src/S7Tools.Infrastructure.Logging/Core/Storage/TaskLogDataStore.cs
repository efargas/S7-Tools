using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using S7Tools.Infrastructure.Logging.Core.Models;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// A dedicated, in-memory circular buffer for task logs.
/// </summary>
public sealed class TaskLogDataStore : ILogDataStore
{
    private readonly ConcurrentQueue<LogModel> _logEntries = new();
    private readonly int _maxEntries;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskLogDataStore"/> class.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to store.</param>
    public TaskLogDataStore(int maxEntries)
    {
        _maxEntries = maxEntries;
    }

    /// <inheritdoc />
    public IEnumerable<LogModel> Entries => _logEntries;

    /// <inheritdoc />
    public int Count => _logEntries.Count;

    /// <inheritdoc />
    public int MaxEntries => _maxEntries;

    /// <inheritdoc />
    public bool IsFull => _logEntries.Count >= _maxEntries;

    /// <inheritdoc />
    public void AddEntry(LogModel logEntry)
    {
        _logEntries.Enqueue(logEntry);
        while (_logEntries.Count > _maxEntries)
        {
            if (!_logEntries.TryDequeue(out _))
            {
                break; // Another thread might have dequeued, so we stop.
            }
        }
        // A Reset is the safest way to notify UI of a circular buffer change,
        // as it forces a full refresh, preventing data inconsistencies.
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc />
    public void AddEntries(IEnumerable<LogModel> logEntries)
    {
        foreach (var logEntry in logEntries)
        {
            AddEntry(logEntry);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _logEntries.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Entries)));
    }

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public IEnumerable<LogModel> GetFilteredEntries(Func<LogModel, bool> filter) => Entries.Where(filter);

    /// <inheritdoc />
    public IEnumerable<LogModel> GetEntriesInTimeRange(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return Entries.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
    }

    /// <inheritdoc />
    public Task<string> ExportAsync(string format = "txt")
    {
        var sb = new System.Text.StringBuilder();
        foreach (var entry in Entries)
        {
            sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Category}: {entry.Message}");
        }
        return Task.FromResult(sb.ToString());
    }
}
