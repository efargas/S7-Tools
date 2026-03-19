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
/// ViewModel for general settings configuration.
/// </summary>
public class GeneralSettingsViewModel : ViewModelBase
{
    private readonly IApplicationSettingsService _settingsService;
    private readonly IPathService _pathService;
    private readonly ILogger<GeneralSettingsViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the GeneralSettingsViewModel class.
    /// </summary>
    public GeneralSettingsViewModel(
        IApplicationSettingsService settingsService,
        IPathService pathService,
        ILogger<GeneralSettingsViewModel> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
        LoadSettingsCommand = ReactiveCommand.CreateFromTask(LoadSettingsAsync);
        ResetSettingsCommand = ReactiveCommand.CreateFromTask(ResetSettingsAsync);
        OpenSettingsFolderCommand = ReactiveCommand.CreateFromTask(OpenSettingsFolderAsync);

        RefreshFromSettings();
        _settingsService.SettingsChanged += (_, _) => RefreshFromSettings();
    }

    // Default constructor for designer
    public GeneralSettingsViewModel()
    {
        _settingsService = null!;
        _pathService = null!;
        _logger = null!;
    }

    private string _settingsStatusMessage = UIStrings.Status_SettingsReady;
    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _settingsStatusMessage, value);
    }

    private string _currentSettingsFilePath = string.Empty;
    public string CurrentSettingsFilePath
    {
        get => _currentSettingsFilePath;
        set => this.RaiseAndSetIfChanged(ref _currentSettingsFilePath, value);
    }

    private DateTime _settingsLastModified = DateTime.UtcNow.ToLocalTime();
    public DateTime SettingsLastModified
    {
        get => _settingsLastModified;
        set => this.RaiseAndSetIfChanged(ref _settingsLastModified, value);
    }

    public ReactiveCommand<Unit, Unit>? SaveSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit>? LoadSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit>? ResetSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit>? OpenSettingsFolderCommand { get; }

    private void RefreshFromSettings()
    {
        if (_pathService == null) return;
        
        CurrentSettingsFilePath = _pathService.AppSettingsPath;

        try
        {
            var fileInfo = new System.IO.FileInfo(CurrentSettingsFilePath);
            SettingsLastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.UtcNow.ToLocalTime();
        }
        catch
        {
            SettingsLastModified = DateTime.UtcNow.ToLocalTime();
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = UIStrings.Status_SavingSettings;
            // No local properties to save in General yet, but trigger global settings validation/save
            await _settingsService.UpdateSettingsAsync(s => { });
            SettingsStatusMessage = UIStrings.Status_SettingsSavedSuccessfully;
            _logger.LogInformation("General settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving general settings");
            SettingsStatusMessage = UIStrings.Status_ErrorSavingSettings;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = UIStrings.Status_LoadingSettings;
            await _settingsService.LoadSettingsAsync();
            RefreshFromSettings();
            SettingsStatusMessage = UIStrings.Status_SettingsLoadedSuccessfully;
            _logger.LogInformation("General settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading general settings");
            SettingsStatusMessage = UIStrings.Status_ErrorLoadingSettings;
        }
    }

    private async Task ResetSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = "Restoring Global Defaults...";
            await _settingsService.RestoreDefaultsAsync();
            RefreshFromSettings();
            SettingsStatusMessage = "Settings Reset to Defaults";
            _logger.LogInformation("General settings reset to defaults successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting generic settings to defaults");
            SettingsStatusMessage = UIStrings.Status_ErrorResettingSettings;
        }
    }

    private async Task OpenSettingsFolderAsync()
    {
        try
        {
            string settingsDir = Path.GetDirectoryName(CurrentSettingsFilePath) ?? _pathService.AppSettingsPath;
            string resolvedPath = _pathService.ResolvePath(settingsDir);

            if (!Directory.Exists(resolvedPath))
            {
                Directory.CreateDirectory(resolvedPath);
            }

            await PlatformHelper.OpenDirectoryInExplorerAsync(resolvedPath);
            _logger.LogInformation("Opened settings directory in explorer: {Path}", resolvedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings directory");
            SettingsStatusMessage = UIStrings.Status_ErrorOpeningSettingsDirectory;
        }
    }
}
