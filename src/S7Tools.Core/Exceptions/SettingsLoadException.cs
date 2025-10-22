namespace S7Tools.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when settings loading fails
    /// </summary>
    public sealed class SettingsLoadException : Exception
    {
        /// <summary>
        /// The settings file path that failed to load
        /// </summary>
        public string? SettingsFilePath { get; }

        /// <summary>
        /// The type of error that occurred (Parse, FileAccess, Validation, etc.)
        /// </summary>
        public string? ErrorType { get; }

        /// <summary>
        /// Initializes a new instance of the SettingsLoadException class
        /// </summary>
        public SettingsLoadException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SettingsLoadException class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public SettingsLoadException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SettingsLoadException class with a specified error message and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SettingsLoadException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SettingsLoadException class with detailed context
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="settingsFilePath">The settings file that failed to load</param>
        /// <param name="errorType">The type of error that occurred</param>
        public SettingsLoadException(string message, string settingsFilePath, string errorType) : base(message)
        {
            SettingsFilePath = settingsFilePath;
            ErrorType = errorType;
        }

        /// <summary>
        /// Initializes a new instance of the SettingsLoadException class with detailed context and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="settingsFilePath">The settings file that failed to load</param>
        /// <param name="errorType">The type of error that occurred</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SettingsLoadException(string message, string settingsFilePath, string errorType, Exception innerException)
            : base(message, innerException)
        {
            SettingsFilePath = settingsFilePath;
            ErrorType = errorType;
        }
    }
}
