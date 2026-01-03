using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

public record TaskLogEntry(
    DateTime Timestamp,
    LogLevel LogLevel,
    string Category,
    string Message,
    string? Exception
);

[JsonSerializable(typeof(TaskLogEntry))]
internal partial class TaskLogJsonContext : JsonSerializerContext
{
}
