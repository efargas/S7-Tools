namespace S7Tools.Infrastructure.Logging.Core.Configuration;

/// <summary>
/// Configuration options for the TaskLogDataStore.
/// </summary>
public class TaskLogDataStoreOptions
{
    /// <summary>
    /// Gets or sets the maximum number of log entries to store in the buffer.
    /// </summary>
    public int MaxEntries { get; set; } = 1000;
}
