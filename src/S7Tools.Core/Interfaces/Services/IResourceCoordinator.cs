using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Interfaces.Services;

/// <summary>
/// Coordinates resource access across concurrent jobs to prevent conflicts.
/// Manages serial ports, TCP ports, and modbus connections with lock-free TryAcquire/Release pattern.
/// </summary>
public interface IResourceCoordinator
{
    /// <summary>
    /// Attempts to acquire all specified resources atomically.
    /// Either acquires ALL resources or acquires NONE (all-or-nothing semantics).
    /// </summary>
    /// <param name="resources">Resources to acquire</param>
    /// <returns>True if all resources acquired, false if any resource locked by another job</returns>
    bool TryAcquire(IEnumerable<ResourceKey> resources);

    /// <summary>
    /// Attempts to acquire all specified resources atomically with timeout.
    /// Retries acquisition for up to the specified timeout period.
    /// </summary>
    /// <param name="resources">Resources to acquire</param>
    /// <param name="timeout">Maximum time to wait for resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all resources acquired within timeout, false otherwise</returns>
    Task<bool> TryAcquireAsync(
        IEnumerable<ResourceKey> resources,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases all specified resources previously acquired via TryAcquire.
    /// Safe to call with resources not currently held (idempotent).
    /// </summary>
    /// <param name="resources">Resources to release</param>
    void Release(IEnumerable<ResourceKey> resources);

    /// <summary>
    /// Checks if specified resources are currently locked by any job.
    /// </summary>
    /// <param name="resources">Resources to check</param>
    /// <returns>True if all resources available, false if any locked</returns>
    bool AreAvailable(IEnumerable<ResourceKey> resources);

    /// <summary>
    /// Retrieves all currently locked resources with job information.
    /// </summary>
    /// <returns>Dictionary mapping resource keys to job IDs holding the lock</returns>
    IReadOnlyDictionary<ResourceKey, int> GetLockedResources();

    /// <summary>
    /// Event raised when resource lock state changes (acquired or released).
    /// Subscribe for real-time resource availability monitoring.
    /// </summary>
    event EventHandler<ResourceLockChangedEventArgs>? ResourceLockChanged;
}
