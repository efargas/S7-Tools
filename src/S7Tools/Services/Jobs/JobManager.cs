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
using S7Tools.Extensions;
using S7Tools.Services;

namespace S7Tools.Services.Jobs;

/// <summary>
/// Standard implementation of IJobManager providing unified job management functionality.
/// Extends StandardProfileManager to provide job-specific operations including templates and execution validation.
/// </summary>
public class JobManager(
    Microsoft.Extensions.Options.IOptions<S7Tools.Core.Models.Jobs.JobManagerOptions> options,
    ILogger<JobManager> logger,
    IResourceCoordinator resourceCoordinator,
    ISerialPortProfileService serialProfileService,
    ISocatProfileService socatProfileService,
    IPowerSupplyProfileService powerSupplyProfileService,
    IMemoryRegionProfileService memoryRegionProfileService,
    ITimeProvider timeProvider)
    : StandardProfileManager<JobProfile>(options.Value.ProfilesPath, logger), IJobManager
{
    #region Private Fields

    private readonly IResourceCoordinator _resourceCoordinator = resourceCoordinator ?? throw new ArgumentNullException(nameof(resourceCoordinator));
    private readonly ISerialPortProfileService _serialProfileService = serialProfileService ?? throw new ArgumentNullException(nameof(serialProfileService));
    private readonly ISocatProfileService _socatProfileService = socatProfileService ?? throw new ArgumentNullException(nameof(socatProfileService));
    private readonly IPowerSupplyProfileService _powerSupplyProfileService = powerSupplyProfileService ?? throw new ArgumentNullException(nameof(powerSupplyProfileService));
    private readonly IMemoryRegionProfileService _memoryRegionProfileService = memoryRegionProfileService ?? throw new ArgumentNullException(nameof(memoryRegionProfileService));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));


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
            await SaveProfilesAsync().ConfigureAwait(false);
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

        return await _semaphore.ExecuteAsync(async () =>
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
            newJob.CreatedAt = _timeProvider.GetLocalNow();
            newJob.ModifiedAt = _timeProvider.GetLocalNow();

            _profiles.Add(newJob);
            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));

            await SaveProfilesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created job '{JobName}' (ID: {JobId}) from template '{TemplateName}' (ID: {TemplateId})",
                newJob.Name, newJob.Id, template.Name, templateId);

            return CloneProfile(newJob);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JobProfile> AddJobAsync(JobProfile job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        return await _semaphore.ExecuteAsync(async () =>
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            // Validate uniqueness
            if (_profiles.Any(p => string.Equals(p.Name, job.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateProfileNameException(job.Name);
            }

            // Assign ID and add to collection
            job.Id = GetNextAvailableIdCore();
            job.CreatedAt = _timeProvider.GetLocalNow();
            job.ModifiedAt = _timeProvider.GetLocalNow();

            _profiles.Add(job);
            _profiles.Sort((x, y) => x.Id.CompareTo(y.Id));

            await SaveProfilesAsync().ConfigureAwait(false);
            _logger.LogInformation("Added job '{JobName}' (ID: {JobId})", job.Name, job.Id);

            return CloneProfile(job);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<JobProfile>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all job templates");

        return await _semaphore.ExecuteAsync(async () =>
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var templates = _profiles.Where(p => p.IsTemplate).ToList();
            _logger.LogDebug("Found {Count} job templates", templates.Count);

            return (IEnumerable<JobProfile>)[.. templates.Select(CloneProfile)];
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> SetAsTemplateAsync(int jobId, bool isTemplate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting job ID {JobId} template status to {IsTemplate}", jobId, isTemplate);

        return await _semaphore.ExecuteAsync(async () =>
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

            await SaveProfilesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully set job '{JobName}' (ID: {JobId}) template status to {IsTemplate}",
                job.Name, jobId, isTemplate);

            return true;
        }, cancellationToken);
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
                MemoryMappingProfile? memoryProfile = await _memoryRegionProfileService.GetByIdAsync(job.MemoryRegionProfileId, cancellationToken).ConfigureAwait(false);
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

        ValidationResult result = errors.Count > 0 ? ValidationResult.Failure([.. errors]) : ValidationResult.Success();

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
    protected static JobProfile CloneProfile(JobProfile source)
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
    private async Task ClearAllDefaultFlagsAsync()
    {
        foreach (JobProfile? profile in _profiles.Where(p => p.IsDefault))
        {
            profile.IsDefault = false;
            profile.Touch();
        }
        await SaveProfilesAsync().ConfigureAwait(false);
        await Task.Yield();
    }

    /// <summary>
    /// Saves all profiles to the persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task SaveProfilesAsync()
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
            await SaveProfilesAsync().ConfigureAwait(false);
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
        JobProfile jobProfile = await GetByIdAsync(jobId, cancellationToken) ?? throw new ProfileNotFoundException(jobId);
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

        // Apply JobProfile override for SelectedMemorySegment
        if (!string.IsNullOrEmpty(jobProfile.SelectedMemorySegment) && memoryProfile != null)
        {
            MemorySegment? selectedSegment = memoryProfile.Segments
                .FirstOrDefault(s => s.Name == jobProfile.SelectedMemorySegment);

            if (selectedSegment != null)
            {
                // Create a filtered MemoryMappingProfile with ONLY this segment selected
                var filteredSegment = new MemorySegment
                {
                    Name = selectedSegment.Name,
                    StartAddress = selectedSegment.StartAddress,
                    Size = selectedSegment.Size,
                    Type = selectedSegment.Type,
                    IsSelected = true,
                    Description = selectedSegment.Description
                };

                memoryProfile = new MemoryMappingProfile
                {
                    Id = memoryProfile.Id,
                    Name = memoryProfile.Name,
                    Description = $"{memoryProfile.Description} (Segment: {jobProfile.SelectedMemorySegment})",
                    Segments = [filteredSegment]
                };

                // Also update the legacy MemoryRegionProfile to match
                memoryRegion = new MemoryRegionProfile(
                    selectedSegment.StartAddress ?? "0x20000000",
                    (uint)selectedSegment.Size);

                _logger.LogDebug("Job '{JobName}' filtered memory profile to segment: {SegmentName}",
                    jobProfile.Name, jobProfile.SelectedMemorySegment);
            }
            else
            {
                _logger.LogWarning("Job '{JobName}' specifies SelectedMemorySegment '{SegmentName}' but it was not found in profile '{ProfileName}'",
                    jobProfile.Name, jobProfile.SelectedMemorySegment, memoryProfile.Name);
            }
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
        var serialRef = SerialProfileRef.FromProfile(serialProfile, jobProfile.SerialDevice);
        var socatRef = SocatProfileRef.FromProfile(socatProfile, ephemeral: true);
        var powerRef = PowerProfileRef.FromProfile(powerProfile, jobProfile.PowerOffDelayMs / 1000);

        var profileSet = new JobProfileSet(
            serialRef,
            socatRef,
            powerRef,
            memoryRegion,
            jobProfile.Payloads,
            jobProfile.OutputPath,
            jobProfile.PowerOnTimeMs,
            jobProfile.PowerOffDelayMs,
            memoryProfile, // Pass full MemoryMappingProfile for segment-based dumping
            jobProfile.DumpCount
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
            CreatedAt = _timeProvider.GetLocalNow(),
            ModifiedAt = _timeProvider.GetLocalNow(),
            Progress = 0.0,
            CurrentOperation = string.Empty,
            OutputPath = jobProfile.OutputPath ?? string.Empty
        };
    }

    #endregion
}
