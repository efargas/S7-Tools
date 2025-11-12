namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Represents the execution state of a job.
/// </summary>
public enum JobState
{
    /// <summary>
    /// Job has been created but not yet queued.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Job is queued and waiting for execution.
    /// </summary>
    Queued = 1,

    /// <summary>
    /// Job is currently running.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Job was canceled before completion.
    /// </summary>
    Canceled = 5
}
