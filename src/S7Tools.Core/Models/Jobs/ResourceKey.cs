using System;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents a unique identifier for a system resource.
/// Used for resource locking and coordination across concurrent jobs.
/// </summary>
/// <param name="Kind">The type of resource (e.g., "serial", "tcp", "power").</param>
/// <param name="Id">The unique identifier for the specific resource instance.</param>
/// <remarks>
/// Equality comparison is case-insensitive for both Kind and Id to prevent duplicate
/// resource keys caused by inconsistent casing (e.g., "Serial" vs "serial", "COM1" vs "com1").
/// </remarks>
public readonly record struct ResourceKey(string Kind, string Id)
{
    /// <summary>
    /// Returns a string representation of the resource key.
    /// </summary>
    /// <returns>A string in the format "Kind:Id".</returns>
    public override string ToString() => $"{Kind}:{Id}";

    /// <summary>
    /// Determines whether this ResourceKey equals another ResourceKey using case-insensitive comparison.
    /// </summary>
    /// <param name="other">The other ResourceKey to compare.</param>
    /// <returns>True if both Kind and Id match case-insensitively; otherwise, false.</returns>
    public bool Equals(ResourceKey other)
    {
        return string.Equals(Kind, other.Kind, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a hash code for this ResourceKey using case-insensitive string comparison.
    /// </summary>
    /// <returns>A hash code computed from the uppercase Kind and Id strings.</returns>
    public override int GetHashCode()
    {
        // Use OrdinalIgnoreCase comparer's hash code to ensure consistent hashing
        // for case-insensitive equality
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(Kind ?? string.Empty),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Id ?? string.Empty)
        );
    }
}
