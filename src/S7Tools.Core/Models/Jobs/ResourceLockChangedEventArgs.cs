namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Event arguments for resource lock state changes.
/// </summary>
public class ResourceLockChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the resource key that was locked or released.
    /// </summary>
    public ResourceKey Resource { get; }

    /// <summary>
    /// Gets the action performed (Acquired or Released).
    /// </summary>
    public ResourceLockAction Action { get; }

    /// <summary>
    /// Gets the job ID associated with the lock operation (null if system operation).
    /// </summary>
    public int? JobId { get; }

    /// <summary>
    /// Gets the timestamp when the lock state changed.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceLockChangedEventArgs"/> class.
    /// </summary>
    /// <param name="resource">The resource key.</param>
    /// <param name="action">The lock action.</param>
    /// <param name="jobId">The job ID (optional).</param>
    public ResourceLockChangedEventArgs(ResourceKey resource, ResourceLockAction action, int? jobId = null)
    {

        Resource = resource;
        Action = action;
        JobId = jobId;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the type of resource lock action.
/// </summary>
public enum ResourceLockAction
{
    /// <summary>
    /// Resource was acquired/locked.
    /// </summary>
    Acquired,

    /// <summary>
    /// Resource was released/unlocked.
    /// </summary>
    Released
}
