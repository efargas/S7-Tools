using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using S7Tools.Infrastructure.Logging.Sinks;

namespace S7Tools.Infrastructure.Logging.Providers.Microsoft;

/// <summary>
/// A logger provider that creates instances of <see cref="UnifiedLogger"/>.
/// </summary>
[ProviderAlias("UnifiedFile")]
public sealed class UnifiedLoggerProvider : ILoggerProvider
{
    private readonly IEnumerable<ILogSink> _sinks;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedLoggerProvider"/> class.
    /// </summary>
    /// <param name="sinks">The collection of log sinks to use.</param>
    public UnifiedLoggerProvider(IEnumerable<ILogSink> sinks)
    {
        _sinks = sinks;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new UnifiedLogger(categoryName, _sinks);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // If sinks need disposing, do it here (if the provider owns them).
        // Usually DI handles sink disposal.
        GC.SuppressFinalize(this);
    }
}
