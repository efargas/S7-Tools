using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Base exception class for all S7Tools domain exceptions.
/// Provides a common base for exception handling and filtering throughout the application.
/// </summary>
public class S7ToolsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="S7ToolsException"/> class.
    /// </summary>
    public S7ToolsException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="S7ToolsException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public S7ToolsException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="S7ToolsException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public S7ToolsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
