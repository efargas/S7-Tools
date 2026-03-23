using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Validation;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Jobs;

namespace S7Tools.ViewModels.Jobs;

/// <summary>
/// Wrapper ViewModel for JobsMainContentView to prevent circular references.
/// This exposes the JobsManagementViewModel properties but is a separate object.
/// </summary>
public class JobsMainContentViewModel : ViewModelBase, IDisposable
{
    private readonly JobsManagementViewModel _parent;
    private readonly CompositeDisposable _disposables = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsMainContentViewModel"/> class.
    /// </summary>
    /// <param name="parent">The parent <see cref="JobsManagementViewModel"/> whose state and commands are forwarded.</param>
    public JobsMainContentViewModel(JobsManagementViewModel parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));

        // Subscribe to parent property changes and re-raise them
        // Use proper property change forwarding to ensure UI updates
        _parent.WhenAnyValue(x => x.SelectedProfile)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedProfile)))
            .DisposeWith(_disposables);

        _parent.WhenAnyValue(x => x.Profiles)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(Profiles)))
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

    /// <summary>
    /// Gets the JobInfoDisplayViewModel for the job details panel.
    /// </summary>
    public ViewModels.Jobs.JobInfoDisplayViewModel? JobInfoDisplayViewModel { get; internal set; }

    // Expose parent properties for data binding
    /// <summary>Gets the collection of all job profiles.</summary>
    public ObservableCollection<JobProfile> Profiles => _parent.Profiles;

    /// <summary>Gets or sets the currently selected job profile.</summary>
    public JobProfile? SelectedProfile
    {
        get => _parent.SelectedProfile;
        set => _parent.SelectedProfile = value;
    }

    /// <summary>Gets the collection of all job profiles.</summary>
    public ObservableCollection<JobProfile> AllJobs => _parent.AllJobs;

    /// <summary>Gets the collection of job template profiles.</summary>
    public ObservableCollection<JobProfile> JobTemplates => _parent.JobTemplates;

    /// <summary>Gets the collection of user-created job profiles.</summary>
    public ObservableCollection<JobProfile> UserJobs => _parent.UserJobs;

    /// <summary>Gets the current status message from the parent ViewModel.</summary>
    public string StatusMessage => _parent.StatusMessage ?? string.Empty;

    /// <summary>Gets a value indicating whether the parent ViewModel is loading data.</summary>
    public bool IsLoading => _parent.IsLoading;

    // Expose parent commands
    /// <summary>Gets the command to open the job creation wizard.</summary>
    public ReactiveCommand<Unit, Unit> CreateCommand => _parent.CreateWizardCommand; // Use wizard instead of base create

    /// <summary>Gets the command to open the edit wizard for the selected job.</summary>
    public ReactiveCommand<Unit, Unit> EditCommand => _parent.EditCommand;

    /// <summary>Gets the command to duplicate the selected job profile.</summary>
    public ReactiveCommand<Unit, Unit> DuplicateCommand => _parent.DuplicateCommand;

    /// <summary>Gets the command to delete the selected job profile.</summary>
    public ReactiveCommand<Unit, Unit> DeleteCommand => _parent.DeleteCommand;

    /// <summary>Gets the command to refresh the list of job profiles.</summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand => _parent.RefreshCommand;

    /// <summary>Gets the command to set the selected job as the default profile.</summary>
    public ReactiveCommand<Unit, Unit> SetDefaultCommand => _parent.SetDefaultCommand;

    // Job-specific commands
    /// <summary>Gets the command to create a new job from a template.</summary>
    public ReactiveCommand<Unit, Unit> CreateFromTemplateCommand => _parent.CreateFromTemplateCommand;

    /// <summary>Gets the command to save the selected job as a template.</summary>
    public ReactiveCommand<Unit, Unit> SaveAsTemplateCommand => _parent.SaveAsTemplateCommand;

    /// <summary>Gets the command to import job profiles from a file.</summary>
    public ReactiveCommand<Unit, Unit> ImportJobCommand => _parent.ImportJobCommand;

    /// <summary>Gets the command to export the selected job profile to a file.</summary>
    public ReactiveCommand<Unit, Unit> ExportJobCommand => _parent.ExportJobCommand;

    /// <summary>Gets the command to create a new task from the selected job.</summary>
    public ReactiveCommand<Unit, Unit> CreateTaskFromJobCommand => _parent.CreateTaskFromJobCommand;

    /// <summary>Gets the command to schedule a task from the selected job.</summary>
    public ReactiveCommand<Unit, Unit> ScheduleTaskFromJobCommand => _parent.ScheduleTaskFromJobCommand;

    /// <summary>Gets the command to enqueue a task from the selected job.</summary>
    public ReactiveCommand<Unit, Unit> EnqueueTaskFromJobCommand => _parent.EnqueueTaskFromJobCommand;

    /// <summary>Gets the command to validate the selected job configuration.</summary>
    public ReactiveCommand<Unit, Unit> ValidateJobCommand => _parent.ValidateJobCommand;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed resources used by this ViewModel.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release managed resources.</param>
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
public class JobsManagementViewModel : ProfileManagementViewModelBase<JobProfile>, IDockableViewModel
{
    // IDockableViewModel implementation
    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "Jobs";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Jobs Management";
    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;
    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    private readonly IJobManager _jobManager;
    private readonly ILogger<JobsManagementViewModel> _logger;
    private readonly IUIThreadService _uiThreadService;
    private readonly IUnifiedProfileDialogService _unifiedDialogService;
    private readonly IDialogService _dialogService;
    private readonly IViewModelFactory? _viewModelFactory;
    private readonly ITaskScheduler? _taskScheduler;
    private readonly IActivityBarService? _activityBarService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly CompositeDisposable _localDisposables = [];
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    // Job-specific collections for UI organization
    private ObservableCollection<JobProfile> _allJobs = [];
    private ObservableCollection<JobProfile> _jobTemplates = [];
    private ObservableCollection<JobProfile> _userJobs = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsManagementViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger for this view model.</param>
    /// <param name="jobManager">The job manager service for job operations.</param>
    /// <param name="profileDialogService">The unified profile dialog service.</param>
    /// <param name="dialogService">The general dialog service for confirmations.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    /// <param name="viewModelFactory">The view model factory for creating child ViewModels.</param>
    /// <param name="taskScheduler">The task scheduler service for creating tasks from jobs.</param>
    /// <param name="activityBarService">The activity bar service for navigation.</param>
    /// <param name="fileDialogService">The file dialog service for import/export operations.</param>
    public JobsManagementViewModel(
        ILogger<JobsManagementViewModel> logger,
        IJobManager jobManager,
        IUnifiedProfileDialogService profileDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService,
        IViewModelFactory? viewModelFactory = null,
        ITaskScheduler? taskScheduler = null,
        IActivityBarService? activityBarService = null,
        IFileDialogService? fileDialogService = null)
        : base(logger, profileDialogService, dialogService, uiThreadService, fileDialogService!)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(jobManager);
        ArgumentNullException.ThrowIfNull(uiThreadService);
        ArgumentNullException.ThrowIfNull(profileDialogService);
        ArgumentNullException.ThrowIfNull(dialogService);

