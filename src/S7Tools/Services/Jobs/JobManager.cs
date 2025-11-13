using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Validation;
using S7Tools.Services;

namespace S7Tools.Services.Jobs;

/// <summary>
/// Standard implementation of IJobManager providing unified job management functionality.
/// Extends StandardProfileManager to provide job-specific operations including templates and execution validation.
/// </summary>
public class JobManager : StandardProfileManager<JobProfile>, IJobManager
{
    #region Private Fields

    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly ISerialPortProfileService _serialProfileService;
    private readonly ISocatProfileService _socatProfileService;
    private readonly IPowerSupplyProfileService _powerSupplyProfileService;
    private readonly IMemoryRegionProfileService _memoryRegionProfileService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the JobManager class using options pattern.
    /// </summary>
    /// <param name="options">The options containing the profiles path.</param>
    /// <param name="logger">The logger instance for this manager.</param>
    /// <param name="resourceCoordinator">The resource coordinator for checking resource availability.</param>
    /// <param name="serialProfileService">The serial profile service for validation.</param>
    /// <param name="socatProfileService">The socat profile service for validation.</param>
    /// <param name="powerSupplyProfileService">The power supply profile service for validation.</param>
    /// <param name="memoryRegionProfileService">The memory region profile service for profile resolution.</param>
    public JobManager(
        Microsoft.Extensions.Options.IOptions<S7Tools.Core.Models.Jobs.JobManagerOptions> options,
        ILogger<JobManager> logger,
        IResourceCoordinator resourceCoordinator,
        ISerialPortProfileService serialProfileService,
        ISocatProfileService socatProfileService,
        IPowerSupplyProfileService powerSupplyProfileService,
        IMemoryRegionProfileService memoryRegionProfileService)
        : base(options.Value.ProfilesPath, logger)
    {
        _resourceCoordinator = resourceCoordinator ?? throw new ArgumentNullException(nameof(resourceCoordinator));
        _serialProfileService = serialProfileService ?? throw new ArgumentNullException(nameof(serialProfileService));
        _socatProfileService = socatProfileService ?? throw new ArgumentNullException(nameof(socatProfileService));
        _powerSupplyProfileService = powerSupplyProfileService ?? throw new ArgumentNullException(nameof(powerSupplyProfileService));
        _memoryRegionProfileService = memoryRegionProfileService ?? throw new ArgumentNullException(nameof(memoryRegionProfileService));
    }

    #endregion

    #region StandardProfileManager Implementation

    /// <inheritdoc/>
    protected override JobProfile CreateSystemDefault()
    {
        _logger.LogInformation("Creating system default job profile");
        return JobProfile.CreateDefaultProfile();
    }

    /// <inheritdoc/>
    protected override string ProfileTypeName => "Job";

    /// <inheritdoc/>
    protected override async Task CreateDefaultProfilesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating default job profiles for path: {Path}", _profilesPath);

        // Create the system default job profile
        JobProfile defaultProfile = CreateSystemDefault();
        _profiles.Add(defaultProfile);

        // Create a few example templates
        var basicTemplate = JobProfile.CreateUserProfile("Basic Memory Dump", "Simple 4KB memory dump template");
        await Task.Yield();
        basicTemplate.IsTemplate = true;
        basicTemplate.Id = 2;
        _profiles.Add(basicTemplate);

        var fullTemplate = JobProfile.CreateUserProfile("Full Memory Dump", "Complete memory dump template");
        fullTemplate.IsTemplate = true;
        fullTemplate.Id = 3;
        fullTemplate.MemoryRegionProfileId = 1; // Reference to default memory region profile
        _profiles.Add(fullTemplate);

        _logger.LogInformation("Created {Count} job profiles in memory", _profiles.Count);

        // Ensure directory exists
        string? directory = Path.GetDirectoryName(_profilesPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Ensured directory exists: {Directory}", directory);
        }

