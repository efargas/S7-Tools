namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents a bootloader job with profile configuration, state tracking, and execution progress.
/// Immutable record with state transition validation.
/// </summary>
public sealed record Job
{
    /// <summary>
    /// Unique job identifier (auto-assigned, gap-filling from 1).
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// User-friendly job name (1-100 chars, unique).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Job purpose/notes (max 500 chars).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Configuration profile references (serial, socat, power, memory region, payload set).
    /// </summary>
    public required JobProfileSet ProfileSet { get; init; }

    /// <summary>
    /// Current execution state (Created → Queued → Running → {Completed, Failed, Canceled}).
    /// </summary>
    public JobState State { get; init; } = JobState.Created;

    /// <summary>
    /// Job creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last state change timestamp (UTC).
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Queue entry timestamp (UTC), null if not yet queued.
    /// </summary>
    public DateTime? QueuedAt { get; init; }

    /// <summary>
    /// Execution start timestamp (UTC), null if not yet started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Execution completion timestamp (UTC), null if not yet completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Completion percentage (0.0-100.0).
    /// </summary>
    public double Progress { get; init; }

    /// <summary>
    /// Current stage description (max 200 chars).
    /// </summary>
    public string CurrentOperation { get; init; } = string.Empty;

    /// <summary>
    /// Memory dump save location (valid directory path).
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Failure reason if State = Failed (max 1000 chars), null otherwise.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Resource keys required by this job (derived from ProfileSet).
    /// </summary>
    public IReadOnlyList<ResourceKey> Resources => DeriveResources();

    /// <summary>
    /// Validates state transitions and returns true if transition is valid.
    /// Valid transitions: Created → Queued → Running → {Completed, Failed, Canceled}
    /// Canceled reachable from Queued or Running.
    /// </summary>
    /// <param name="newState">The target state.</param>
    /// <returns>True if transition is valid, false otherwise.</returns>
    public bool CanTransitionTo(JobState newState)
    {
        return State switch
        {
            JobState.Created => newState == JobState.Queued,
            JobState.Queued => newState is JobState.Running or JobState.Canceled,
            JobState.Running => newState is JobState.Completed or JobState.Failed or JobState.Canceled,
            JobState.Completed or JobState.Failed or JobState.Canceled => false, // Terminal states
            _ => false
        };
    }

    /// <summary>
    /// Derives resource keys from the profile set for resource coordination.
    /// </summary>
    private IReadOnlyList<ResourceKey> DeriveResources()
    {
        return new List<ResourceKey>
        {
            new ResourceKey("serial", ProfileSet.Serial.Device),
            new ResourceKey("tcp", ProfileSet.Socat.Port.ToString()),
            new ResourceKey("modbus", $"{ProfileSet.Power.Host}:{ProfileSet.Power.Port}")
        }.AsReadOnly();
    }
}
