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
    private bool _isInitializing;

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

        _isInitializing = true;
        RefreshFromSettings();
        _isInitializing = false;

        _settingsService.SettingsChanged += (_, _) =>
        {
            _isInitializing = true;
            RefreshFromSettings();
            _isInitializing = false;
        };

        // Auto-save when any property changes
        this.PropertyChanged += (_, e) =>
        {
            if (_isInitializing) return;
            if (e.PropertyName is
                nameof(MemoryDumpDefaultFolder) or
                nameof(SegmentDumpDelayMs) or
                nameof(IterationDumpDelayMs) or
                nameof(PlcConnectionTimeout) or
                nameof(PlcReadTimeout) or
                nameof(PlcRetryAttempts))
            {
                _ = SaveGeneralSettingsAsync();
            }
        };
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

    // MemoryDump settings
    private string _memoryDumpDefaultFolder = string.Empty;
    public string MemoryDumpDefaultFolder
    {
        get => _memoryDumpDefaultFolder;
        set => this.RaiseAndSetIfChanged(ref _memoryDumpDefaultFolder, value);
    }

    private int _segmentDumpDelayMs = 5000;
    public int SegmentDumpDelayMs
    {
        get => _segmentDumpDelayMs;
        set => this.RaiseAndSetIfChanged(ref _segmentDumpDelayMs, value);
    }

    private int _iterationDumpDelayMs = 5000;
    public int IterationDumpDelayMs
    {
        get => _iterationDumpDelayMs;
        set => this.RaiseAndSetIfChanged(ref _iterationDumpDelayMs, value);
    }

    // PLC settings
    private int _plcConnectionTimeout = 5000;
    public int PlcConnectionTimeout
    {
        get => _plcConnectionTimeout;
        set => this.RaiseAndSetIfChanged(ref _plcConnectionTimeout, value);
    }

    private int _plcReadTimeout = 2000;
    public int PlcReadTimeout
    {
        get => _plcReadTimeout;
        set => this.RaiseAndSetIfChanged(ref _plcReadTimeout, value);
    }

    private int _plcRetryAttempts = 3;
    public int PlcRetryAttempts
    {
        get => _plcRetryAttempts;
        set => this.RaiseAndSetIfChanged(ref _plcRetryAttempts, value);
    }

    public ReactiveCommand<Unit, Unit>? SaveSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit>? LoadSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit>? ResetSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit>? OpenSettingsFolderCommand { get; }

    private void RefreshFromSettings()
    {
        if (_pathService == null)
        {
            return;
        }

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

        var current = _settingsService.Current;
        MemoryDumpDefaultFolder = current.MemoryDump.DefaultFolder;
        SegmentDumpDelayMs = current.MemoryDump.SegmentDumpDelayMilliseconds;
        IterationDumpDelayMs = current.MemoryDump.IterationDumpDelayMilliseconds;
        PlcConnectionTimeout = current.Plc.ConnectionTimeout;
        PlcReadTimeout = current.Plc.ReadTimeout;
        PlcRetryAttempts = current.Plc.RetryAttempts;
    }

    private async Task SaveGeneralSettingsAsync()
    {
        try
        {
            await _settingsService.UpdateSettingsAsync(s =>
            {
                s.MemoryDump.DefaultFolder = MemoryDumpDefaultFolder;
                s.MemoryDump.SegmentDumpDelayMilliseconds = SegmentDumpDelayMs;
                s.MemoryDump.IterationDumpDelayMilliseconds = IterationDumpDelayMs;
                s.Plc.ConnectionTimeout = PlcConnectionTimeout;
                s.Plc.ReadTimeout = PlcReadTimeout;
                s.Plc.RetryAttempts = PlcRetryAttempts;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-saving general settings");
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            SettingsStatusMessage = UIStrings.Status_SavingSettings;
            await SaveGeneralSettingsAsync();
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
