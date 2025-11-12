namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Event arguments for job progress updates.
/// </summary>
public class JobProgressChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the job ID that has progress updates.
    /// </summary>
    public int JobId { get; }

    /// <summary>
    /// Gets the progress percentage (0.0 to 100.0).
    /// </summary>
    public double ProgressPercentage { get; }

    /// <summary>
    /// Gets the current operation description.
    /// </summary>
    public string CurrentOperation { get; }

    /// <summary>
    /// Gets the timestamp of the progress update.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobProgressChangedEventArgs"/> class.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="progressPercentage">The progress percentage.</param>
    /// <param name="currentOperation">The current operation description.</param>
    public JobProgressChangedEventArgs(int jobId, double progressPercentage, string currentOperation)
    {
        JobId = jobId;
        ProgressPercentage = Math.Clamp(progressPercentage, 0.0, 100.0);
        CurrentOperation = currentOperation ?? string.Empty;
        Timestamp = DateTime.UtcNow;
    }
}
