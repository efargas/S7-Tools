namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when required resources are unavailable or locked.
/// </summary>
public class ResourceUnavailableException : BootloaderException
{
    /// <summary>
    /// Gets the resources that were unavailable.
    /// </summary>
    public IReadOnlyList<string> UnavailableResources { get; init; }

    /// <summary>
    /// Gets the job ID that currently holds the conflicting resource (null if unknown).
    /// </summary>
    public int? ConflictingJobId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ResourceUnavailableException(string message) : base(message)
    {
        UnavailableResources = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="unavailableResources">The list of unavailable resources.</param>
    public ResourceUnavailableException(string message, IEnumerable<string> unavailableResources) : base(message)
    {
        UnavailableResources = unavailableResources?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="unavailableResources">The list of unavailable resources.</param>
    /// <param name="conflictingJobId">The job ID holding the conflicting resource.</param>
    public ResourceUnavailableException(string message, IEnumerable<string> unavailableResources, int? conflictingJobId)
        : base(message)
    {
        UnavailableResources = unavailableResources?.ToList() ?? new List<string>();
        ConflictingJobId = conflictingJobId;
    }
}
