using S7Tools.Infrastructure.Logging.Core.Models;
using System.Collections.Specialized;
using System.ComponentModel;

namespace S7Tools.Infrastructure.Logging.Core.Storage;

/// <summary>
/// Interface for storing and retrieving log entries with real-time notifications.
/// </summary>
public interface ILogDataStore : INotifyPropertyChanged, INotifyCollectionChanged, IDisposable
{
    /// <summary>
    /// Gets all log entries currently stored.
    /// </summary>
    IReadOnlyList<LogModel> Entries { get; }

    /// <summary>
    /// Gets the current count of stored log entries.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the maximum number of entries that can be stored.
    /// </summary>
    int MaxEntries { get; }

    /// <summary>
    /// Gets a value indicating whether the store has reached its maximum capacity.
    /// </summary>
    bool IsFull { get; }

    /// <summary>
    /// Adds a new log entry to the store.
    /// </summary>
    /// <param name="logEntry">The log entry to add.</param>
    void AddEntry(LogModel logEntry);

    /// <summary>
    /// Adds multiple log entries to the store in a batch operation.
    /// </summary>
    /// <param name="logEntries">The log entries to add.</param>
    void AddEntries(IEnumerable<LogModel> logEntries);

    /// <summary>
    /// Clears all log entries from the store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets log entries that match the specified filter criteria.
    /// </summary>
    /// <param name="filter">The filter function to apply.</param>
    /// <returns>Filtered log entries.</returns>
    IEnumerable<LogModel> GetFilteredEntries(Func<LogModel, bool> filter);

    /// <summary>
    /// Gets log entries within the specified time range.
    /// </summary>
    /// <param name="startTime">The start time for filtering.</param>
    /// <param name="endTime">The end time for filtering.</param>
    /// <returns>Log entries within the time range.</returns>
    IEnumerable<LogModel> GetEntriesInTimeRange(DateTime startTime, DateTime endTime);

    /// <summary>
    /// Exports all log entries to a string format.
    /// </summary>
    /// <param name="format">The export format (e.g., "json", "csv", "txt").</param>
    /// <returns>Formatted log entries as a string.</returns>
    Task<string> ExportAsync(string format = "txt");
}