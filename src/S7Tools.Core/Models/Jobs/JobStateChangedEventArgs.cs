namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Event arguments for job state changes.
/// </summary>
public class JobStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the job ID that changed state.
    /// </summary>
    public int JobId { get; }

    /// <summary>
    /// Gets the previous job state.
    /// </summary>
    public JobState PreviousState { get; }

    /// <summary>
    /// Gets the new job state.
    /// </summary>
    public JobState NewState { get; }

    /// <summary>
    /// Gets the timestamp when the state changed.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the error message if the state changed to Failed (null otherwise).
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="previousState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="errorMessage">The error message (for Failed state).</param>
    public JobStateChangedEventArgs(int jobId, JobState previousState, JobState newState, string? errorMessage = null)
    {
        JobId = jobId;
        PreviousState = previousState;
        NewState = newState;
        Timestamp = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}
