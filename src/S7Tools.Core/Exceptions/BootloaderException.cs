namespace S7Tools.Core.Exceptions;

/// <summary>
/// Base exception for bootloader-related errors.
/// </summary>
public class BootloaderException : S7ToolsException
{
    /// <summary>
    /// Gets the job ID associated with this exception (null if not job-specific).
    /// </summary>
    public int? JobId { get; init; }

    /// <summary>
    /// Gets the bootloader stage where the error occurred.
    /// </summary>
    public string? Stage { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BootloaderException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BootloaderException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="jobId">The job ID.</param>
    /// <param name="stage">The bootloader stage.</param>
    public BootloaderException(string message, int? jobId, string? stage) : base(message)
    {
        JobId = jobId;
        Stage = stage;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="jobId">The job ID.</param>
    /// <param name="stage">The bootloader stage.</param>
    public BootloaderException(string message, Exception innerException, int? jobId, string? stage)
        : base(message, innerException)
    {
        JobId = jobId;
        Stage = stage;
    }
}
