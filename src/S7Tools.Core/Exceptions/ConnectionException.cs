using System;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when a connection-related error occurs (PLC, serial port, network, etc.).
/// </summary>
public class ConnectionException : S7ToolsException
{
    /// <summary>
    /// Gets the connection target (e.g., IP address, serial port name, etc.).
    /// </summary>
    public string? ConnectionTarget { get; init; }

    /// <summary>
    /// Gets the connection type (e.g., "PLC", "SerialPort", "TCP", etc.).
    /// </summary>
    public string? ConnectionType { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class.
    /// </summary>
    public ConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with connection details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="connectionTarget">The connection target (e.g., IP address, port name).</param>
    /// <param name="connectionType">The connection type (e.g., "PLC", "SerialPort").</param>
    public ConnectionException(string message, string connectionTarget, string connectionType)
        : base(message)
    {
        ConnectionTarget = connectionTarget;
        ConnectionType = connectionType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with connection details and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="connectionTarget">The connection target.</param>
    /// <param name="connectionType">The connection type.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionException(string message, string connectionTarget, string connectionType, Exception innerException)
        : base(message, innerException)
    {
        ConnectionTarget = connectionTarget;
        ConnectionType = connectionType;
    }
}
