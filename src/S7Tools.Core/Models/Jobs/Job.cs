namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents a bootloader job for memory dumping operations.
/// Contains all configuration and state information for job execution.
/// </summary>
/// <param name="Id">Unique identifier for the job.</param>
/// <param name="Name">Human-readable name for the job.</param>
/// <param name="Resources">Collection of resources required by this job.</param>
/// <param name="Profiles">Configuration profiles for the job.</param>
/// <param name="State">Current execution state of the job.</param>
/// <param name="CreatedAt">Timestamp when the job was created.</param>
public sealed record Job(
    Guid Id,
    string Name,
    IReadOnlyList<ResourceKey> Resources,
    JobProfileSet Profiles,
    JobState State,
    DateTimeOffset CreatedAt
);
