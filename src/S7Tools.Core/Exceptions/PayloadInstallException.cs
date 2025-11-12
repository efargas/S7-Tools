namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when payload installation fails.
/// </summary>
public class PayloadInstallException : BootloaderException
{
    /// <summary>
    /// Gets the payload type that failed to install (e.g., "stager", "dumper").
    /// </summary>
    public string? PayloadType { get; init; }

    /// <summary>
    /// Gets the payload size in bytes.
    /// </summary>
    public long? PayloadSize { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadInstallException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PayloadInstallException(string message) : base(message)
    {
        Stage = "payload_install";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadInstallException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PayloadInstallException(string message, Exception innerException) : base(message, innerException)
    {
        Stage = "payload_install";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadInstallException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="payloadType">The payload type.</param>
    /// <param name="payloadSize">The payload size.</param>
    public PayloadInstallException(string message, string? payloadType, long? payloadSize) : base(message)
    {
        Stage = "payload_install";
        PayloadType = payloadType;
        PayloadSize = payloadSize;
    }
}
