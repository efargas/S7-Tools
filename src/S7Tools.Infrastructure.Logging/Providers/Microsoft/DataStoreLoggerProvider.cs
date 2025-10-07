using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Storage;
using System.Collections.Concurrent;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// Logger provider that creates DataStore loggers for capturing log entries in memory.
/// </summary>
[ProviderAlias("DataStore")]
public sealed class DataStoreLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ILogDataStore _dataStore;
    private readonly DataStoreLoggerConfiguration _configuration;
    private readonly ConcurrentDictionary<string, DataStoreLogger> _loggers = new();
    private IExternalScopeProvider? _scopeProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the DataStoreLoggerProvider class.
    /// </summary>
    /// <param name="dataStore">The data store to write log entries to.</param>
    /// <param name="configuration">The logger configuration.</param>
    public DataStoreLoggerProvider(ILogDataStore dataStore, DataStoreLoggerConfiguration? configuration = null)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _configuration = configuration ?? new DataStoreLoggerConfiguration();
    }

    /// <summary>
    /// Initializes a new instance of the DataStoreLoggerProvider class with options.
    /// </summary>
    /// <param name="dataStore">The data store to write log entries to.</param>
    /// <param name="options">The logger configuration options.</param>
    public DataStoreLoggerProvider(ILogDataStore dataStore, IOptionsMonitor<DataStoreLoggerConfiguration> options)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _configuration = options?.CurrentValue ?? new DataStoreLoggerConfiguration();
        
        // Monitor configuration changes
        options?.OnChange(config =>
        {
            // Update configuration for existing loggers
            foreach (var logger in _loggers.Values)
            {
                // Note: In a real implementation, you might want to recreate loggers
                // or provide a way to update their configuration
            }
        });
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DataStoreLoggerProvider));

        return _loggers.GetOrAdd(categoryName, name => new DataStoreLogger(name, _dataStore, _configuration));
    }

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Gets the data store used by this provider.
    /// </summary>
    public ILogDataStore DataStore => _dataStore;

    /// <summary>
    /// Gets the current configuration for this provider.
    /// </summary>
    public DataStoreLoggerConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets all active loggers created by this provider.
    /// </summary>
    public IReadOnlyDictionary<string, DataStoreLogger> Loggers => _loggers;

    /// <summary>
    /// Updates the configuration for this provider and all its loggers.
    /// </summary>
    /// <param name="configuration">The new configuration to apply.</param>
    public void UpdateConfiguration(DataStoreLoggerConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (_disposed)
            return;

        // Update the configuration
        _configuration.LogLevel = configuration.LogLevel;
        _configuration.EventId = configuration.EventId;
        _configuration.IncludeScopes = configuration.IncludeScopes;
        _configuration.CaptureProperties = configuration.CaptureProperties;
        _configuration.CategoryFilter = configuration.CategoryFilter;
        _configuration.FormatMessages = configuration.FormatMessages;
        _configuration.MaxMessageLength = configuration.MaxMessageLength;
        _configuration.CaptureStackTrace = configuration.CaptureStackTrace;

        // Note: Existing loggers will use the updated configuration on their next log call
    }

    /// <summary>
    /// Clears all cached loggers and forces recreation on next use.
    /// </summary>
    public void ClearLoggers()
    {
        if (_disposed)
            return;

        _loggers.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _loggers.Clear();
        _scopeProvider = null;
        _disposed = true;
    }
}