using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

public record LogEntry(
    DateTime Timestamp,
    LogLevel LogLevel,
    string Category,
    string Message,
    string? Exception
);

[JsonSerializable(typeof(LogEntry))]
internal partial class LogJsonContext : JsonSerializerContext
{
}
