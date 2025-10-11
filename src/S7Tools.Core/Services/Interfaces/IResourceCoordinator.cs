using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for resource coordination and locking.
/// Manages exclusive access to shared resources across concurrent jobs.
/// </summary>
public interface IResourceCoordinator
{
    /// <summary>
    /// Attempts to acquire exclusive access to the specified resources.
    /// </summary>
    /// <param name="keys">Collection of resource keys to acquire.</param>
    /// <returns>True if all resources were successfully acquired; otherwise, false.</returns>
    bool TryAcquire(IEnumerable<ResourceKey> keys);

    /// <summary>
    /// Releases previously acquired resources.
    /// </summary>
    /// <param name="keys">Collection of resource keys to release.</param>
    void Release(IEnumerable<ResourceKey> keys);
}
