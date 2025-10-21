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
/// Wrapper ViewModel for JobsMainContentView to prevent circular references.
/// This exposes the JobsManagementViewModel properties but is a separate object.
/// </summary>
public class JobsMainContentViewModel : ViewModelBase, IDisposable
{
    private readonly JobsManagementViewModel _parent;
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    public JobsMainContentViewModel(JobsManagementViewModel parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));

        // Subscribe to parent property changes and re-raise them
        // Use proper property change forwarding to ensure UI updates
        _parent.WhenAnyValue(x => x.SelectedProfile)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SelectedProfile));
            })
            .DisposeWith(_disposables);

        _parent.WhenAnyValue(x => x.Profiles)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(Profiles));
            })
            .DisposeWith(_disposables);

        _parent.WhenAnyValue(x => x.StatusMessage)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(StatusMessage)))
            .DisposeWith(_disposables);

        _parent.WhenAnyValue(x => x.IsLoading)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(IsLoading)))
            .DisposeWith(_disposables);

        // Subscribe to collection-specific property changes
        _parent.WhenAnyValue(x => x.AllJobs)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(AllJobs)))
            .DisposeWith(_disposables);

        _parent.WhenAnyValue(x => x.JobTemplates)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(JobTemplates)))
            .DisposeWith(_disposables);

        _parent.WhenAnyValue(x => x.UserJobs)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(UserJobs)))
            .DisposeWith(_disposables);
    }

    // Expose parent properties for data binding
    public ObservableCollection<JobProfile> Profiles => _parent.Profiles;
    public JobProfile? SelectedProfile
    {
        get => _parent.SelectedProfile;
        set => _parent.SelectedProfile = value;
    }
    public ObservableCollection<JobProfile> AllJobs => _parent.AllJobs;
    public ObservableCollection<JobProfile> JobTemplates => _parent.JobTemplates;
    public ObservableCollection<JobProfile> UserJobs => _parent.UserJobs;
    public string StatusMessage => _parent.StatusMessage ?? string.Empty;
    public bool IsLoading => _parent.IsLoading;

    // Expose parent commands
    public ReactiveCommand<Unit, Unit> CreateCommand => _parent.CreateWizardCommand; // Use wizard instead of base create
    public ReactiveCommand<Unit, Unit> EditCommand => _parent.EditCommand;
    public ReactiveCommand<Unit, Unit> DuplicateCommand => _parent.DuplicateCommand;
    public ReactiveCommand<Unit, Unit> DeleteCommand => _parent.DeleteCommand;
    public ReactiveCommand<Unit, Unit> RefreshCommand => _parent.RefreshCommand;
    public ReactiveCommand<Unit, Unit> SetDefaultCommand => _parent.SetDefaultCommand;

    // Job-specific commands
    public ReactiveCommand<Unit, Unit> CreateFromTemplateCommand => _parent.CreateFromTemplateCommand;
    public ReactiveCommand<Unit, Unit> SaveAsTemplateCommand => _parent.SaveAsTemplateCommand;
    public ReactiveCommand<Unit, Unit> ImportJobCommand => _parent.ImportJobCommand;
    public ReactiveCommand<Unit, Unit> ExportJobCommand => _parent.ExportJobCommand;
    public ReactiveCommand<Unit, Unit> CreateTaskFromJobCommand => _parent.CreateTaskFromJobCommand;
    public ReactiveCommand<Unit, Unit> ValidateJobCommand => _parent.ValidateJobCommand;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposables?.Dispose();
            _disposed = true;
        }
    }
}

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
    private readonly IUIThreadService _uiThreadService;
    private readonly IUnifiedProfileDialogService _unifiedDialogService;
    private readonly IDialogService _dialogService;
    private readonly IViewModelFactory? _viewModelFactory;
    private readonly CompositeDisposable _localDisposables = new();

    // Job-specific collections for UI organization
    private ObservableCollection<JobProfile> _allJobs = new();
    private ObservableCollection<JobProfile> _jobTemplates = new();
    private ObservableCollection<JobProfile> _userJobs = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsManagementViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger for this view model.</param>
    /// <param name="jobManager">The job manager service for job operations.</param>
    /// <param name="profileDialogService">The unified profile dialog service.</param>
    /// <param name="dialogService">The general dialog service for confirmations.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    /// <param name="viewModelFactory">The view model factory for creating child ViewModels.</param>
    public JobsManagementViewModel(
        ILogger<JobsManagementViewModel> logger,
        IJobManager jobManager,
        IUnifiedProfileDialogService profileDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService,
        IViewModelFactory? viewModelFactory = null)
        : base(logger, profileDialogService, dialogService, uiThreadService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _unifiedDialogService = profileDialogService ?? throw new ArgumentNullException(nameof(profileDialogService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _viewModelFactory = viewModelFactory;

        SetupJobSpecificCommands();
        SetupJobCollections();
        SetupSideMenuItems();

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

    #endregion

    #region Sidebar Navigation (Settings Pattern)

    /// <summary>
    /// Gets the collection of sidebar menu items for navigation.
    /// </summary>
    public ObservableCollection<string> SideMenuItems { get; } = new();

    private string? _selectedSideMenuItem = "Main View";
    /// <summary>
    /// Gets or sets the selected sidebar menu item.
    /// </summary>
    public string? SelectedSideMenuItem
    {
        get => _selectedSideMenuItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSideMenuItem, value);
            this.RaisePropertyChanged(nameof(SelectedContentViewModel));
        }
    }

    /// <summary>
    /// Gets the selected content ViewModel for the main area based on sidebar selection.
    /// This follows the same pattern as SettingsViewModel.SelectedCategoryViewModel.
    /// </summary>
    public object? SelectedContentViewModel
    {
        get
        {
            return SelectedSideMenuItem switch
            {
                "Create (Wizard)" => CreateWizardViewModel(),
                "Edit (Wizard)" => CreateWizardViewModelFromSelected(),
                _ => CreateMainJobsContentViewModel() // Main View uses a dedicated ViewModel
            };
        }
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

    /// <summary>
    /// Gets the command to create a new job using the wizard interface.
    /// </summary>
    /// <remarks>
    /// Navigates to the wizard instead of opening the direct create dialog.
    /// Provides a guided step-by-step job creation experience.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> CreateWizardCommand { get; private set; } = null!;

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
        return await _unifiedDialogService.ShowJobCreateDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the edit dialog for job profiles.
    /// </summary>
    /// <param name="request">The edit request with profile ID.</param>
    /// <returns>The dialog result with updated job or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<JobProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        return await _unifiedDialogService.ShowJobEditDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the duplicate input dialog for job profiles.
    /// </summary>
    /// <param name="request">The duplicate request with source profile ID and suggested name.</param>
    /// <returns>The dialog result with new profile name or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            _logger.LogDebug("Showing duplicate input dialog for job profile ID: {SourceProfileId}", request.SourceProfileId);

            // Use the input dialog service since the job-specific duplicate dialog is not implemented yet
            var inputResult = await _dialogService.ShowInputAsync(
                request.Title,
                "Enter a name for the duplicated job profile:",
                request.SuggestedName,
                "Job profile name").ConfigureAwait(false);

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                _logger.LogDebug("Job duplicate dialog cancelled");
                return ProfileDialogResult<string>.Cancelled();
            }

            _logger.LogDebug("Job duplicate dialog completed with name: {Name}", inputResult.Value);
            return ProfileDialogResult<string>.Success(inputResult.Value.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing duplicate dialog for job profile ID: {SourceProfileId}", request.SourceProfileId);
            return ProfileDialogResult<string>.Failure($"Error duplicating profile: {ex.Message}");
        }
    }

    #endregion

    #region Private Implementation

    private void SetupSideMenuItems()
    {
        SideMenuItems.Clear();
        SideMenuItems.Add("Main View");
        SideMenuItems.Add("Create (Wizard)");
        SideMenuItems.Add("Edit (Wizard)");
    }

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

        // Override the Create command to navigate to wizard instead of direct dialog
        CreateWizardCommand = ReactiveCommand.Create(ExecuteCreateWizard);

        // Subscribe to command execution for logging
        CreateFromTemplateCommand.Subscribe(_ => _logger.LogDebug("Create from template command executed")).DisposeWith(_localDisposables);
        SaveAsTemplateCommand.Subscribe(_ => _logger.LogDebug("Save as template command executed for job {JobId}", SelectedProfile?.Id)).DisposeWith(_localDisposables);
        ImportJobCommand.Subscribe(_ => _logger.LogDebug("Import job command executed")).DisposeWith(_localDisposables);
        ExportJobCommand.Subscribe(_ => _logger.LogDebug("Export job command executed for job {JobId}", SelectedProfile?.Id)).DisposeWith(_localDisposables);
        CreateWizardCommand.Subscribe(_ => _logger.LogDebug("Create wizard command executed")).DisposeWith(_localDisposables);
    }

    private void SetupJobCollections()
    {
        // Job collections are updated automatically when Profiles collection changes
        // No additional reactive subscriptions needed for filtering
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

    #region Content ViewModel Creation (Settings Pattern)

    private object? CreateWizardViewModel()
    {
        try
        {
            // Create a new JobWizardViewModel for creating a new job
            _logger.LogInformation("Creating new job wizard");

            // Try to use the factory if available, otherwise fall back to placeholder
            if (_viewModelFactory != null)
            {
                try
                {
                    var wizard = _viewModelFactory.Create<JobWizardViewModel>();
                    _logger.LogDebug("Successfully created JobWizardViewModel via factory");

                    // Subscribe to wizard completion to auto-refresh
                    wizard.WhenAnyValue(w => w.Completed)
                        .Where(completed => completed)
                        .Take(1) // Only handle the first completion
                        .Subscribe(async _ =>
                        {
                            try
                            {
                                _logger.LogInformation("Wizard completed, refreshing jobs list and selecting created job");

                                // Wait a moment for the job to be fully persisted
                                await Task.Delay(500);

                                // Use the public refresh method
                                await RefreshJobsAsync();

                                // If a job was created, select it after refresh
                                if (wizard.CreatedJobId.HasValue)
                                {
                                    await SelectCreatedJobAsync(wizard.CreatedJobId.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to handle wizard completion");
                            }
                        })
                        .DisposeWith(_localDisposables);

                    return wizard;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create JobWizardViewModel via factory, falling back to placeholder");
                }
            }

            // Fallback to placeholder when factory is not available or fails
            return new JobWizardPlaceholderViewModel("Create New Job", "Use the job creation wizard to create a new job profile with guided setup.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job wizard");
            return CreateMainJobsContentViewModel(); // Fallback to main view
        }
    }

    private object? CreateWizardViewModelFromSelected()
    {
        try
        {
            if (SelectedProfile == null)
            {
                StatusMessage = "No job selected to edit";
                return CreateMainJobsContentViewModel(); // Fallback to main view
            }

            // Create a JobWizardViewModel pre-populated with selected job data
            _logger.LogInformation("Creating job wizard from selected job {JobId}", SelectedProfile.Id);

            // Try to use the factory if available, otherwise fall back to placeholder
            if (_viewModelFactory != null)
            {
                try
                {
                    var wizard = _viewModelFactory.Create<JobWizardViewModel>();

                    // Pre-populate the wizard with selected job data
                    wizard.PreselectJobName = SelectedProfile.Name + " (Copy)";
                    wizard.PreselectJobDescription = SelectedProfile.Description;
                    wizard.PreselectSerialId = SelectedProfile.SerialProfileId;
                    wizard.PreselectSocatId = SelectedProfile.SocatProfileId;
                    wizard.PreselectPowerId = SelectedProfile.PowerSupplyProfileId;
                    wizard.IsEditMode = true;

                    _logger.LogDebug("Successfully created JobWizardViewModel for editing job {JobId}", SelectedProfile.Id);
                    return wizard;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create JobWizardViewModel via factory for editing, falling back to placeholder");
                }
            }

            // Fallback to placeholder when factory is not available or fails
            return new JobWizardPlaceholderViewModel($"Edit Job: {SelectedProfile.Name}",
                $"Use the job editing wizard to modify the job profile '{SelectedProfile.Name}' with guided setup.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job wizard from selected job");
            return CreateMainJobsContentViewModel(); // Fallback to main view
        }
    }

    private object CreateMainJobsContentViewModel()
    {
        try
        {
            // Create a dedicated content ViewModel for the main jobs view
            // This prevents circular references by not returning 'this'
            _logger.LogDebug("Creating main jobs content ViewModel");

            // Return wrapper ViewModel that will be resolved to JobsMainContentView by ViewLocator
            return new JobsMainContentViewModel(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create main jobs content ViewModel");
            return new JobsMainContentViewModel(this); // Safe fallback
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

    private void ExecuteCreateWizard()
    {
        try
        {
            _logger.LogInformation("Navigating to create wizard");

            // Navigate to the wizard by setting the sidebar selection
            SelectedSideMenuItem = "Create (Wizard)";

            StatusMessage = "Opening job creation wizard...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to create wizard");
            StatusMessage = "Error opening wizard";
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

    /// <summary>
    /// Public method to refresh the jobs list from external callers (like wizard completion).
    /// </summary>
    public async Task RefreshJobsAsync()
    {
        try
        {
            _logger.LogDebug("Refreshing jobs list");

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                RefreshCommand.Execute().Subscribe();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh jobs list");
        }
    }

    /// <summary>
    /// Selects a job by ID after a short delay to allow for the collection to be refreshed.
    /// </summary>
    /// <param name="jobId">The ID of the job to select.</param>
    private async Task SelectCreatedJobAsync(int jobId)
    {
        try
        {
            // Wait a bit for the refresh to complete
            await Task.Delay(500);

            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                var jobToSelect = Profiles.FirstOrDefault(j => j.Id == jobId);
                if (jobToSelect != null)
                {
                    SelectedProfile = jobToSelect;
                    _logger.LogDebug("Selected newly created job {JobId}", jobId);
                }
                else
                {
                    _logger.LogWarning("Could not find newly created job {JobId} for selection", jobId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select newly created job {JobId}", jobId);
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
