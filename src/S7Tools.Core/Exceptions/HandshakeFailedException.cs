namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when bootloader handshake fails.
/// </summary>
public class HandshakeFailedException : BootloaderException
{
    /// <summary>
    /// Gets the expected bootloader version.
    /// </summary>
    public string? ExpectedVersion { get; init; }

    /// <summary>
    /// Gets the actual bootloader version received (null if no response).
    /// </summary>
    public string? ActualVersion { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandshakeFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public HandshakeFailedException(string message) : base(message)
    {
        Stage = "handshake";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandshakeFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public HandshakeFailedException(string message, Exception innerException) : base(message, innerException)
    {
        Stage = "handshake";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandshakeFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version.</param>
    public HandshakeFailedException(string message, string? expectedVersion, string? actualVersion) : base(message)
    {
        Stage = "handshake";
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
