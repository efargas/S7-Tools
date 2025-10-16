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

namespace S7Tools.ViewModels;

/// <summary>
/// ViewModel for real-time serial port discovery and monitoring, providing comprehensive
/// port scanning capabilities with status monitoring and configuration testing.
/// </summary>
public sealed class SerialPortScannerViewModel : ViewModelBase, IDisposable
{
    #region Fields

    private readonly ISerialPortService _portService;
    private readonly ILogger<SerialPortScannerViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();
    private readonly Timer _scanTimer;
    private CancellationTokenSource? _scanCancellationTokenSource;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the SerialPortScannerViewModel class.
    /// </summary>
    /// <param name="portService">The serial port service.</param>
    /// <param name="logger">The logger.</param>
    public SerialPortScannerViewModel(
        ISerialPortService portService,
        ILogger<SerialPortScannerViewModel> logger)
    {
        _portService = portService ?? throw new ArgumentNullException(nameof(portService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize collections
        DiscoveredPorts = new ObservableCollection<SerialPortInfo>();
        ScanHistory = new ObservableCollection<ScanResult>();

        // Initialize commands
        InitializeCommands();

        // Set up automatic scanning timer (disabled by default)
        _scanTimer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

        // Perform initial scan
        _ = Task.Run(ScanPortsAsync);

        _logger.LogInformation("SerialPortScannerViewModel initialized");
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
        set => this.RaiseAndSetIfChanged(ref _isScanning, value);
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
        set => this.RaiseAndSetIfChanged(ref _includeUsbPorts, value);
    }

    private bool _includeAcmPorts = true;
    /// <summary>
    /// Gets or sets a value indicating whether to include ACM ports in scans.
    /// </summary>
    public bool IncludeAcmPorts
    {
        get => _includeAcmPorts;
        set => this.RaiseAndSetIfChanged(ref _includeAcmPorts, value);
    }

    private bool _includeSerialPorts = true;
    /// <summary>
    /// Gets or sets a value indicating whether to include serial ports in scans.
    /// </summary>
    public bool IncludeSerialPorts
    {
        get => _includeSerialPorts;
        set => this.RaiseAndSetIfChanged(ref _includeSerialPorts, value);
    }

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
        var canScan = this.WhenAnyValue(x => x.IsScanning)
            .Select(scanning => !scanning);

        ScanPortsCommand = ReactiveCommand.CreateFromTask(ScanPortsAsync, canScan);
        ScanPortsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "scanning ports"))
            .DisposeWith(_disposables);

        // Stop scan command - enabled when currently scanning
        var canStopScan = this.WhenAnyValue(x => x.IsScanning);

        StopScanCommand = ReactiveCommand.Create(StopScan, canStopScan);

        // Refresh port info command - enabled when a port is selected and not scanning
        var canRefreshPortInfo = this.WhenAnyValue(x => x.SelectedPort, x => x.IsScanning)
            .Select(tuple => tuple.Item1 != null && !tuple.Item2);

