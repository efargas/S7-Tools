using S7Tools.Infrastructure.Logging.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// Thread-safe circular buffer implementation for storing log entries with real-time notifications.
/// </summary>
public sealed class LogDataStore : ILogDataStore
{
    private readonly object _lock = new();
    private readonly LogModel[] _buffer;
    private readonly LogDataStoreOptions _options;
    private int _head;
    private int _count;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the LogDataStore class.
    /// </summary>
    /// <param name="options">Configuration options for the data store.</param>
    public LogDataStore(LogDataStoreOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _buffer = new LogModel[_options.MaxEntries];
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc />
    public IReadOnlyList<LogModel> Entries
    {
        get
        {
            lock (_lock)
            {
                if (_count == 0)
                    return Array.Empty<LogModel>();

                var result = new LogModel[_count];
                for (int i = 0; i < _count; i++)
                {
                    var index = (_head - _count + i + _buffer.Length) % _buffer.Length;
                    result[i] = _buffer[index];
                }
                return result;
            }
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    /// <inheritdoc />
    public int MaxEntries => _options.MaxEntries;

    /// <inheritdoc />
    public bool IsFull
    {
        get
        {
            lock (_lock)
            {
                return _count == _buffer.Length;
            }
        }
    }

    /// <inheritdoc />
    public void AddEntry(LogModel logEntry)
    {
        if (logEntry == null)
            throw new ArgumentNullException(nameof(logEntry));

        if (_disposed)
            return;

        LogModel? removedEntry = null;
        bool wasAdded = false;

        lock (_lock)
        {
            // Store the entry that will be overwritten if buffer is full
            if (_count == _buffer.Length)
            {
                var oldIndex = _head;
                removedEntry = _buffer[oldIndex];
            }

            // Add the new entry
            _buffer[_head] = logEntry;
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
            {
                _count++;
            }

            wasAdded = true;
        }

        if (wasAdded)
        {
            // Notify outside of lock to prevent deadlocks
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(IsFull));
            OnPropertyChanged(nameof(Entries));

            if (removedEntry != null)
            {
                // Item was replaced
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, logEntry, removedEntry, _count - 1));
            }
            else
            {
                // Item was added
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, logEntry, _count - 1));
            }
        }
    }

    /// <inheritdoc />
    public void AddEntries(IEnumerable<LogModel> logEntries)
    {
        if (logEntries == null)
            throw new ArgumentNullException(nameof(logEntries));

        if (_disposed)
            return;

        var entries = logEntries.ToList();
        if (entries.Count == 0)
            return;

        lock (_lock)
        {
            foreach (var entry in entries)
            {
                if (entry != null)
                {
                    _buffer[_head] = entry;
                    _head = (_head + 1) % _buffer.Length;

                    if (_count < _buffer.Length)
                    {
                        _count++;
                    }
                }
            }
        }

        // Notify outside of lock
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(IsFull));
        OnPropertyChanged(nameof(Entries));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _count = 0;
        }

        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(IsFull));
        OnPropertyChanged(nameof(Entries));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc />
    public IEnumerable<LogModel> GetFilteredEntries(Func<LogModel, bool> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        lock (_lock)
        {
            var entries = Entries;
            return entries.Where(filter).ToList();
        }
    }

    /// <inheritdoc />
    public IEnumerable<LogModel> GetEntriesInTimeRange(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            var entries = Entries;
            return entries.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToList();
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportAsync(string format = "txt")
    {
        var entries = Entries;
        
        return format.ToLowerInvariant() switch
        {
            "json" => await ExportAsJsonAsync(entries).ConfigureAwait(false),
            "csv" => await ExportAsCsvAsync(entries).ConfigureAwait(false),
            "txt" or _ => await ExportAsTextAsync(entries).ConfigureAwait(false)
        };
    }

    private static async Task<string> ExportAsJsonAsync(IReadOnlyList<LogModel> entries)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var exportData = entries.Select(e => new
        {
            e.Id,
            e.Timestamp,
            Level = e.Level.ToString(),
            e.Category,
            e.Message,
            Exception = e.Exception?.ToString(),
            EventId = e.EventId.Id,
            EventName = e.EventId.Name,
            e.Scope,
            e.Properties
        });

        return await Task.Run(() => JsonSerializer.Serialize(exportData, options)).ConfigureAwait(false);
    }

    private static async Task<string> ExportAsCsvAsync(IReadOnlyList<LogModel> entries)
    {
        return await Task.Run(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Level,Category,Message,Exception,EventId,EventName,Scope");

            foreach (var entry in entries)
            {
                sb.AppendLine($"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\"," +
                             $"\"{entry.Level}\"," +
                             $"\"{EscapeCsv(entry.Category)}\"," +
                             $"\"{EscapeCsv(entry.Message)}\"," +
                             $"\"{EscapeCsv(entry.Exception?.ToString() ?? "")}\"," +
                             $"\"{entry.EventId.Id}\"," +
                             $"\"{EscapeCsv(entry.EventId.Name ?? "")}\"," +
                             $"\"{EscapeCsv(entry.Scope ?? "")}\"");
            }

            return sb.ToString();
        }).ConfigureAwait(false);
    }

    private static async Task<string> ExportAsTextAsync(IReadOnlyList<LogModel> entries)
    {
        return await Task.Run(() =>
        {
            var sb = new StringBuilder();
            
            foreach (var entry in entries)
            {
                sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Category}: {entry.Message}");
                
                if (entry.Exception != null)
                {
                    sb.AppendLine($"Exception: {entry.Exception}");
                }
                
                if (!string.IsNullOrEmpty(entry.Scope))
                {
                    sb.AppendLine($"Scope: {entry.Scope}");
                }
                
                sb.AppendLine();
            }

            return sb.ToString();
        }).ConfigureAwait(false);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\"", "\"\"");
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            Array.Clear(_buffer, 0, _buffer.Length);
            _disposed = true;
        }

        PropertyChanged = null;
        CollectionChanged = null;
    }
}