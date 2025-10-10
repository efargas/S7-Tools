using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace S7Tools.Core.Logging;

/// <summary>
/// Defines a structured logger with enhanced logging capabilities.
/// </summary>
public interface IStructuredLogger : ILogger
{
    /// <summary>
    /// Logs a structured message with properties.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="message">The message template.</param>
    /// <param name="properties">The properties to include in the log entry.</param>
    void LogStructured(LogLevel logLevel, string message, IDictionary<string, object> properties);

    /// <summary>
    /// Logs a structured message with properties and exception.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template.</param>
    /// <param name="properties">The properties to include in the log entry.</param>
    void LogStructured(LogLevel logLevel, Exception exception, string message, IDictionary<string, object> properties);

    /// <summary>
    /// Logs an operation start event.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="properties">Additional properties for the operation.</param>
    /// <returns>A disposable operation context that logs completion when disposed.</returns>
    IDisposable LogOperation(string operationName, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a performance metric.
    /// </summary>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="properties">Additional properties for the metric.</param>
    void LogMetric(string metricName, double value, string unit, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a business event.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="properties">Properties associated with the event.</param>
    void LogEvent(string eventName, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs an error with context information.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="context">The context in which the error occurred.</param>
    /// <param name="properties">Additional properties for the error.</param>
    void LogError(Exception exception, string context, IDictionary<string, object>? properties = null);
}

/// <summary>
/// Defines a structured logger factory.
/// </summary>
public interface IStructuredLoggerFactory
{
    /// <summary>
    /// Creates a structured logger for the specified category.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A structured logger instance.</returns>
    IStructuredLogger CreateLogger(string categoryName);

    /// <summary>
    /// Creates a structured logger for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create the logger for.</typeparam>
    /// <returns>A structured logger instance.</returns>
    IStructuredLogger CreateLogger<T>();
}

/// <summary>
/// Represents an operation context for logging operation duration and outcome.
/// </summary>
public interface IOperationContext : IDisposable
{
    /// <summary>
    /// Gets the operation name.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Gets the operation start time.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets or sets additional properties for the operation.
    /// </summary>
    IDictionary<string, object> Properties { get; set; }

    /// <summary>
    /// Marks the operation as successful.
    /// </summary>
    /// <param name="result">The operation result.</param>
    void SetSuccess(object? result = null);

    /// <summary>
    /// Marks the operation as failed.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    void SetFailure(string error, Exception? exception = null);

    /// <summary>
    /// Adds a property to the operation context.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    void AddProperty(string key, object value);
}
