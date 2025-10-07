using Microsoft.Extensions.Logging;

namespace S7Tools.Core.Validation
{
    /// <summary>
    /// Provee una instancia de NullLogger para uso en validadores cuando no se requiere logging real.
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();
        private NullLogger() { }
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        private class NullScope : IDisposable { public static readonly NullScope Instance = new NullScope(); public void Dispose() { } }
    }
}
