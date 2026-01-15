using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// A dedicated, in-memory circular buffer for task logs with thread-safe access.
/// </summary>
public class TaskLogDataStore : ITaskLogDataStore
{
    private readonly ObservableCollection<LogModel> _logs = new();
    private readonly object _lock = new();
    private readonly int _maxEntries;
    private readonly Action<Action> _uiDispatch;

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskLogDataStore"/> class.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to store (optional).</param>
    /// <param name="uiDispatch">Optional delegate to dispatch updates to the UI thread. If null, updates run synchronously.</param>
    public TaskLogDataStore(int maxEntries = 1000, Action<Action>? uiDispatch = null)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "Max entries must be greater than zero.");
        }

        _maxEntries = maxEntries;

        // Default: marshal back to the creating thread if possible (typically UI thread).
        var ctx = SynchronizationContext.Current;
        _uiDispatch = uiDispatch ?? (action =>
        {
            if (ctx != null)
            {
                ctx.Post(_ => action(), null);
                return;
            }

            action();
        });

        _logs.CollectionChanged += (s, e) => CollectionChanged?.Invoke(s, e);
    }

    /// <summary>
    /// Adds a new log entry to the store in a thread-safe manner.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    public void AddEntry(LogModel entry)
    {
        // UI updates must happen on the UI thread or synchronized context
        _uiDispatch(() =>
        {
            lock (_lock)
            {
                _logs.Add(entry);

                // Enforce circular buffer limit
                while (_logs.Count > _maxEntries)
                {
                    _logs.RemoveAt(0);
                }
            }
        });
    }

    /// <summary>
    /// Clears all log entries from the store.
    /// </summary>
    public void Clear()
    {
        _uiDispatch(() =>
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        });
    }

    /// <inheritdoc />
    public IEnumerator<LogModel> GetEnumerator()
    {
        // Snapshot for thread-safe enumeration
        List<LogModel> snapshot;
        lock (_lock)
        {
            snapshot = _logs.ToList();
        }
        return snapshot.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
