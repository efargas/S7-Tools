using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Logging;

namespace S7Tools.Services;

/// <summary>
/// Implementation of structured logger with enhanced logging capabilities.
/// </summary>
public class StructuredLogger : IStructuredLogger
{
    private readonly ILogger _baseLogger;
    private readonly string _categoryName;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuredLogger"/> class.
    /// </summary>
    /// <param name="baseLogger">The base logger to wrap.</param>
    /// <param name="categoryName">The category name for the logger.</param>
    public StructuredLogger(ILogger baseLogger, string categoryName)
    {
        _baseLogger = baseLogger ?? throw new ArgumentNullException(nameof(baseLogger));
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _baseLogger.BeginScope(state);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return _baseLogger.IsEnabled(logLevel);
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _baseLogger.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <inheritdoc/>
    public void LogStructured(LogLevel logLevel, string message, IDictionary<string, object> properties)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var enrichedProperties = new Dictionary<string, object>(properties)
        {
            ["Category"] = _categoryName,
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["LogType"] = "Structured"
        };

        using IDisposable? scope = BeginScope(enrichedProperties);
        _baseLogger.Log(logLevel, "{Message}", message);
    }

    /// <inheritdoc/>
    public void LogStructured(LogLevel logLevel, Exception exception, string message, IDictionary<string, object> properties)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var enrichedProperties = new Dictionary<string, object>(properties)
        {
            ["Category"] = _categoryName,
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["LogType"] = "Structured",
            ["ExceptionType"] = exception.GetType().Name,
            ["ExceptionMessage"] = exception.Message
        };

        using IDisposable? scope = BeginScope(enrichedProperties);
        _baseLogger.Log(logLevel, exception, "{Message}", message);
    }

    /// <inheritdoc/>
    public IDisposable LogOperation(string operationName, IDictionary<string, object>? properties = null)
    {
        return new OperationContext(this, operationName, properties ?? new Dictionary<string, object>());
    }

    /// <inheritdoc/>
    public void LogMetric(string metricName, double value, string unit, IDictionary<string, object>? properties = null)
    {
        if (!IsEnabled(LogLevel.Information))
        {
            return;
        }

        var metricProperties = new Dictionary<string, object>
        {
            ["MetricName"] = metricName,
            ["MetricValue"] = value,
            ["MetricUnit"] = unit,
            ["Category"] = _categoryName,
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["LogType"] = "Metric"
        };

        if (properties != null)
        {
            foreach (KeyValuePair<string, object> kvp in properties)
            {
                metricProperties[kvp.Key] = kvp.Value;
            }
        }

        using IDisposable? scope = BeginScope(metricProperties);
        _baseLogger.LogInformation("Metric: {MetricName} = {MetricValue} {MetricUnit}", metricName, value, unit);
    }

    /// <inheritdoc/>
    public void LogEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        if (!IsEnabled(LogLevel.Information))
        {
            return;
        }

        var eventProperties = new Dictionary<string, object>
        {
            ["EventName"] = eventName,
            ["Category"] = _categoryName,
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["LogType"] = "Event"
        };

        if (properties != null)
        {
            foreach (KeyValuePair<string, object> kvp in properties)
            {
                eventProperties[kvp.Key] = kvp.Value;
            }
        }

        using IDisposable? scope = BeginScope(eventProperties);
        _baseLogger.LogInformation("Event: {EventName}", eventName);
    }

    /// <inheritdoc/>
    public void LogError(Exception exception, string context, IDictionary<string, object>? properties = null)
    {
        if (!IsEnabled(LogLevel.Error))
        {
            return;
        }

        var errorProperties = new Dictionary<string, object>
        {
            ["Context"] = context,
            ["ExceptionType"] = exception.GetType().Name,
            ["ExceptionMessage"] = exception.Message,
            ["StackTrace"] = exception.StackTrace ?? string.Empty,
            ["Category"] = _categoryName,
            ["Timestamp"] = DateTimeOffset.UtcNow,
            ["LogType"] = "Error"
        };

        if (properties != null)
        {
            foreach (KeyValuePair<string, object> kvp in properties)
            {
                errorProperties[kvp.Key] = kvp.Value;
            }
        }

        using IDisposable? scope = BeginScope(errorProperties);
        _baseLogger.LogError(exception, "Error in {Context}: {ExceptionMessage}", context, exception.Message);
    }
}

