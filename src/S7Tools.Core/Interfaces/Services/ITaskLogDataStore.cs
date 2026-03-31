using System.Collections.Specialized;
using S7Tools.Core.Models;

namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Defines a data store for task logs, supporting collection change notifications and log entry management.
/// </summary>
public interface ITaskLogDataStore : INotifyCollectionChanged, IEnumerable<LogModel>
{
    /// <summary>
    /// Adds a new log entry to the store.
    /// </summary>
    /// <param name="entry">The log entry to add.</param>
    void AddEntry(LogModel entry);

    /// <summary>
    /// Clears all log entries from the store.
    /// </summary>
    void Clear();
}
