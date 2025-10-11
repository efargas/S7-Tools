namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents a unique identifier for a system resource.
/// Used for resource locking and coordination across concurrent jobs.
/// </summary>
/// <param name="Kind">The type of resource (e.g., "serial", "tcp", "power").</param>
/// <param name="Id">The unique identifier for the specific resource instance.</param>
public readonly record struct ResourceKey(string Kind, string Id)
{
    /// <summary>
    /// Returns a string representation of the resource key.
    /// </summary>
    /// <returns>A string in the format "Kind:Id".</returns>
    public override string ToString() => $"{Kind}:{Id}";
}
