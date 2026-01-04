using S7Tools.Infrastructure.Logging.Providers.Microsoft;

namespace S7Tools.Infrastructure.Logging.Sinks;

public interface ILogSink
{
    void Write(LogEntry logEntry);
}
