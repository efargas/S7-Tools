using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Resources;
using S7Tools.Services.Interfaces;

namespace S7Tools.ViewModels.Controls;

/// <summary>
/// ViewModel for the SerialPortDiscoveryControl, providing real-time serial port discovery
/// and monitoring with comprehensive port scanning capabilities, status monitoring, and configuration testing.
/// </summary>
public sealed class SerialPortDiscoveryViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly ISerialPortService _portService;
    private readonly IUIThreadService _uiThreadService;
    private readonly IUIRefreshService _uiRefreshService;
    private readonly ILogger<SerialPortDiscoveryViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();
    private readonly Timer _scanTimer;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private readonly List<SerialPortInfo> _allDiscoveredPorts = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SerialPortDiscoveryViewModel class.
    /// </summary>
    /// <param name="portService">The serial port service.</param>
    /// <param name="uiThreadService">The UI thread service.</param>
    /// <param name="uiRefreshService">The UI refresh service.</param>
    /// <param name="logger">The logger.</param>
    public SerialPortDiscoveryViewModel(
        ISerialPortService portService,
        IUIThreadService uiThreadService,
        IUIRefreshService uiRefreshService,
        ILogger<SerialPortDiscoveryViewModel> logger)
    {
        _portService = portService ?? throw new ArgumentNullException(nameof(portService));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
        _uiRefreshService = uiRefreshService ?? throw new ArgumentNullException(nameof(uiRefreshService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize collections
        DiscoveredPorts = new ObservableCollection<SerialPortInfo>();
        ScanHistory = new ObservableCollection<ScanResult>();

        // Initialize commands
        InitializeCommands();

        // Set up auto-refresh using the reusable service
        // NOTE: Use targeted refresh that only affects SerialPortDiscovery UI elements
        // This maintains checkbox responsiveness without interfering with LogViewer scroll
        var refreshOptions = new UIRefreshOptions
        {
            PeriodicRefreshIntervalSeconds = 1.0,  // Less frequent to reduce interference
            EnablePeriodicRefresh = true,
            SkipInitialValue = true,
            EnableLogging = false,
            MonitoredProperties = new[]
            {
                nameof(IncludeUsbPorts),
                nameof(IncludeAcmPorts),
                nameof(IncludeSerialPorts),
                nameof(IsScanning)
            }
        };

        // Use custom refresh action that only refreshes serial port discovery properties
        _uiRefreshService.SetupPropertyMonitoring(this, refreshOptions.MonitoredProperties.ToArray(),
            RefreshSerialPortDiscoveryUI, _disposables, refreshOptions.SkipInitialValue);

        // Set up periodic refresh with limited scope to avoid LogViewer interference
        _uiRefreshService.SetupPeriodicRefresh(RefreshSerialPortDiscoveryUI,
            refreshOptions.PeriodicRefreshIntervalSeconds, _disposables);

        // Set up automatic scanning timer (disabled by default)
        _scanTimer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

        // Perform initial scan
        _ = Task.Run(ScanPortsAsync);

        _logger.LogInformation("SerialPortDiscoveryViewModel initialized");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of discovered serial ports.
    /// </summary>
    public ObservableCollection<SerialPortInfo> DiscoveredPorts { get; }

    /// <summary>
    /// Gets the collection of scan history results.
    /// </summary>
    public ObservableCollection<ScanResult> ScanHistory { get; }

    private SerialPortInfo? _selectedPort;
    /// <summary>
    /// Gets or sets the currently selected port.
    /// </summary>
    public SerialPortInfo? SelectedPort
    {
        get => _selectedPort;
        set => this.RaiseAndSetIfChanged(ref _selectedPort, value);
    }

    private bool _isScanning;
    /// <summary>
    /// Gets or sets a value indicating whether scanning is in progress.
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _isScanning, value))
            {
                // Notify CanToggle properties when scanning state changes
                this.RaisePropertyChanged(nameof(CanToggleUsbPorts));
                this.RaisePropertyChanged(nameof(CanToggleAcmPorts));
                this.RaisePropertyChanged(nameof(CanToggleSerialPorts));

                _logger.LogDebug("IsScanning changed to {Value}, CanToggle properties notified", value);
            }
        }
    }

    private bool _autoScanEnabled;
    /// <summary>
    /// Gets or sets a value indicating whether automatic scanning is enabled.
    /// </summary>
    public bool AutoScanEnabled
    {
        get => _autoScanEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _autoScanEnabled, value);
            UpdateAutoScanTimer();
        }
    }

    private int _autoScanInterval = 5000;
    /// <summary>
    /// Gets or sets the automatic scan interval in milliseconds.
    /// </summary>
    public int AutoScanInterval
    {
        get => _autoScanInterval;
        set
        {
            this.RaiseAndSetIfChanged(ref _autoScanInterval, value);
            UpdateAutoScanTimer();
        }
    }

    private int _totalPortsFound;
    /// <summary>
    /// Gets or sets the total number of ports found in the last scan.
    /// </summary>
    public int TotalPortsFound
    {
        get => _totalPortsFound;
        set => this.RaiseAndSetIfChanged(ref _totalPortsFound, value);
    }

    private int _accessiblePortsCount;
    /// <summary>
    /// Gets or sets the number of accessible ports found in the last scan.
    /// </summary>
    public int AccessiblePortsCount
    {
        get => _accessiblePortsCount;
        set => this.RaiseAndSetIfChanged(ref _accessiblePortsCount, value);
    }

    private DateTime _lastScanTime = DateTime.Now;
    /// <summary>
    /// Gets or sets the timestamp of the last scan.
    /// </summary>
    public DateTime LastScanTime
    {
        get => _lastScanTime;
        set => this.RaiseAndSetIfChanged(ref _lastScanTime, value);
    }

    private TimeSpan _lastScanDuration;
    /// <summary>
    /// Gets or sets the duration of the last scan.
    /// </summary>
    public TimeSpan LastScanDuration
    {
        get => _lastScanDuration;
        set => this.RaiseAndSetIfChanged(ref _lastScanDuration, value);
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

    private string _scanFilter = string.Empty;
    /// <summary>
    /// Gets or sets the scan filter pattern.
    /// </summary>
    public string ScanFilter
    {
        get => _scanFilter;
        set => this.RaiseAndSetIfChanged(ref _scanFilter, value);
    }

    private bool _includeUsbPorts = true;
    /// <summary>
    /// Gets or sets a value indicating whether to include USB ports in scans.
    /// </summary>
    public bool IncludeUsbPorts
    {
        get => _includeUsbPorts;
        set => SetFilterProperty(ref _includeUsbPorts, value, nameof(IncludeUsbPorts));
    }

    private bool _includeAcmPorts = true;
    /// <summary>
    /// Gets or sets a value indicating whether to include ACM ports in scans.
    /// </summary>
    public bool IncludeAcmPorts
    {
        get => _includeAcmPorts;
        set => SetFilterProperty(ref _includeAcmPorts, value, nameof(IncludeAcmPorts));
    }

    private bool _includeSerialPorts = true;
    /// <summary>
    /// Gets or sets a value indicating whether to include serial ports in scans.
    /// </summary>
    public bool IncludeSerialPorts
    {
        get => _includeSerialPorts;
        set => SetFilterProperty(ref _includeSerialPorts, value, nameof(IncludeSerialPorts));
    }

    /// <summary>
    /// Centralized filter property setter that handles validation and UI updates properly.
    /// </summary>
    /// <param name="field">The backing field to update.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property being set.</param>
    private void SetFilterProperty(ref bool field, bool value, string propertyName)
    {
        // Special logic for ACM and Serial: if unchecking would leave no filters, force USB=true
        if (!value && (propertyName == nameof(IncludeAcmPorts) || propertyName == nameof(IncludeSerialPorts)))
        {
            // Check if this would leave no filters active
            bool usbAfter = _includeUsbPorts;
            bool acmAfter = propertyName == nameof(IncludeAcmPorts) ? false : _includeAcmPorts;
            bool serialAfter = propertyName == nameof(IncludeSerialPorts) ? false : _includeSerialPorts;

            if (!usbAfter && !acmAfter && !serialAfter)
            {
                _logger.LogDebug("Unchecking {PropertyName} would leave no filters - forcing USB to true", propertyName);
                _includeUsbPorts = true;

                // The UIRefreshService will handle the property change notifications automatically
                this.RaisePropertyChanged(nameof(IncludeUsbPorts));
            }
        }

        // Normal property change - UIRefreshService handles the rest
        if (this.RaiseAndSetIfChanged(ref field, value))
        {
            _logger.LogDebug("{PropertyName} changed to {Value}", propertyName, value);
            ApplyFiltersToDiscoveredPorts();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the USB ports checkbox can be toggled.
    /// USB can only be unchecked when at least one other filter is active.
    /// </summary>
    public bool CanToggleUsbPorts => !IsScanning && (_includeAcmPorts || _includeSerialPorts);

    /// <summary>
    /// Gets a value indicating whether the ACM ports checkbox can be toggled.
    /// ACM can always be toggled when not scanning.
    /// </summary>
    public bool CanToggleAcmPorts => !IsScanning;

    /// <summary>
    /// Gets a value indicating whether the serial ports checkbox can be toggled.
    /// Serial can always be toggled when not scanning.
    /// </summary>
    public bool CanToggleSerialPorts => !IsScanning;

    private bool _checkAccessibility = true;
    /// <summary>
    /// Gets or sets a value indicating whether to check port accessibility during scans.
    /// </summary>
    public bool CheckAccessibility
    {
        get => _checkAccessibility;
        set => this.RaiseAndSetIfChanged(ref _checkAccessibility, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to scan for ports.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ScanPortsCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to stop scanning.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopScanCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to refresh the selected port information.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshPortInfoCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to test the selected port.
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestSelectedPortCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to clear the scan history.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearHistoryCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to export scan results.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportResultsCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the command to copy port information to clipboard.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CopyPortInfoCommand { get; private set; } = null!;

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes all reactive commands.
    /// </summary>
    private void InitializeCommands()
    {
        // Scan ports command - enabled when not currently scanning
        IObservable<bool> canScan = this.WhenAnyValue(x => x.IsScanning)
            .Select(scanning => !scanning);

        ScanPortsCommand = ReactiveCommand.CreateFromTask(ScanPortsAsync, canScan);
        ScanPortsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "scanning ports"))
            .DisposeWith(_disposables);

        // Stop scan command - enabled when currently scanning
        IObservable<bool> canStopScan = this.WhenAnyValue(x => x.IsScanning);

        StopScanCommand = ReactiveCommand.Create(StopScan, canStopScan);

        // Refresh port info command - enabled when a port is selected and not scanning
        IObservable<bool> canRefreshPortInfo = this.WhenAnyValue(x => x.SelectedPort, x => x.IsScanning)
            .Select(tuple => tuple.Item1 != null && !tuple.Item2);

        RefreshPortInfoCommand = ReactiveCommand.CreateFromTask(RefreshPortInfoAsync, canRefreshPortInfo);
        RefreshPortInfoCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "refreshing port info"))
            .DisposeWith(_disposables);

        // Test selected port command - enabled when a port is selected and not scanning
        IObservable<bool> canTestSelectedPort = this.WhenAnyValue(x => x.SelectedPort, x => x.IsScanning)
            .Select(tuple => tuple.Item1 != null && !tuple.Item2);

        TestSelectedPortCommand = ReactiveCommand.CreateFromTask(TestSelectedPortAsync, canTestSelectedPort);
        TestSelectedPortCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing port"))
            .DisposeWith(_disposables);

        // Clear history command - enabled when history is not empty
        IObservable<bool> canClearHistory = this.WhenAnyValue(x => x.ScanHistory.Count)
            .Select(count => count > 0);

        ClearHistoryCommand = ReactiveCommand.Create(ClearHistory, canClearHistory);

        // Export results command - enabled when ports are found
        IObservable<bool> canExportResults = this.WhenAnyValue(x => x.TotalPortsFound)
            .Select(count => count > 0);

        ExportResultsCommand = ReactiveCommand.CreateFromTask(ExportResultsAsync, canExportResults);
        ExportResultsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting results"))
            .DisposeWith(_disposables);

        // Copy port info command - enabled when a port is selected
        IObservable<bool> canCopyPortInfo = this.WhenAnyValue(x => x.SelectedPort)
            .Select(port => port != null);

        CopyPortInfoCommand = ReactiveCommand.CreateFromTask(CopyPortInfoAsync, canCopyPortInfo);
        CopyPortInfoCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "copying port info"))
            .DisposeWith(_disposables);
    }

    /// <summary>
    /// Scans for available serial ports.
    /// </summary>
    private async Task ScanPortsAsync()
    {
        if (IsScanning)
        {
            return;
        }

        try
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                IsScanning = true;
                StatusMessage = UIStrings.Status_ScanningForPorts;
            });
            DateTime startTime = DateTime.Now;

            _scanCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _scanCancellationTokenSource.Token;

            // Get available ports
            IEnumerable<Core.Services.Interfaces.SerialPortInfo> availablePorts = await _portService.ScanAvailablePortsAsync(cancellationToken);

            // Extract port paths - store ALL ports, filtering will be done by ApplyFiltersToDiscoveredPorts
            IEnumerable<string> portPaths = availablePorts.Select(p => p.PortPath);

            // Create port info objects for ALL discovered ports
            var portInfos = new List<SerialPortInfo>();
            foreach (string portName in portPaths)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                PortTypeEnum portType = GetPortType(portName);
                var portInfo = new SerialPortInfo
                {
                    PortName = portName,
                    DisplayName = GetPortDisplayName(portName),
                    PortType = portType,
                    PortTypeDisplay = GetPortTypeDisplay(portType),
                    IsAccessible = !CheckAccessibility || await _portService.IsPortAccessibleAsync(portName, 1000, cancellationToken),
                    LastChecked = DateTime.Now
                };

                // Get additional port information if accessible
                if (portInfo.IsAccessible && CheckAccessibility)
                {
                    try
                    {
                        Core.Services.Interfaces.SerialPortInfo? portDetails = await _portService.GetPortInfoAsync(portName, cancellationToken).ConfigureAwait(false);
                        if (portDetails != null)
                        {
                            portInfo.Description = portDetails.Description ?? "";
                            UsbDeviceInfo? usbInfo = portDetails.UsbInfo;
                            portInfo.Manufacturer = usbInfo?.VendorName ?? "Unknown";
                            portInfo.SerialNumber = usbInfo?.SerialNumber ?? "Unknown";
                        }
                        else
                        {
                            portInfo.Description = "Details unavailable";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not get details for port {PortName}", portName);
                        portInfo.Description = "Details unavailable";
                    }
                }

                portInfos.Add(portInfo);
            }

            // Store all ports (before UI filtering)
            _allDiscoveredPorts.Clear();
            _allDiscoveredPorts.AddRange(portInfos.OrderBy(p => p.PortName));

            // Apply UI filters and update all UI-bound properties on the UI thread
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                // Apply filters to update DiscoveredPorts collection
                ApplyFiltersToDiscoveredPorts();

                // Update statistics
                DateTime endTime = DateTime.Now;
                LastScanTime = endTime;
                LastScanDuration = endTime - startTime;
                TotalPortsFound = DiscoveredPorts.Count;
                AccessiblePortsCount = DiscoveredPorts.Count(p => p.IsAccessible);

                // Add to scan history
                var scanResult = new ScanResult
                {
                    ScanTime = LastScanTime,
                    Duration = LastScanDuration,
                    PortsFound = TotalPortsFound,
                    AccessiblePorts = AccessiblePortsCount,
                    FilterUsed = !string.IsNullOrEmpty(ScanFilter) ? ScanFilter : "None"
                };

                ScanHistory.Insert(0, scanResult);

                // Keep only last 50 scan results
                while (ScanHistory.Count > 50)
                {
                    ScanHistory.RemoveAt(ScanHistory.Count - 1);
                }

                StatusMessage = $"Found {TotalPortsFound} port(s) ({AccessiblePortsCount} accessible) in {LastScanDuration.TotalMilliseconds:F0}ms";

                // Force complete UI refresh after scan
                this.RaisePropertyChanged(nameof(DiscoveredPorts));
                this.RaisePropertyChanged(nameof(TotalPortsFound));
                this.RaisePropertyChanged(nameof(AccessiblePortsCount));
            });

            _logger.LogInformation("Port scan completed: {TotalPorts} total, {AccessiblePorts} accessible", TotalPortsFound, AccessiblePortsCount);
        }
        catch (OperationCanceledException)
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_ScanCancelled;
            });
            _logger.LogInformation("Port scan cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for ports");
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                StatusMessage = UIStrings.Status_ErrorScanningForPorts;
            });
        }
        finally
        {
            await _uiThreadService.InvokeOnUIThreadAsync(() =>
            {
                IsScanning = false;
                // Force re-evaluation of all CanToggle properties
                this.RaisePropertyChanged(nameof(CanToggleUsbPorts));
                this.RaisePropertyChanged(nameof(CanToggleAcmPorts));
                this.RaisePropertyChanged(nameof(CanToggleSerialPorts));
            });
            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Stops the current scan operation.
    /// </summary>
    private void StopScan()
    {
        _scanCancellationTokenSource?.Cancel();
        StatusMessage = UIStrings.Status_StoppingScan;
        _logger.LogInformation("Port scan stop requested");
    }

    /// <summary>
    /// Refreshes information for the selected port.
    /// </summary>
    private async Task RefreshPortInfoAsync()
    {
        if (SelectedPort == null)
        {
            return;
        }

        try
        {
            StatusMessage = UIStrings.Status_RefreshingPortInformation;

            string portName = SelectedPort.PortName;
            bool isAccessible = await _portService.IsPortAccessibleAsync(portName).ConfigureAwait(false);

            SelectedPort.IsAccessible = isAccessible;
            SelectedPort.LastChecked = DateTime.Now;

            if (isAccessible)
            {
                try
                {
                    Core.Services.Interfaces.SerialPortInfo? portDetails = await _portService.GetPortInfoAsync(portName).ConfigureAwait(false);
                    if (portDetails != null)
                    {
                        SelectedPort.Description = portDetails.Description ?? "";
                        UsbDeviceInfo? usbInfo = portDetails.UsbInfo;
                        SelectedPort.Manufacturer = usbInfo?.VendorName ?? "Unknown";
                        SelectedPort.SerialNumber = usbInfo?.SerialNumber ?? "Unknown";
                    }
                    else
                    {
                        SelectedPort.Description = "Details unavailable";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get details for port {PortName}", portName);
                    SelectedPort.Description = "Details unavailable";
                }
            }

            StatusMessage = $"Port {portName} information refreshed";
            _logger.LogInformation("Refreshed information for port: {PortName}", portName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing port information");
            StatusMessage = UIStrings.Status_ErrorRefreshingPortInformation;
        }
    }

    /// <summary>
    /// Tests the selected port with a default configuration.
    /// </summary>
    private async Task TestSelectedPortAsync()
    {
        if (SelectedPort == null)
        {
            return;
        }

        try
        {
            StatusMessage = UIStrings.Status_TestingPort;

            var defaultConfig = SerialPortConfiguration.CreateDefault();
            bool success = await _portService.ApplyConfigurationAsync(SelectedPort.PortName, defaultConfig);

            SelectedPort.LastTestResult = success ? "Success" : "Failed";
            SelectedPort.LastTested = DateTime.Now;

            StatusMessage = success
                ? $"Port {SelectedPort.PortName} test successful"
                : $"Port {SelectedPort.PortName} test failed";

            _logger.LogInformation("Port test result for {PortName}: {Success}", SelectedPort.PortName, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing port");
            StatusMessage = UIStrings.Status_ErrorTestingPort;
        }
    }

    /// <summary>
    /// Clears the scan history.
    /// </summary>
    private void ClearHistory()
    {
        ScanHistory.Clear();
        StatusMessage = UIStrings.Status_ScanHistoryCleared;
        _logger.LogInformation("Scan history cleared");
    }

    /// <summary>
    /// Exports scan results to a file.
    /// </summary>
    private async Task ExportResultsAsync()
    {
        try
        {
            StatusMessage = UIStrings.Status_ExportingScanResults;

            // TODO: Implement file dialog and export functionality
            await Task.CompletedTask;

            StatusMessage = UIStrings.Status_ScanResultsExported;
            _logger.LogInformation("Scan results exported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting scan results");
            StatusMessage = UIStrings.Status_ErrorExportingScanResults;
        }
    }

    /// <summary>
    /// Copies port information to the clipboard.
    /// </summary>
    private async Task CopyPortInfoAsync()
    {
        if (SelectedPort == null)
        {
            return;
        }

        try
        {
            // TODO: Implement clipboard functionality
            await Task.CompletedTask;

            StatusMessage = UIStrings.Status_PortInformationCopied;
            _logger.LogInformation("Port information copied to clipboard for: {PortName}", SelectedPort.PortName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying port information");
            StatusMessage = UIStrings.Status_ErrorCopyingPortInformation;
        }
    }

    /// <summary>
    /// Filters ports based on current settings.
    /// </summary>
    /// <param name="ports">The list of ports to filter.</param>
    /// <returns>The filtered list of ports.</returns>
    private IEnumerable<string> FilterPorts(IEnumerable<string> ports)
    {
        IEnumerable<string> filtered = ports.AsEnumerable();

        // Apply type filters
        if (!IncludeUsbPorts)
        {
            filtered = filtered.Where(p => !p.Contains("ttyUSB"));
        }

        if (!IncludeAcmPorts)
        {
            filtered = filtered.Where(p => !p.Contains("ttyACM"));
        }

        if (!IncludeSerialPorts)
        {
            filtered = filtered.Where(p => !p.Contains("ttyS"));
        }

        // Apply text filter
        if (!string.IsNullOrEmpty(ScanFilter))
        {
            filtered = filtered.Where(p => p.Contains(ScanFilter, StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }

    /// <summary>
    /// Gets a display name for a port.
    /// </summary>
    /// <param name="portName">The port name.</param>
    /// <returns>A user-friendly display name.</returns>
    private static string GetPortDisplayName(string portName)
    {
        return portName switch
        {
            var name when name.Contains("ttyUSB") => $"USB Serial ({Path.GetFileName(name)})",
            var name when name.Contains("ttyACM") => $"USB Modem ({Path.GetFileName(name)})",
            var name when name.Contains("ttyS") => $"Serial Port ({Path.GetFileName(name)})",
            _ => Path.GetFileName(portName)
        };
    }

    /// <summary>
    /// Gets the port type enum for a port name.
    /// </summary>
    /// <param name="portName">The port name.</param>
    /// <returns>The port type enum.</returns>
    private static PortTypeEnum GetPortType(string portName)
    {
        return portName switch
        {
            var name when name.Contains("ttyUSB") => PortTypeEnum.Usb,
            var name when name.Contains("ttyACM") => PortTypeEnum.Acm,
            var name when name.Contains("ttyS") => PortTypeEnum.Standard,
            _ => PortTypeEnum.Unknown
        };
    }

    /// <summary>
    /// Gets the port type display string for a port type enum.
    /// </summary>
    /// <param name="portType">The port type enum.</param>
    /// <returns>The port type display string.</returns>
    private static string GetPortTypeDisplay(PortTypeEnum portType)
    {
        return portType switch
        {
            PortTypeEnum.Usb => "USB",
            PortTypeEnum.Acm => "ACM",
            PortTypeEnum.Standard => "Standard",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Updates the automatic scan timer based on current settings.
    /// </summary>
    private void UpdateAutoScanTimer()
    {
        if (AutoScanEnabled && AutoScanInterval > 0)
        {
            _scanTimer.Change(AutoScanInterval, AutoScanInterval);
            _logger.LogInformation("Auto-scan enabled with interval: {Interval}ms", AutoScanInterval);
        }
        else
        {
            _scanTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Auto-scan disabled");
        }
    }

    /// <summary>
    /// Refreshes only the UI elements specific to SerialPortDiscovery to avoid interfering with LogViewer.
    /// This method specifically targets the Can* properties that control checkbox enable/disable states.
    /// </summary>
    private void RefreshSerialPortDiscoveryUI()
    {
        try
        {
            // Only refresh the specific properties that are part of SerialPortDiscovery UI
            // These are scoped to this ViewModel and won't affect other UI components like LogViewer
            this.RaisePropertyChanged(nameof(CanToggleUsbPorts));
            this.RaisePropertyChanged(nameof(CanToggleAcmPorts));
            this.RaisePropertyChanged(nameof(CanToggleSerialPorts));

            // Only refresh counts if not actively scanning to avoid interference
            if (!IsScanning)
            {
                this.RaisePropertyChanged(nameof(TotalPortsFound));
                this.RaisePropertyChanged(nameof(AccessiblePortsCount));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SerialPortDiscovery UI refresh");
        }
    }

    /// <summary>
    /// Handles timer elapsed events for automatic scanning.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private async void OnTimerElapsed(object? state)
    {
        if (!IsScanning && AutoScanEnabled)
        {
            await ScanPortsAsync();
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

    /// <summary>
    /// Applies the current filter settings to the discovered ports list.
    /// </summary>
    private void ApplyFiltersToDiscoveredPorts()
    {
        if (_allDiscoveredPorts.Count == 0)
        {
            return;
        }

        // Filter the ports based on current settings using enum comparison
        var filteredPorts = _allDiscoveredPorts.Where(port =>
            (port.PortType == PortTypeEnum.Usb && IncludeUsbPorts) ||
            (port.PortType == PortTypeEnum.Acm && IncludeAcmPorts) ||
            (port.PortType == PortTypeEnum.Standard && IncludeSerialPorts) ||
            (port.PortType == PortTypeEnum.Unknown)).ToList();

        // Update the observable collection on UI thread
        _uiThreadService.InvokeOnUIThread(() =>
        {
            UpdateObservableCollection(DiscoveredPorts, filteredPorts);

            // Update statistics
            TotalPortsFound = DiscoveredPorts.Count;
            AccessiblePortsCount = DiscoveredPorts.Count(p => p.IsAccessible);

            // Force UI refresh of all filter-related properties
            this.RaisePropertyChanged(nameof(IncludeUsbPorts));
            this.RaisePropertyChanged(nameof(IncludeAcmPorts));
            this.RaisePropertyChanged(nameof(IncludeSerialPorts));
            this.RaisePropertyChanged(nameof(CanToggleUsbPorts));
            this.RaisePropertyChanged(nameof(CanToggleAcmPorts));
            this.RaisePropertyChanged(nameof(CanToggleSerialPorts));
        });

        _logger.LogInformation("Applied filters: USB={IncludeUsb}, ACM={IncludeAcm}, Serial={IncludeSerial}, Result={Count} ports",
            IncludeUsbPorts, IncludeAcmPorts, IncludeSerialPorts, filteredPorts.Count);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of the resources used by this ViewModel.
    /// </summary>
    public void Dispose()
    {
        _scanTimer?.Dispose();
        _scanCancellationTokenSource?.Cancel();
        _scanCancellationTokenSource?.Dispose();
        _disposables?.Dispose();
        _logger.LogInformation("SerialPortDiscoveryViewModel disposed");
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Represents information about a discovered serial port.
/// </summary>
/// <summary>
/// Defines the type of serial port.
/// </summary>
public enum PortTypeEnum
{
    /// <summary>
    /// USB serial port (ttyUSB*).
    /// </summary>
    Usb,

    /// <summary>
    /// USB modem/ACM port (ttyACM*).
    /// </summary>
    Acm,

    /// <summary>
    /// Standard serial port (ttyS*).
    /// </summary>
    Standard,

    /// <summary>
    /// Unknown port type.
    /// </summary>
    Unknown
}

public class SerialPortInfo : ReactiveObject
{
    private string _portName = string.Empty;
    /// <summary>
    /// Gets or sets the port name.
    /// </summary>
    public string PortName
    {
        get => _portName;
        set => this.RaiseAndSetIfChanged(ref _portName, value);
    }

    private string _displayName = string.Empty;
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set => this.RaiseAndSetIfChanged(ref _displayName, value);
    }

    private PortTypeEnum _portType = PortTypeEnum.Unknown;
    /// <summary>
    /// Gets or sets the port type enum.
    /// </summary>
    public PortTypeEnum PortType
    {
        get => _portType;
        set => this.RaiseAndSetIfChanged(ref _portType, value);
    }

    private string _portTypeDisplay = string.Empty;
    /// <summary>
    /// Gets or sets the port type display string (for UI binding).
    /// </summary>
    public string PortTypeDisplay
    {
        get => _portTypeDisplay;
        set => this.RaiseAndSetIfChanged(ref _portTypeDisplay, value);
    }

    private bool _isAccessible;
    /// <summary>
    /// Gets or sets a value indicating whether the port is accessible.
    /// </summary>
    public bool IsAccessible
    {
        get => _isAccessible;
        set => this.RaiseAndSetIfChanged(ref _isAccessible, value);
    }

    private string _description = string.Empty;
    /// <summary>
    /// Gets or sets the port description.
    /// </summary>
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    private string _manufacturer = string.Empty;
    /// <summary>
    /// Gets or sets the manufacturer.
    /// </summary>
    public string Manufacturer
    {
        get => _manufacturer;
        set => this.RaiseAndSetIfChanged(ref _manufacturer, value);
    }

    private string _serialNumber = string.Empty;
    /// <summary>
    /// Gets or sets the serial number.
    /// </summary>
    public string SerialNumber
    {
        get => _serialNumber;
        set => this.RaiseAndSetIfChanged(ref _serialNumber, value);
    }

    private DateTime _lastChecked = DateTime.Now;
    /// <summary>
    /// Gets or sets the last checked timestamp.
    /// </summary>
    public DateTime LastChecked
    {
        get => _lastChecked;
        set => this.RaiseAndSetIfChanged(ref _lastChecked, value);
    }

    private string _lastTestResult = string.Empty;
    /// <summary>
    /// Gets or sets the last test result.
    /// </summary>
    public string LastTestResult
    {
        get => _lastTestResult;
        set => this.RaiseAndSetIfChanged(ref _lastTestResult, value);
    }

    private DateTime? _lastTested;
    /// <summary>
    /// Gets or sets the last tested timestamp.
    /// </summary>
    public DateTime? LastTested
    {
        get => _lastTested;
        set => this.RaiseAndSetIfChanged(ref _lastTested, value);
    }
}

/// <summary>
/// Represents the result of a port scan operation.
/// </summary>
public class ScanResult : ReactiveObject
{
    private DateTime _scanTime = DateTime.Now;
    /// <summary>
    /// Gets or sets the scan timestamp.
    /// </summary>
    public DateTime ScanTime
    {
        get => _scanTime;
        set => this.RaiseAndSetIfChanged(ref _scanTime, value);
    }

    private TimeSpan _duration;
    /// <summary>
    /// Gets or sets the scan duration.
    /// </summary>
    public TimeSpan Duration
    {
        get => _duration;
        set => this.RaiseAndSetIfChanged(ref _duration, value);
    }

    private int _portsFound;
    /// <summary>
    /// Gets or sets the number of ports found.
    /// </summary>
    public int PortsFound
    {
        get => _portsFound;
        set => this.RaiseAndSetIfChanged(ref _portsFound, value);
    }

    private int _accessiblePorts;
    /// <summary>
    /// Gets or sets the number of accessible ports.
    /// </summary>
    public int AccessiblePorts
    {
        get => _accessiblePorts;
        set => this.RaiseAndSetIfChanged(ref _accessiblePorts, value);
    }

    private string _filterUsed = string.Empty;
    /// <summary>
    /// Gets or sets the filter that was used.
    /// </summary>
    public string FilterUsed
    {
        get => _filterUsed;
        set => this.RaiseAndSetIfChanged(ref _filterUsed, value);
    }
}
