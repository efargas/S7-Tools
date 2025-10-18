using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Helpers;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for the Serial Ports settings category, providing comprehensive profile management
/// and port discovery capabilities for Linux-based serial communication.
///
/// This implementation extends ProfileManagementViewModelBase to provide unified profile management
/// while preserving existing serial port specific functionality through composition and delegation.
/// </summary>
public class SerialPortsSettingsViewModel : ProfileManagementViewModelBase<SerialPortProfile>
{
    #region Fields

    private readonly ISerialPortProfileService _profileService;
    private readonly ISerialPortService _portService;
    private readonly IDialogService _dialogService;
    private readonly IProfileEditDialogService _profileEditDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<SerialPortsSettingsViewModel> _specificLogger;
    private readonly S7Tools.Services.Interfaces.ISettingsService _settingsService;
    private readonly IUnifiedProfileDialogService _unifiedDialogService;
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
    /// <param name="unifiedProfileDialogService">The unified profile dialog service.</param>
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
        IUnifiedProfileDialogService unifiedProfileDialogService,
        ILogger<SerialPortsSettingsViewModel> logger)
        : base(logger, unifiedProfileDialogService, dialogService, uiThreadService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _portService = portService ?? throw new ArgumentNullException(nameof(portService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _profileEditDialogService = profileEditDialogService ?? throw new ArgumentNullException(nameof(profileEditDialogService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _fileDialogService = fileDialogService;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _specificLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unifiedDialogService = unifiedProfileDialogService ?? throw new ArgumentNullException(nameof(unifiedProfileDialogService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));

        // Initialize serial port specific collections
        AvailablePorts = new ObservableCollection<string>();

        // Initialize serial port specific commands
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
            await base.InitializeAsync();
            await ScanPortsAsync();
        });

        _specificLogger.LogInformation("SerialPortsSettingsViewModel initialized");

        // Update STTY preview when either profile or port selection changes
        this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedPort)
            .Subscribe(values =>
            {
                (SerialPortProfile? profile, string? selectedPort) = values;
                try
                {
                    if (profile == null)
                    {
                        SelectedProfileSttyString = string.Empty;
                    }
                    else
                    {
                        // Use the actual selected port if available, otherwise use placeholder
                        string portToUse = !string.IsNullOrEmpty(selectedPort) ? selectedPort : "/dev/ttyS0";

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
    /// Gets the collection of available serial ports.
    /// </summary>
    public ObservableCollection<string> AvailablePorts { get; }

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

    private bool _isScanning;
    /// <summary>
    /// Gets or sets a value indicating whether port scanning is in progress.
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set => this.RaiseAndSetIfChanged(ref _isScanning, value);
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

    #endregion

    #region Abstract Method Implementations

    /// <summary>
    /// Gets the profile manager for serial port profiles.
    /// </summary>
    /// <returns>The serial port profile manager instance.</returns>
    protected override IProfileManager<SerialPortProfile> GetProfileManager()
    {
        return _profileService;
    }

    /// <summary>
    /// Gets the default profile name for create operations.
    /// </summary>
    /// <returns>The standardized default name "SerialDefault".</returns>
    protected override string GetDefaultProfileName()
    {
        return "SerialDefault";
    }

    /// <summary>
    /// Gets the profile type name for display and logging purposes.
    /// </summary>
    /// <returns>The human-readable profile type name "Serial Port".</returns>
    protected override string GetProfileTypeName()
    {
        return "Serial Port";
    }

    /// <summary>
    /// Creates a default profile instance with standard configuration.
    /// </summary>
    /// <returns>A new serial port profile instance with default values.</returns>
    protected override SerialPortProfile CreateDefaultProfile()
    {
        return SerialPortProfile.CreateDefaultProfile();
    }

    /// <summary>
    /// Shows the create dialog for serial port profiles.
    /// </summary>
    /// <param name="request">The create request with default values.</param>
    /// <returns>The dialog result with created profile or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<SerialPortProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        return await _unifiedDialogService.ShowSerialCreateDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the edit dialog for serial port profiles.
    /// </summary>
    /// <param name="request">The edit request with profile ID.</param>
    /// <returns>The dialog result with updated profile or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<SerialPortProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        return await _unifiedDialogService.ShowSerialEditDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the duplicate input dialog for serial port profiles.
    /// </summary>
    /// <param name="request">The duplicate request with source profile ID and suggested name.</param>
    /// <returns>The dialog result with new profile name or cancellation status.</returns>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        return await _unifiedDialogService.ShowSerialDuplicateDialogAsync(request).ConfigureAwait(false);
    }

    #endregion

    #region Commands

    // Serial Port Specific Commands (not provided by base class)

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
    /// Initializes serial port specific reactive commands with their execution logic and conditions.
    /// CRUD commands (Create, Edit, Delete, Duplicate, SetDefault, Refresh) are provided by base class.
    /// </summary>
    private void InitializeCommands()
    {
        // Serial port specific commands only - base class provides CRUD commands

        // Scan ports command - always enabled
        ScanPortsCommand = ReactiveCommand.CreateFromTask(ScanPortsAsync);
        ScanPortsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "scanning ports"))
            .DisposeWith(_disposables);

        // Test port command - enabled when both profile and port are selected
        IObservable<bool> canTestPort = this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedPort)
            .Select(tuple => tuple.Item1 != null && !string.IsNullOrEmpty(tuple.Item2));

        TestPortCommand = ReactiveCommand.CreateFromTask(TestPortAsync, canTestPort);
        TestPortCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing port"))
            .DisposeWith(_disposables);

        // Export profiles command - enabled when profiles exist
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

        // Export selected profile command - enabled when a profile is selected and has a valid Id
        IObservable<bool> canExportSelectedProfile = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null && profile.Id > 0);

        ExportSelectedProfileCommand = ReactiveCommand.CreateFromTask(ExportSelectedProfileAsync, canExportSelectedProfile);
        ExportSelectedProfileCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting selected profile"))
            .DisposeWith(_disposables);

        // Show profile details command - enabled when a profile is selected
        IObservable<bool> canShowProfileDetails = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null);

        ShowProfileDetailsCommand = ReactiveCommand.CreateFromTask(ShowProfileDetailsAsync, canShowProfileDetails);
        ShowProfileDetailsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "showing profile details"))
            .DisposeWith(_disposables);

        // Subscribe to settings changes to update ProfilesPath
        // no-op: primary command initialization happened above
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

            IEnumerable<Core.Services.Interfaces.SerialPortInfo> portInfos = await _portService.ScanAvailablePortsAsync();

            AvailablePorts.Clear();

            // Sort ports with ttyUSB* first (external serial adapters), then others alphabetically
            IOrderedEnumerable<Core.Services.Interfaces.SerialPortInfo> sortedPortInfos = portInfos
                .OrderBy(p => !p.PortPath.Contains("/ttyUSB")) // ttyUSB* ports come first (false sorts before true)
                .ThenBy(p => p.PortPath); // Then sort alphabetically within each group

            foreach (Core.Services.Interfaces.SerialPortInfo? portInfo in sortedPortInfos)
            {
                AvailablePorts.Add(portInfo.PortPath);
            }

            PortCount = AvailablePorts.Count;
            StatusMessage = $"Found {PortCount} port(s)";

            _specificLogger.LogInformation("Found {PortCount} available ports", PortCount);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error scanning for ports");
            StatusMessage = "Error scanning for ports";
        }
        finally
        {
            IsScanning = false;
        }
    }

    // CRUD methods (CreateProfileAsync, EditProfileAsync, DeleteProfileAsync,
    // DuplicateProfileAsync, SetDefaultProfileAsync) are handled by the base class

    #endregion

    #region Port Management Methods

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
            bool success = await _portService.ApplyConfigurationAsync(SelectedPort, SelectedProfile.Configuration);

            string message = success
                ? $"Port {SelectedPort} test successful"
                : $"Port {SelectedPort} test failed";

            await _dialogService.ShowErrorAsync("Port Test Result", message);

            StatusMessage = success ? "Port test successful" : "Port test failed";
            _specificLogger.LogInformation("Port test result for {Port}: {Success}", SelectedPort, success);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error testing port");
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
            StatusMessage = "Exporting profiles...";

            IEnumerable<SerialPortProfile> profiles = await _profileService.ExportAsync();
            string jsonData = System.Text.Json.JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fileName, jsonData);

            StatusMessage = $"Exported {Profiles.Count} profile(s) to {Path.GetFileName(fileName)}";
            _specificLogger.LogInformation("Exported {ProfileCount} profiles to {FileName}", Profiles.Count, fileName);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error exporting profiles");
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
            string? result = await _fileDialogService.ShowFolderBrowserDialogAsync("Select Profiles Directory", ProfilesPath);
            if (!string.IsNullOrEmpty(result))
            {
                ProfilesPath = result;
                await UpdateProfilesPathInSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error browsing for profiles path");
            StatusMessage = "Error selecting directory";
        }
    }

    /// <summary>
    /// Opens the profiles directory in the system file explorer.
    /// </summary>
    private async Task OpenProfilesPathAsync()
    {
        System.Diagnostics.Debug.WriteLine($"DEBUG: SerialPortsSettingsViewModel.OpenProfilesPathAsync called - Start");

        try
        {
            StatusMessage = "Opening profiles folder...";
            System.Diagnostics.Debug.WriteLine($"DEBUG: Opening profiles folder: {ProfilesPath}");

            if (string.IsNullOrEmpty(ProfilesPath))
            {
                StatusMessage = "Profiles path not available";
                _specificLogger.LogError("Profiles path is null or empty");
                System.Diagnostics.Debug.WriteLine($"ERROR: Profiles path is null or empty");
                return;
            }

            // Ensure the directory exists before trying to open it
            if (!Directory.Exists(ProfilesPath))
            {
                StatusMessage = "Creating profiles folder...";
                Directory.CreateDirectory(ProfilesPath);
                _specificLogger.LogInformation("Created profiles directory: {ProfilesPath}", ProfilesPath);
                System.Diagnostics.Debug.WriteLine($"DEBUG: Created profiles directory: {ProfilesPath}");
            }

            _specificLogger.LogInformation("Opening profiles folder: {ProfilesPath}", ProfilesPath);
            System.Diagnostics.Debug.WriteLine($"DEBUG: About to call PlatformHelper.OpenDirectoryInExplorerAsync");

            await PlatformHelper.OpenDirectoryInExplorerAsync(ProfilesPath);

            StatusMessage = "Profiles folder opened";
            _specificLogger.LogInformation("Successfully opened profiles folder");
            System.Diagnostics.Debug.WriteLine($"DEBUG: SerialPortsSettingsViewModel.OpenProfilesPathAsync completed successfully");
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error opening profiles folder");
            StatusMessage = "Error opening profiles folder";
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception in SerialPortsSettingsViewModel.OpenProfilesPathAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"ERROR: Exception details: {ex}");
        }

        System.Diagnostics.Debug.WriteLine($"DEBUG: SerialPortsSettingsViewModel.OpenProfilesPathAsync called - End");
    }

    private async Task ResetProfilesPathAsync()
    {
        try
        {
            // Reset to default path inside application resources
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string defaultPath = Path.Combine(baseDir, "resources", "SerialProfiles");
            ProfilesPath = defaultPath;
            await UpdateProfilesPathInSettingsAsync();
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error resetting profiles path");
            StatusMessage = "Error resetting profiles path";
        }
    }

    private async Task UpdateProfilesPathInSettingsAsync()
    {
        try
        {
            // Persist through the injected settings service
            Models.ApplicationSettings settings = _settingsService.Settings.Clone();
            settings.SerialPorts.ProfilesPath = ProfilesPath;
            await _settingsService.UpdateSettingsAsync(settings).ConfigureAwait(false);
            StatusMessage = "Profiles path updated";
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Failed to update settings with new profiles path");
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
            Models.ApplicationSettings settings = _settingsService.Settings;
            ProfilesPath = settings.SerialPorts?.ProfilesPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "SerialProfiles");
        }
        catch (Exception ex)
        {
            _specificLogger.LogWarning(ex, "Failed to refresh profiles path from settings");
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
            string? fileName = await _fileDialogService.ShowOpenFileDialogAsync(
                "Import Profiles",
                "JSON files (*.json)|*.json|All files (*.*)|*.*");

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Importing profiles...";

            string jsonData = await File.ReadAllTextAsync(fileName);
            List<SerialPortProfile> profiles = JsonSerializer.Deserialize<List<SerialPortProfile>>(jsonData) ?? new List<SerialPortProfile>();
            IEnumerable<SerialPortProfile> importedProfiles = await _profileService.ImportAsync(profiles, replaceExisting: false);

            int importedCount = importedProfiles.Count();
            await RefreshCommand.Execute(); // Refresh the list

            StatusMessage = $"Imported {importedCount} profile(s) from {Path.GetFileName(fileName)}";
            _specificLogger.LogInformation("Imported {ImportedCount} profiles from {FileName}", importedCount, fileName);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error importing profiles");
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
            StatusMessage = "Exporting profile...";

            string jsonData;

            // If the selected profile has a valid persisted Id, use the profile service which performs
            // any canonical serialization and validation. Otherwise fall back to directly serializing
            // the in-memory profile to allow exporting unsaved/imported profiles without throwing.
            if (SelectedProfile.Id > 0)
            {
                SerialPortProfile? profile = await _profileService.GetByIdAsync(SelectedProfile.Id);
                jsonData = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
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
            _specificLogger.LogInformation("Exported profile {ProfileName} to {FileName}", SelectedProfile.Name, fileName);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error exporting profile");
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
            string details = $"Profile: {SelectedProfile.Name}\n" +
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

            _specificLogger.LogInformation("Showed details for profile: {ProfileName}", SelectedProfile.Name);
        }
        catch (Exception ex)
        {
            _specificLogger.LogError(ex, "Error showing profile details");
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
        _specificLogger.LogError(exception, "Error {Operation}", operation);
        StatusMessage = $"Error {operation}";
    }

    #endregion

    #region Disposal Overrides

    /// <summary>
    /// Protected dispose pattern implementation.
    /// </summary>
    /// <param name="disposing">True when called from Dispose()</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _disposables?.Dispose();
                if (_settingsChangedHandler != null)
                {
                    _settingsService.SettingsChanged -= _settingsChangedHandler;
                }
            }
            catch { }
        }

        // Call base class disposal
        base.Dispose(disposing);
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
            await RefreshCommand.Execute();

            // If a specific profile Id was requested, try to select it
            if (selectProfileId.HasValue)
            {
                SerialPortProfile? match = Profiles.FirstOrDefault(p => p.Id == selectProfileId.Value);
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
            _specificLogger.LogWarning(ex, "Failed to refresh profiles while preserving selection");
        }
    }

    #endregion
}