        _logger = logger;
        _jobManager = jobManager;
        _uiThreadService = uiThreadService;
        _unifiedDialogService = profileDialogService;
        _dialogService = dialogService;
        _viewModelFactory = viewModelFactory;
        _taskScheduler = taskScheduler;
        _activityBarService = activityBarService;
        _fileDialogService = fileDialogService;

        SetupJobSpecificCommands();
        SetupSideMenuItems();

        // Subscribe to profile changes to update job-specific collections
        this.WhenAnyValue(x => x.Profiles)
            .Subscribe(_ => UpdateJobCollections())
            .DisposeWith(_localDisposables);

        // Load initial data on UI thread to avoid 'Call from invalid thread' errors
        // when updating ObservableCollections during initialization
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => await base.InitializeAsync());
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
    public ObservableCollection<string> SideMenuItems { get; } = [];

    private string? _selectedSideMenuItem = "Main View";
    /// <summary>
    /// Gets or sets the selected sidebar menu item.
    /// </summary>
    public string? SelectedSideMenuItem
    {
        get => _selectedSideMenuItem;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedSideMenuItem, value);
            this.RaisePropertyChanged(nameof(SelectedContentViewModel));
        }
    }

    /// <summary>
    /// Gets the selected content ViewModel for the main area based on sidebar selection.
    /// This follows the same pattern as SettingsViewModel.SelectedCategoryViewModel.
    /// </summary>
    public object? SelectedContentViewModel => SelectedSideMenuItem switch
    {
        "Create (Wizard)" => CreateWizardViewModel(),
        "Edit (Wizard)" => CreateWizardViewModelFromSelected(),
        _ => CreateMainJobsContentViewModel() // Main View uses a dedicated ViewModel
    };

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
    /// Gets the command to schedule a task from the selected job.
    /// </summary>
    /// <remarks>
    /// Creates a scheduled task execution from the selected job profile.
    /// Allows specifying execution date/time for future job runs.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> ScheduleTaskFromJobCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to enqueue a task from the selected job.
    /// </summary>
    /// <remarks>
    /// Creates an enqueued task execution from the selected job profile.
    /// Task is immediately added to the execution queue with normal priority.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> EnqueueTaskFromJobCommand { get; private set; } = null!;

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
            global::S7Tools.ViewModels.Dialogs.Models.InputResult inputResult = await _dialogService.ShowInputAsync(
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
        // Intercept Edit button clicks and navigate to Edit (Wizard) instead of showing edit dialog
        EditCommand.Subscribe(_ =>
        {
            if (SelectedProfile != null)
            {
                _logger.LogInformation("Edit button clicked: Navigating to Edit (Wizard) for job {JobId}", SelectedProfile.Id);
                SelectedSideMenuItem = "Edit (Wizard)";
            }
        }).DisposeWith(_localDisposables);

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
        ScheduleTaskFromJobCommand = ReactiveCommand.CreateFromTask(ExecuteScheduleTaskFromJobAsync, hasSelectedJob);
        EnqueueTaskFromJobCommand = ReactiveCommand.CreateFromTask(ExecuteEnqueueTaskFromJobAsync, hasSelectedJob);
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
                    JobWizardViewModel wizard = _viewModelFactory.Create<JobWizardViewModel>();
                    _logger.LogDebug("Successfully created JobWizardViewModel via factory");

                    // Subscribe to wizard completion to auto-refresh
                    wizard.WhenAnyValue(w => w.Completed)
                        .Where(completed => completed)
                        .Take(1) // Only handle the first completion
                        .Subscribe(async _ =>
                        {
                            try
                            {
                                _logger.LogInformation("Wizard completed, refreshing jobs list and navigating to Main View");

                                // Navigate back to Main View
                                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                                {
                                    SelectedSideMenuItem = "Main View";
                                }).ConfigureAwait(false);

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

                    // Subscribe to cancellation to navigate back
                    wizard.WhenAnyValue(w => w.CancelRequested)
                        .Where(cancel => cancel)
                        .Take(1)
                        .Subscribe(async _ =>
                        {
                            _logger.LogInformation("Wizard cancelled");
                            await _uiThreadService.InvokeOnUIThreadAsync(() =>
                            {
                                SelectedSideMenuItem = "Main View";
                            });
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
                StatusMessage = UIStrings.Status_NoJobSelectedToEdit;
                return CreateMainJobsContentViewModel(); // Fallback to main view
            }

            // Create a JobWizardViewModel pre-populated with selected job data
            _logger.LogInformation("Creating job wizard from selected job {JobId}", SelectedProfile.Id);

            // Try to use the factory if available, otherwise fall back to placeholder
            if (_viewModelFactory != null)
            {
                try
                {
                    JobWizardViewModel wizard = _viewModelFactory.Create<JobWizardViewModel>();

                    // Subscribe to wizard completion to navigate back and refresh
                    wizard.WhenAnyValue(w => w.Completed)
                        .Where(completed => completed)
                        .Take(1)
                        .Subscribe(async _ =>
                        {
                            try
                            {
                                _logger.LogInformation("Edit wizard completed, refreshing jobs list and navigating to Main View");

                                // Navigate back to Main View
                                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                                {
                                    SelectedSideMenuItem = "Main View";
                                }).ConfigureAwait(false);

                                // Wait a moment for changes to be persisted
                                await Task.Delay(500);

                                // Refresh jobs list
                                await RefreshJobsAsync();

                                // Keep the edited job selected
                                int? editedJobId = SelectedProfile?.Id;
                                if (editedJobId.HasValue)
                                {
                                    SelectedProfile = Profiles.FirstOrDefault(p => p.Id == editedJobId.Value);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to handle edit wizard completion");
                            }
                        })
                        .DisposeWith(_localDisposables);

                    // Subscribe to cancellation to navigate back
                    wizard.WhenAnyValue(w => w.CancelRequested)
                        .Where(cancel => cancel)
                        .Take(1)
                        .Subscribe(async _ =>
                        {
                            _logger.LogInformation("Edit wizard cancelled");
                            await _uiThreadService.InvokeOnUIThreadAsync(() =>
                            {
                                SelectedSideMenuItem = "Main View";
                            });
                        })
                        .DisposeWith(_localDisposables);

                    // Load the selected job into the wizard for editing
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await wizard.LoadJobForEditAsync(SelectedProfile.Id).ConfigureAwait(false);
                            _logger.LogDebug("Successfully loaded job {JobId} into wizard for editing", SelectedProfile.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to load job {JobId} into wizard", SelectedProfile.Id);
                        }
                    });

                    _logger.LogDebug("Successfully created JobWizardViewModel for editing job {JobId}", SelectedProfile.Id);
                    return wizard;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create JobWizardViewModel via factory for editing, falling back to placeholder");
                }
            }

            // Fallback to placeholder when factory is not available or fails
            return new JobWizardPlaceholderViewModel($"Edit Job: {SelectedProfile?.Name ?? "Unknown"}",
                $"Use the job editing wizard to modify the job profile '{SelectedProfile?.Name ?? "Unknown"}' with guided setup.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job wizard from selected job");
            return CreateMainJobsContentViewModel(); // Fallback to main view
        }
    }

    private JobsMainContentViewModel CreateMainJobsContentViewModel()
    {
        try
        {
            // Create a dedicated content ViewModel for the main jobs view
            // This prevents circular references by not returning 'this'
            _logger.LogDebug("Creating main jobs content ViewModel");

            // Create the wrapper ViewModel
            var contentViewModel = new JobsMainContentViewModel(this);

            // Create the JobInfoDisplayViewModel using the factory if available
            if (_viewModelFactory != null)
            {
                try
                {
                    contentViewModel.JobInfoDisplayViewModel = _viewModelFactory.Create<JobInfoDisplayViewModel>();
                    _logger.LogDebug("Successfully created JobInfoDisplayViewModel via factory");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create JobInfoDisplayViewModel via factory");
                }
            }
            else
            {
                _logger.LogDebug("ViewModelFactory not available for JobInfoDisplayViewModel creation");
            }

            return contentViewModel;
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
            StatusMessage = UIStrings.Status_CreatingJobFromTemplate;

            // Check if templates are available
            if (!JobTemplates.Any())
            {
                StatusMessage = UIStrings.Status_NoTemplatesAvailable;
                await _dialogService.ShowErrorAsync("No Templates",
                    "No job templates are available. Please save a job as a template first.");
                return;
            }

            // Show template selection dialog
            string templateListText = string.Join("\n", JobTemplates.Select((t, i) => $"{i + 1}. {t.Name}"));
            string message = $"Select a template number:\n\n{templateListText}";

            global::S7Tools.ViewModels.Dialogs.Models.InputResult inputResult = await _dialogService.ShowInputAsync(
                "Select Template",
                message,
                "1",
                "Enter template number").ConfigureAwait(false);

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                StatusMessage = "Operation cancelled";
                return;
            }

            // Parse template selection
            if (!int.TryParse(inputResult.Value, out int templateIndex) ||
                templateIndex < 1 || templateIndex > JobTemplates.Count)
            {
                StatusMessage = "Invalid template selection";
                await _dialogService.ShowErrorAsync("Invalid Selection",
                    $"Please enter a valid template number between 1 and {JobTemplates.Count}");
                return;
            }

            JobProfile template = JobTemplates[templateIndex - 1];

            // Ask for new job name
            global::S7Tools.ViewModels.Dialogs.Models.InputResult nameResult = await _dialogService.ShowInputAsync(
                "New Job Name",
                $"Enter a name for the job created from template '{template.Name}':",
                $"Job from {template.Name}",
                "Enter job name").ConfigureAwait(false);

            if (nameResult.IsCancelled || string.IsNullOrWhiteSpace(nameResult.Value))
            {
                StatusMessage = "Operation cancelled";
                return;
            }

            string newName = nameResult.Value;
            JobProfile newJob = await _jobManager.CreateFromTemplateAsync(template.Id, newName);

            StatusMessage = string.Format(UIStrings.Status_CreatedJobFromTemplate, newJob.Name, template.Name);
            _logger.LogInformation("Created job {JobId} ({JobName}) from template {TemplateId} ({TemplateName})",
                newJob.Id, newJob.Name, template.Id, template.Name);

            // Refresh profiles and select the new job
            await LoadProfilesAsync();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == newJob.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorCreatingJobFromTemplate, ex.Message);
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
            StatusMessage = UIStrings.Status_SavingJobAsTemplate;

            // Show template name input dialog
            global::S7Tools.ViewModels.Dialogs.Models.InputResult nameResult = await _dialogService.ShowInputAsync(
                "Save as Template",
                $"Enter a name for the template based on job '{SelectedProfile.Name}':",
                $"{SelectedProfile.Name} Template",
                "Enter template name").ConfigureAwait(false);

            if (nameResult.IsCancelled || string.IsNullOrWhiteSpace(nameResult.Value))
            {
                StatusMessage = "Operation cancelled";
                return;
            }

            string templateName = nameResult.Value;

            // Mark the job as a template
            bool success = await _jobManager.SetAsTemplateAsync(SelectedProfile.Id, true);

            if (success)
            {
                // If the user wants a different name, update it after marking as template
                if (templateName != SelectedProfile.Name)
                {
                    SelectedProfile.Name = templateName;
                    await _jobManager.UpdateAsync(SelectedProfile);
                }

                StatusMessage = $"{UIStrings.Status_JobSavedAsTemplate} {templateName}";
                _logger.LogInformation("Saved job {JobId} as template with name: {TemplateName}",
                    SelectedProfile.Id, templateName);

                // Refresh to update template collections
                await LoadProfilesAsync();
            }
            else
            {
                StatusMessage = string.Format(UIStrings.Status_FailedToSaveJobAsTemplate, SelectedProfile.Name);
                _logger.LogWarning("Failed to save job {JobId} ({JobName}) as template",
                    SelectedProfile.Id, SelectedProfile.Name);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorSavingJobAsTemplate, ex.Message);
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
            StatusMessage = UIStrings.Status_ImportingJobs;

            // Check if file dialog service is available
            if (_fileDialogService == null)
            {
                StatusMessage = "File dialog service not available";
                await _dialogService.ShowErrorAsync("Service Unavailable",
                    "File import functionality requires the file dialog service.");
                _logger.LogError("FileDialogService not available - was not injected in constructor");
                return;
            }

            // Show open file dialog
            string? filePath = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Job Profile",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                null).ConfigureAwait(false);

            if (string.IsNullOrEmpty(filePath))
            {
                StatusMessage = "Operation cancelled";
                _logger.LogInformation("Job import cancelled by user");
                return;
            }

            // Read and deserialize the job profile
            JobProfile? importedJob = null;
            string jsonContent = string.Empty;
            try
            {
                jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                importedJob = System.Text.Json.JsonSerializer.Deserialize<JobProfile>(jsonContent);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                StatusMessage = "File not found";
                await _dialogService.ShowErrorAsync("Import Failed",
                    $"The selected file could not be found:\n{ex.Message}");
                _logger.LogWarning(ex, "File not found during job import: {FilePath}", filePath);
                return;
            }
            catch (System.UnauthorizedAccessException ex)
            {
                StatusMessage = "Access denied";
                await _dialogService.ShowErrorAsync("Import Failed",
                    $"Access to the selected file was denied:\n{ex.Message}");
                _logger.LogWarning(ex, "Unauthorized access during job import: {FilePath}", filePath);
                return;
            }
            catch (System.IO.IOException ex)
            {
                StatusMessage = "File I/O error";
                await _dialogService.ShowErrorAsync("Import Failed",
                    $"An error occurred while reading the file:\n{ex.Message}");
                _logger.LogWarning(ex, "I/O error during job import: {FilePath}", filePath);
                return;
            }
            catch (System.Text.Json.JsonException ex)
            {
                StatusMessage = $"Invalid JSON format: {ex.Message}";
                await _dialogService.ShowErrorAsync("Import Failed",
                    $"The selected file contains invalid JSON:\n{ex.Message}");
                _logger.LogWarning(ex, "JSON deserialization error during job import: {FilePath}", filePath);
                return;
            }

            if (importedJob == null)
            {
                StatusMessage = "Failed to import job - file contains null or invalid data";
                await _dialogService.ShowErrorAsync("Import Failed",
                    "The selected file does not contain a valid job profile.");
                return;
            }

            // Reset ID for import (will be assigned new ID)
            importedJob.Id = 0;
            importedJob.CreatedAt = DateTime.UtcNow;
            importedJob.ModifiedAt = DateTime.UtcNow;

            // Add the imported job
            JobProfile addedJob = await _jobManager.CreateAsync(importedJob);

            StatusMessage = $"Job '{addedJob.Name}' imported successfully";
            _logger.LogInformation("Job imported from {FilePath}: {JobName} (ID: {JobId})",
                filePath, addedJob.Name, addedJob.Id);

            // Refresh and select the imported job
            await LoadProfilesAsync();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == addedJob.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorImportingJobs, ex.Message);
            _logger.LogError(ex, "Error importing job");
            await _dialogService.ShowErrorAsync("Import Error", string.Format(UIStrings.Status_ImportFailed, ex.Message));
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
            StatusMessage = UIStrings.Status_ExportingJob;

            // Check if file dialog service is available
            if (_fileDialogService == null)
            {
                StatusMessage = "File dialog service not available";
                await _dialogService.ShowErrorAsync("Service Unavailable",
                    "File export functionality requires the file dialog service.");
                _logger.LogError("FileDialogService not available - was not injected in constructor");
                return;
            }

            // Show save file dialog
            string defaultFileName = $"{SelectedProfile.Name.Replace(" ", "_")}.json";
            string? filePath = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Job Profile",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                null,
                defaultFileName).ConfigureAwait(false);

            if (string.IsNullOrEmpty(filePath))
            {
                StatusMessage = "Operation cancelled";
                _logger.LogInformation("Job export cancelled by user");
                return;
            }

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(SelectedProfile, JsonOptions);
            await System.IO.File.WriteAllTextAsync(filePath, jsonContent);

            StatusMessage = $"Job '{SelectedProfile.Name}' exported successfully";
            _logger.LogInformation("Job {JobId} ({JobName}) exported to {FilePath}",
                SelectedProfile.Id, SelectedProfile.Name, filePath);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorExportingJob, ex.Message);
            _logger.LogError(ex, "Error exporting job {JobId}", SelectedProfile?.Id);
            await _dialogService.ShowErrorAsync("Export Error", string.Format(UIStrings.Status_ExportFailed, ex.Message));
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
            StatusMessage = UIStrings.Status_CreatingTaskFromJob;

            // Check if task scheduler is available
            if (_taskScheduler == null)
            {
                StatusMessage = "Task scheduler service not available";
                _logger.LogError("TaskScheduler service not available - was not injected in constructor");
                return;
            }

            // Create task from selected job profile with Normal priority
            Core.Models.Jobs.TaskExecution createdTask = await _taskScheduler.CreateTaskAsync(
                SelectedProfile,
                Core.Models.Jobs.TaskPriority.Normal).ConfigureAwait(false);

            StatusMessage = $"Task created successfully from job '{SelectedProfile.Name}' (Task ID: {createdTask.TaskId})";
            _logger.LogInformation("Task {TaskId} created from job {JobId} ({JobName})",
                createdTask.TaskId, SelectedProfile.Id, SelectedProfile.Name);

            // Show confirmation dialog with option to navigate to Task Manager
            bool navigateToTaskManager = await _dialogService.ShowConfirmationAsync(
                "Task Created",
                $"Task has been created successfully from job '{SelectedProfile.Name}'.\n\n" +
                $"Task ID: {createdTask.TaskId}\n" +
                $"State: {createdTask.State}\n\n" +
                $"Would you like to navigate to the Task Manager to view and start the task?").ConfigureAwait(false);

            if (navigateToTaskManager)
            {
                // Navigate to Task Manager using IActivityBarService
                if (_activityBarService != null)
                {
                    await _uiThreadService.InvokeOnUIThreadAsync(() =>
                    {
                        bool navigationSuccess = _activityBarService.SelectItem("taskmanager");
                        if (navigationSuccess)
                        {
                            _logger.LogInformation("Successfully navigated to Task Manager");
                            StatusMessage = "Navigated to Task Manager";
                        }
                        else
                        {
                            _logger.LogWarning("Failed to navigate to Task Manager - activity bar item not found");
                            StatusMessage = "Task Manager navigation failed - please use Activity Bar";
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("Activity bar service not available for navigation");
                    StatusMessage = "Task Manager navigation not available - please use Activity Bar";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorCreatingTaskFromJob, ex.Message);
            _logger.LogError(ex, "Error creating task from job {JobId}", SelectedProfile?.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteScheduleTaskFromJobAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Scheduling task from job...";

            // Check if task scheduler is available
            if (_taskScheduler == null)
            {
                StatusMessage = "Task scheduler service not available";
                _logger.LogError("TaskScheduler service not available - was not injected in constructor");
                return;
            }

            // Show date/time picker dialog using input dialog
            string currentTime = DateTime.UtcNow.ToLocalTime().AddMinutes(5).ToString(S7Tools.Constants.AppConstants.StandardUserInputDateFormat);
            global::S7Tools.ViewModels.Dialogs.Models.InputResult inputResult = await _dialogService.ShowInputAsync(
                "Schedule Task",
                $"Enter the scheduled execution time for job '{SelectedProfile.Name}':\n\nFormat: {S7Tools.Constants.AppConstants.StandardUserInputDateFormat} (24-hour format)",
                currentTime,
                S7Tools.Constants.AppConstants.StandardUserInputDateFormat).ConfigureAwait(false);

            if (inputResult.IsCancelled || string.IsNullOrWhiteSpace(inputResult.Value))
            {
                StatusMessage = "Operation cancelled";
                _logger.LogInformation("Task scheduling cancelled by user");
                return;
            }

            // Parse the scheduled time
            if (!DateTime.TryParseExact(inputResult.Value, S7Tools.Constants.AppConstants.StandardUserInputDateFormat,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime scheduledTime))
            {
                StatusMessage = "Invalid date/time format";
                await _dialogService.ShowErrorAsync("Invalid Format",
                    $"Please enter the date and time in the format: {S7Tools.Constants.AppConstants.StandardUserInputDateFormat}\nExample: 2025-11-21 14:30");
                return;
            }

            DateTime validationTime = DateTime.UtcNow;
            // Check if scheduled time is in the past (allow 1-minute tolerance for "now")
            if (scheduledTime < validationTime.AddMinutes(-1))
            {
                bool confirmPast = await _dialogService.ShowConfirmationAsync(
                    "Past Time Detected",
                    $"The specified time ({scheduledTime:yyyy-MM-dd HH:mm}) is in the past.\n\n" +
                    "The task will be queued immediately. Continue?").ConfigureAwait(false);

                if (!confirmPast)
                {
                    StatusMessage = "Operation cancelled";
                    return;
                }
            }

            // Create task from selected job profile with scheduled time
            Core.Models.Jobs.TaskExecution createdTask = await _taskScheduler.CreateTaskAsync(
                SelectedProfile,
                Core.Models.Jobs.TaskPriority.Normal).ConfigureAwait(false);

            // Schedule the task
            await _taskScheduler.ScheduleTaskAsync(createdTask.TaskId, scheduledTime).ConfigureAwait(false);

            StatusMessage = $"Task scheduled successfully from job '{SelectedProfile.Name}' at {scheduledTime:g}";
            _logger.LogInformation("Task {TaskId} scheduled from job {JobId} ({JobName}) for {ScheduledTime}",
                createdTask.TaskId, SelectedProfile.Id, SelectedProfile.Name, scheduledTime);

            // Show confirmation dialog
            await _dialogService.ShowConfirmationAsync(
                "Task Scheduled",
                $"Task has been scheduled successfully from job '{SelectedProfile.Name}'.\n\n" +
                $"Task ID: {createdTask.TaskId}\n" +
                $"Scheduled for: {scheduledTime:g}\n\n" +
                $"The task will run automatically at the scheduled time.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scheduling task from job: {ex.Message}";
            _logger.LogError(ex, "Error scheduling task from job {JobId}", SelectedProfile?.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteEnqueueTaskFromJobAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Enqueueing task from job...";

            // Check if task scheduler is available
            if (_taskScheduler == null)
            {
                StatusMessage = "Task scheduler service not available";
                _logger.LogError("TaskScheduler service not available - was not injected in constructor");
                return;
            }

            // Create task from selected job profile with Normal priority
            Core.Models.Jobs.TaskExecution createdTask = await _taskScheduler.CreateTaskAsync(
                SelectedProfile,
                Core.Models.Jobs.TaskPriority.Normal).ConfigureAwait(false);

            // Enqueue the task immediately
            await _taskScheduler.EnqueueTaskAsync(createdTask.TaskId).ConfigureAwait(false);

            StatusMessage = $"Task enqueued successfully from job '{SelectedProfile.Name}' (Task ID: {createdTask.TaskId})";
            _logger.LogInformation("Task {TaskId} enqueued from job {JobId} ({JobName})",
                createdTask.TaskId, SelectedProfile.Id, SelectedProfile.Name);

            // Show confirmation dialog with option to navigate to Task Manager
            bool navigateToTaskManager = await _dialogService.ShowConfirmationAsync(
                "Task Enqueued",
                $"Task has been enqueued successfully from job '{SelectedProfile.Name}'.\n\n" +
                $"Task ID: {createdTask.TaskId}\n" +
                $"State: {createdTask.State}\n\n" +
                $"The task is now in the execution queue and will start when resources are available.\n\n" +
                $"Would you like to navigate to the Task Manager to monitor the task?").ConfigureAwait(false);

            if (navigateToTaskManager)
            {
                // Navigate to Task Manager using activity bar service
                if (_activityBarService != null)
                {
                    _activityBarService.SelectItem("TaskManager");
                    _logger.LogInformation("Navigated to Task Manager activity");
                    StatusMessage = "Navigated to Task Manager";
                }
                else
                {
                    _logger.LogWarning("ActivityBarService not available - cannot navigate to Task Manager");
                    StatusMessage = "Activity bar service not available - please use Activity Bar manually";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error enqueueing task from job: {ex.Message}";
            _logger.LogError(ex, "Error enqueueing task from job {JobId}", SelectedProfile?.Id);
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
            StatusMessage = UIStrings.Status_ValidatingJob;

            ValidationResult validationResult = await _jobManager.ValidateJobAsync(SelectedProfile);
            await Task.Yield();

            if (validationResult.IsValid)
            {
                StatusMessage = string.Format(UIStrings.Status_JobIsValid, SelectedProfile.Name);
                _logger.LogInformation("Job {JobId} ({JobName}) validation passed",
                    SelectedProfile.Id, SelectedProfile.Name);
            }
            else
            {
                string errorSummary = string.Join(", ", validationResult.Errors.Take(3));
                StatusMessage = string.Format(UIStrings.Status_JobHasValidationErrors, SelectedProfile.Name, errorSummary);
                _logger.LogWarning("Job {JobId} ({JobName}) validation failed: {Errors}",
                    SelectedProfile.Id, SelectedProfile.Name, string.Join("; ", validationResult.Errors));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorValidatingJob, ex.Message);
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

            StatusMessage = UIStrings.Status_OpeningJobCreationWizard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to create wizard");
            StatusMessage = UIStrings.Status_ErrorOpeningWizard;
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
                JobProfile? jobToSelect = Profiles.FirstOrDefault(j => j.Id == jobId);
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
