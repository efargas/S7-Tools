using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Sinks;

/// <summary>
/// Defines a sink for writing log entries.
/// </summary>
public interface ILogSink : IDisposable
{
    /// <summary>
    /// Writes a log entry to the sink.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    void Write(LogEntry entry);
}
