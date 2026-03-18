using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Helpers;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Dialogs;
using S7Tools.Views.Dialogs;

namespace S7Tools.ViewModels.Settings;

/// <summary>
/// ViewModel for the Memory Region settings category, providing comprehensive profile management
/// for PLC firmware memory mapping configurations.
/// </summary>
/// <remarks>
/// This ViewModel extends the unified profile management pattern with memory-specific functionality
/// for managing memory region profiles that define memory segments for firmware dumps.
/// Follows the established ProfileManagementViewModelBase pattern for consistency.
/// </remarks>
public class MemoryRegionSettingsViewModel : ProfileManagementViewModelBase<MemoryMappingProfile>
{
    #region Fields

    private readonly IMemoryRegionProfileService _profileService;
    private readonly IDialogService _dialogService;
    private readonly IUnifiedProfileDialogService _unifiedDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<MemoryRegionSettingsViewModel> _specificLogger;
    private readonly S7Tools.Core.Interfaces.Services.IApplicationSettingsService _settingsService;
    private readonly S7Tools.Services.Interfaces.IUIThreadService _uiThreadService;
    private readonly IPathService _pathService;

    private readonly CompositeDisposable _disposables = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the MemoryRegionSettingsViewModel class.
    /// </summary>
    /// <param name="unifiedDialogService">The unified profile dialog service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="profileService">The memory region profile service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="settingsService">The settings service used to persist application settings.</param>
    /// <param name="pathService">The path service for dynamic path resolution.</param>
    public MemoryRegionSettingsViewModel(
        IUnifiedProfileDialogService unifiedDialogService,
        ILogger<ProfileManagementViewModelBase<MemoryMappingProfile>> logger,
        S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
        IMemoryRegionProfileService profileService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        IFileDialogService? fileDialogService,
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService,
        IPathService pathService)
        : base(logger, unifiedDialogService, dialogService, uiThreadService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _unifiedDialogService = unifiedDialogService;
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _uiThreadService = uiThreadService;
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

        // Store specific logger (use constructor parameter, not create new factory)
        _specificLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { }).CreateLogger<MemoryRegionSettingsViewModel>();

        // Path commands have been moved to PathSettingsViewModel

        // Initialize profile commands
        InitializeProfileCommands();

        // Initialize export/import commands
        InitializeImportExportCommands();



        // Subscribe to SelectedProfile changes to update computed properties
        this.WhenAnyValue(x => x.SelectedProfile)
            .Subscribe(profile =>
            {
                _specificLogger.LogDebug("SelectedProfile changed to: {ProfileName} (ID: {ProfileId})", profile?.Name ?? "(null)", profile?.Id ?? 0);
                if (profile != null)
                {
                    _specificLogger.LogDebug("Profile properties: IsReadOnly={IsReadOnly}, IsDefault={IsDefault}, CanModify={CanModify}, CanDelete={CanDelete}",
                        profile.IsReadOnly, profile.IsDefault, profile.CanModify(), profile.CanDelete());
                }

                _specificLogger.LogDebug("Updating computed properties");
                this.RaisePropertyChanged(nameof(SelectedSegments));
                this.RaisePropertyChanged(nameof(HasValidSelection));
                this.RaisePropertyChanged(nameof(SegmentCount));
                this.RaisePropertyChanged(nameof(SelectedSegmentCount));

                // Subscribe to segment changes if we have a profile
                SubscribeToSegmentChanges();
            })
            .DisposeWith(_disposables);



