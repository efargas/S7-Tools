using System.Collections.Concurrent;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Jobs;

namespace S7Tools.Services.Tasking;

/// <summary>
/// Manages resource coordination and locking for concurrent job execution.
/// Ensures exclusive access to shared resources across multiple jobs.
/// </summary>
public sealed class ResourceCoordinator : IResourceCoordinator
{
    private readonly ConcurrentDictionary<ResourceKey, int> _locks = new();
    private readonly object _syncRoot = new();

    /// <inheritdoc />
    public event EventHandler<ResourceLockChangedEventArgs>? ResourceLockChanged;

    /// <inheritdoc />
    public bool TryAcquire(IEnumerable<ResourceKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        ResourceKey[] keysArray = keys.ToArray();

        lock (_syncRoot)
        {
            // Check if any of the requested resources are already locked
            if (keysArray.Any(k => _locks.ContainsKey(k)))
            {
                return false;
            }

            // Acquire all resources (use jobId = 0 as default for now)
            foreach (ResourceKey key in keysArray)
            {
                _locks[key] = 0;
                ResourceLockChanged?.Invoke(this,
                    new ResourceLockChangedEventArgs(key, ResourceLockAction.Acquired, 0));
            }

            return true;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(
        IEnumerable<ResourceKey> resources,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resources);

        DateTime deadline = DateTime.UtcNow.Add(timeout);
        ResourceKey[] resourcesArray = resources.ToArray();

        while (DateTime.UtcNow < deadline)
        {
            if (TryAcquire(resourcesArray))
            {
                return true;
            }

            // Wait 100ms before retrying
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    /// <inheritdoc />
    public void Release(IEnumerable<ResourceKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        lock (_syncRoot)
        {
            foreach (ResourceKey key in keys)
            {
                if (_locks.TryRemove(key, out int jobId))
                {
                    ResourceLockChanged?.Invoke(this,
                        new ResourceLockChangedEventArgs(key, ResourceLockAction.Released, jobId));
                }
            }
        }
    }

    /// <inheritdoc />
    public bool AreAvailable(IEnumerable<ResourceKey> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        lock (_syncRoot)
        {
            return !resources.Any(r => _locks.ContainsKey(r));
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<ResourceKey, int> GetLockedResources()
    {
        return _locks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
