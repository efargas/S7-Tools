using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Helpers;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Serial Ports settings category, providing comprehensive profile management
/// and port discovery capabilities for Linux-based serial communication.
/// </summary>
public class SerialPortsSettingsViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly ISerialPortProfileService _profileService;
    private readonly ISerialPortService _portService;
    private readonly IDialogService _dialogService;
    private readonly IProfileEditDialogService _profileEditDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<SerialPortsSettingsViewModel> _logger;
    private readonly S7Tools.Services.Interfaces.ISettingsService _settingsService;
    private readonly S7Tools.Services.Interfaces.IUIThreadService _uiThreadService;
    private EventHandler<S7Tools.Models.ApplicationSettings>? _settingsChangedHandler;
    private readonly CompositeDisposable _disposables = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SerialPortsSettingsViewModel class.
    /// </summary>
    /// <param name="profileService">The serial port profile service.</param>
    /// <param name="portService">The serial port service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="profileEditDialogService">The profile edit dialog service.</param>
    /// <param name="clipboardService">The clipboard service.</param>
    /// <param name="fileDialogService">The file dialog service.</param>
    /// <param name="settingsService">The settings service used to persist application settings.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="logger">The logger.</param>
    public SerialPortsSettingsViewModel(
    ISerialPortProfileService profileService,
    ISerialPortService portService,
    IDialogService dialogService,
    IProfileEditDialogService profileEditDialogService,
    IClipboardService clipboardService,
    IFileDialogService? fileDialogService,
    S7Tools.Services.Interfaces.ISettingsService settingsService,
    S7Tools.Services.Interfaces.IUIThreadService uiThreadService,
    ILogger<SerialPortsSettingsViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _portService = portService ?? throw new ArgumentNullException(nameof(portService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _profileEditDialogService = profileEditDialogService ?? throw new ArgumentNullException(nameof(profileEditDialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize collections
        Profiles = new ObservableCollection<SerialPortProfile>();
        AvailablePorts = new ObservableCollection<string>();

        // Initialize commands
        InitializeCommands();

        // Initialize path commands
        BrowseProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowseProfilesPathAsync);
        OpenProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenProfilesPathAsync);
        ResetProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetProfilesPathAsync);

        // Initialize ProfilesPath from settings and subscribe to changes
        RefreshFromSettings();
        _settingsChangedHandler = (_, __) => RefreshFromSettings();
        _settingsService.SettingsChanged += _settingsChangedHandler;

        // Load initial data
        // Load profiles and scan ports in background but marshal collection updates to UI thread
        _ = Task.Run(async () =>
        {
            await LoadProfilesAsync();
            await ScanPortsAsync();
        });

        _logger.LogInformation("SerialPortsSettingsViewModel initialized");

        // Update STTY preview when either profile or port selection changes
        this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedPort)
            .Subscribe(values =>
            {
                var (profile, selectedPort) = values;
                try
                {
                    if (profile == null)
                    {
                        SelectedProfileSttyString = string.Empty;
                    }
                    else
                    {
                        // Use the actual selected port if available, otherwise use placeholder
                        var portToUse = !string.IsNullOrEmpty(selectedPort) ? selectedPort : "/dev/ttyS0";

                        try
                        {
                            // Generate command with the actual selected port for accurate preview
                            SelectedProfileSttyString = _portService.GenerateSttyCommandForProfile(portToUse, profile);
                        }
                        catch
                        {
                            // Fallback to serializing configuration flags manually
                            SelectedProfileSttyString = System.Text.Json.JsonSerializer.Serialize(profile.Configuration, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        }
                    }
                }
                catch { }
            })
            .DisposeWith(_disposables);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of available serial port profiles.
    /// </summary>
    public ObservableCollection<SerialPortProfile> Profiles { get; }

    /// <summary>
    /// Gets the collection of available serial ports.
    /// </summary>
    public ObservableCollection<string> AvailablePorts { get; }

    private SerialPortProfile? _selectedProfile;
    /// <summary>
    /// Gets or sets the currently selected profile.
    /// </summary>
    public SerialPortProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    private string _selectedProfileSttyString = string.Empty;
    /// <summary>
    /// Gets the generated stty command string for the currently selected profile.
    /// </summary>
    public string SelectedProfileSttyString
    {
        get => _selectedProfileSttyString;
        private set => this.RaiseAndSetIfChanged(ref _selectedProfileSttyString, value);
    }

    private string? _selectedPort;
    /// <summary>
    /// Gets or sets the currently selected port.
    /// </summary>
    public string? SelectedPort
    {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    private bool _isLoading;
    /// <summary>
    /// Gets or sets a value indicating whether a loading operation is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private string _statusMessage = "Ready";
    /// <summary>
    /// Gets or sets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private bool _isScanning;
    /// <summary>
    /// Gets or sets a value indicating whether port scanning is in progress.
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }

    private string _newProfileName = string.Empty;
    /// <summary>
    /// Gets or sets the name for a new profile being created.
    /// </summary>
    public string NewProfileName
    {
        get => _newProfileName;
        set => this.RaiseAndSetIfChanged(ref _newProfileName, value);
    }

    private string _newProfileDescription = string.Empty;
    /// <summary>
    /// Gets or sets the description for a new profile being created.
    /// </summary>
    public string NewProfileDescription
    {
        get => _newProfileDescription;
        set => this.RaiseAndSetIfChanged(ref _newProfileDescription, value);
    }

    private int _profileCount;
    /// <summary>
    /// Gets or sets the total number of profiles.
    /// </summary>
    public int ProfileCount
    {
        get => _profileCount;
        set => this.RaiseAndSetIfChanged(ref _profileCount, value);
    }

    private int _portCount;
    /// <summary>
    /// Gets or sets the total number of available ports.
    /// </summary>
    public int PortCount
    {
        get => _portCount;
        set => this.RaiseAndSetIfChanged(ref _portCount, value);
    }

    private string _profilesPath = string.Empty;
    /// <summary>
    /// Gets or sets the current profiles directory path shown in the UI.
    /// This value is persisted to application settings when changed.
    /// </summary>
    public string ProfilesPath
    {
        get => _profilesPath;
        set => this.RaiseAndSetIfChanged(ref _profilesPath, value);
    }


    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to create a new profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to edit the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EditProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to delete the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to duplicate the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DuplicateProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to set the selected profile as default.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SetDefaultProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to refresh the profiles list.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshProfilesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to scan for available ports.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ScanPortsCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to test the selected port with the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestPortCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to export profiles.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportProfilesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to import profiles.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportProfilesCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to export the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportSelectedProfileCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to show profile details.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowProfileDetailsCommand { get; private set; } = null!;

    // Path management commands
    /// <summary>
    /// Command to open a folder browser to select the profiles directory.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseProfilesPathCommand { get; private set; } = null!;

    /// <summary>
    /// Command to open the profiles directory in the system file explorer.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenProfilesPathCommand { get; private set; } = null!;

    /// <summary>
    /// Command to reset the profiles path to the default within the application resources.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetProfilesPathCommand { get; private set; } = null!;

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes all reactive commands with their execution logic and conditions.
    /// </summary>
    private void InitializeCommands()
    {
        // Create profile command - enabled when name is not empty
        var canCreateProfile = this.WhenAnyValue(x => x.NewProfileName)
            .Select(name => !string.IsNullOrWhiteSpace(name));

        CreateProfileCommand = ReactiveCommand.CreateFromTask(CreateProfileAsync, canCreateProfile);
        CreateProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "creating profile"))
            .DisposeWith(_disposables);

        // Edit profile command - enabled when a profile is selected and not read-only
        var canEditProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && profile.CanModify());

        EditProfileCommand = ReactiveCommand.CreateFromTask(EditProfileAsync, canEditProfile);
        EditProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "editing profile"))
            .DisposeWith(_disposables);

        // Delete profile command - enabled when a profile is selected and can be deleted
        var canDeleteProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && profile.CanDelete());

        DeleteProfileCommand = ReactiveCommand.CreateFromTask(DeleteProfileAsync, canDeleteProfile);
        DeleteProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "deleting profile"))
            .DisposeWith(_disposables);

        // Duplicate profile command - enabled when a profile is selected
        var canDuplicateProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        DuplicateProfileCommand = ReactiveCommand.CreateFromTask(DuplicateProfileAsync, canDuplicateProfile);
        DuplicateProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "duplicating profile"))
            .DisposeWith(_disposables);

        // Set default profile command - enabled when a profile is selected and not already default
        var canSetDefaultProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && !profile.IsDefault);

        SetDefaultProfileCommand = ReactiveCommand.CreateFromTask(SetDefaultProfileAsync, canSetDefaultProfile);
        SetDefaultProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "setting default profile"))
            .DisposeWith(_disposables);

        // Refresh profiles command - always enabled
        RefreshProfilesCommand = ReactiveCommand.CreateFromTask(LoadProfilesAsync);
        RefreshProfilesCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "refreshing profiles"))
            .DisposeWith(_disposables);

        // Scan ports command - always enabled
        ScanPortsCommand = ReactiveCommand.CreateFromTask(ScanPortsAsync);
        ScanPortsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "scanning ports"))
            .DisposeWith(_disposables);

        // Test port command - enabled when both profile and port are selected
        var canTestPort = this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedPort)
            .Select(tuple => tuple.Item1 != null && !string.IsNullOrEmpty(tuple.Item2));

        TestPortCommand = ReactiveCommand.CreateFromTask(TestPortAsync, canTestPort);
        TestPortCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing port"))
            .DisposeWith(_disposables);

        // Export profiles command - enabled when profiles exist
        var canExportProfiles = this.WhenAnyValue(x => x.ProfileCount)
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

        // Export selected profile command - enabled when a profile is selected and has a valid Id
        var canExportSelectedProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && profile.Id > 0);

        ExportSelectedProfileCommand = ReactiveCommand.CreateFromTask(ExportSelectedProfileAsync, canExportSelectedProfile);
        ExportSelectedProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting selected profile"))
            .DisposeWith(_disposables);

        // Show profile details command - enabled when a profile is selected
        var canShowProfileDetails = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        ShowProfileDetailsCommand = ReactiveCommand.CreateFromTask(ShowProfileDetailsAsync, canShowProfileDetails);
        ShowProfileDetailsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "showing profile details"))
            .DisposeWith(_disposables);

        // Subscribe to settings changes to update ProfilesPath
        // no-op: primary command initialization happened above
    }

    /// <summary>
    /// Loads all profiles from the profile service.
    /// </summary>
    private async Task LoadProfilesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading profiles...";

            var profiles = await _profileService.GetAllProfilesAsync();

            // Update the ObservableCollection on the UI thread to avoid cross-thread exceptions
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                Profiles.Clear();
                foreach (var profile in profiles.OrderBy(p => p.IsDefault ? 0 : 1).ThenBy(p => p.Name))
                {
                    Profiles.Add(profile);
                }
                ProfileCount = Profiles.Count;

                // Ensure a SelectedProfile exists to avoid null-binding errors in the view.
                if (Profiles.Count > 0 && SelectedProfile == null)
                {
                    SelectedProfile = Profiles.First();
                }
            });
            StatusMessage = $"Loaded {ProfileCount} profile(s)";

            _logger.LogInformation("Loaded {ProfileCount} profiles", ProfileCount);
            // ProfilesPath is managed via settings; RefreshFromSettings handles syncing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profiles");
            StatusMessage = "Error loading profiles";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Scans for available serial ports.
    /// </summary>
    private async Task ScanPortsAsync()
    {
        try
        {
            IsScanning = true;
            StatusMessage = "Scanning for ports...";

            var portInfos = await _portService.ScanAvailablePortsAsync();

            AvailablePorts.Clear();

            // Sort ports with ttyUSB* first (external serial adapters), then others alphabetically
            var sortedPortInfos = portInfos
                .OrderBy(p => !p.PortPath.Contains("/ttyUSB")) // ttyUSB* ports come first (false sorts before true)
                .ThenBy(p => p.PortPath); // Then sort alphabetically within each group

            foreach (var portInfo in sortedPortInfos)
            {
                AvailablePorts.Add(portInfo.PortPath);
            }

            PortCount = AvailablePorts.Count;
            StatusMessage = $"Found {PortCount} port(s)";

            _logger.LogInformation("Found {PortCount} available ports", PortCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for ports");
            StatusMessage = "Error scanning for ports";
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>
    /// Creates a new profile with the specified name and description.
    /// </summary>
    private async Task CreateProfileAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Creating profile...";

            // Check if name is available
            var isAvailable = await _profileService.IsProfileNameAvailableAsync(NewProfileName);
            if (!isAvailable)
            {
                StatusMessage = "Profile name already exists";
                return;
            }

            // Create new profile
            var newProfile = SerialPortProfile.CreateUserProfile(NewProfileName, NewProfileDescription);
            var createdProfile = await _profileService.CreateProfileAsync(newProfile);

            // Refresh list and select created profile to ensure canonical state
            await RefreshProfilesPreserveSelectionAsync(createdProfile.Id);

            // Clear input fields
            NewProfileName = string.Empty;
            NewProfileDescription = string.Empty;

            StatusMessage = $"Profile '{createdProfile.Name}' created successfully";
            _logger.LogInformation("Created new profile: {ProfileName}", createdProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            StatusMessage = "Error creating profile";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Opens the profile editor for the selected profile.
    /// </summary>
    private async Task EditProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            StatusMessage = "Opening profile editor...";

            // Preserve the profile ID for refresh after editing
            var profileId = SelectedProfile.Id;

            // Create a new SerialPortProfileViewModel for editing
            var profileViewModel = new SerialPortProfileViewModel(
                _profileService,
                _portService,
                _clipboardService,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SerialPortProfileViewModel>.Instance);

            // Load the profile into the ViewModel
            profileViewModel.LoadProfile(SelectedProfile);

            // Show the profile edit dialog
            var result = await _profileEditDialogService.ShowSerialProfileEditAsync(
                $"Edit Profile - {SelectedProfile.Name}",
                profileViewModel);

            if (result.IsSuccess && result.ProfileViewModel != null)
            {
                StatusMessage = "Profile changes saved successfully";

                // The profile has been saved by the dialog's SaveCommand
                // Refresh our profiles collection to reflect the changes
                await RefreshProfilesPreserveSelectionAsync(profileId);

                StatusMessage = $"Profile updated successfully";
                _logger.LogInformation("Successfully edited serial profile with ID: {ProfileId}", profileId);
            }
            else
            {
                StatusMessage = "Profile editing cancelled";
                _logger.LogInformation("Serial profile editing cancelled for profile ID: {ProfileId}", profileId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening profile editor");
            StatusMessage = "Error opening profile editor";
        }
    }

    /// <summary>
    /// Deletes the selected profile after confirmation.
    /// </summary>
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            // Confirm deletion
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Delete Profile",
                $"Are you sure you want to delete the profile '{SelectedProfile.Name}'?");

            if (!confirmed)
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Deleting profile...";

            var profileName = SelectedProfile.Name;
            var idToDelete = SelectedProfile.Id;
            var success = await _profileService.DeleteProfileAsync(idToDelete);

            if (success)
            {
                // Refresh profiles; preserve selection if possible (select next available)
                await RefreshProfilesPreserveSelectionAsync(null);

                StatusMessage = $"Profile '{profileName}' deleted successfully";
                _logger.LogInformation("Deleted profile: {ProfileName}", profileName);
            }
            else
            {
                StatusMessage = "Failed to delete profile";
                _logger.LogWarning("Failed to delete profile: {ProfileName}", profileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile");
            StatusMessage = "Error deleting profile";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Duplicates the selected profile with a new name.
    /// </summary>
    private async Task DuplicateProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            // TODO: Implement name input dialog
            var newName = $"{SelectedProfile.Name} (Copy)";

            IsLoading = true;
            StatusMessage = "Duplicating profile...";

            var duplicatedProfile = await _profileService.DuplicateProfileAsync(SelectedProfile.Id, newName);

            // Refresh and select duplicated profile
            await RefreshProfilesPreserveSelectionAsync(duplicatedProfile.Id);

            StatusMessage = $"Profile duplicated as '{duplicatedProfile.Name}'";
            _logger.LogInformation("Duplicated profile: {OriginalName} -> {NewName}", SelectedProfile?.Name, duplicatedProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile");
            StatusMessage = "Error duplicating profile";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Sets the selected profile as the default profile.
    /// </summary>
    private async Task SetDefaultProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Setting default profile...";

            // Preserve the profile ID for refresh after setting default
            var profileId = SelectedProfile.Id;
            var profileName = SelectedProfile.Name;

            await _profileService.SetDefaultProfileAsync(profileId);

            // Persisted change made; refresh profiles and keep selection
            await RefreshProfilesPreserveSelectionAsync(profileId);

            StatusMessage = $"'{profileName}' set as default profile";
            _logger.LogInformation("Set default profile: {ProfileName}", profileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default profile");
            StatusMessage = "Error setting default profile";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Tests the selected port with the selected profile configuration.
    /// </summary>
    private async Task TestPortAsync()
    {
        if (SelectedProfile == null || string.IsNullOrEmpty(SelectedPort))
        {
            return;
        }

        try
        {
            StatusMessage = "Testing port configuration...";

            // Apply configuration to test the port
            var success = await _portService.ApplyConfigurationAsync(SelectedPort, SelectedProfile.Configuration);

            var message = success
                ? $"Port {SelectedPort} test successful"
                : $"Port {SelectedPort} test failed";

            await _dialogService.ShowErrorAsync("Port Test Result", message);

            StatusMessage = success ? "Port test successful" : "Port test failed";
            _logger.LogInformation("Port test result for {Port}: {Success}", SelectedPort, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing port");
            StatusMessage = "Error testing port";
        }
    }

    /// <summary>
    /// Exports all profiles to a JSON file.
    /// </summary>
    private async Task ExportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = "File dialog service not available";
            return;
        }

        try
        {
            var fileName = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Profiles",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                null,
                "profiles.json");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Exporting profiles...";

            var jsonData = await _profileService.ExportAllProfilesToJsonAsync();
            await File.WriteAllTextAsync(fileName, jsonData);

            StatusMessage = $"Exported {ProfileCount} profile(s) to {Path.GetFileName(fileName)}";
            _logger.LogInformation("Exported {ProfileCount} profiles to {FileName}", ProfileCount, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting profiles");
            StatusMessage = "Error exporting profiles";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BrowseProfilesPathAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = "File dialog service not available";
            return;
        }

        try
        {
            var result = await _fileDialogService.ShowFolderBrowserDialogAsync("Select Profiles Directory", ProfilesPath);
            if (!string.IsNullOrEmpty(result))
            {
                ProfilesPath = result;
                await UpdateProfilesPathInSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for profiles path");
            StatusMessage = "Error selecting directory";
        }
    }

    /// <summary>
    /// Opens the profiles directory in the system file explorer.
    /// </summary>
    private async Task OpenProfilesPathAsync()
    {
        try
        {
            StatusMessage = "Opening profiles folder...";

            if (string.IsNullOrEmpty(ProfilesPath))
            {
                StatusMessage = "Profiles path not available";
                _logger.LogError("Profiles path is null or empty");
                return;
            }

            // Ensure the directory exists before trying to open it
            if (!Directory.Exists(ProfilesPath))
            {
                StatusMessage = "Creating profiles folder...";
                Directory.CreateDirectory(ProfilesPath);
                _logger.LogInformation("Created profiles directory: {ProfilesPath}", ProfilesPath);
            }

            _logger.LogInformation("Opening profiles folder: {ProfilesPath}", ProfilesPath);

            await PlatformHelper.OpenDirectoryInExplorerAsync(ProfilesPath);

            StatusMessage = "Profiles folder opened";
            _logger.LogInformation("Successfully opened profiles folder");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening profiles folder");
            StatusMessage = "Error opening profiles folder";
        }
    }

    private async Task ResetProfilesPathAsync()
    {
        try
        {
            // Reset to default path inside application resources
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var defaultPath = Path.Combine(baseDir, "resources", "SerialProfiles");
            ProfilesPath = defaultPath;
            await UpdateProfilesPathInSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting profiles path");
            StatusMessage = "Error resetting profiles path";
        }
    }

    private async Task UpdateProfilesPathInSettingsAsync()
    {
        try
        {
            // Persist through the injected settings service
            var settings = _settingsService.Settings.Clone();
            settings.SerialPorts.ProfilesPath = ProfilesPath;
            await _settingsService.UpdateSettingsAsync(settings).ConfigureAwait(false);
            StatusMessage = "Profiles path updated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings with new profiles path");
            StatusMessage = "Failed to update settings";
        }
    }



    /// <summary>
    /// Refreshes the view model properties from the persisted settings.
    /// </summary>
    private void RefreshFromSettings()
    {
        try
        {
            var settings = _settingsService.Settings;
            ProfilesPath = settings.SerialPorts?.ProfilesPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "SerialProfiles");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh profiles path from settings");
        }
    }

    /// <summary>
    /// Imports profiles from a JSON file.
    /// </summary>
    private async Task ImportProfilesAsync()
    {
        if (_fileDialogService == null)
        {
            StatusMessage = "File dialog service not available";
            return;
        }

        try
        {
            var fileName = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Profiles",
                "JSON files (*.json)|*.json|All files (*.*)|*.*");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Importing profiles...";

            var jsonData = await File.ReadAllTextAsync(fileName);
            var importedProfiles = await _profileService.ImportProfilesFromJsonAsync(jsonData, overwriteExisting: false);

            var importedCount = importedProfiles.Count();
            await LoadProfilesAsync(); // Refresh the list

            StatusMessage = $"Imported {importedCount} profile(s) from {Path.GetFileName(fileName)}";
            _logger.LogInformation("Imported {ImportedCount} profiles from {FileName}", importedCount, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing profiles");
            StatusMessage = "Error importing profiles";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Exports the selected profile to a JSON file.
    /// </summary>
    private async Task ExportSelectedProfileAsync()
    {
        if (SelectedProfile == null || _fileDialogService == null)
        {
            return;
        }

        try
        {
            var fileName = await _fileDialogService.ShowSaveFileDialogAsync(
                "Export Profile",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                null,
                $"{SelectedProfile.Name}.json");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Exporting profile...";

            string jsonData;

            // If the selected profile has a valid persisted Id, use the profile service which performs
            // any canonical serialization and validation. Otherwise fall back to directly serializing
            // the in-memory profile to allow exporting unsaved/imported profiles without throwing.
            if (SelectedProfile.Id > 0)
            {
                jsonData = await _profileService.ExportProfileToJsonAsync(SelectedProfile.Id);
            }
            else
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                jsonData = System.Text.Json.JsonSerializer.Serialize(SelectedProfile, options);
            }

            await File.WriteAllTextAsync(fileName, jsonData);

            StatusMessage = $"Exported profile '{SelectedProfile.Name}' to {Path.GetFileName(fileName)}";
            _logger.LogInformation("Exported profile {ProfileName} to {FileName}", SelectedProfile.Name, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting profile");
            StatusMessage = "Error exporting profile";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Shows detailed information about the selected profile.
    /// </summary>
    private async Task ShowProfileDetailsAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        try
        {
            var details = $"Profile: {SelectedProfile.Name}\n" +
                         $"Description: {SelectedProfile.Description}\n" +
                         $"Baud Rate: {SelectedProfile.Configuration.BaudRate}\n" +
                         $"Character Size: {SelectedProfile.Configuration.CharacterSize}\n" +
                         $"Parity: {SelectedProfile.Configuration.Parity}\n" +
                         $"Stop Bits: {SelectedProfile.Configuration.StopBits}\n" +
                         $"Raw Mode: {SelectedProfile.Configuration.RawMode}\n" +
                         $"Default: {SelectedProfile.IsDefault}\n" +
                         $"Read-Only: {SelectedProfile.IsReadOnly}\n" +
                         $"Created: {SelectedProfile.CreatedAt:yyyy-MM-dd HH:mm:ss}\n" +
                         $"Modified: {SelectedProfile.ModifiedAt:yyyy-MM-dd HH:mm:ss}";

            await _dialogService.ShowErrorAsync("Profile Details", details);

            _logger.LogInformation("Showed details for profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing profile details");
            StatusMessage = "Error showing profile details";
        }
    }

    /// <summary>
    /// Handles exceptions thrown by reactive commands.
    /// </summary>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="operation">The operation that was being performed.</param>
    private void HandleCommandException(Exception exception, string operation)
    {
        _logger.LogError(exception, "Error {Operation}", operation);
        StatusMessage = $"Error {operation}";
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of the resources used by this ViewModel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose pattern implementation.
    /// </summary>
    /// <param name="disposing">True when called from Dispose()</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _disposables?.Dispose();
                _settingsService.SettingsChanged -= (_, _) => RefreshFromSettings();
            }
            catch { }
        }

        try
        {
            _logger.LogInformation("SerialPortsSettingsViewModel disposed");
        }
        catch { }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Refreshes profiles from the service and optionally restores selection by profile Id.
    /// </summary>
    private async Task RefreshProfilesPreserveSelectionAsync(int? selectProfileId)
    {
        try
        {
            // Reload profiles from storage/service
            await LoadProfilesAsync();

            // If a specific profile Id was requested, try to select it
            if (selectProfileId.HasValue)
            {
                var match = Profiles.FirstOrDefault(p => p.Id == selectProfileId.Value);
                if (match != null)
                {
                    SelectedProfile = match;
                    return;
                }
            }

            // Otherwise, ensure there's a sensible selection
            if (Profiles.Count > 0 && SelectedProfile == null)
            {
                SelectedProfile = Profiles.First();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh profiles while preserving selection");
        }
    }

    #endregion
}
