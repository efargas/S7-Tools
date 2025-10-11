using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Tasking;

/// <summary>
/// Manages resource coordination and locking for concurrent job execution.
/// Ensures exclusive access to shared resources across multiple jobs.
/// </summary>
public sealed class ResourceCoordinator : IResourceCoordinator
{
    private readonly HashSet<ResourceKey> _locks = new();
    private readonly object _syncRoot = new();

    /// <inheritdoc />
    public bool TryAcquire(IEnumerable<ResourceKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var keysArray = keys.ToArray();

        lock (_syncRoot)
        {
            // Check if any of the requested resources are already locked
            if (keysArray.Any(_locks.Contains))
            {
                return false;
            }

            // Acquire all resources
            foreach (var key in keysArray)
            {
                _locks.Add(key);
            }

            return true;
        }
    }

    /// <inheritdoc />
    public void Release(IEnumerable<ResourceKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        lock (_syncRoot)
        {
            foreach (var key in keys)
            {
                _locks.Remove(key);
            }
        }
    }
}
