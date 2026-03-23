using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Resources;
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
    protected IUnifiedProfileDialogService UnifiedDialogService => _profileDialogService;
    private readonly IDialogService _dialogService;
    private readonly IUIThreadService _uiThreadService;
    private readonly IFileDialogService _fileDialogService;
    private readonly CompositeDisposable _disposables = new();

    // Profile collection and selection state
    private ObservableCollection<TProfile> _profiles = new();
    private NotifyCollectionChangedEventHandler? _profilesChangedHandler;
    private TProfile? _selectedProfile;
    private bool _isLoading;
    private string? _statusMessage;

    // UI state for enhanced features
    private bool _hasChanges;
    private string _searchText = string.Empty;


    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileManagementViewModelBase{TProfile}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this view model.</param>
    /// <param name="profileDialogService">The unified profile dialog service.</param>
    /// <param name="dialogService">The general dialog service for confirmations.</param>
    /// <param name="uiThreadService">The UI thread service for cross-thread operations.</param>
    /// <param name="fileDialogService">The file dialog service for import/export operations.</param>
    protected ProfileManagementViewModelBase(
        ILogger logger,
        IUnifiedProfileDialogService profileDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService,
        IFileDialogService fileDialogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileDialogService = profileDialogService ?? throw new ArgumentNullException(nameof(profileDialogService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));

        SetupCommands();
        SetupValidation();

        // Track collection changes to surface HasChanges and other UI state updates
        _profilesChangedHandler = (_, __) => { HasChanges = true; };
        try
        { _profiles.CollectionChanged += _profilesChangedHandler; }
        catch { /* ignore if handler already attached */ }

        // NOTE: Initialization is deferred to InitializeAsync() which must be called
        // after derived class construction completes. This ensures all derived class
        // fields (like _profileService) are initialized before LoadProfilesAsync() is called.
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
        protected set
        {
            if (!ReferenceEquals(_profiles, value))
            {
                if (_profilesChangedHandler != null)
                {
                    try
                    { _profiles.CollectionChanged -= _profilesChangedHandler; }
                    catch { }
                }

                this.RaiseAndSetIfChanged(ref _profiles, value);

                if (_profilesChangedHandler != null)
                {
                    try
                    { _profiles.CollectionChanged += _profilesChangedHandler; }
                    catch { }
                }
            }
        }
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

    public ReactiveCommand<Unit, Unit> ExportProfilesCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ImportProfilesCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportSelectedProfileCommand { get; private set; } = null!;

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
            StatusMessage = UIStrings.Status_LoadingProfiles;

            await LoadProfilesAsync().ConfigureAwait(false);

            StatusMessage = $"Loaded {Profiles.Count} {GetProfileTypeName().ToLowerInvariant()} profiles.";
            _logger.LogInformation("Profile management initialized for {ProfileType} with {Count} profiles",
                GetProfileTypeName(), Profiles.Count);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Failed to initialize {GetProfileTypeName().ToLowerInvariant()} profiles: {ex.Message}";
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
        IObservable<bool> hasSelection = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        IObservable<bool> canModify = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile =>
            {
                bool canMod = profile?.CanModify() ?? false;
                System.Diagnostics.Debug.WriteLine($"DEBUG: CanModify for profile '{profile?.Name}' (ID: {profile?.Id}): {canMod} (IsReadOnly: {profile?.IsReadOnly}, IsDefault: {profile?.IsDefault})");
                return canMod;
            });

        IObservable<bool> canDelete = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile?.CanDelete() ?? false);

        EditCommand = ReactiveCommand.CreateFromTask(ExecuteEditAsync, canModify);
        DuplicateCommand = ReactiveCommand.CreateFromTask(ExecuteDuplicateAsync, hasSelection);
        DeleteCommand = ReactiveCommand.CreateFromTask(ExecuteDeleteAsync, canDelete);

        // Set default command - enabled when profile is selected and not already default
        IObservable<bool> canSetDefault = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && !profile.IsDefault);
        SetDefaultCommand = ReactiveCommand.CreateFromTask(ExecuteSetDefaultAsync, canSetDefault);

        // General commands
        RefreshCommand = ReactiveCommand.CreateFromTask(ExecuteRefreshAsync);

        // Subscribe to command execution for logging and status updates
        CreateCommand.Subscribe(_ => _logger.LogDebug("Create command executed for {ProfileType}", GetProfileTypeName())).DisposeWith(_disposables);
        EditCommand.Subscribe(_ => _logger.LogDebug("Edit command executed for {ProfileType} profile {ProfileId}", GetProfileTypeName(), SelectedProfile?.Id)).DisposeWith(_disposables);
        DuplicateCommand.Subscribe(_ => _logger.LogDebug("Duplicate command executed for {ProfileType} profile {ProfileId}", GetProfileTypeName(), SelectedProfile?.Id)).DisposeWith(_disposables);
        DeleteCommand.Subscribe(_ => _logger.LogDebug("Delete command executed for {ProfileType} profile {ProfileId}", GetProfileTypeName(), SelectedProfile?.Id)).DisposeWith(_disposables);

        // Export/Import commands
        IObservable<bool> canExportProfiles = this.WhenAnyValue(x => x.Profiles.Count)
            .Select(count => count > 0);

        ExportProfilesCommand = ReactiveCommand.CreateFromTask(ExportProfilesAsync, canExportProfiles);
        ExportProfilesCommand.ThrownExceptions
            .Subscribe(ex => _logger.LogError(ex, "Error exporting profiles"))
            .DisposeWith(_disposables);

        ImportProfilesCommand = ReactiveCommand.CreateFromTask(ImportProfilesAsync);
        ImportProfilesCommand.ThrownExceptions
            .Subscribe(ex => _logger.LogError(ex, "Error importing profiles"))
            .DisposeWith(_disposables);

        IObservable<bool> canExportSelectedProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && profile.Id > 0);

        ExportSelectedProfileCommand = ReactiveCommand.CreateFromTask(ExportSelectedProfileAsync, canExportSelectedProfile);
        ExportSelectedProfileCommand.ThrownExceptions
            .Subscribe(ex => _logger.LogError(ex, "Error exporting selected profile"))
            .DisposeWith(_disposables);
    }

    private void SetupValidation()
    {
        // Collection change tracking is configured via Profiles property setter

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
        _logger.LogInformation("LoadProfilesAsync started for {ProfileType}", GetProfileTypeName());

        IProfileManager<TProfile> profileManager = GetProfileManager();
        IEnumerable<TProfile> profiles = await profileManager.GetAllAsync().ConfigureAwait(false);

        _logger.LogInformation("Loaded {ProfileCount} profiles from manager for {ProfileType}", profiles?.Count() ?? 0, GetProfileTypeName());

        // Store current selection to restore after refresh
        int? selectedId = SelectedProfile?.Id;
        _logger.LogDebug("Current selected profile ID: {SelectedId} for {ProfileType}", selectedId, GetProfileTypeName());

        // Replace collection on UI thread to force DataGrid refresh
        await _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            _logger.LogDebug("Updating profiles collection on UI thread for {ProfileType}", GetProfileTypeName());
            Profiles = new ObservableCollection<TProfile>(profiles ?? Enumerable.Empty<TProfile>());
            _logger.LogInformation("Profiles collection updated with {Count} items for {ProfileType}", Profiles.Count, GetProfileTypeName());

            // Restore selection if possible
            if (selectedId.HasValue)
            {
                _logger.LogDebug("Attempting to restore selection for profile ID {SelectedId}", selectedId);
                TProfile? profileToSelect = Profiles.FirstOrDefault(p => p.Id == selectedId.Value);
                if (profileToSelect != null)
                {
                    SelectedProfile = profileToSelect;
                    _logger.LogDebug("Restored selection to profile {ProfileId} ({ProfileName})", profileToSelect.Id, profileToSelect.Name);
                }
                else if (Profiles.Count > 0)
                {
                    // If previously selected profile no longer exists, select first
                    SelectedProfile = Profiles.First();
                    _logger.LogDebug("Previous selection not found, selected first profile {ProfileId} ({ProfileName})", SelectedProfile.Id, SelectedProfile.Name);
                }
                else
                {
                    _logger.LogWarning("No profiles available to select after refresh");
                }
            }
            else if (Profiles.Count > 0 && SelectedProfile == null)
            {
                // If no previous selection, select first profile
                SelectedProfile = Profiles.First();
                _logger.LogDebug("No previous selection, selected first profile {ProfileId} ({ProfileName})", SelectedProfile.Id, SelectedProfile.Name);
            }
            else if (Profiles.Count == 0)
            {
                SelectedProfile = null;
                _logger.LogInformation("No profiles available, SelectedProfile set to null");
            }
        });

        _logger.LogInformation("LoadProfilesAsync completed for {ProfileType}, SelectedProfile: {SelectedProfileId}", GetProfileTypeName(), SelectedProfile?.Id);
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

        int counter = 1;
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
            StatusMessage = UIStrings.Status_CreatingProfile;
            _logger.LogInformation("Starting create profile operation for {ProfileType}", GetProfileTypeName());
            System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteCreateAsync called for {GetProfileTypeName()}");

            // Create request with default name
            string defaultName = GetDefaultProfileName();
            _logger.LogDebug("Getting default profile name: {DefaultName}", defaultName);

            string nextAvailableName = await GetNextAvailableNameAsync(defaultName).ConfigureAwait(false);
            _logger.LogDebug("Next available name: {AvailableName}", nextAvailableName);

            var request = new ProfileCreateRequest
            {
                Title = $"Create {GetProfileTypeName()} Profile",
                DefaultName = nextAvailableName,
                DefaultDescription = $"New {GetProfileTypeName()} profile"
            };
            _logger.LogDebug("Created ProfileCreateRequest: Title={Title}, DefaultName={DefaultName}", request.Title, request.DefaultName);

            // Show create dialog using template method
            _logger.LogDebug("Calling ShowCreateDialogAsync for {ProfileType}", GetProfileTypeName());
            ProfileDialogResult<TProfile> result = await ShowCreateDialogAsync(request).ConfigureAwait(false);
            _logger.LogInformation("ShowCreateDialogAsync result: IsSuccess={IsSuccess}, HasResult={HasResult}", result.IsSuccess, result.Result != null);

            if (result.IsSuccess && result.Result != null)
            {
                // Dialog SaveAsync already persisted. Refresh and reselect by name
                string createdName = result.Result.Name;
                _logger.LogInformation("Profile created successfully with name: {CreatedName}", createdName);

                _logger.LogDebug("Loading profiles after creation");
                await LoadProfilesAsync().ConfigureAwait(false);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    _logger.LogDebug("Selecting newly created profile: {CreatedName}", createdName);
                    SelectedProfile = Profiles.FirstOrDefault(p => string.Equals(p.Name, createdName, StringComparison.OrdinalIgnoreCase))
                                      ?? Profiles.FirstOrDefault();
                    _logger.LogDebug("Profile selection after creation: SelectedProfile={SelectedProfileId}", SelectedProfile?.Id);
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{createdName}' created successfully";
                _logger.LogInformation("Successfully completed create operation for {ProfileType} profile: {ProfileName}", GetProfileTypeName(), createdName);
            }
            else if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage ?? UIStrings.Status_ProfileCreationCancelled;
                _logger.LogWarning("Profile creation failed: {ErrorMessage}", result.ErrorMessage);
            }
            else
            {
                StatusMessage = UIStrings.Status_ProfileCreationCancelled;
                _logger.LogInformation("Profile creation was cancelled by user");
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
            _logger.LogDebug("ExecuteCreateAsync completed for {ProfileType}, IsLoading={IsLoading}", GetProfileTypeName(), IsLoading);
        }
    }

    /// <summary>
    /// Executes the edit profile command with proper validation and UI feedback.
    /// </summary>
    private async Task ExecuteEditAsync()
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteEditAsync called for {GetProfileTypeName()}");

        if (SelectedProfile == null)
        {
            StatusMessage = UIStrings.Status_NoProfileSelectedForEditing;
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = UIStrings.Status_EditingProfile;
            _logger.LogDebug("Starting edit profile operation for {ProfileName}", SelectedProfile.Name);

            // Create edit request with current profile
            var request = new ProfileEditRequest
            {
                Title = $"Edit {GetProfileTypeName()} Profile",
                ProfileId = SelectedProfile.Id
            };

            // Show edit dialog using template method
            ProfileDialogResult<TProfile> result = await ShowEditDialogAsync(request).ConfigureAwait(false);

            if (result.IsSuccess && result.Result != null)
            {
                // Dialog SaveAsync already persisted. Refresh and reselect by original Id or new name
                int previousId = SelectedProfile.Id;
                string newName = result.Result.Name;

                await LoadProfilesAsync().ConfigureAwait(false);
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    SelectedProfile = Profiles.FirstOrDefault(p => p.Id == previousId)
                                      ?? Profiles.FirstOrDefault(p => string.Equals(p.Name, newName, StringComparison.OrdinalIgnoreCase))
                                      ?? Profiles.FirstOrDefault();
                }).ConfigureAwait(false);

                StatusMessage = $"Profile '{newName}' updated successfully";
                _logger.LogInformation("Updated {ProfileType} profile: {ProfileName}", GetProfileTypeName(), newName);
            }
            else
            {
                StatusMessage = UIStrings.Status_ProfileEditCancelled;
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

    private async Task ExportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = UIStrings.Status_FileDialogServiceNotAvailable;
            return;
        }

        try
        {
            string? fileName = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Profiles",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                null,
                "profiles.json");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = UIStrings.Status_ExportingProfiles;

            IEnumerable<TProfile> profiles = await GetProfileManager().ExportAsync();
            string jsonData = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fileName, jsonData);

            StatusMessage = $"Exported {Profiles.Count} profile(s) to {Path.GetFileName(fileName)}";
            _logger.LogInformation("Exported {ProfileCount} profiles to {FileName}", Profiles.Count, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting profiles");
            StatusMessage = UIStrings.Status_ErrorExportingProfiles;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ImportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = UIStrings.Status_FileDialogServiceNotAvailable;
            return;
        }

        try
        {
            string? fileName = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Profiles",
                "JSON files (*.json)|*.json|All files (*.*)|*.*");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = UIStrings.Status_ImportingProfiles;

            string jsonData = await File.ReadAllTextAsync(fileName);
            List<TProfile> profiles = JsonSerializer.Deserialize<List<TProfile>>(jsonData) ?? new List<TProfile>();
            IEnumerable<TProfile> importedProfiles = await GetProfileManager().ImportAsync(profiles, replaceExisting: false);

            int importedCount = importedProfiles.Count();
            await LoadProfilesAsync();

            StatusMessage = $"Imported {importedCount} profile(s) from {Path.GetFileName(fileName)}";
            _logger.LogInformation("Imported {ImportedCount} profiles from {FileName}", importedCount, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing profiles");
            StatusMessage = UIStrings.Status_ErrorImportingProfiles;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportSelectedProfileAsync()
    {
        if (SelectedProfile == null || _fileDialogService == null)
        {
            return;
        }

        try
        {
            string? fileName = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Profile",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                null,
                $"{SelectedProfile.Name}.json");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = UIStrings.Status_ExportingSelectedProfile;

            string jsonData;

            if (SelectedProfile.Id > 0)
            {
                TProfile? profile = await GetProfileManager().GetByIdAsync(SelectedProfile.Id);
                jsonData = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                jsonData = JsonSerializer.Serialize(SelectedProfile, options);
            }

            await File.WriteAllTextAsync(fileName, jsonData);

            StatusMessage = $"Exported profile '{SelectedProfile.Name}' to {Path.GetFileName(fileName)}";
            _logger.LogInformation("Exported selected profile {ProfileName} to {FileName}", SelectedProfile.Name, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting selected profile");
            StatusMessage = "Error exporting selected profile";
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
            StatusMessage = UIStrings.Status_NoProfileSelectedForDuplication;
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = UIStrings.Status_DuplicatingProfile;
            _logger.LogDebug("Starting duplicate profile operation for {ProfileName}", SelectedProfile.Name);

            // Create duplicate request with suggested name
            string suggestedName = await GetNextAvailableNameAsync($"{SelectedProfile.Name} (Copy)").ConfigureAwait(false);
            var request = new ProfileDuplicateRequest
            {
                Title = $"Duplicate {GetProfileTypeName()} Profile",
                SourceProfileId = SelectedProfile.Id,
                SuggestedName = suggestedName
            };

            // Show duplicate dialog using template method
            ProfileDialogResult<string> result = await ShowDuplicateDialogAsync(request).ConfigureAwait(false);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Result))
            {
                // Validate name uniqueness
                if (!await IsNameUniqueAsync(result.Result).ConfigureAwait(false))
                {
                    StatusMessage = $"Profile name '{result.Result}' already exists";
                    return;
                }

                // Duplicate profile using manager
                TProfile duplicatedProfile = await GetProfileManager().DuplicateAsync(SelectedProfile.Id, result.Result).ConfigureAwait(false);

                // Refresh profiles from storage to ensure UI is in sync
                await LoadProfilesAsync().ConfigureAwait(false);

                // Select the newly duplicated profile
                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    TProfile? duplicate = Profiles.FirstOrDefault(p => p.Id == duplicatedProfile.Id);
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
                StatusMessage = UIStrings.Status_ProfileDuplicationCancelled;
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
            StatusMessage = UIStrings.Status_NoProfileSelectedForDeletion;
            return;
        }

        string profileName = SelectedProfile.Name;
        int profileId = SelectedProfile.Id;

        // Show confirmation dialog
        bool confirmed = await _dialogService.ShowConfirmationAsync(
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
            StatusMessage = UIStrings.Status_DeletingProfile;
            _logger.LogDebug("Starting delete profile operation for {ProfileName}", profileName);

            bool success = await GetProfileManager().DeleteAsync(profileId).ConfigureAwait(false);

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
        int profileId = SelectedProfile.Id;
        string profileName = SelectedProfile.Name;

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
            StatusMessage = UIStrings.Status_RefreshingProfiles;
            _logger.LogDebug("Refreshing {ProfileType} profiles", GetProfileTypeName());

            // Preserve current selection
            int? currentSelectedId = SelectedProfile?.Id;

            // Load all profiles from manager
            IEnumerable<TProfile> profiles = await GetProfileManager().GetAllAsync().ConfigureAwait(false);

            // Update UI on main thread (replace collection instance)
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                Profiles = new ObservableCollection<TProfile>(profiles);

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
