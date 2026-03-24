using Serilog.Context;
using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Services.Logging;

/// <summary>
/// Serilog-based implementation of ITaskLogScope. Use LogContext.PushProperty to
/// flow task metadata (TaskId, TaskName, LogScope) through the execution context.
/// Ensures downstream Serilog sinks receive these properties transparently.
/// </summary>
public class SerilogTaskLogScope : ITaskLogScope
{
    /// <inheritdoc />
    public IDisposable BeginScope(Guid taskId, string taskName, string logScope = "Main")
    {
        var properties = new[]
        {
            LogContext.PushProperty("TaskId", taskId),
            LogContext.PushProperty("TaskName", taskName),
            LogContext.PushProperty("LogScope", logScope)
        };

        return new CompositeDisposable(properties);
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] _disposables;

        public CompositeDisposable(IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            // Dispose in reverse order so the context is unwound correctly
            for (int i = _disposables.Length - 1; i >= 0; i--)
            {
                _disposables[i]?.Dispose();
            }
        }
    }
}
