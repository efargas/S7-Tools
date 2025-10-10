using System.Text;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Models;
using S7Tools.Infrastructure.Logging.Core.Storage;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// Logger implementation that stores log entries in a data store for real-time viewing.
/// </summary>
public sealed class DataStoreLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ILogDataStore _dataStore;
    private readonly DataStoreLoggerConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the DataStoreLogger class.
    /// </summary>
    /// <param name="categoryName">The category name for this logger.</param>
    /// <param name="dataStore">The data store to write log entries to.</param>
    /// <param name="configuration">The logger configuration.</param>
    public DataStoreLogger(string categoryName, ILogDataStore dataStore, DataStoreLoggerConfiguration configuration)
    {
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _configuration.IncludeScopes ? new LoggerScope<TState>(state) : null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return _configuration.IsEnabled(logLevel) && _configuration.MatchesCategory(_categoryName);
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (_configuration.EventId.HasValue && _configuration.EventId.Value != eventId.Id)
        {
            return;
        }

        var message = _configuration.FormatMessages && formatter != null
            ? formatter(state, exception)
            : state?.ToString() ?? string.Empty;

        if (_configuration.MaxMessageLength.HasValue && message.Length > _configuration.MaxMessageLength.Value)
        {
            message = message.Substring(0, _configuration.MaxMessageLength.Value) + "... [truncated]";
        }

        var logEntry = new LogModel
        {
            Timestamp = DateTime.Now,
            Level = logLevel,
            Category = _categoryName,
            Message = message,
            Exception = _configuration.CaptureStackTrace ? exception : null,
            EventId = eventId,
            Scope = GetCurrentScope(),
            Properties = _configuration.CaptureProperties ? ExtractProperties(state) : new Dictionary<string, object?>()
        };

        try
        {
            _dataStore.AddEntry(logEntry);
        }
        catch
        {
            // Silently ignore errors when adding to data store to prevent logging loops
        }
    }

    private string? GetCurrentScope()
    {
        if (!_configuration.IncludeScopes)
        {
            return null;
        }

        var scope = LoggerScope<object>.Current;
        if (scope == null)
        {
            return null;
        }

        var sb = new StringBuilder();
        var currentScope = scope;

        while (currentScope != null)
        {
            if (sb.Length > 0)
            {
                sb.Insert(0, " => ");
            }

            sb.Insert(0, currentScope.State?.ToString() ?? "null");
            currentScope = currentScope.Parent as LoggerScope<object>;
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private Dictionary<string, object?> ExtractProperties<TState>(TState state)
    {
        var properties = new Dictionary<string, object?>();

        if (state is IEnumerable<KeyValuePair<string, object?>> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs)
            {
                if (!string.IsNullOrEmpty(kvp.Key) && kvp.Key != "{OriginalFormat}")
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }
        }
        else if (state != null)
        {
            properties["State"] = state;
        }

        return properties;
    }

    /// <summary>
    /// Represents a logging scope that can be nested.
    /// </summary>
    /// <typeparam name="TState">The type of the scope state.</typeparam>
    private sealed class LoggerScope<TState> : IDisposable where TState : notnull
    {
        private static readonly AsyncLocal<LoggerScope<object>?> _current = new();
        private readonly LoggerScope<object>? _parent;
        private bool _disposed;

        public LoggerScope(TState state)
        {
            State = state;
            _parent = Current;
            Current = this as LoggerScope<object>;
        }

        public TState State { get; }

        public LoggerScope<object>? Parent => _parent;

        public static LoggerScope<object>? Current
        {
            get => _current.Value;
            private set => _current.Value = value;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Current = _parent;
                _disposed = true;
            }
        }
    }
}
