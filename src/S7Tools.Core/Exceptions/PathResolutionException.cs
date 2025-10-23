namespace S7Tools.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when path resolution fails
    /// </summary>
    public sealed class PathResolutionException : Exception
    {
        /// <summary>
        /// The path that failed to resolve
        /// </summary>
        public string? FailedPath { get; }

        /// <summary>
        /// The operation that was being performed when the path resolution failed
        /// </summary>
        public string? Operation { get; }

        /// <summary>
        /// Initializes a new instance of the PathResolutionException class
        /// </summary>
        public PathResolutionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PathResolutionException class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public PathResolutionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PathResolutionException class with a specified error message and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public PathResolutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PathResolutionException class with detailed context
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="failedPath">The path that failed to resolve</param>
        /// <param name="operation">The operation being performed</param>
        public PathResolutionException(string message, string failedPath, string operation) : base(message)
        {
            FailedPath = failedPath;
            Operation = operation;
        }

        /// <summary>
        /// Initializes a new instance of the PathResolutionException class with detailed context and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="failedPath">The path that failed to resolve</param>
        /// <param name="operation">The operation being performed</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public PathResolutionException(string message, string failedPath, string operation, Exception innerException)
            : base(message, innerException)
        {
            FailedPath = failedPath;
            Operation = operation;
        }
    }
}
