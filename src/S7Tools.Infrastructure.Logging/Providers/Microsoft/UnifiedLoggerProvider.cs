using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Sinks;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

[ProviderAlias("Unified")]
public sealed class UnifiedLoggerProvider : ILoggerProvider
{
    private readonly IEnumerable<ILogSink> _sinks;

    public UnifiedLoggerProvider(IEnumerable<ILogSink> sinks)
    {
        _sinks = sinks;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new UnifiedLogger(categoryName, _sinks);
    }

    public void Dispose()
    {
        foreach (var sink in _sinks)
        {
            if (sink is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
