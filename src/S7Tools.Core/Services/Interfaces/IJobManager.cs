using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Validation;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for job management operations.
/// Provides CRUD operations, template management, and validation for JobProfile entities.
/// </summary>
public interface IJobManager : IProfileManager<JobProfile>
{
    /// <summary>
    /// Creates a new job from an existing template.
    /// </summary>
    /// <param name="templateId">The ID of the template job to copy.</param>
    /// <param name="newName">The name for the new job.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The newly created job.</returns>
    Task<JobProfile> CreateFromTemplateAsync(int templateId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all job templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of job templates.</returns>
    Task<IEnumerable<JobProfile>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or unsets a job as a template.
    /// </summary>
    /// <param name="jobId">The ID of the job to modify.</param>
    /// <param name="isTemplate">Whether the job should be marked as a template.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the operation succeeded, false otherwise.</returns>
    Task<bool> SetAsTemplateAsync(int jobId, bool isTemplate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a job configuration.
    /// </summary>
    /// <param name="job">The job to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A validation result indicating success or failure with error messages.</returns>
    Task<ValidationResult> ValidateJobAsync(JobProfile job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs by their current state.
    /// </summary>
    /// <param name="state">The job state to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Jobs in the specified state.</returns>
    Task<IEnumerable<JobProfile>> GetJobsByStateAsync(JobState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a job can be executed (resources available, valid configuration).
    /// </summary>
    /// <param name="jobId">The ID of the job to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the job can be executed, false otherwise.</returns>
    Task<bool> CanExecuteJobAsync(int jobId, CancellationToken cancellationToken = default);
}
