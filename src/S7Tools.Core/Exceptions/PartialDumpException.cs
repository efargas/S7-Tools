using S7Tools.Core.Models;

namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when a dump operation was interrupted (cancelled or failed)
/// but partial data was already written to disk and is available for recovery.
/// </summary>
public class PartialDumpException : MemoryDumpException
{
    /// <summary>
    /// Gets the partial <see cref="BootloaderResult"/> containing the paths of
    /// any files that were fully or partially written before the interruption.
    /// </summary>
    public BootloaderResult PartialResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartialDumpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused the interruption.</param>
    /// <param name="partialResult">The partial result with saved file paths.</param>
    public PartialDumpException(string message, Exception innerException, BootloaderResult partialResult)
        : base(message, innerException)
    {
        PartialResult = partialResult ?? throw new ArgumentNullException(nameof(partialResult));
    }
}
