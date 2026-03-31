using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Models;
using Serilog.Core;
using Serilog.Events;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// Thread-safe circular buffer implementation for storing log entries with real-time notifications.
/// Now acts as a native Serilog sink for optimal performance.
/// </summary>
public sealed class LogDataStore : ILogDataStore, ITaskLogDataStore, ILogEventSink
{
    /// <summary>
    /// Export format constants.
    /// </summary>
    public static class ExportFormats
    {
        /// <summary>
        /// Plain text export format.
        /// </summary>
        public const string Text = "txt";

        /// <summary>
        /// JSON export format.
        /// </summary>
        public const string Json = "json";

        /// <summary>
        /// CSV export format.
        /// </summary>
        public const string Csv = "csv";
    }

    private readonly object _lock = new();
    private readonly LogModel[] _buffer;
    private readonly LogDataStoreOptions _options;
    private int _head;
    private int _count;
    private bool _disposed;

    // Batching support
    private readonly ConcurrentQueue<LogModel> _logQueue = new();
    private readonly System.Timers.Timer _flushTimer;
    private const int FlushIntervalMs = 100; // 100ms batching interval

    /// <summary>
    /// Initializes a new instance of the LogDataStore class.
    /// </summary>
    /// <param name="options">Configuration options for the data store.</param>
    public LogDataStore(LogDataStoreOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _buffer = new LogModel[_options.MaxEntries];

        // Initialize and start the flush timer
        _flushTimer = new System.Timers.Timer(FlushIntervalMs);
        _flushTimer.Elapsed += (s, e) => FlushQueue();
        _flushTimer.AutoReset = true;
        _flushTimer.Start();
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
                {
                    return [];
                }

                var result = new LogModel[_count];
                for (int i = 0; i < _count; i++)
                {
                    int index = (_head - _count + i + _buffer.Length) % _buffer.Length;
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
        ArgumentNullException.ThrowIfNull(logEntry);

        if (_disposed)
        {
            return;
        }

        // Just queue the entry and let the timer handle the rest
        _logQueue.Enqueue(logEntry);
    }

    /// <summary>
    /// Flushes the queued log entries into the main buffer and notifies the UI in a single batch.
    /// </summary>
    private void FlushQueue()
    {
        if (_disposed || _logQueue.IsEmpty)
        {
            return;
        }

        var batch = new List<LogModel>();
        while (_logQueue.TryDequeue(out LogModel? entry))
        {
            batch.Add(entry);
        }

        if (batch.Count == 0)
        {
            return;
        }

        int startIndex;
        lock (_lock)
        {
            startIndex = _count;
            foreach (LogModel logEntry in batch)
            {
                // Add the new entry to circular buffer
                _buffer[_head] = logEntry;
                _head = (_head + 1) % _buffer.Length;

                if (_count < _buffer.Length)
                {
                    _count++;
                }
            }
        }

        // Notify UI in a single batch outside of lock
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(IsFull));
        OnPropertyChanged(nameof(Entries));

        // Use Add action with the batch for better UI performance
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add, batch, startIndex >= _buffer.Length ? _buffer.Length - 1 : startIndex));
    }

    /// <inheritdoc />
    public void AddEntries(IEnumerable<LogModel> logEntries)
    {
        ArgumentNullException.ThrowIfNull(logEntries);

        if (!_disposed)
        {
            var entries = logEntries.ToList();
            if (entries.Count == 0)
            {
                return;
            }

            var addedEntries = new List<LogModel>();
            int startIndex = _count;

            lock (_lock)
            {
                foreach (LogModel? entry in entries)
                {
                    if (entry != null)
                    {
                        _buffer[_head] = entry;
                        _head = (_head + 1) % _buffer.Length;
                        addedEntries.Add(entry);

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

            // Use Add action for better UI performance instead of Reset
            if (addedEntries.Count > 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, addedEntries, startIndex));
            }
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_disposed)
        {
            return;
        }

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
        ArgumentNullException.ThrowIfNull(filter);

        lock (_lock)
        {
            if (_count == 0)
            {
                return [];
            }

            var result = new List<LogModel>(_count);
            for (int i = 0; i < _count; i++)
            {
                int index = (_head - _count + i + _buffer.Length) % _buffer.Length;
                LogModel entry = _buffer[index];
                if (entry is not null && filter(entry))
                {
                    result.Add(entry);
                }
            }
            return result;
        }
    }

    /// <inheritdoc />
    public IEnumerable<LogModel> GetEntriesInTimeRange(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                return [];
            }

            var result = new List<LogModel>(_count);
            for (int i = 0; i < _count; i++)
            {
                int index = (_head - _count + i + _buffer.Length) % _buffer.Length;
                LogModel entry = _buffer[index];
                if (entry is not null && entry.Timestamp >= startTime && entry.Timestamp <= endTime)
                {
                    result.Add(entry);
                }
            }
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportAsync(string format = ExportFormats.Text)
    {
        IReadOnlyList<LogModel> entries = Entries;

        return format.ToLowerInvariant() switch
        {
            ExportFormats.Json => await ExportAsJsonAsync(entries).ConfigureAwait(false),
            ExportFormats.Csv => await ExportAsCsvAsync(entries).ConfigureAwait(false),
            ExportFormats.Text or _ => await ExportAsTextAsync(entries).ConfigureAwait(false)
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

            foreach (LogModel entry in entries)
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

            foreach (LogModel entry in entries)
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
        {
            return string.Empty;
        }

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

    /// <summary>
    /// Returns an enumerator that iterates through the log entries.
    /// </summary>
    public IEnumerator<LogModel> GetEnumerator()
    {
        return Entries.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the log entries.
    /// </summary>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        LogLevel level = logEvent.Level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.None
        };

        string category = logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? sourceContext)
            ? sourceContext.ToString().Trim('"')
            : string.Empty;

        EventId eventId = logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? eventIdProp) && eventIdProp is StructureValue sv
                    && sv.Properties.FirstOrDefault(p => p.Name == "Id")?.Value is ScalarValue idVal
                    && idVal.Value is int id
            ? new EventId(id)
            : new EventId(0);

        string? scope = logEvent.Properties.TryGetValue("LogScope", out LogEventPropertyValue? scopeProp)
            ? scopeProp.ToString().Trim('"')
            : null;

        var properties = logEvent.Properties.ToDictionary(
            kvp => kvp.Key,
            kvp => (object?)(kvp.Value is ScalarValue scalar ? scalar.Value : kvp.Value.ToString())
        );

        if (logEvent.Properties.TryGetValue("TaskId", out LogEventPropertyValue? taskIdValue) && Guid.TryParse(taskIdValue.ToString().Trim('"'), out Guid parsedTaskId))
        {
            properties["TaskId"] = parsedTaskId;
        }

        var model = new LogModel
        {
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = level,
            Category = category,
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception,
            EventId = eventId,
            Scope = scope,
            Properties = properties
        };

        AddEntry(model);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _flushTimer.Stop();
            _flushTimer.Dispose();

            Array.Clear(_buffer, 0, _buffer.Length);
            _count = 0; // Reset the entry counter
            _head = 0;  // Optional: reset the head pointer
            _disposed = true;

            // Clear events inside lock for thread safety
            PropertyChanged = null;
            CollectionChanged = null;
        }
    }
}
