using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models.Configuration.StrongSettings;
using S7Tools.Helpers;
using S7Tools.Resources;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Settings;

/// <summary>
/// ViewModel for general settings configuration.
/// </summary>
public class GeneralSettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IApplicationSettingsService _settingsService;
    private readonly IPathService _pathService;
    private readonly ILogger<GeneralSettingsViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();
    private bool _isInitializing;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The application settings service for reading and writing settings.</param>
    /// <param name="pathService">The path service for resolving application paths.</param>
    /// <param name="logger">The logger instance.</param>
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

        // Subscribe to settings-changed and unsubscribe on disposal via _disposables
        Observable.FromEventPattern<SettingsChangedEventArgs>(
                h => _settingsService.SettingsChanged += h,
                h => _settingsService.SettingsChanged -= h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                _isInitializing = true;
                RefreshFromSettings();
                _isInitializing = false;
            })
            .DisposeWith(_disposables);

        // Auto-save when any relevant property changes, throttled to avoid excessive disk I/O
        this.Changed
            .Where(e => e.PropertyName is
                nameof(MemoryDumpDefaultFolder) or
                nameof(SegmentDumpDelayMs) or
                nameof(IterationDumpDelayMs) or
                nameof(PlcConnectionTimeout) or
                nameof(PlcReadTimeout) or
                nameof(PlcRetryAttempts))
            .Where(_ => !_isInitializing)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(__ => { _ = SaveGeneralSettingsAsync(); })
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralSettingsViewModel"/> class for XAML designer use.
    /// </summary>
    public GeneralSettingsViewModel()
    {
        _settingsService = null!;
        _pathService = null!;
        _logger = null!;
    }

    private string _settingsStatusMessage = UIStrings.Status_SettingsReady;

    /// <summary>
    /// Gets or sets the user-facing status message for settings operations.
    /// </summary>
    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _settingsStatusMessage, value);
    }

    private string _currentSettingsFilePath = string.Empty;

    /// <summary>
    /// Gets or sets the full file-system path of the current settings file.
    /// </summary>
    public string CurrentSettingsFilePath
    {
        get => _currentSettingsFilePath;
        set => this.RaiseAndSetIfChanged(ref _currentSettingsFilePath, value);
    }

    private DateTime _settingsLastModified = DateTime.UtcNow.ToLocalTime();

    /// <summary>
    /// Gets or sets the last-modified timestamp of the settings file.
    /// </summary>
    public DateTime SettingsLastModified
    {
        get => _settingsLastModified;
        set => this.RaiseAndSetIfChanged(ref _settingsLastModified, value);
    }

    // MemoryDump settings
    private string _memoryDumpDefaultFolder = string.Empty;

    /// <summary>
    /// Gets or sets the default folder path for memory dump output files.
    /// </summary>
    public string MemoryDumpDefaultFolder
    {
        get => _memoryDumpDefaultFolder;
        set => this.RaiseAndSetIfChanged(ref _memoryDumpDefaultFolder, value);
    }

    private int _segmentDumpDelayMs = 5000;

    /// <summary>
    /// Gets or sets the delay in milliseconds between dumping each memory segment.
    /// </summary>
    public int SegmentDumpDelayMs
    {
        get => _segmentDumpDelayMs;
        set => this.RaiseAndSetIfChanged(ref _segmentDumpDelayMs, value);
    }

    private int _iterationDumpDelayMs = 5000;

    /// <summary>
    /// Gets or sets the delay in milliseconds between full dump iterations.
    /// </summary>
    public int IterationDumpDelayMs
    {
        get => _iterationDumpDelayMs;
        set => this.RaiseAndSetIfChanged(ref _iterationDumpDelayMs, value);
    }

    // PLC settings
    private int _plcConnectionTimeout = 5000;

    /// <summary>
    /// Gets or sets the PLC connection timeout in milliseconds.
    /// </summary>
    public int PlcConnectionTimeout
    {
        get => _plcConnectionTimeout;
        set => this.RaiseAndSetIfChanged(ref _plcConnectionTimeout, value);
    }

    private int _plcReadTimeout = 2000;

    /// <summary>
    /// Gets or sets the PLC read operation timeout in milliseconds.
    /// </summary>
    public int PlcReadTimeout
    {
        get => _plcReadTimeout;
        set => this.RaiseAndSetIfChanged(ref _plcReadTimeout, value);
    }

    private int _plcRetryAttempts = 3;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed PLC operations.
    /// </summary>
    public int PlcRetryAttempts
    {
        get => _plcRetryAttempts;
        set => this.RaiseAndSetIfChanged(ref _plcRetryAttempts, value);
    }

    /// <summary>
    /// Gets the command to manually save the current general settings.
    /// </summary>
    public ReactiveCommand<Unit, Unit>? SaveSettingsCommand { get; }

    /// <summary>
    /// Gets the command to reload general settings from the settings file.
    /// </summary>
    public ReactiveCommand<Unit, Unit>? LoadSettingsCommand { get; }

    /// <summary>
    /// Gets the command to reset all general settings to their default values.
    /// </summary>
    public ReactiveCommand<Unit, Unit>? ResetSettingsCommand { get; }

    /// <summary>
    /// Gets the command to open the settings file directory in the system file explorer.
    /// </summary>
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

        AppSettings current = _settingsService.Current;
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

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by this ViewModel.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        { return; }
        if (disposing)
        {
            _disposables.Dispose();
        }
        _disposed = true;
    }
}
