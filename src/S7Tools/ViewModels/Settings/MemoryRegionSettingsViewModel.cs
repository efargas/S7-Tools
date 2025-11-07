using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Helpers;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

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
    private EventHandler<S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs>? _settingsChangedHandler;
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

        // Initialize path commands
        InitializePathCommands();

        // Initialize profile commands
        InitializeProfileCommands();

        // Initialize export/import commands
        InitializeImportExportCommands();

        // Subscribe to settings changes for path updates
        SubscribeToSettingsChanges();

        // Initialize with current settings
        RefreshFromSettings();

        // Load initial data
        _ = Task.Run(async () =>
        {
            try
            {
                await InitializeAsync().ConfigureAwait(false);
                RefreshCommand.Execute().Subscribe();
            }
            catch (Exception ex)
            {
                _specificLogger.LogError(ex, "Error during initialization");
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

    private string _profilesPath = string.Empty;
    /// <summary>
    /// Gets or sets the path where memory region profiles are stored.
    /// </summary>
    public new string ProfilesPath
    {
        get => _profilesPath;
        set => this.RaiseAndSetIfChanged(ref _profilesPath, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to browse for a new profiles directory.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseProfilesPathCommand { get; private set; } = null!;

    /// <summary>
    /// Command to open the profiles directory in the file manager.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenProfilesPathCommand { get; private set; } = null!;

    /// <summary>
    /// Command to reset the profiles path to the default value.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetProfilesPathCommand { get; private set; } = null!;

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
            // For now, use the name input dialog until specific memory region dialogs are implemented
            ProfileDialogResult<string> nameResult = await _unifiedDialogService.ShowNameInputDialogAsync(
                $"Create {GetProfileTypeName()}",
                "Enter a name for the new memory region profile:",
                request.DefaultName ?? GetDefaultProfileName()
            );

            if (!nameResult.IsSuccess)
            {
                return ProfileDialogResult<MemoryMappingProfile>.Cancelled();
            }

            // Create a new profile with the provided name
            var newProfile = MemoryMappingProfile.CreateUserProfile(nameResult.Result ?? "New Profile");
            return ProfileDialogResult<MemoryMappingProfile>.Success(newProfile);
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
    protected override Task<ProfileDialogResult<MemoryMappingProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        try
        {
            // For now, just return the existing profile until specific edit dialogs are implemented
            if (SelectedProfile == null)
            {
                return Task.FromResult(ProfileDialogResult<MemoryMappingProfile>.Failure("No profile selected for editing"));
            }

            // TODO: Implement proper edit dialog when memory region edit dialogs are available
            _specificLogger.LogWarning("Memory region profile edit dialog not yet implemented, returning existing profile");
            return Task.FromResult(ProfileDialogResult<MemoryMappingProfile>.Success(SelectedProfile));
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error showing edit dialog for memory region profile");
            return Task.FromResult(ProfileDialogResult<MemoryMappingProfile>.Failure($"Error editing profile: {ex.Message}"));
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
    /// Initializes path management commands.
    /// </summary>
    private void InitializePathCommands()
    {
        BrowseProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowseProfilesPathAsync);
        OpenProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenProfilesPathAsync);
        ResetProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetProfilesPathAsync);
    }

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
        ExportProfilesCommand = ReactiveCommand.CreateFromTask(ExportProfilesAsync);
        ImportProfilesCommand = ReactiveCommand.CreateFromTask(ImportProfilesAsync);
    }

    /// <summary>
    /// Subscribes to settings changes for automatic path updates.
    /// </summary>
    private void SubscribeToSettingsChanges()
    {
        _settingsChangedHandler = OnSettingsChanged;
        _settingsService.SettingsChanged += _settingsChangedHandler;
    }

    #endregion

    #region Path Management

    /// <summary>
    /// Handles settings changes and refreshes path if relevant settings changed.
    /// </summary>
    private async void OnSettingsChanged(object? sender, S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs e)
    {
        if (e.Key.StartsWith("memoryRegion.", StringComparison.OrdinalIgnoreCase))
        {
            await _uiThreadService.InvokeOnUIThreadAsync(RefreshFromSettings);
        }
    }

    /// <summary>
    /// Refreshes the ViewModel state from current application settings.
    /// </summary>
    private void RefreshFromSettings()
    {
        try
        {
            // Refresh the resolved path
            ProfilesPath = _pathService.MemoryRegionProfilesPath;

            this.RaisePropertyChanged(nameof(ProfilesPath));

            _specificLogger.LogDebug("Refreshed memory region settings - ProfilesPath: {ProfilesPath}", ProfilesPath);
        }
        catch (Exception ex)
        {
            _specificLogger.LogWarning(ex, "Error refreshing memory region settings from application settings");
        }
    }

    /// <summary>
    /// Browses for a new profiles directory and updates the setting.
    /// </summary>
    private async Task BrowseProfilesPathAsync()
    {
        try
        {
            if (_fileDialogService == null)
            {
                await _dialogService.ShowErrorAsync(
                    "File Dialog Unavailable",
                    "The file dialog service is not available.");
                return;
            }

            string currentDirectory = Path.GetDirectoryName(ProfilesPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string? selectedPath = await _fileDialogService.ShowFolderBrowserDialogAsync(
                "Select Memory Region Profiles Directory",
                currentDirectory);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Create filename for the new path
                string newProfilesPath = Path.Combine(selectedPath, "profiles.json");

                // Update setting which will trigger refresh
                await _settingsService.SetSettingAsync("memoryRegion.profilesPath", newProfilesPath);

                StatusMessage = $"Profiles path updated to: {newProfilesPath}";
                _specificLogger.LogInformation("Memory region profiles path updated to: {ProfilesPath}", newProfilesPath);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error browsing for memory region profiles path");
            await _dialogService.ShowErrorAsync("Browse Error", $"Failed to browse for profiles path: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the profiles directory in the system file manager.
    /// </summary>
    private async Task OpenProfilesPathAsync()
    {
        try
        {
            string? directory = Path.GetDirectoryName(ProfilesPath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = directory,
                    UseShellExecute = true
                });

                StatusMessage = $"Opened directory: {directory}";
            }
            else
            {
                await _dialogService.ShowErrorAsync(
                    "Directory Not Found",
                    $"The profiles directory does not exist: {directory}");
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error opening memory region profiles directory");
            await _dialogService.ShowErrorAsync("Open Error", $"Failed to open profiles directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets the profiles path to the default value.
    /// </summary>
    private async Task ResetProfilesPathAsync()
    {
        try
        {
            string defaultPath = Path.Combine("src", "resources", "MemoryRegionProfiles", "profiles.json");

            // Update setting which will trigger refresh
            await _settingsService.SetSettingAsync("memoryRegion.profilesPath", defaultPath);

            StatusMessage = "Profiles path reset to default";
            _specificLogger.LogInformation("Memory region profiles path reset to default: {DefaultPath}", defaultPath);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error resetting memory region profiles path");
            await _dialogService.ShowErrorAsync("Reset Error", $"Failed to reset profiles path: {ex.Message}");
        }
    }

    #endregion

    #region Import/Export

    /// <summary>
    /// Exports all profiles to a JSON file.
    /// </summary>
    private async Task ExportProfilesAsync()
    {
        try
        {
            if (_fileDialogService == null)
            {
                await _dialogService.ShowErrorAsync(
                    "File Dialog Unavailable",
                    "The file dialog service is not available for export.");
                return;
            }

            string? fileName = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Memory Region Profiles",
                "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Path.GetDirectoryName(ProfilesPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"memory-region-profiles-{DateTime.Now:yyyyMMdd-HHmmss}.json");

            if (!string.IsNullOrEmpty(fileName))
            {
                IEnumerable<MemoryMappingProfile> profiles = await _profileService.GetAllAsync();
                var profileList = profiles.ToList();
                string json = JsonSerializer.Serialize(profileList, new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(fileName, json);

                StatusMessage = $"Exported {profileList.Count} profiles to: {Path.GetFileName(fileName)}";
                _specificLogger.LogInformation("Exported {Count} memory region profiles to {FileName}", profileList.Count, fileName);
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error exporting memory region profiles");
            await _dialogService.ShowErrorAsync("Export Error", $"Failed to export profiles: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports profiles from a JSON file.
    /// </summary>
    private async Task ImportProfilesAsync()
    {
        try
        {
            if (_fileDialogService == null)
            {
                await _dialogService.ShowErrorAsync(
                    "File Dialog Unavailable",
                    "The file dialog service is not available for import.");
                return;
            }

            string? fileName = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Memory Region Profiles",
                "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Path.GetDirectoryName(ProfilesPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                string json = await File.ReadAllTextAsync(fileName);
                List<MemoryMappingProfile>? importedProfiles = JsonSerializer.Deserialize<List<MemoryMappingProfile>>(json);
                if (importedProfiles == null)
                {
                    await _dialogService.ShowErrorAsync("Import Error", "Failed to parse profiles from file");
                    return;
                }

                if (importedProfiles?.Count > 0)
                {
                    int imported = 0;
                    int skipped = 0;

                    foreach (MemoryMappingProfile profile in importedProfiles)
                    {
                        try
                        {
                            // Reset ID to ensure new ID assignment
                            profile.Id = 0;
                            await _profileService.CreateAsync(profile);
                            imported++;
                        }
                        catch (Exception ex)
                        {
                            _specificLogger.LogWarning(ex, "Skipped importing profile {ProfileName}", profile.Name);
                            skipped++;
                        }
                    }

                    // Refresh the profiles list
                    RefreshCommand.Execute().Subscribe();

                    StatusMessage = $"Imported {imported} profiles, skipped {skipped} profiles";
                    _specificLogger.LogInformation("Imported {Imported} memory region profiles, skipped {Skipped}", imported, skipped);
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        "No Profiles Found",
                        "The selected file contains no valid memory region profiles.");
                }
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error importing memory region profiles");
            await _dialogService.ShowErrorAsync("Import Error", $"Failed to import profiles: {ex.Message}");
        }
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
                // Unsubscribe from settings changes
                if (_settingsChangedHandler != null)
                {
                    _settingsService.SettingsChanged -= _settingsChangedHandler;
                }

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
