using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Base;

/// <summary>
/// Base view model for unified profile management operations.
/// Provides common functionality for Serial, Socat, and Power Supply profile ViewModels.
/// </summary>
/// <typeparam name="TProfile">The type of profile managed by this view model.</typeparam>
/// <remarks>
/// This base class implements the unified profile management patterns:
/// - Dialog-only CRUD operations (no inline name/description editing)
/// - Consistent button layout (Create first, then Edit, Duplicate, Delete)
/// - Standardized default naming (SerialDefault/SocatDefault/PowerSupplyDefault)
/// - Enhanced DataGrid with ID column first and complete metadata
/// - Simplified duplicate workflow (input dialog → direct list addition)
///
/// Architecture principles:
/// - Template Method Pattern: Provides framework with derived class customization points
/// - Single Responsibility: Manages profile UI operations and state
/// - Open/Closed Principle: Open for extension via virtual methods, closed for modification
/// - Dependency Inversion: Depends on profile management abstractions
/// </remarks>
public abstract class ProfileManagementViewModelBase<TProfile> : ViewModelBase, IDisposable
    where TProfile : class, IProfileBase
{
    private readonly ILogger _logger;
    private readonly IUnifiedProfileDialogService _dialogService;
    private readonly IUIThreadService _uiThreadService;
    private readonly CompositeDisposable _disposables = new();

    // Profile collection and selection state
    private ObservableCollection<TProfile> _profiles = new();
    private TProfile? _selectedProfile;
    private bool _isLoading;
    private string? _statusMessage;

    // UI state for enhanced features
    private bool _hasChanges;
    private string _searchText = string.Empty;
    private string _profilesPath = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileManagementViewModelBase{TProfile}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this view model.</param>
    /// <param name="dialogService">The unified profile dialog service.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    protected ProfileManagementViewModelBase(
        ILogger logger,
        IUnifiedProfileDialogService dialogService,
        IUIThreadService uiThreadService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));

        SetupCommands();
        SetupValidation();
    }

    #region Properties

    /// <summary>
    /// Gets the collection of profiles displayed in the DataGrid.
    /// </summary>
    /// <remarks>
    /// Observable collection that automatically updates the UI when profiles are added, removed, or modified.
    /// Enhanced with ID column first and complete metadata display.
    /// </remarks>
    public ObservableCollection<TProfile> Profiles
    {
        get => _profiles;
        protected set => this.RaiseAndSetIfChanged(ref _profiles, value);
    }

    /// <summary>
    /// Gets or sets the currently selected profile in the DataGrid.
    /// </summary>
    /// <remarks>
    /// Drives the enable/disable state of Edit, Duplicate, and Delete commands.
    /// Used for context-sensitive operations.
    /// </remarks>
    public TProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether profile operations are in progress.
    /// </summary>
    /// <remarks>
    /// Used to show loading indicators and disable UI during async operations.
    /// Prevents concurrent modifications during dialog operations.
    /// </remarks>
    public bool IsLoading
    {
        get => _isLoading;
        protected set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Gets or sets the current status message for user feedback.
    /// </summary>
    /// <remarks>
    /// Displays operation results, validation errors, and general status information.
    /// Automatically cleared after successful operations.
    /// </remarks>
    public string? StatusMessage
    {
        get => _statusMessage;
        protected set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes.
    /// </summary>
    /// <remarks>
    /// In the new dialog-only approach, this primarily tracks profile list changes
    /// rather than inline editing changes.
    /// </remarks>
    public bool HasChanges
    {
        get => _hasChanges;
        protected set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
    }

    /// <summary>
    /// Gets or sets the search text for profile filtering.
    /// </summary>
    /// <remarks>
    /// Enables real-time filtering of the profile list based on name, description, or other properties.
    /// Empty string shows all profiles.
    /// </remarks>
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    /// <summary>
    /// Gets or sets the path where profiles are stored.
    /// </summary>
    /// <remarks>
    /// Displays the current storage location for user reference.
    /// Used by Browse, Open in Explorer, and Load Default operations.
    /// </remarks>
    public string ProfilesPath
    {
        get => _profilesPath;
        protected set => this.RaiseAndSetIfChanged(ref _profilesPath, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to create a new profile with dialog-based input.
    /// </summary>
    /// <remarks>
    /// New behavior: Opens dialog immediately with pre-populated default values.
    /// Button positioned first in the layout as per requirements.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> CreateCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to edit the selected profile via dialog.
    /// </summary>
    /// <remarks>
    /// Requires a selected profile to be enabled.
    /// Opens edit dialog with current profile data pre-populated.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> EditCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to duplicate the selected profile with simplified workflow.
    /// </summary>
    /// <remarks>
    /// New behavior: Input dialog for name → direct list addition (no edit dialog step).
    /// Auto-assigns free ID and adds directly to the profile list.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to delete the selected profile after confirmation.
    /// </summary>
    /// <remarks>
    /// Shows confirmation dialog before deletion.
    /// Respects read-only and default profile protection rules.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to refresh the profile list from storage.
    /// </summary>
    /// <remarks>
    /// Reloads profiles from persistent storage.
    /// Useful for recovering from external changes or resolving conflicts.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to browse for a different profiles directory.
    /// </summary>
    /// <remarks>
    /// Opens folder picker dialog to select new profile storage location.
    /// Updates ProfilesPath and reloads profiles from new location.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> BrowseCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to open the current profiles directory in the file explorer.
    /// </summary>
    /// <remarks>
    /// Opens the current ProfilesPath in the system file manager.
    /// Useful for manual profile file management or troubleshooting.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> OpenInExplorerCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to load default profiles from the application resources.
    /// </summary>
    /// <remarks>
    /// Restores factory default profiles to the current profiles directory.
    /// Shows confirmation dialog if existing profiles would be affected.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> LoadDefaultCommand { get; private set; } = null!;

    #endregion

    #region Abstract Methods - Customization Points

    /// <summary>
    /// Gets the profile manager for the specific profile type.
    /// </summary>
    /// <returns>The profile manager instance for CRUD operations.</returns>
    protected abstract IProfileManager<TProfile> GetProfileManager();

    /// <summary>
    /// Gets the default profile name for create operations.
    /// </summary>
    /// <returns>The standardized default name (SerialDefault/SocatDefault/PowerSupplyDefault).</returns>
    protected abstract string GetDefaultProfileName();

    /// <summary>
    /// Gets the profile type name for display and logging purposes.
    /// </summary>
    /// <returns>The human-readable profile type name.</returns>
    protected abstract string GetProfileTypeName();

    /// <summary>
    /// Creates a default profile instance with standard configuration.
    /// </summary>
    /// <returns>A new profile instance with default values.</returns>
    protected abstract TProfile CreateDefaultProfile();

    /// <summary>
    /// Shows the create dialog for the specific profile type.
    /// </summary>
    /// <param name="request">The create request with default values.</param>
    /// <returns>The dialog result with created profile or cancellation status.</returns>
    protected abstract Task<ProfileDialogResult<TProfile>> ShowCreateDialogAsync(ProfileCreateRequest request);

    /// <summary>
    /// Shows the edit dialog for the specific profile type.
    /// </summary>
    /// <param name="request">The edit request with profile ID.</param>
    /// <returns>The dialog result with updated profile or cancellation status.</returns>
    protected abstract Task<ProfileDialogResult<TProfile>> ShowEditDialogAsync(ProfileEditRequest request);

    /// <summary>
    /// Shows the duplicate input dialog for the specific profile type.
    /// </summary>
    /// <param name="request">The duplicate request with source profile ID and suggested name.</param>
    /// <returns>The dialog result with new profile name or cancellation status.</returns>
    protected abstract Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request);

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the view model and loads initial profile data.
    /// </summary>
    /// <remarks>
    /// Called after construction to perform async initialization.
    /// Loads profiles from storage and sets up initial UI state.
    /// </remarks>
    public virtual async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading profiles...";

            await LoadProfilesAsync().ConfigureAwait(false);
            await LoadProfilesPathAsync().ConfigureAwait(false);

            StatusMessage = $"Loaded {Profiles.Count} {GetProfileTypeName().ToLowerInvariant()} profiles.";
            _logger.LogInformation("Profile management initialized for {ProfileType} with {Count} profiles",
                GetProfileTypeName(), Profiles.Count);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to initialize {GetProfileTypeName().ToLowerInvariant()} profiles: {ex.Message}";
            StatusMessage = errorMessage;
            _logger.LogError(ex, "Profile management initialization failed for {ProfileType}", GetProfileTypeName());
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Implementation

    private void SetupCommands()
    {
        // Create command - always available
        CreateCommand = ReactiveCommand.CreateFromTask(ExecuteCreateAsync);

        // Selection-dependent commands
        var hasSelection = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        var canModify = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile?.CanModify() ?? false);

        var canDelete = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile?.CanDelete() ?? false);

        EditCommand = ReactiveCommand.CreateFromTask(ExecuteEditAsync, canModify);
        DuplicateCommand = ReactiveCommand.CreateFromTask(ExecuteDuplicateAsync, hasSelection);
        DeleteCommand = ReactiveCommand.CreateFromTask(ExecuteDeleteAsync, canDelete);

        // General commands
        RefreshCommand = ReactiveCommand.CreateFromTask(ExecuteRefreshAsync);
        BrowseCommand = ReactiveCommand.CreateFromTask(ExecuteBrowseAsync);
        OpenInExplorerCommand = ReactiveCommand.CreateFromTask(ExecuteOpenInExplorerAsync);
        LoadDefaultCommand = ReactiveCommand.CreateFromTask(ExecuteLoadDefaultAsync);

        // Subscribe to command execution for logging and status updates
        CreateCommand.Subscribe(_ => _logger.LogDebug("Create command executed for {ProfileType}", GetProfileTypeName())).DisposeWith(_disposables);
        EditCommand.Subscribe(_ => _logger.LogDebug("Edit command executed for {ProfileType} profile {ProfileId}", GetProfileTypeName(), SelectedProfile?.Id)).DisposeWith(_disposables);
        DuplicateCommand.Subscribe(_ => _logger.LogDebug("Duplicate command executed for {ProfileType} profile {ProfileId}", GetProfileTypeName(), SelectedProfile?.Id)).DisposeWith(_disposables);
        DeleteCommand.Subscribe(_ => _logger.LogDebug("Delete command executed for {ProfileType} profile {ProfileId}", GetProfileTypeName(), SelectedProfile?.Id)).DisposeWith(_disposables);
    }

    private void SetupValidation()
    {
        // Track changes when profiles collection is modified
        Profiles.CollectionChanged += (_, _) => HasChanges = true;

        // Clear status message after delays
        this.WhenAnyValue(x => x.StatusMessage)
            .Where(msg => !string.IsNullOrEmpty(msg))
            .Delay(TimeSpan.FromSeconds(5))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => StatusMessage = null)
            .DisposeWith(_disposables);

        // Log property changes for debugging
        this.WhenAnyValue(x => x.SelectedProfile)
            .Skip(1)
            .Subscribe(profile => _logger.LogDebug("Selected profile changed to {ProfileId} in {ProfileType}", profile?.Id, GetProfileTypeName()))
            .DisposeWith(_disposables);
    }

    private async Task LoadProfilesAsync()
    {
        var profileManager = GetProfileManager();
        var profiles = await profileManager.GetAllAsync().ConfigureAwait(false);

        // Update collection on UI thread
        await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            Profiles.Clear();
            foreach (var profile in profiles)
            {
                Profiles.Add(profile);
            }
        });
    }    private async Task LoadProfilesPathAsync()
    {
        // Get the current profiles path from the manager
        var profileManager = GetProfileManager();
        // Note: This would need to be implemented in the profile manager interface
        // For now, use a placeholder
        ProfilesPath = "resources/Profiles";
        await Task.CompletedTask;
    }

    #endregion

    #region Command Implementation Stubs

    // These would be implemented with the actual dialog and service calls
    private async Task ExecuteCreateAsync() => await Task.CompletedTask;
    private async Task ExecuteEditAsync() => await Task.CompletedTask;
    private async Task ExecuteDuplicateAsync() => await Task.CompletedTask;
    private async Task ExecuteDeleteAsync() => await Task.CompletedTask;
    private async Task ExecuteRefreshAsync() => await Task.CompletedTask;
    private async Task ExecuteBrowseAsync() => await Task.CompletedTask;
    private async Task ExecuteOpenInExplorerAsync() => await Task.CompletedTask;
    private async Task ExecuteLoadDefaultAsync() => await Task.CompletedTask;

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ProfileManagementViewModelBase{TProfile}"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables?.Dispose();
        }
    }

    #endregion
}
