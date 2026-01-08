namespace S7Tools.Infrastructure.Logging.Core.Models;

/// <summary>
/// Configuration options for the LogDataStore.
/// </summary>
public class LogDataStoreOptions
{
    /// <summary>
    /// Gets or sets the maximum number of log entries to store.
    /// Defaults to 1000.
    /// </summary>
    public int MaxEntries { get; set; } = 1000;
}
