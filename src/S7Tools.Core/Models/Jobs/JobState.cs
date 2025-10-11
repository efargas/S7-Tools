namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents the execution state of a job.
/// </summary>
public enum JobState
{
    /// <summary>
    /// Job has been created but not yet queued.
    /// </summary>
    Created,

    /// <summary>
    /// Job is queued and waiting for execution.
    /// </summary>
    Queued,

    /// <summary>
    /// Job is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Job was canceled before completion.
    /// </summary>
    Canceled
}