/// <summary>
/// Implementation of operation context for tracking operation duration and outcome.
/// </summary>
internal class OperationContext : IOperationContext
{
    private readonly IStructuredLogger _logger;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;
    private bool _completed;
    private string? _result;
    private string? _error;
    private Exception? _exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationContext"/> class.
    /// </summary>
    /// <param name="logger">The structured logger.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="properties">Initial properties for the operation.</param>
    public OperationContext(IStructuredLogger logger, string operationName, IDictionary<string, object> properties)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        Properties = new Dictionary<string, object>(properties);
        StartTime = DateTimeOffset.UtcNow;
        _stopwatch = Stopwatch.StartNew();

        // Log operation start
        var startProperties = new Dictionary<string, object>(Properties)
        {
            ["OperationName"] = OperationName,
            ["OperationId"] = Guid.NewGuid().ToString(),
            ["StartTime"] = StartTime,
            ["LogType"] = "OperationStart"
        };

        _logger.LogStructured(LogLevel.Information, "Operation started: {OperationName}", startProperties);
    }

    /// <inheritdoc/>
    public string OperationName { get; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    public IDictionary<string, object> Properties { get; set; }

    /// <inheritdoc/>
    public void SetSuccess(object? result = null)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _result = result?.ToString();
    }

    /// <inheritdoc/>
    public void SetFailure(string error, Exception? exception = null)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _error = error;
        _exception = exception;
    }

    /// <inheritdoc/>
    public void AddProperty(string key, object value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        Properties[key] = value;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopwatch.Stop();

        // Log operation completion
        var endProperties = new Dictionary<string, object>(Properties)
        {
            ["OperationName"] = OperationName,
            ["StartTime"] = StartTime,
            ["EndTime"] = DateTimeOffset.UtcNow,
            ["Duration"] = _stopwatch.Elapsed.TotalMilliseconds,
            ["DurationUnit"] = "milliseconds",
            ["Success"] = _error == null,
            ["LogType"] = "OperationEnd"
        };

        if (_result != null)
        {
            endProperties["Result"] = _result;
        }

        if (_error != null)
        {
            endProperties["Error"] = _error;
        }

        if (_exception != null)
        {
            endProperties["ExceptionType"] = _exception.GetType().Name;
            endProperties["ExceptionMessage"] = _exception.Message;
        }

        LogLevel logLevel = _error == null ? LogLevel.Information : LogLevel.Error;
        var message = _error == null
            ? "Operation completed successfully: {OperationName} in {Duration}ms"
            : "Operation failed: {OperationName} in {Duration}ms - {Error}";

        if (_exception != null)
        {
            _logger.LogStructured(logLevel, _exception, message, endProperties);
        }
        else
        {
            _logger.LogStructured(logLevel, message, endProperties);
        }
    }
}

/// <summary>
/// Factory for creating structured loggers.
/// </summary>
public class StructuredLoggerFactory : IStructuredLoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuredLoggerFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The base logger factory.</param>
    public StructuredLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc/>
    public IStructuredLogger CreateLogger(string categoryName)
    {
        ILogger baseLogger = _loggerFactory.CreateLogger(categoryName);
        return new StructuredLogger(baseLogger, categoryName);
    }

    /// <inheritdoc/>
    public IStructuredLogger CreateLogger<T>()
    {
        return CreateLogger(typeof(T).FullName ?? typeof(T).Name);
    }
}