        // Load initial data
        _ = Task.Run(async () =>
        {
            try
            {
                _specificLogger.LogInformation("Starting MemoryRegionSettingsViewModel initialization");
                await InitializeAsync().ConfigureAwait(false);
                _specificLogger.LogInformation("InitializeAsync completed, executing refresh command");
                RefreshCommand.Execute().Subscribe();
                _specificLogger.LogInformation("MemoryRegionSettingsViewModel initialization completed");
            }
            catch (Exception ex)
            {
                _specificLogger.LogError(ex, "Error during MemoryRegionSettingsViewModel initialization");
            }
        });
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of selected memory segments from the currently selected profile.
    /// </summary>
    /// <remarks>
    /// This computed property provides a filtered view of segments that are marked as selected
    /// for memory operations, useful for displaying segment selection status.
    /// </remarks>
    public ObservableCollection<MemorySegment> SelectedSegments =>
        new ObservableCollection<MemorySegment>(SelectedProfile?.Segments?.Where(s => s.IsSelected) ?? Enumerable.Empty<MemorySegment>());

    /// <summary>
    /// Gets a value indicating whether the selected profile has valid segment selections.
    /// </summary>
    /// <remarks>
    /// Used for validation and enabling/disabling UI operations that require valid segments.
    /// </remarks>
    public bool HasValidSelection =>
        SelectedProfile?.Segments?.Any(s => s.IsSelected) == true;

    /// <summary>
    /// Gets the total number of segments in the selected profile.
    /// </summary>
    public int SegmentCount => SelectedProfile?.Segments?.Count ?? 0;

    /// <summary>
    /// Gets the number of selected segments in the current profile.
    /// </summary>
    public int SelectedSegmentCount => SelectedProfile?.Segments?.Count(s => s.IsSelected) ?? 0;



    #endregion

    #region Commands

    // Path commands are in PathSettingsViewModel

    /// <summary>
    /// Command to export all profiles to a JSON file.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportProfilesCommand { get; private set; } = null!;

    /// <summary>
    /// Command to import profiles from a JSON file.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportProfilesCommand { get; private set; } = null!;

    #endregion

    #region ProfileManagementViewModelBase Implementation

    /// <summary>
    /// Gets the profile service used by the base class.
    /// </summary>
    protected override IProfileManager<MemoryMappingProfile> GetProfileManager() => _profileService;

    /// <summary>
    /// Gets the default profile name used when creating new profiles.
    /// </summary>
    protected override string GetDefaultProfileName() => "Memory Region Default";

    /// <summary>
    /// Gets the profile type name for display purposes.
    /// </summary>
    protected override string GetProfileTypeName() => "Memory Region Profile";

    /// <summary>
    /// Creates a default profile instance.
    /// </summary>
    protected override MemoryMappingProfile CreateDefaultProfile() =>
        MemoryMappingProfile.CreateDefaultProfile();

