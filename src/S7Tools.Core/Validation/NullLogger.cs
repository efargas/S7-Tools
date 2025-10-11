using Microsoft.Extensions.Logging;

namespace S7Tools.Core.Validation
{
    /// <summary>
    /// Provides a NullLogger instance for use in validators when real logging is not required.
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        /// <summary>
        /// Gets the singleton instance of the NullLogger.
        /// </summary>
        public static readonly NullLogger Instance = new NullLogger();
        
        private NullLogger() { }
        
        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>A disposable object that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        
        /// <summary>
        /// Checks if the given logLevel is enabled.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>Always returns false for NullLogger.</returns>
        public bool IsEnabled(LogLevel logLevel) => false;
        
        /// <summary>
        /// Writes a log entry. This implementation does nothing.
        /// </summary>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a string message of the state and exception.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        
        private class NullScope : IDisposable { public static readonly NullScope Instance = new NullScope(); public void Dispose() { } }
    }
}
