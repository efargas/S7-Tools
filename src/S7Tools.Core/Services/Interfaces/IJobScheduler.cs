using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Delegate for job state change notifications.
/// </summary>
/// <param name="jobId">The unique identifier of the job.</param>
/// <param name="state">The new state of the job.</param>
/// <param name="message">Optional message describing the state change.</param>
public delegate void JobStateChanged(Guid jobId, JobState state, string? message);

/// <summary>
/// Defines the contract for job scheduling and execution.
/// Manages job queuing, resource coordination, and parallel execution.
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Event raised when a job's state changes.
    /// </summary>
    event JobStateChanged? JobStateChanged;

    /// <summary>
    /// Enqueues a job for execution.
    /// </summary>
    /// <param name="job">The job to enqueue.</param>
    /// <returns>The unique identifier of the enqueued job.</returns>
    Guid Enqueue(Job job);

    /// <summary>
    /// Gets all jobs in the scheduler.
    /// </summary>
    /// <returns>A read-only collection of all jobs.</returns>
    IReadOnlyCollection<Job> GetAll();
}