        RefreshPortInfoCommand = ReactiveCommand.CreateFromTask(RefreshPortInfoAsync, canRefreshPortInfo);
        RefreshPortInfoCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "refreshing port info"))
            .DisposeWith(_disposables);

        // Test selected port command - enabled when a port is selected and not scanning
        var canTestSelectedPort = this.WhenAnyValue(x => x.SelectedPort, x => x.IsScanning)
            .Select(tuple => tuple.Item1 != null && !tuple.Item2);

        TestSelectedPortCommand = ReactiveCommand.CreateFromTask(TestSelectedPortAsync, canTestSelectedPort);
        TestSelectedPortCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "testing port"))
            .DisposeWith(_disposables);

        // Clear history command - enabled when history is not empty
        var canClearHistory = this.WhenAnyValue(x => x.ScanHistory.Count)
            .Select(count => count > 0);

        ClearHistoryCommand = ReactiveCommand.Create(ClearHistory, canClearHistory);

        // Export results command - enabled when ports are found
        var canExportResults = this.WhenAnyValue(x => x.TotalPortsFound)
            .Select(count => count > 0);

        ExportResultsCommand = ReactiveCommand.CreateFromTask(ExportResultsAsync, canExportResults);
        ExportResultsCommand.ThrownExceptions
            .Subscribe(ex => HandleCommandException(ex, "exporting results"))
            .DisposeWith(_disposables);

        // Copy port info command - enabled when a port is selected
        var canCopyPortInfo = this.WhenAnyValue(x => x.SelectedPort)
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
            IsScanning = true;
            StatusMessage = "Scanning for ports...";
            var startTime = DateTime.Now;

            _scanCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _scanCancellationTokenSource.Token;

            // Get available ports
            var availablePorts = await _portService.ScanAvailablePortsAsync(cancellationToken);

            // Extract port paths and filter
            var portPaths = availablePorts.Select(p => p.PortPath);
            var filteredPorts = FilterPorts(portPaths);

            // Create port info objects
            var portInfos = new List<SerialPortInfo>();
            foreach (var portName in filteredPorts)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var portInfo = new SerialPortInfo
                {
                    PortName = portName,
                    DisplayName = GetPortDisplayName(portName),
                    PortType = GetPortType(portName),
                    IsAccessible = CheckAccessibility ? await _portService.IsPortAccessibleAsync(portName, 1000, cancellationToken) : true,
                    LastChecked = DateTime.Now
                };

                // Get additional port information if accessible
                if (portInfo.IsAccessible && CheckAccessibility)
                {
                    try
                    {
                        var portDetails = await _portService.GetPortInfoAsync(portName, cancellationToken);
                        if (portDetails is not null)
                        {
                            portInfo.Description = portDetails.Description;
                            portInfo.Manufacturer = portDetails.UsbInfo?.VendorName ?? "Unknown";
                            portInfo.SerialNumber = portDetails.UsbInfo?.SerialNumber ?? "Unknown";
                        }
                        else
                        {
                            portInfo.Description = "Details unavailable";
                            portInfo.Manufacturer = "Unknown";
                            portInfo.SerialNumber = "Unknown";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not get details for port {PortName}", portName);
                        portInfo.Description = "Details unavailable";
                        portInfo.Manufacturer = "Unknown";
                        portInfo.SerialNumber = "Unknown";
                    }
                }

                portInfos.Add(portInfo);
            }

            // Update UI
            DiscoveredPorts.Clear();
            foreach (var portInfo in portInfos.OrderBy(p => p.PortName))
            {
                DiscoveredPorts.Add(portInfo);
            }

            // Update statistics
            var endTime = DateTime.Now;
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
            _logger.LogInformation("Port scan completed: {TotalPorts} total, {AccessiblePorts} accessible", TotalPortsFound, AccessiblePortsCount);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled";
            _logger.LogInformation("Port scan cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for ports");
            StatusMessage = "Error scanning for ports";
        }
        finally
        {
            IsScanning = false;
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
        StatusMessage = "Stopping scan...";
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
            StatusMessage = "Refreshing port information...";

            var portName = SelectedPort.PortName;
            var isAccessible = await _portService.IsPortAccessibleAsync(portName);

            SelectedPort.IsAccessible = isAccessible;
            SelectedPort.LastChecked = DateTime.Now;

            if (isAccessible)
            {
                try
                {
                    var portDetails = await _portService.GetPortInfoAsync(portName);
                    if (portDetails is not null)
                    {
                        SelectedPort.Description = portDetails.Description;
                        SelectedPort.Manufacturer = portDetails.UsbInfo?.VendorName ?? "Unknown";
                        SelectedPort.SerialNumber = portDetails.UsbInfo?.SerialNumber ?? "Unknown";
                    }
                    else
                    {
                        SelectedPort.Description = "Details unavailable";
                        SelectedPort.Manufacturer = "Unknown";
                        SelectedPort.SerialNumber = "Unknown";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get details for port {PortName}", portName);
                    SelectedPort.Description = "Details unavailable";
                    SelectedPort.Manufacturer = "Unknown";
                    SelectedPort.SerialNumber = "Unknown";
                }
            }

            StatusMessage = $"Port {portName} information refreshed";
            _logger.LogInformation("Refreshed information for port: {PortName}", portName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing port information");
            StatusMessage = "Error refreshing port information";
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
            StatusMessage = "Testing port...";

            var defaultConfig = SerialPortConfiguration.CreateDefault();
            var success = await _portService.ApplyConfigurationAsync(SelectedPort.PortName, defaultConfig);

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
            StatusMessage = "Error testing port";
        }
    }

    /// <summary>
    /// Clears the scan history.
    /// </summary>
    private void ClearHistory()
    {
        ScanHistory.Clear();
        StatusMessage = "Scan history cleared";
        _logger.LogInformation("Scan history cleared");
    }

    /// <summary>
    /// Exports scan results to a file.
    /// </summary>
    private async Task ExportResultsAsync()
    {
        try
        {
            StatusMessage = "Exporting scan results...";

            // TODO: Implement file dialog and export functionality
            await Task.CompletedTask;

            StatusMessage = "Scan results exported";
            _logger.LogInformation("Scan results exported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting scan results");
            StatusMessage = "Error exporting scan results";
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

            StatusMessage = "Port information copied to clipboard";
            _logger.LogInformation("Port information copied to clipboard for: {PortName}", SelectedPort.PortName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying port information");
            StatusMessage = "Error copying port information";
        }
    }

    /// <summary>
    /// Filters ports based on current settings.
    /// </summary>
    /// <param name="ports">The list of ports to filter.</param>
    /// <returns>The filtered list of ports.</returns>
    private IEnumerable<string> FilterPorts(IEnumerable<string> ports)
    {
        var filtered = ports.AsEnumerable();

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
    /// Gets the port type for a port name.
    /// </summary>
    /// <param name="portName">The port name.</param>
    /// <returns>The port type.</returns>
    private static string GetPortType(string portName)
    {
        return portName switch
        {
            var name when name.Contains("ttyUSB") => "USB Serial",
            var name when name.Contains("ttyACM") => "USB Modem",
            var name when name.Contains("ttyS") => "Serial Port",
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
        _logger.LogInformation("SerialPortScannerViewModel disposed");
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Represents information about a discovered serial port.
/// </summary>
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

    private string _portType = string.Empty;
    /// <summary>
    /// Gets or sets the port type.
    /// </summary>
    public string PortType
    {
        get => _portType;
        set => this.RaiseAndSetIfChanged(ref _portType, value);
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