    /// <summary>
    /// Shows the create profile dialog.
    /// </summary>
    protected override async Task<ProfileDialogResult<MemoryMappingProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        try
        {
            _specificLogger.LogDebug("ShowCreateDialogAsync called for memory region profile");

            // For now, use the name input dialog until specific memory region dialogs are implemented
            ProfileDialogResult<string> nameResult = await _unifiedDialogService.ShowNameInputDialogAsync(
                $"Create {GetProfileTypeName()}",
                "Enter a name for the new memory region profile:",
                request.DefaultName ?? GetDefaultProfileName()
            );

            _specificLogger.LogInformation("Name input dialog result: IsSuccess={IsSuccess}, Name={Name}", nameResult.IsSuccess, nameResult.Result);

            if (!nameResult.IsSuccess)
            {
                _specificLogger.LogInformation("Memory region profile creation cancelled by user");
                return ProfileDialogResult<MemoryMappingProfile>.Cancelled();
            }

            // Create a new profile with the provided name
            var newProfile = MemoryMappingProfile.CreateUserProfile(nameResult.Result ?? "New Profile");
            _specificLogger.LogDebug("Created new memory region profile: Name={Name}, Id={Id}, SegmentCount={SegmentCount}",
                newProfile.Name, newProfile.Id, newProfile.Segments?.Count ?? 0);

            // Save the profile to the manager
            _specificLogger.LogDebug("Saving new memory region profile to manager");
            MemoryMappingProfile savedProfile = await _profileService.CreateAsync(newProfile);
            _specificLogger.LogInformation("Successfully saved memory region profile: Name={Name}, Id={Id}",
                savedProfile.Name, savedProfile.Id);

            return ProfileDialogResult<MemoryMappingProfile>.Success(savedProfile);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error showing create dialog for memory region profile");
            return ProfileDialogResult<MemoryMappingProfile>.Failure($"Error creating profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the edit profile dialog.
    /// </summary>
    protected override async Task<ProfileDialogResult<MemoryMappingProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        try
        {
            _specificLogger.LogDebug("ShowEditDialogAsync called for memory region profile");

            if (SelectedProfile == null)
            {
                _specificLogger.LogWarning("Edit dialog called with no selected profile");
                return ProfileDialogResult<MemoryMappingProfile>.Failure("No profile selected for editing");
            }

            _specificLogger.LogDebug("Opening edit dialog for memory region profile: Name={Name}, Id={Id}", SelectedProfile.Name, SelectedProfile.Id);

            // Create a logger for the dialog ViewModel
            _specificLogger.LogDebug("Creating dialog logger");
            ILogger<EditMemoryRegionProfileDialogViewModel> dialogLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { }).CreateLogger<EditMemoryRegionProfileDialogViewModel>();
            _specificLogger.LogDebug("Created dialog logger successfully");

            // Create the edit dialog ViewModel and view
            _specificLogger.LogDebug("Creating dialog ViewModel with profile: {ProfileName} (ID: {ProfileId})", SelectedProfile.Name, SelectedProfile.Id);
            var dialogViewModel = new EditMemoryRegionProfileDialogViewModel(SelectedProfile, dialogLogger);
            _specificLogger.LogDebug("Created dialog ViewModel successfully");

            _specificLogger.LogDebug("Creating dialog view");
            var dialog = new EditMemoryRegionProfileDialog(dialogViewModel);
            _specificLogger.LogDebug("Created dialog view successfully");

            // Show the dialog on UI thread
            _specificLogger.LogDebug("Invoking dialog on UI thread");
            bool? dialogResult = null;

            try
            {
                dialogResult = await _uiThreadService.InvokeOnUIThreadAsync(async () =>
                {
                    _specificLogger.LogDebug("Inside UI thread invocation - getting main window");

                    // Get the main window as parent
                    Avalonia.Controls.Window? mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : null;

                    if (mainWindow == null)
                    {
                        _specificLogger.LogError("Could not get main window for dialog parent - ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime or MainWindow is null");
                        throw new DialogParentNotFoundException();
                    }

                    _specificLogger.LogDebug("Main window found, showing dialog with parent window: {WindowTitle}", mainWindow.Title);

                    bool? result = await dialog.ShowDialog<bool?>(mainWindow);

                    _specificLogger.LogDebug("Dialog closed with result: {Result}", result);
                    return result;
                }).ConfigureAwait(false);
            }
            catch (Exception invokeEx)
            {
                _specificLogger.LogError(invokeEx, "Exception during UI thread invocation or dialog display");
                throw; // Re-throw to be caught by outer catch
            }

            _specificLogger.LogInformation("Edit dialog completed with result: {Result} (Success={Success})",
                dialogResult, dialogResult == true);

            if (dialogResult == true)
            {
                _specificLogger.LogDebug("Dialog succeeded, creating updated profile");
                MemoryMappingProfile? updatedProfile = dialogViewModel.CreateUpdatedProfile();

                if (updatedProfile != null)
                {
                    // Save the updated profile
                    _specificLogger.LogDebug("Saving updated memory region profile to manager");
                    MemoryMappingProfile savedProfile = await _profileService.UpdateAsync(updatedProfile);
                    _specificLogger.LogInformation("Successfully updated memory region profile: Name={Name}, Id={Id}",
                        savedProfile.Name, savedProfile.Id);

                    return ProfileDialogResult<MemoryMappingProfile>.Success(savedProfile);
                }
                else
                {
                    _specificLogger.LogError("Dialog succeeded but CreateUpdatedProfile returned null");
                    return ProfileDialogResult<MemoryMappingProfile>.Failure(
                        UIStrings.ResourceManager.GetString("Error_ProfileCreationFailed") ?? "Failed to create updated profile from dialog");
                }
            }
            else if (dialogResult == false)
            {
                _specificLogger.LogInformation("Memory region profile edit was cancelled by user (dialog returned false)");
                return ProfileDialogResult<MemoryMappingProfile>.Cancelled();
            }
            else // dialogResult is null
            {
                _specificLogger.LogWarning("Dialog returned null - this may indicate the dialog was closed without a result");
                return ProfileDialogResult<MemoryMappingProfile>.Cancelled();
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error showing edit dialog for memory region profile");
            return ProfileDialogResult<MemoryMappingProfile>.Failure($"Error editing profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the duplicate profile dialog.
    /// </summary>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        try
        {
            // Use the name input dialog for duplication
            ProfileDialogResult<string> nameResult = await _unifiedDialogService.ShowNameInputDialogAsync(
                $"Duplicate {GetProfileTypeName()}",
                $"Enter a name for the duplicated profile:",
                request.SuggestedName ?? "Copy"
            );

            return nameResult;
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error showing duplicate dialog for memory region profile");
            return ProfileDialogResult<string>.Failure($"Error duplicating profile: {ex.Message}");
        }
    }

    #endregion

    #region Initialization Methods



    /// <summary>
    /// Initializes profile management commands.
    /// </summary>
    private void InitializeProfileCommands()
    {
        // Base class provides Create, Edit, Duplicate, Delete commands
        // Additional memory-specific commands can be added here if needed
    }

    /// <summary>
    /// Initializes import/export commands.
    /// </summary>
    private void InitializeImportExportCommands()
    {
        IObservable<bool> canExportProfiles = this.WhenAnyValue(x => x.Profiles.Count)
            .Select(count => count > 0);

        ExportProfilesCommand = ReactiveCommand.CreateFromTask(ExportProfilesAsync, canExportProfiles);
        ExportProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting profiles"))
            .DisposeWith(_disposables);

        // Import profiles command - always enabled
        ImportProfilesCommand = ReactiveCommand.CreateFromTask(ImportProfilesAsync);
        ImportProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "importing profiles"))
            .DisposeWith(_disposables);
    }



    /// <summary>
    /// Subscribes to memory segment changes to update computed properties.
    /// </summary>
    private void SubscribeToSegmentChanges()
    {
        // First, unsubscribe from any previous segments
        UnsubscribeFromSegmentChanges();

        if (SelectedProfile?.Segments != null)
        {
            _specificLogger.LogDebug("Subscribing to segment property changes for profile {ProfileName} with {SegmentCount} segments",
                SelectedProfile.Name, SelectedProfile.Segments.Count);

            foreach (MemorySegment segment in SelectedProfile.Segments)
            {
                segment.PropertyChanged += OnSegmentPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Unsubscribes from memory segment change events.
    /// </summary>
    private void UnsubscribeFromSegmentChanges()
    {
        if (SelectedProfile?.Segments != null)
        {
            foreach (MemorySegment segment in SelectedProfile.Segments)
            {
                segment.PropertyChanged -= OnSegmentPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Handles property changes in memory segments to update computed properties.
    /// </summary>
    private void OnSegmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MemorySegment.IsSelected))
        {
            _specificLogger.LogDebug("Segment IsSelected property changed, updating computed properties");

            // Update all computed properties that depend on segment selection
            this.RaisePropertyChanged(nameof(SelectedSegments));
            this.RaisePropertyChanged(nameof(HasValidSelection));
            this.RaisePropertyChanged(nameof(SelectedSegmentCount));
        }
    }

    #endregion

    #region Path Management





    #endregion

    #region Import/Export

    /// <summary>
    /// Exports all profiles to a JSON file.
    /// </summary>
    private async Task ExportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_FileDialogServiceNotAvailable;
            });
            _specificLogger.LogWarning("Export profiles failed: File dialog service not available");
            return;
        }

        try
        {
            _specificLogger.LogDebug("Exporting memory region profiles");

            string? filePath = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Memory Region Profiles",
                "*.json",
                null,
                "memory-region-profiles.json").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(filePath))
            {
                IEnumerable<MemoryMappingProfile> profiles = await _profileService.ExportAsync().ConfigureAwait(false);
                string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    StatusMessage = string.Format(UIStrings.Status_ProfilesExportedToFile, Profiles.Count, Path.GetFileName(filePath));
                });
                _specificLogger.LogInformation("Exported {Count} memory region profiles to {FilePath}",
                    Profiles.Count, filePath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _specificLogger.LogError(ex, "Access denied while exporting profiles to {FilePath}", ex.Message);
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_ExportFailedAccessDenied;
            });
        }
        catch (IOException ex)
        {
            _specificLogger.LogError(ex, "I/O error while exporting profiles");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = string.Format(UIStrings.Status_ExportFailed, ex.Message);
            });
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to export memory region profiles");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = string.Format(UIStrings.Status_ExportFailed, ex.Message);
            });
        }
    }

    /// <summary>
    /// Imports profiles from a JSON file.
    /// </summary>
    private async Task ImportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_FileDialogServiceNotAvailable;
            });
            _specificLogger.LogWarning("Import profiles failed: File dialog service not available");
            return;
        }

        try
        {
            _specificLogger.LogDebug("Importing memory region profiles");

            string? filePath = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Memory Region Profiles",
                "*.json").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(filePath))
            {
                string json = await System.IO.File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                List<MemoryMappingProfile>? profiles = JsonSerializer.Deserialize<List<MemoryMappingProfile>>(json) ?? new List<MemoryMappingProfile>();

                if (profiles.Count == 0)
                {
                    await _uiThreadService.InvokeOnUIThreadAsync(() =>
                    {
                        StatusMessage = UIStrings.Status_ImportFailedNoValidProfiles;
                    });
                    _specificLogger.LogWarning("Import failed: No profiles found in {FilePath}", filePath);
                    return;
                }

                IEnumerable<MemoryMappingProfile> importedProfiles = await _profileService.ImportAsync(profiles, replaceExisting: false).ConfigureAwait(false);
                int count = importedProfiles.Count();

                _ = RefreshCommand.Execute();

                await _uiThreadService.InvokeOnUIThreadAsync(() =>
                {
                    StatusMessage = string.Format(UIStrings.Status_ProfilesImportedFromFile, count, Path.GetFileName(filePath));
                });
                _specificLogger.LogInformation("Imported {Count} memory region profiles from {FilePath}",
                    count, filePath);
            }
        }
        catch (FileNotFoundException ex)
        {
            _specificLogger.LogError(ex, "Import failed: File not found");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_ImportFailedFileNotFound;
            });
        }
        catch (JsonException ex)
        {
            _specificLogger.LogError(ex, "Import failed: Invalid JSON format");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_ImportFailedInvalidFormat;
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _specificLogger.LogError(ex, "Import failed: Access denied");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_ImportFailedAccessDenied;
            });
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to import memory region profiles");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = string.Format(UIStrings.Status_ImportFailed, ex.Message);
            });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Handles command exceptions with logging and user notification.
    /// </summary>
    private void HandleCommandException(Exception ex, string operation)
    {
        _specificLogger.LogError(ex, "Error {Operation}", operation);
        _ = _uiThreadService.InvokeOnUIThreadAsync(() =>
        {
            StatusMessage = string.Format(UIStrings.Status_ErrorOperation, operation, ex.Message);
        });
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Disposes of resources used by this ViewModel.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Unsubscribe from segment changes
                UnsubscribeFromSegmentChanges();



                _disposables.Dispose();
            }
            catch (Exception ex)
            {
                _specificLogger.LogWarning(ex, "Error during MemoryRegionSettingsViewModel disposal");
            }
        }

        base.Dispose(disposing);
    }

    #endregion
}
