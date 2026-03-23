using S7Tools.ViewModels.Base;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Helpers;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Settings;

/// <summary>
/// ViewModel for paths settings configuration.
/// </summary>
public class PathSettingsViewModel : ViewModelBase
{
    private readonly S7Tools.Core.Interfaces.Services.IApplicationSettingsService _settingsService;
    private readonly IPathService _pathService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly ILogger<PathSettingsViewModel> _logger;
    private EventHandler<S7Tools.Core.Interfaces.Services.SettingsChangedEventArgs>? _settingsChangedHandler;

    public PathSettingsViewModel(
        S7Tools.Core.Interfaces.Services.IApplicationSettingsService settingsService,
        IPathService pathService,
        IFileDialogService? fileDialogService,
        ILogger<PathSettingsViewModel> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _fileDialogService = fileDialogService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create commands
        BrowseSerialProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowseSerialProfilesPathAsync);
        OpenSerialProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenSerialProfilesPathAsync);
        ResetSerialProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetSerialProfilesPathAsync);

        BrowseSocatProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowseSocatProfilesPathAsync);
        OpenSocatProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenSocatProfilesPathAsync);
        ResetSocatProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetSocatProfilesPathAsync);

        BrowsePowerSupplyProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowsePowerSupplyProfilesPathAsync);
        OpenPowerSupplyProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenPowerSupplyProfilesPathAsync);
        ResetPowerSupplyProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetPowerSupplyProfilesPathAsync);

        BrowseMemoryRegionProfilesPathCommand = ReactiveCommand.CreateFromTask(BrowseMemoryRegionProfilesPathAsync);
        OpenMemoryRegionProfilesPathCommand = ReactiveCommand.CreateFromTask(OpenMemoryRegionProfilesPathAsync);
        ResetMemoryRegionProfilesPathCommand = ReactiveCommand.CreateFromTask(ResetMemoryRegionProfilesPathAsync);

        RefreshFromSettings();

        // Subscribe to settings changes
        _settingsChangedHandler = (_, args) =>
        {
            RefreshFromSettings();
        };
        _settingsService.SettingsChanged += _settingsChangedHandler;
    }

    private string? _statusMessage;
    public string? StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    // Serial
    private string _serialProfilesPath = string.Empty;
    public string SerialProfilesPath
    {
        get => _serialProfilesPath;
        set => this.RaiseAndSetIfChanged(ref _serialProfilesPath, value);
    }
    public ReactiveCommand<Unit, Unit> BrowseSerialProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSerialProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSerialProfilesPathCommand { get; }

    // Socat
    private string _socatProfilesPath = string.Empty;
    public string SocatProfilesPath
    {
        get => _socatProfilesPath;
        set => this.RaiseAndSetIfChanged(ref _socatProfilesPath, value);
    }
    public ReactiveCommand<Unit, Unit> BrowseSocatProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSocatProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSocatProfilesPathCommand { get; }

    // Power Supply
    private string _powerSupplyProfilesPath = string.Empty;
    public string PowerSupplyProfilesPath
    {
        get => _powerSupplyProfilesPath;
        set => this.RaiseAndSetIfChanged(ref _powerSupplyProfilesPath, value);
    }
    public ReactiveCommand<Unit, Unit> BrowsePowerSupplyProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenPowerSupplyProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetPowerSupplyProfilesPathCommand { get; }

    // Memory Region
    private string _memoryRegionProfilesPath = string.Empty;
    public string MemoryRegionProfilesPath
    {
        get => _memoryRegionProfilesPath;
        set => this.RaiseAndSetIfChanged(ref _memoryRegionProfilesPath, value);
    }
    public ReactiveCommand<Unit, Unit> BrowseMemoryRegionProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenMemoryRegionProfilesPathCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetMemoryRegionProfilesPathCommand { get; }


    private void RefreshFromSettings()
    {
        var current = _settingsService.Current;

        // Serial
        string serialPath = !string.IsNullOrEmpty(current.Profiles.SerialPath) ? current.Profiles.SerialPath : _pathService.SerialProfilesPath;
        SerialProfilesPath = GetDirectoryFromPath(serialPath, _pathService.SerialProfilesPath);

        // Socat
        string socatPath = !string.IsNullOrEmpty(current.Profiles.SocatPath) ? current.Profiles.SocatPath : _pathService.SocatProfilesPath;
        SocatProfilesPath = GetDirectoryFromPath(socatPath, _pathService.SocatProfilesPath);

        // Power Supply
        string powerSupplyPath = !string.IsNullOrEmpty(current.Profiles.PowerSupplyPath) ? current.Profiles.PowerSupplyPath : _pathService.PowerSupplyProfilesPath;
        PowerSupplyProfilesPath = GetDirectoryFromPath(powerSupplyPath, _pathService.PowerSupplyProfilesPath);

        // Memory Region
        string memoryRegionPath = !string.IsNullOrEmpty(current.Profiles.MemoryRegionPath) ? current.Profiles.MemoryRegionPath : _pathService.MemoryRegionProfilesPath;
        MemoryRegionProfilesPath = GetDirectoryFromPath(memoryRegionPath, _pathService.MemoryRegionProfilesPath);
    }

    private string GetDirectoryFromPath(string fullPath, string defaultPath)
    {
        try
        {
            string? directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                if (Path.IsPathRooted(directoryPath))
                {
                    return directoryPath;
                }

                return _pathService.ResolvePath(directoryPath);
            }
        }
        catch { }
        return _pathService.ProfilesDirectory;
    }

    private async Task HandleBrowsePathAsync(string currentPath, Action<string> pathSetter, Action<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings, string> updateAction, string fileName)
    {
        if (_fileDialogService == null)
        {
            StatusMessage = UIStrings.Status_FileDialogServiceNotAvailable;
            return;
        }

        try
        {
            string? result = await _fileDialogService.ShowFolderBrowserDialogAsync("Select Directory", currentPath);
            if (!string.IsNullOrEmpty(result))
            {
                pathSetter(result);
                await UpdatePathInSettingsAsync(updateAction, result, fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing for directory");
            StatusMessage = "Error selecting directory";
        }
    }

    private async Task HandleOpenPathAsync(string path)
    {
        try
        {
            StatusMessage = "Opening folder...";
            if (string.IsNullOrEmpty(path))
            {
                StatusMessage = "Path is empty";
                return;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            await PlatformHelper.OpenDirectoryInExplorerAsync(path);
            StatusMessage = "Folder opened";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening folder");
            StatusMessage = "Error opening folder";
        }
    }

    private async Task HandleResetPathAsync(string defaultPathValue, Action<string> pathSetter, Action<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings, string> updateAction, string fileName)
    {
        try
        {
            string defaultFolder = Path.GetDirectoryName(defaultPathValue) ?? _pathService.ProfilesDirectory;
            pathSetter(defaultFolder);
            await UpdatePathInSettingsAsync(updateAction, defaultFolder, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting path");
            StatusMessage = "Error resetting path";
        }
    }

    private async Task UpdatePathInSettingsAsync(Action<S7Tools.Core.Models.Configuration.StrongSettings.AppSettings, string> updateAction, string folderPath, string fileName)
    {
        try
        {
            await _settingsService.UpdateSettingsAsync(s => updateAction(s, Path.Combine(folderPath, fileName))).ConfigureAwait(false);
            StatusMessage = "Path updated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings");
            StatusMessage = "Failed to update settings";
        }
    }

    // Serial
    private Task BrowseSerialProfilesPathAsync() => HandleBrowsePathAsync(SerialProfilesPath, p => SerialProfilesPath = p, (s, path) => s.Profiles.SerialPath = path, "SerialProfiles.json");
    private Task OpenSerialProfilesPathAsync() => HandleOpenPathAsync(SerialProfilesPath);
    private Task ResetSerialProfilesPathAsync() => HandleResetPathAsync(_pathService.SerialProfilesPath, p => SerialProfilesPath = p, (s, path) => s.Profiles.SerialPath = path, "SerialProfiles.json");

    // Socat
    private Task BrowseSocatProfilesPathAsync() => HandleBrowsePathAsync(SocatProfilesPath, p => SocatProfilesPath = p, (s, path) => s.Profiles.SocatPath = path, "SocatProfiles.json");
    private Task OpenSocatProfilesPathAsync() => HandleOpenPathAsync(SocatProfilesPath);
    private Task ResetSocatProfilesPathAsync() => HandleResetPathAsync(_pathService.SocatProfilesPath, p => SocatProfilesPath = p, (s, path) => s.Profiles.SocatPath = path, "SocatProfiles.json");

    // Power Supply
    private Task BrowsePowerSupplyProfilesPathAsync() => HandleBrowsePathAsync(PowerSupplyProfilesPath, p => PowerSupplyProfilesPath = p, (s, path) => s.Profiles.PowerSupplyPath = path, "PowerSupplyProfiles.json");
    private Task OpenPowerSupplyProfilesPathAsync() => HandleOpenPathAsync(PowerSupplyProfilesPath);
    private Task ResetPowerSupplyProfilesPathAsync() => HandleResetPathAsync(_pathService.PowerSupplyProfilesPath, p => PowerSupplyProfilesPath = p, (s, path) => s.Profiles.PowerSupplyPath = path, "PowerSupplyProfiles.json");

    // Memory Region
    private Task BrowseMemoryRegionProfilesPathAsync() => HandleBrowsePathAsync(MemoryRegionProfilesPath, p => MemoryRegionProfilesPath = p, (s, path) => s.Profiles.MemoryRegionPath = path, "MemoryMappingProfiles.json");
    private Task OpenMemoryRegionProfilesPathAsync() => HandleOpenPathAsync(MemoryRegionProfilesPath);
    private Task ResetMemoryRegionProfilesPathAsync() => HandleResetPathAsync(_pathService.MemoryRegionProfilesPath, p => MemoryRegionProfilesPath = p, (s, path) => s.Profiles.MemoryRegionPath = path, "MemoryMappingProfiles.json");
}