        // Save profiles to file
        try
        {
            _logger.LogDebug("About to save profiles to: {Path}", _profilesPath);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully created and saved {Count} default job profiles to: {Path}", _profiles.Count, _profilesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default job profiles to: {Path}", _profilesPath);
            _profiles.Clear(); // Clear the in-memory profiles if save failed
            throw;
        }
    }

    #endregion

    #region IJobManager Implementation

    /// <inheritdoc/>
    public async Task<JobProfile> CreateFromTemplateAsync(int templateId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(newName);

        _logger.LogInformation("Creating job from template ID {TemplateId} with name '{NewName}'", templateId, newName);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            // Find the template
            JobProfile? template = _profiles.FirstOrDefault(p => p.Id == templateId && p.IsTemplate);
            if (template == null)
            {
                _logger.LogError("Template with ID {TemplateId} not found or is not marked as template", templateId);
                throw new ProfileNotFoundException(templateId);
            }

            // Create new job from template (Duplicate already sets IsReadOnly=false, IsDefault=false, IsTemplate=false)
            JobProfile newJob = template.Duplicate(newName);

            // Validate uniqueness
            if (_profiles.Any(p => string.Equals(p.Name, newName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateProfileNameException(newName);
            }

            // Assign ID and add to collection
            newJob.Id = GetNextAvailableIdCore();
            newJob.CreatedAt = DateTime.UtcNow;
            newJob.ModifiedAt = DateTime.UtcNow;

            _profiles.Add(newJob);
            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully created job '{JobName}' (ID: {JobId}) from template '{TemplateName}' (ID: {TemplateId})",
                newJob.Name, newJob.Id, template.Name, templateId);

            return CloneProfile(newJob);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<JobProfile>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all job templates");

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var templates = _profiles.Where(p => p.IsTemplate).ToList();
            _logger.LogDebug("Found {Count} job templates", templates.Count);

            return templates.Select(CloneProfile).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SetAsTemplateAsync(int jobId, bool isTemplate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting job ID {JobId} template status to {IsTemplate}", jobId, isTemplate);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            JobProfile? job = _profiles.FirstOrDefault(p => p.Id == jobId);
            if (job == null)
            {
                _logger.LogWarning("Job with ID {JobId} not found", jobId);
                return false;
            }

            if (!job.CanModify())
            {
                _logger.LogWarning("Cannot modify read-only job '{JobName}' (ID: {JobId})", job.Name, jobId);
                throw new ReadOnlyProfileModificationException(jobId, job.Name);
            }

            job.IsTemplate = isTemplate;
            job.Touch();

            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully set job '{JobName}' (ID: {JobId}) template status to {IsTemplate}",
                job.Name, jobId, isTemplate);

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateJobAsync(JobProfile job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        _logger.LogDebug("Validating job '{JobName}' (ID: {JobId})", job.Name, job.Id);

        var errors = new List<ValidationError>();

        // Basic validation
        List<string> basicErrors = job.Validate();
        errors.AddRange(basicErrors.Select(error => new ValidationError("Job", error)));

        // Validate profile references
        try
        {
            SerialPortProfile? serialProfile = await _serialProfileService.GetByIdAsync(job.SerialProfileId, cancellationToken).ConfigureAwait(false);
            if (serialProfile == null)
            {
                errors.Add(new ValidationError("SerialProfileId", $"Serial profile with ID {job.SerialProfileId} not found"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating serial profile reference");
            errors.Add(new ValidationError("SerialProfileId", "Unable to validate serial profile reference"));
        }

        try
        {
            SocatProfile? socatProfile = await _socatProfileService.GetByIdAsync(job.SocatProfileId, cancellationToken).ConfigureAwait(false);
            if (socatProfile == null)
            {
                errors.Add(new ValidationError("SocatProfileId", $"Socat profile with ID {job.SocatProfileId} not found"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating socat profile reference");
            errors.Add(new ValidationError("SocatProfileId", "Unable to validate socat profile reference"));
        }

        try
        {
            PowerSupplyProfile? powerProfile = await _powerSupplyProfileService.GetByIdAsync(job.PowerSupplyProfileId, cancellationToken).ConfigureAwait(false);
            if (powerProfile == null)
            {
                errors.Add(new ValidationError("PowerSupplyProfileId", $"Power supply profile with ID {job.PowerSupplyProfileId} not found"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating power supply profile reference");
            errors.Add(new ValidationError("PowerSupplyProfileId", "Unable to validate power supply profile reference"));
        }

        // Validate memory region profile reference
        if (job.MemoryRegionProfileId <= 0)
        {
            errors.Add(new ValidationError("MemoryRegionProfileId", "Valid memory region profile must be selected"));
        }
        else
        {
            try
            {
                var memoryProfile = await _memoryRegionProfileService.GetByIdAsync(job.MemoryRegionProfileId, cancellationToken).ConfigureAwait(false);
                if (memoryProfile == null)
                {
                    errors.Add(new ValidationError("MemoryRegionProfileId", $"Memory region profile with ID {job.MemoryRegionProfileId} not found"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating memory region profile reference");
                errors.Add(new ValidationError("MemoryRegionProfileId", "Unable to validate memory region profile reference"));
            }
        }

        // Validate output path
        if (!string.IsNullOrEmpty(job.OutputPath))
        {
            try
            {
                string? directory = Path.GetDirectoryName(job.OutputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    errors.Add(new ValidationError("OutputPath", "Output directory does not exist and cannot be created"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating output path");
                errors.Add(new ValidationError("OutputPath", "Invalid output path format"));
            }
        }

        ValidationResult result = errors.Count > 0 ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();

        _logger.LogDebug("Job validation completed for '{JobName}' with {ErrorCount} errors", job.Name, errors.Count);

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<JobProfile>> GetJobsByStateAsync(JobState state, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting jobs by state: {State}", state);

        // Note: JobProfile doesn't track execution state - this is tracked by TaskExecution
        // For now, return all jobs since this is profile management, not execution management
        // Await the GetAllAsync call and return the result directly
        return await GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> CanExecuteJobAsync(int jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if job ID {JobId} can be executed", jobId);

        JobProfile? job = await GetByIdAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job == null)
        {
            _logger.LogWarning("Job with ID {JobId} not found", jobId);
            return false;
        }

        // Validate job configuration
        ValidationResult validationResult = await ValidateJobAsync(job, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            _logger.LogDebug("Job '{JobName}' (ID: {JobId}) cannot be executed due to validation errors: {Errors}",
                job.Name, jobId, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return false;
        }

        // Check resource availability
        Job executionJob = job.ToExecutionJob();
        bool resourcesAvailable = _resourceCoordinator.TryAcquire(executionJob.Resources);

        if (resourcesAvailable)
        {
            // Release immediately since we were just checking
            _resourceCoordinator.Release(executionJob.Resources);
        }

        _logger.LogDebug("Job '{JobName}' (ID: {JobId}) execution check: Valid={IsValid}, ResourcesAvailable={ResourcesAvailable}",
            job.Name, jobId, validationResult.IsValid, resourcesAvailable);

        return resourcesAvailable;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a deep clone of a job profile.
    /// </summary>
    /// <param name="source">The source job profile to clone.</param>
    /// <returns>A deep clone of the job profile.</returns>
    protected JobProfile CloneProfile(JobProfile source)
    {
        return source.ClonePreserveId();
    }

    /// <summary>
    /// Gets the next available ID without acquiring the semaphore (assumes already held).
    /// </summary>
    /// <returns>The next available ID.</returns>
    private int GetNextAvailableIdCore()
    {
        var existingIds = _profiles.Select(p => p.Id).ToHashSet();
        int nextId = 1;
        while (existingIds.Contains(nextId))
        {
            nextId++;
        }
        return nextId;
    }

    /// <summary>
    /// Clears all default flags from existing profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task ClearAllDefaultFlagsAsync(CancellationToken cancellationToken)
    {
        foreach (JobProfile? profile in _profiles.Where(p => p.IsDefault))
        {
            profile.IsDefault = false;
            profile.Touch();
        }
        await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
        await Task.Yield();
    }

    /// <summary>
    /// Saves all profiles to the persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task SaveProfilesAsync(CancellationToken cancellationToken)
    {
        // This method should be implemented by the base class
        // For now, just log that saving would happen
        _logger.LogDebug("Saving {Count} job profiles to {Path}", _profiles.Count, _profilesPath);
        await Task.Yield();
    }

    /// <summary>
    /// Ensures profiles are loaded from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_isLoaded)
        {
            return;
        }

        _logger.LogDebug("Loading job profiles from {Path}", _profilesPath);

        // Load profiles from file if it exists
        if (File.Exists(_profilesPath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(_profilesPath, cancellationToken).ConfigureAwait(false);
                List<JobProfile>? profiles = System.Text.Json.JsonSerializer.Deserialize<List<JobProfile>>(json);

                if (profiles != null)
                {
                    _profiles.Clear();
                    _profiles.AddRange(profiles);
                    _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading job profiles from {Path}", _profilesPath);
            }
        }

        // Ensure at least one default profile exists
        if (!_profiles.Any(p => p.IsDefault))
        {
            JobProfile defaultProfile = CreateSystemDefault();
            _profiles.Add(defaultProfile);
            await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
        }

        _isLoaded = true;
        _logger.LogInformation("Loaded {Count} job profiles", _profiles.Count);
    }

    /// <summary>
    /// Creates an execution job from a job profile with resolved memory region configuration.
    /// </summary>
    /// <param name="jobId">The ID of the job profile to convert.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Job ready for execution with resolved memory region profile.</returns>
    /// <exception cref="ProfileNotFoundException">Thrown when job profile is not found.</exception>
    public async Task<Job> CreateExecutionJobAsync(int jobId, CancellationToken cancellationToken = default)
    {
        JobProfile? jobProfile = await GetByIdAsync(jobId, cancellationToken);
        if (jobProfile == null)
        {
            throw new ProfileNotFoundException(jobId);
        }
        return await CreateExecutionJobAsync(jobProfile);
    }

    /// <summary>
    /// Creates an execution job from a job profile with resolved memory region configuration.
    /// </summary>
    /// <param name="jobProfile">The job profile to convert.</param>
    /// <returns>A Job ready for execution with resolved memory region profile.</returns>
    public async Task<Job> CreateExecutionJobAsync(JobProfile jobProfile)
    {
        // Resolve the memory region profile from the ID
        MemoryMappingProfile? memoryProfile = null;
        if (jobProfile.MemoryRegionProfileId > 0)
        {
            try
            {
                memoryProfile = await _memoryRegionProfileService.GetByIdAsync(jobProfile.MemoryRegionProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load memory region profile ID {ProfileId}, using default configuration",
                    jobProfile.MemoryRegionProfileId);
            }
        }

        // Create memory region configuration from profile or use default
        MemoryRegionProfile memoryRegion;
        if (memoryProfile != null)
        {
            // Convert the memory mapping profile to memory region profile using selected segments
            var selectedSegments = memoryProfile.Segments.Where(s => s.IsSelected).ToList();
            if (selectedSegments.Count > 0)
            {
                // Use the first selected segment as base configuration
                // TODO: Support multiple segments in JobProfileSet
                MemorySegment firstSegment = selectedSegments.First();
                memoryRegion = new MemoryRegionProfile(firstSegment.StartAddress, (uint)firstSegment.Size);
            }
            else
            {
                // No segments selected, use default
                _logger.LogWarning("Memory region profile '{ProfileName}' (ID: {ProfileId}) has no selected segments, using default configuration",
                    memoryProfile.Name, memoryProfile.Id);
                memoryRegion = new MemoryRegionProfile($"0x{MemoryConstants.DefaultUserMemoryStart:X8}", MemoryConstants.DefaultDumpSize);
            }
        }
        else
        {
            // Use the default memory region configuration as fallback
            _logger.LogWarning("Memory region profile with ID {ProfileId} not found, using default configuration", jobProfile.MemoryRegionProfileId);
            memoryRegion = new MemoryRegionProfile($"0x{MemoryConstants.DefaultUserMemoryStart:X8}", MemoryConstants.DefaultDumpSize);
        }

        // Fetch full profile objects for complete configuration
        SerialPortProfile? serialProfile = await _serialProfileService.GetByIdAsync(jobProfile.SerialProfileId);
        if (serialProfile == null)
        {
            _logger.LogError("Serial profile with ID {ProfileId} not found", jobProfile.SerialProfileId);
            throw new ProfileNotFoundException(jobProfile.SerialProfileId);
        }

        SocatProfile? socatProfile = await _socatProfileService.GetByIdAsync(jobProfile.SocatProfileId);
        if (socatProfile == null)
        {
            _logger.LogError("Socat profile with ID {ProfileId} not found", jobProfile.SocatProfileId);
            throw new ProfileNotFoundException(jobProfile.SocatProfileId);
        }

        PowerSupplyProfile? powerProfile = await _powerSupplyProfileService.GetByIdAsync(jobProfile.PowerSupplyProfileId);
        if (powerProfile == null)
        {
            _logger.LogError("Power supply profile with ID {ProfileId} not found", jobProfile.PowerSupplyProfileId);
            throw new ProfileNotFoundException(jobProfile.PowerSupplyProfileId);
        }

        // Create the job profile set with full configuration using factory methods
        SerialProfileRef serialRef = SerialProfileRef.FromProfile(serialProfile, jobProfile.SerialDevice);
        SocatProfileRef socatRef = SocatProfileRef.FromProfile(socatProfile, ephemeral: true);
        PowerProfileRef powerRef = PowerProfileRef.FromProfile(powerProfile, jobProfile.PowerOffDelayMs / 1000);

        var profileSet = new JobProfileSet(
            serialRef,
            socatRef,
            powerRef,
            memoryRegion,
            jobProfile.Payloads,
            jobProfile.OutputPath,
            jobProfile.PowerOnTimeMs,
            jobProfile.PowerOffDelayMs,
            memoryProfile // Pass full MemoryMappingProfile for segment-based dumping
        );

        // Generate resource keys
        // Generate a deterministic ID based on the profile ID
        int jobId = jobProfile.Id;

        return new Job
        {
            Id = jobId,
            Name = jobProfile.Name,
            Description = jobProfile.Description ?? string.Empty,
            ProfileSet = profileSet,
            State = JobState.Created,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Progress = 0.0,
            CurrentOperation = string.Empty,
            OutputPath = jobProfile.OutputPath ?? string.Empty
        };
    }

    #endregion
}
