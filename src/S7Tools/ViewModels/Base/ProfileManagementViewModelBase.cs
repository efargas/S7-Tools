using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly IUnifiedProfileDialogService _profileDialogService;
    private readonly IDialogService _dialogService;
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
    /// <param name="profileDialogService">The unified profile dialog service.</param>
    /// <param name="dialogService">The general dialog service for confirmations.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    protected ProfileManagementViewModelBase(
        ILogger logger,
        IUnifiedProfileDialogService profileDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileDialogService = profileDialogService ?? throw new ArgumentNullException(nameof(profileDialogService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));

        SetupCommands();
        SetupValidation();

        // Initialize profile data
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadProfilesAsync().ConfigureAwait(false);
                _logger.LogInformation("Profile management initialized for {ProfileType} with {Count} profiles",
                    GetProfileTypeName(), Profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize profiles for {ProfileType}", GetProfileTypeName());
            }
        });
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
    /// Gets the command to set the selected profile as the default.
    /// </summary>
    /// <remarks>
    /// Marks the currently selected profile as the default profile for this type.
    /// Only enabled when a profile is selected and it's not already the default.
    /// </remarks>
    public ReactiveCommand<Unit, Unit> SetDefaultCommand { get; private set; } = null!;

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

        // Set default command - enabled when profile is selected and not already default
        var canSetDefault = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && !profile.IsDefault);
        SetDefaultCommand = ReactiveCommand.CreateFromTask(ExecuteSetDefaultAsync, canSetDefault);

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

        // Store current selection to restore after refresh
        var selectedId = SelectedProfile?.Id;

        // Update collection on UI thread - Clear and re-add to force DataGrid refresh
        await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            // Clear collection completely
            Profiles.Clear();

            // Re-add all profiles
            foreach (var profile in profiles)
            {
                Profiles.Add(profile);
            }

            // Restore selection if possible
            if (selectedId.HasValue)
            {
                var profileToSelect = Profiles.FirstOrDefault(p => p.Id == selectedId.Value);
                if (profileToSelect != null)
                {
                    SelectedProfile = profileToSelect;
                }
                else if (Profiles.Count > 0)
                {
                    // If previously selected profile no longer exists, select first
                    SelectedProfile = Profiles.First();
                }
            }
            else if (Profiles.Count > 0 && SelectedProfile == null)
            {
                // If no previous selection, select first profile
                SelectedProfile = Profiles.First();
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

    #region Helper Methods

    /// <summary>
    /// Gets the next available unique name based on a base name.
    /// </summary>
    /// <param name="baseName">The base name to use for generation.</param>
    /// <returns>A unique name that doesn't conflict with existing profiles.</returns>
    private async Task<string> GetNextAvailableNameAsync(string baseName)
    {
        if (await IsNameUniqueAsync(baseName).ConfigureAwait(false))
        {
            return baseName;
        }

        var counter = 1;
        string candidateName;
        do
        {
            candidateName = $"{baseName} ({counter})";
            counter++;
        }
        while (!await IsNameUniqueAsync(candidateName).ConfigureAwait(false));

        return candidateName;
    }

    /// <summary>
    /// Checks if a profile name is unique within the current collection.
    /// </summary>
    /// <param name="name">The name to check for uniqueness.</param>
    /// <param name="excludeId">Optional profile ID to exclude from the check (for editing).</param>
    /// <returns>True if the name is unique, false otherwise.</returns>
    private async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null)
    {
        return await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            return !Profiles.Any(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                (!excludeId.HasValue || p.Id != excludeId.Value));
        }).ConfigureAwait(false);
    }

    #endregion

    #region Command Implementation Stubs

    /// <summary>
    /// Executes the create profile command with proper error handling and UI feedback.
    /// </summary>
    private async Task ExecuteCreateAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating profile...";
            _logger.LogDebug("Starting create profile operation for {ProfileType}", GetProfileTypeName());
            System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteCreateAsync called for {GetProfileTypeName()}");

            // Create request with default name
            var request = new ProfileCreateRequest
            {
                Title = $"Create {GetProfileTypeName()} Profile",
                DefaultName = await GetNextAvailableNameAsync(GetDefaultProfileName()).ConfigureAwait(false),
                DefaultDescription = $"New {GetProfileTypeName()} profile"
            };

            // Show create dialog using template method
            var result = await ShowCreateDialogAsync(request).ConfigureAwait(false);

            if (result.IsSuccess && result.Result != null)
            {
                // Validate name uniqueness
                if (!await IsNameUniqueAsync(result.Result.Name).ConfigureAwait(false))
                {
                    StatusMessage = $"Profile name '{result.Result.Name}' already exists";
                    return;
                }

                // Create profile using manager
                var createdProfile = await GetProfileManager().CreateAsync(result.Result).ConfigureAwait(false);

                // Refresh profiles from storage to ensure UI is in sync
                await LoadProfilesAsync().ConfigureAwait(false);

                // Select the newly created profile
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    var newProfile = Profiles.FirstOrDefault(p => p.Id == createdProfile.Id);
                    if (newProfile != null)
                    {
                        SelectedProfile = newProfile;
                    }
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{createdProfile.Name}' created successfully";
                _logger.LogInformation("Created {ProfileType} profile: {ProfileName}", GetProfileTypeName(), createdProfile.Name);
            }
            else
            {
                StatusMessage = "Profile creation cancelled";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {ProfileType} profile", GetProfileTypeName());
            StatusMessage = $"Failed to create profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes the edit profile command with proper validation and UI feedback.
    /// </summary>
    private async Task ExecuteEditAsync()
    {
        if (SelectedProfile == null)
        {
            StatusMessage = "No profile selected for editing";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Editing profile...";
            _logger.LogDebug("Starting edit profile operation for {ProfileName}", SelectedProfile.Name);

            // Create edit request with current profile
            var request = new ProfileEditRequest
            {
                Title = $"Edit {GetProfileTypeName()} Profile",
                ProfileId = SelectedProfile.Id
            };

            // Show edit dialog using template method
            var result = await ShowEditDialogAsync(request).ConfigureAwait(false);

            if (result.IsSuccess && result.Result != null)
            {
                // Validate name uniqueness (excluding current profile)
                if (!await IsNameUniqueAsync(result.Result.Name, SelectedProfile.Id).ConfigureAwait(false))
                {
                    StatusMessage = $"Profile name '{result.Result.Name}' already exists";
                    return;
                }

                // Update profile using manager
                var updatedProfile = await GetProfileManager().UpdateAsync(result.Result).ConfigureAwait(false);

                // Refresh profiles from storage to ensure UI is in sync
                await LoadProfilesAsync().ConfigureAwait(false);

                // Reselect the updated profile
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    var updated = Profiles.FirstOrDefault(p => p.Id == updatedProfile.Id);
                    if (updated != null)
                    {
                        SelectedProfile = updated;
                    }
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{updatedProfile.Name}' updated successfully";
                _logger.LogInformation("Updated {ProfileType} profile: {ProfileName}", GetProfileTypeName(), updatedProfile.Name);
            }
            else
            {
                StatusMessage = "Profile edit cancelled";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit {ProfileType} profile: {ProfileName}", GetProfileTypeName(), SelectedProfile?.Name);
            StatusMessage = $"Failed to edit profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes the duplicate profile command with name input and validation.
    /// </summary>
    private async Task ExecuteDuplicateAsync()
    {
        if (SelectedProfile == null)
        {
            StatusMessage = "No profile selected for duplication";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Duplicating profile...";
            _logger.LogDebug("Starting duplicate profile operation for {ProfileName}", SelectedProfile.Name);

            // Create duplicate request with suggested name
            var suggestedName = await GetNextAvailableNameAsync($"{SelectedProfile.Name} (Copy)").ConfigureAwait(false);
            var request = new ProfileDuplicateRequest
            {
                Title = $"Duplicate {GetProfileTypeName()} Profile",
                SourceProfileId = SelectedProfile.Id,
                SuggestedName = suggestedName
            };

            // Show duplicate dialog using template method
            var result = await ShowDuplicateDialogAsync(request).ConfigureAwait(false);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Result))
            {
                // Validate name uniqueness
                if (!await IsNameUniqueAsync(result.Result).ConfigureAwait(false))
                {
                    StatusMessage = $"Profile name '{result.Result}' already exists";
                    return;
                }

                // Duplicate profile using manager
                var duplicatedProfile = await GetProfileManager().DuplicateAsync(SelectedProfile.Id, result.Result).ConfigureAwait(false);

                // Refresh profiles from storage to ensure UI is in sync
                await LoadProfilesAsync().ConfigureAwait(false);

                // Select the newly duplicated profile
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    var duplicate = Profiles.FirstOrDefault(p => p.Id == duplicatedProfile.Id);
                    if (duplicate != null)
                    {
                        SelectedProfile = duplicate;
                    }
                }).ConfigureAwait(false);

                StatusMessage = $"Profile duplicated as '{duplicatedProfile.Name}'";
                _logger.LogInformation("Duplicated {ProfileType} profile: {SourceName} -> {NewName}",
                    GetProfileTypeName(), SelectedProfile.Name, duplicatedProfile.Name);
            }
            else
            {
                StatusMessage = "Profile duplication cancelled";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate {ProfileType} profile: {ProfileName}", GetProfileTypeName(), SelectedProfile?.Name);
            StatusMessage = $"Failed to duplicate profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes the delete profile command with confirmation and proper cleanup.
    /// </summary>
    private async Task ExecuteDeleteAsync()
    {
        if (SelectedProfile == null)
        {
            StatusMessage = "No profile selected for deletion";
            return;
        }

        var profileName = SelectedProfile.Name;
        var profileId = SelectedProfile.Id;

        // Show confirmation dialog
        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Delete Profile",
            $"Are you sure you want to delete the profile '{profileName}'? This action cannot be undone.").ConfigureAwait(false);

        if (!confirmed)
        {
            _logger.LogDebug("Profile deletion cancelled by user for {ProfileName}", profileName);
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting profile...";
            _logger.LogDebug("Starting delete profile operation for {ProfileName}", profileName);

            var success = await GetProfileManager().DeleteAsync(profileId).ConfigureAwait(false);

            if (success)
            {
                // Refresh profiles from storage to ensure UI is in sync
                await LoadProfilesAsync().ConfigureAwait(false);

                // Select next available profile
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    SelectedProfile = Profiles.FirstOrDefault();
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{profileName}' deleted successfully";
                _logger.LogInformation("Deleted {ProfileType} profile: {ProfileName}", GetProfileTypeName(), profileName);
            }
            else
            {
                StatusMessage = $"Failed to delete profile '{profileName}'";
                _logger.LogWarning("Delete operation returned false for {ProfileType} profile: {ProfileName}", GetProfileTypeName(), profileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {ProfileType} profile: {ProfileName}", GetProfileTypeName(), SelectedProfile?.Name);
            StatusMessage = $"Failed to delete profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Sets the selected profile as the default profile.
    /// </summary>
    private async Task ExecuteSetDefaultAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        // Capture profile info before async operations that might change SelectedProfile
        var profileId = SelectedProfile.Id;
        var profileName = SelectedProfile.Name;

        try
        {
            await GetProfileManager().SetDefaultAsync(profileId).ConfigureAwait(false);
            await LoadProfilesAsync().ConfigureAwait(false);

            _logger.LogInformation("Profile '{Name}' set as default", profileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set profile '{Name}' as default", profileName);
            // Could show error message to user here
        }
    }

    /// <summary>
    /// Executes the refresh profiles command to reload data from storage.
    /// </summary>
    private async Task ExecuteRefreshAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing profiles...";
            _logger.LogDebug("Refreshing {ProfileType} profiles", GetProfileTypeName());

            // Preserve current selection
            var currentSelectedId = SelectedProfile?.Id;

            // Load all profiles from manager
            var profiles = await GetProfileManager().GetAllAsync().ConfigureAwait(false);

            // Update UI on main thread
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                Profiles.Clear();
                foreach (var profile in profiles)
                {
                    Profiles.Add(profile);
                }

                // Restore selection or select first/default
                if (currentSelectedId.HasValue)
                {
                    SelectedProfile = Profiles.FirstOrDefault(p => p.Id == currentSelectedId.Value);
                }

                SelectedProfile ??= Profiles.FirstOrDefault(p => p.IsDefault) ?? Profiles.FirstOrDefault();
                HasChanges = false;
            }).ConfigureAwait(false);

            StatusMessage = $"Refreshed {Profiles.Count} profiles";
            _logger.LogInformation("Refreshed {Count} {ProfileType} profiles", Profiles.Count, GetProfileTypeName());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh {ProfileType} profiles", GetProfileTypeName());
            StatusMessage = $"Failed to refresh profiles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes the browse command for path selection.
    /// NOTE: This is a placeholder for path management functionality.
    /// </summary>
    private async Task ExecuteBrowseAsync()
    {
        StatusMessage = "Browse functionality not yet implemented";
        _logger.LogDebug("Browse command executed (placeholder)");
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the open in explorer command for path viewing.
    /// NOTE: This is a placeholder for path management functionality.
    /// </summary>
    private async Task ExecuteOpenInExplorerAsync()
    {
        StatusMessage = "Open in explorer functionality not yet implemented";
        _logger.LogDebug("Open in explorer command executed (placeholder)");
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the load default command for path reset.
    /// NOTE: This is a placeholder for path management functionality.
    /// </summary>
    private async Task ExecuteLoadDefaultAsync()
    {
        StatusMessage = "Load default functionality not yet implemented";
        _logger.LogDebug("Load default command executed (placeholder)");
        await Task.CompletedTask.ConfigureAwait(false);
    }

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
