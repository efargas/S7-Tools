using Microsoft.Extensions.Logging;

namespace S7Tools.Core.Validation
{
    /// <summary>
    /// Provee una instancia de NullLogger para uso en validadores cuando no se requiere logging real.
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        /// <summary>
        /// Gets the singleton instance of the NullLogger.
        /// </summary>
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger() { }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => false;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }
}
