using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// Represents a single log entry.
/// </summary>
/// <param name="Timestamp">The time when the log occurred.</param>
/// <param name="LogLevel">The severity level of the log.</param>
/// <param name="Category">The category or source of the log.</param>
/// <param name="Message">The log message.</param>
/// <param name="Exception">Optional exception associated with the log.</param>
public record LogEntry(DateTime Timestamp, LogLevel LogLevel, string Category, string Message, Exception? Exception = null);

[JsonSerializable(typeof(LogEntry))]
internal partial class LogJsonContext : JsonSerializerContext
{
}
