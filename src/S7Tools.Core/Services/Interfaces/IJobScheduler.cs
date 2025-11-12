using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Manages job queue and execution for bootloader operations.
/// Coordinates parallel execution on independent hardware while preventing resource conflicts.
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Enqueues a job for execution. Job transitions from Created → Queued.
    /// </summary>
    /// <param name="job">Job to enqueue (must be in Created state)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queued job with updated state and QueuedAt timestamp</returns>
    /// <exception cref="InvalidOperationException">Job not in Created state</exception>
    Task<Job> EnqueueAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a queued or running job. Job transitions to Canceled state.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if job was canceled, false if already terminal (Completed/Failed/Canceled)</returns>
    Task<bool> CancelJobAsync(int jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all jobs matching the specified states.
    /// </summary>
    /// <param name="states">Job states to filter by (null = all states)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of jobs in specified states</returns>
    Task<IReadOnlyList<Job>> GetJobsByStateAsync(
        JobState[]? states = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific job by identifier.
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job if found, null otherwise</returns>
    Task<Job?> GetJobByIdAsync(int jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when job state changes (Queued, Running, Completed, Failed, Canceled).
    /// Subscribe to this event for real-time UI updates.
    /// </summary>
    event EventHandler<JobStateChangedEventArgs>? JobStateChanged;

    /// <summary>
    /// Event raised when job progress updates (percentage, current operation).
    /// Subscribe to this event for progress bar updates.
    /// </summary>
    event EventHandler<JobProgressChangedEventArgs>? JobProgressChanged;

    /// <summary>
    /// Starts the scheduler background processing loop.
    /// Monitors queue and executes jobs when resources available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop scheduler</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the scheduler and waits for all running jobs to complete or cancel.
    /// </summary>
    /// <param name="gracefulTimeout">Maximum time to wait for graceful shutdown</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken = default);
}
