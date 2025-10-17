using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Validation;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for managing job profiles using the unified profile management pattern.
/// Provides job-specific operations including template management and job categories.
/// </summary>
/// <remarks>
/// This ViewModel extends the established ProfileManagementViewModelBase pattern for jobs:
/// - Full CRUD operations through profile dialogs
/// - Template management for job reuse and standardization
/// - Job categories for organization
/// - Integration with the unified profile management system
/// - Proper validation and error handling patterns
///
/// Architecture:
/// - Extends ProfileManagementViewModelBase&lt;JobProfile&gt; for consistent UI patterns
/// - Uses IJobManager for job-specific operations (templates, validation)
/// - Integrates with existing dialog services for create/edit operations
/// - Follows established S7Tools patterns for profile management
/// </remarks>
public class JobsManagementViewModel : ProfileManagementViewModelBase<JobProfile>
{
    private readonly IJobManager _jobManager;
    private readonly ILogger<JobsManagementViewModel> _logger;
    private readonly CompositeDisposable _localDisposables = new();

    // Job-specific collections for UI organization
    private ObservableCollection<JobProfile> _allJobs = new();
    private ObservableCollection<JobProfile> _jobTemplates = new();
    private ObservableCollection<JobProfile> _userJobs = new();

    // Additional UI state for job management
    private string _selectedCategory = "All";
    private bool _showTemplatesOnly;
    private string _categoryFilter = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsManagementViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger for this view model.</param>
    /// <param name="jobManager">The job manager service for job operations.</param>
    /// <param name="profileDialogService">The unified profile dialog service.</param>
    /// <param name="dialogService">The general dialog service for confirmations.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    public JobsManagementViewModel(
        ILogger<JobsManagementViewModel> logger,
        IJobManager jobManager,
        IUnifiedProfileDialogService profileDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService)
        : base(logger, profileDialogService, dialogService, uiThreadService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

        SetupJobSpecificCommands();
        SetupJobCollections();

        // Subscribe to profile changes to update job-specific collections
        this.WhenAnyValue(x => x.Profiles)
            .Subscribe(_ => UpdateJobCollections())
            .DisposeWith(_localDisposables);
    }

    #region Job-Specific Properties

    /// <summary>
    /// Gets the collection of all job profiles.
    /// </summary>
    /// <remarks>
    /// Complete collection of all job profiles for comprehensive management.
    /// Updated automatically when the base Profiles collection changes.
    /// </remarks>
    public ObservableCollection<JobProfile> AllJobs
    {
        get => _allJobs;
        private set => this.RaiseAndSetIfChanged(ref _allJobs, value);
    }

    /// <summary>
    /// Gets the collection of job templates.
    /// </summary>
    /// <remarks>
    /// Job profiles marked as templates for creating new jobs with predefined configurations.
    /// Templates provide standardization and reuse across different job executions.
    /// </remarks>
    public ObservableCollection<JobProfile> JobTemplates
    {
        get => _jobTemplates;
        private set => this.RaiseAndSetIfChanged(ref _jobTemplates, value);
    }

    /// <summary>
    /// Gets the collection of user-created job profiles (non-templates).
    /// </summary>
    /// <remarks>
    /// Regular job profiles created by users for specific execution scenarios.
    /// Excludes system profiles and templates for cleaner organization.
    /// </remarks>
    public ObservableCollection<JobProfile> UserJobs
    {
        get => _userJobs;
        private set => this.RaiseAndSetIfChanged(ref _userJobs, value);
    }

    /// <summary>
    /// Gets or sets the selected job category for filtering.
    /// </summary>
    /// <remarks>
    /// Category filter for organizing jobs by type, purpose, or target hardware.
    /// Default categories include "All", "Templates", "User Jobs", and custom categories.
    /// </remarks>
    public string SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show only job templates.
    /// </summary>
    /// <remarks>
    /// Filter toggle for focusing on template management operations.
    /// When enabled, only templates are shown in the main job list.
    /// </remarks>
    public bool ShowTemplatesOnly
    {
        get => _showTemplatesOnly;
        set => this.RaiseAndSetIfChanged(ref _showTemplatesOnly, value);
    }

    /// <summary>
    /// Gets or sets the category filter text for custom category filtering.
    /// </summary>
    /// <remarks>
    /// Free-text filter for finding jobs by category metadata.
    /// Supports partial matching for flexible job discovery.
    /// </remarks>
    public string CategoryFilter
    {
        get => _categoryFilter;
        set => this.RaiseAndSetIfChanged(ref _categoryFilter, value);
    }

    #endregion

    #region Job-Specific Commands

    /// <summary>
    /// Gets the command to create a new job from a template.
    /// </summary>
    /// <remarks>
    /// Opens template selection dialog and creates a new job with template configuration.
    /// Template provides baseline settings that can be customized during creation.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> CreateFromTemplateCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to save the selected job as a template.
    /// </summary>
    /// <remarks>
    /// Converts the selected job profile into a reusable template.
    /// Template can be used to create new jobs with the same configuration.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> SaveAsTemplateCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to import job profiles from a file.
    /// </summary>
    /// <remarks>
    /// Allows importing job configurations from JSON or other supported formats.
    /// Useful for sharing job configurations between installations.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> ImportJobCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to export selected jobs to a file.
    /// </summary>
    /// <remarks>
    /// Exports job configurations for backup or sharing purposes.
    /// Supports various formats including JSON for portability.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> ExportJobCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to create a new task from the selected job.
    /// </summary>
    /// <remarks>
    /// Creates a new task execution from the selected job profile.
    /// Provides direct integration with the task management system.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> CreateTaskFromJobCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to validate the selected job configuration.
    /// </summary>
    /// <remarks>
    /// Performs comprehensive validation of the job profile configuration.
    /// Checks profile references, memory regions, and output settings.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> ValidateJobCommand { get; private set; } = null!;

    #endregion

    #region ProfileManagementViewModelBase Implementation

    /// <summary>
    /// Gets the profile manager for job profile operations.
    /// </summary>
    /// <returns>The job manager cast as IProfileManager&lt;JobProfile&gt;.</returns>
    protected override IProfileManager<JobProfile> GetProfileManager()
    {
        return _jobManager;
    }

    /// <summary>
    /// Gets the default profile name for new job creation.
    /// </summary>
    /// <returns>The standardized default name "JobDefault".</returns>
    protected override string GetDefaultProfileName()
    {
        return "JobDefault";
    }

    /// <summary>
    /// Gets the profile type name for display and logging purposes.
    /// </summary>
    /// <returns>The display name "Job".</returns>
    protected override string GetProfileTypeName()
    {
        return "Job";
    }

    /// <summary>
    /// Creates a default job profile instance with standard configuration.
    /// </summary>
    /// <returns>A new JobProfile with default settings.</returns>
    protected override JobProfile CreateDefaultProfile()
    {
        return JobProfile.CreateUserProfile("New Job", "User-created job profile");
    }

    /// <summary>
    /// Shows the create dialog for job profiles.
    /// </summary>
    /// <param name="request">The create request with default values.</param>
    /// <returns>The dialog result with created job or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<JobProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        // TODO: Implement job-specific create dialog
        // For now, create a basic job profile
        var defaultJob = JobProfile.CreateUserProfile(request.DefaultName, request.DefaultDescription);
        await Task.Yield();
        return new ProfileDialogResult<JobProfile>
        {
            IsSuccess = true,
            Result = defaultJob
        };
    }

    /// <summary>
    /// Shows the edit dialog for job profiles.
    /// </summary>
    /// <param name="request">The edit request with profile ID.</param>
    /// <returns>The dialog result with updated job or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<JobProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        // TODO: Implement job-specific edit dialog
        // For now, return the existing profile unchanged
        JobProfile? existingJob = await _jobManager.GetByIdAsync(request.ProfileId);
        await Task.Yield();
        return new ProfileDialogResult<JobProfile>
        {
            IsSuccess = existingJob != null,
            Result = existingJob
        };
    }

    /// <summary>
    /// Shows the duplicate input dialog for job profiles.
    /// </summary>
    /// <param name="request">The duplicate request with source profile ID and suggested name.</param>
    /// <returns>The dialog result with new profile name or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        // TODO: Implement input dialog for job duplication
        // For now, return the suggested name
        await Task.Yield();
        return new ProfileDialogResult<string>
        {
            IsSuccess = true,
            Result = request.SuggestedName
        };
    }

    #endregion

    #region Private Implementation

    private void SetupJobSpecificCommands()
    {
        // Template-related commands
        IObservable<bool> hasTemplates = this.WhenAnyValue(x => x.JobTemplates.Count)
            .Select(count => count > 0);

        IObservable<bool> hasSelectedJob = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(job => job != null);

        IObservable<bool> canCreateTemplate = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(job => job != null && !job.IsTemplate);

        CreateFromTemplateCommand = ReactiveCommand.CreateFromTask(ExecuteCreateFromTemplateAsync, hasTemplates);
        SaveAsTemplateCommand = ReactiveCommand.CreateFromTask(ExecuteSaveAsTemplateAsync, canCreateTemplate);

        // Import/Export commands
        ImportJobCommand = ReactiveCommand.CreateFromTask(ExecuteImportJobAsync);
        ExportJobCommand = ReactiveCommand.CreateFromTask(ExecuteExportJobAsync, hasSelectedJob);

        // Task integration commands
        CreateTaskFromJobCommand = ReactiveCommand.CreateFromTask(ExecuteCreateTaskFromJobAsync, hasSelectedJob);
        ValidateJobCommand = ReactiveCommand.CreateFromTask(ExecuteValidateJobAsync, hasSelectedJob);

        // Subscribe to command execution for logging
        CreateFromTemplateCommand.Subscribe(_ => _logger.LogDebug("Create from template command executed")).DisposeWith(_localDisposables);
        SaveAsTemplateCommand.Subscribe(_ => _logger.LogDebug("Save as template command executed for job {JobId}", SelectedProfile?.Id)).DisposeWith(_localDisposables);
        ImportJobCommand.Subscribe(_ => _logger.LogDebug("Import job command executed")).DisposeWith(_localDisposables);
        ExportJobCommand.Subscribe(_ => _logger.LogDebug("Export job command executed for job {JobId}", SelectedProfile?.Id)).DisposeWith(_localDisposables);
    }

    private void SetupJobCollections()
    {
        // Set up reactive updates for job-specific collections
        this.WhenAnyValue(x => x.ShowTemplatesOnly, x => x.CategoryFilter)
            .Subscribe(_ => UpdateJobCollections())
            .DisposeWith(_localDisposables);
    }

    private void UpdateJobCollections()
    {
        if (Profiles == null)
        {
            return;
        }

        try
        {
            // Update AllJobs collection
            AllJobs = new ObservableCollection<JobProfile>(Profiles);

            // Update JobTemplates collection
            var templates = Profiles.Where(j => j.IsTemplate).ToList();
            JobTemplates = new ObservableCollection<JobProfile>(templates);

            // Update UserJobs collection (exclude templates and system profiles)
            var userJobs = Profiles.Where(j => !j.IsTemplate && !j.IsReadOnly).ToList();
            UserJobs = new ObservableCollection<JobProfile>(userJobs);

            _logger.LogDebug("Updated job collections: {AllCount} total, {TemplateCount} templates, {UserCount} user jobs",
                AllJobs.Count, JobTemplates.Count, UserJobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job collections");
        }
    }

    #endregion

    #region Command Implementations

    private async Task ExecuteCreateFromTemplateAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating job from template...";

            // TODO: Show template selection dialog
            // For now, use the first available template
            JobProfile? template = JobTemplates.FirstOrDefault();
            if (template == null)
            {
                StatusMessage = "No templates available";
                return;
            }

            string newName = $"Job from {template.Name}";
            JobProfile newJob = await _jobManager.CreateFromTemplateAsync(template.Id, newName);

            StatusMessage = $"Created job '{newJob.Name}' from template '{template.Name}'";
            _logger.LogInformation("Created job {JobId} ({JobName}) from template {TemplateId} ({TemplateName})",
                newJob.Id, newJob.Name, template.Id, template.Name);

            // Refresh profiles and select the new job
            await LoadProfilesAsync();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == newJob.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating job from template: {ex.Message}";
            _logger.LogError(ex, "Error creating job from template");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteSaveAsTemplateAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving job as template...";

            // TODO: Show template name input dialog
            string templateName = $"{SelectedProfile.Name} Template";

            bool success = await _jobManager.SetAsTemplateAsync(SelectedProfile.Id, true);

            if (success)
            {
                StatusMessage = $"Job '{SelectedProfile.Name}' saved as template";
                _logger.LogInformation("Saved job {JobId} ({JobName}) as template",
                    SelectedProfile.Id, SelectedProfile.Name);

                // Refresh to update template collections
                await LoadProfilesAsync();
            }
            else
            {
                StatusMessage = $"Failed to save job '{SelectedProfile.Name}' as template";
                _logger.LogWarning("Failed to save job {JobId} ({JobName}) as template",
                    SelectedProfile.Id, SelectedProfile.Name);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving job as template: {ex.Message}";
            _logger.LogError(ex, "Error saving job {JobId} as template", SelectedProfile?.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteImportJobAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Importing jobs...";

            // TODO: Implement job import functionality
            // For now, just show a placeholder message
            StatusMessage = "Job import functionality not yet implemented";
            _logger.LogInformation("Job import requested but not yet implemented");
            await Task.Yield();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing jobs: {ex.Message}";
            _logger.LogError(ex, "Error importing jobs");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteExportJobAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Exporting job...";

            // TODO: Implement job export functionality
            // For now, just show a placeholder message
            StatusMessage = $"Export functionality for job '{SelectedProfile.Name}' not yet implemented";
            _logger.LogInformation("Job export requested for {JobId} ({JobName}) but not yet implemented",
                SelectedProfile.Id, SelectedProfile.Name);
            await Task.Yield();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting job: {ex.Message}";
            _logger.LogError(ex, "Error exporting job {JobId}", SelectedProfile?.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteCreateTaskFromJobAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Creating task from job...";

            // TODO: Integrate with TaskScheduler to create task
            // For now, just show a placeholder message
            StatusMessage = $"Task creation from job '{SelectedProfile.Name}' not yet implemented";
            _logger.LogInformation("Task creation requested from job {JobId} ({JobName}) but not yet implemented",
                SelectedProfile.Id, SelectedProfile.Name);
            await Task.Yield();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating task from job: {ex.Message}";
            _logger.LogError(ex, "Error creating task from job {JobId}", SelectedProfile?.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteValidateJobAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Validating job...";

            ValidationResult validationResult = await _jobManager.ValidateJobAsync(SelectedProfile);
            await Task.Yield();

            if (validationResult.IsValid)
            {
                StatusMessage = $"Job '{SelectedProfile.Name}' is valid";
                _logger.LogInformation("Job {JobId} ({JobName}) validation passed",
                    SelectedProfile.Id, SelectedProfile.Name);
            }
            else
            {
                string errorSummary = string.Join(", ", validationResult.Errors.Take(3));
                StatusMessage = $"Job '{SelectedProfile.Name}' has validation errors: {errorSummary}";
                _logger.LogWarning("Job {JobId} ({JobName}) validation failed: {Errors}",
                    SelectedProfile.Id, SelectedProfile.Name, string.Join("; ", validationResult.Errors));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error validating job: {ex.Message}";
            _logger.LogError(ex, "Error validating job {JobId}", SelectedProfile?.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Loads profiles and updates job-specific collections.
    /// </summary>
    private async Task LoadProfilesAsync()
    {
        try
        {
            await InitializeAsync();
            UpdateJobCollections();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load job profiles");
            throw;
        }
    }

    #endregion

    #region IDisposable Override

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="JobsManagementViewModel"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _localDisposables?.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion
}
