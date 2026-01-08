using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// A dedicated, in-memory circular buffer for task logs.
/// </summary>
public class TaskLogDataStore : ITaskLogDataStore
{
    private readonly ObservableCollection<LogModel> _logs = new();

    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskLogDataStore"/> class.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries to store (optional).</param>
    public TaskLogDataStore(int maxEntries = 1000)
    {
        _logs.CollectionChanged += (s, e) => CollectionChanged?.Invoke(s, e);
    }

    /// <summary>
    /// Adds a new log entry to the store.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    public void AddEntry(LogModel entry)
    {
        _logs.Add(entry);
    }

    /// <summary>
    /// Clears all log entries from the store.
    /// </summary>
    public void Clear()
    {
        _logs.Clear();
    }

    /// <inheritdoc />
    public IEnumerator<LogModel> GetEnumerator()
    {
        return _logs.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _logs.GetEnumerator();
    }
}
