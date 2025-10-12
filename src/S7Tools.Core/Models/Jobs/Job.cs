// Job.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record Job(
    Guid Id,
    string Name,
    IReadOnlyList<ResourceKey> Resources,
    JobProfileSet Profiles,
    JobState State,
    DateTimeOffset CreatedAt
);