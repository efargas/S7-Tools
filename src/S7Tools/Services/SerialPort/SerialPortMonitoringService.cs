using S7Tools.Core.Interfaces.Services;
using S7Tools.Extensions;

namespace S7Tools.Services.SerialPort;

/// <summary>
/// Service responsible for serial port monitoring and change detection.
/// This service manages port monitoring timers and events.
/// </summary>
public sealed class SerialPortMonitoringService : IDisposable
{
    private readonly ILogger<SerialPortMonitoringService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly SerialPortDiscoveryService _discoveryService;
    private readonly Timer _monitoringTimer;
    private readonly Dictionary<string, SerialPortInfo> _lastKnownPorts = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isMonitoring;
    private int _monitoringCallbackRunning;
    private int _scanIntervalSeconds;

    /// <summary>
    /// Initializes a new instance of the SerialPortMonitoringService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="timeProvider">The time provider for abstracting time operations.</param>
    /// <param name="discoveryService">The discovery service for port scanning.</param>
    public SerialPortMonitoringService(
        ILogger<SerialPortMonitoringService> logger,
        ITimeProvider timeProvider,
        SerialPortDiscoveryService discoveryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));

        // Initialize monitoring timer in stopped state to satisfy analyzers and manage lifecycle cleanly
        // Using self-rescheduling timer to support dynamic interval updates
        _monitoringTimer = new Timer(static async state =>
        {
            if (state is not SerialPortMonitoringService service)
            {
                return;
            }

            // Capture the timer instance to prevent race conditions with Dispose
            Timer? timer = service._monitoringTimer;
            if (timer == null)
            {
                return;
            }

            if (Interlocked.Exchange(ref service._monitoringCallbackRunning, 1) == 1)
            {
                return; // Skip overlapping execution
            }

            try
            {
                try
                {
                    await service.MonitorPortChangesAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    service._logger.LogError(ex, "Unhandled exception in serial port monitoring callback");
                }

                // Reschedule the next run using the captured timer instance
                try
                {
                    timer.Change(TimeSpan.FromSeconds(service._scanIntervalSeconds), Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // Timer disposed during shutdown; ignore
                }
            }
            finally
            {
                Interlocked.Exchange(ref service._monitoringCallbackRunning, 0);
            }
        }, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    #region Events

    /// <summary>
    /// Event raised when a port is added.
    /// </summary>
    public event EventHandler<SerialPortEventArgs>? PortAdded;

    /// <summary>
    /// Event raised when a port is removed.
    /// </summary>
    public event EventHandler<SerialPortEventArgs>? PortRemoved;

    /// <summary>
    /// Event raised when a port's status changes.
    /// </summary>
    public event EventHandler<SerialPortStatusChangedEventArgs>? PortStatusChanged;

    #endregion

    /// <summary>
    /// Starts monitoring serial ports for changes.
    /// </summary>
    /// <param name="includeUsbPorts">Whether to monitor USB ports.</param>
    /// <param name="includeAcmPorts">Whether to monitor ACM ports.</param>
    /// <param name="includeStandardPorts">Whether to monitor standard ports.</param>
    /// <param name="maxScanPorts">Maximum number of ports to scan per type.</param>
    /// <param name="scanIntervalSeconds">Interval between scans in seconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task StartMonitoringAsync(
        bool includeUsbPorts,
        bool includeAcmPorts,
        bool includeStandardPorts,
        int maxScanPorts,
        int scanIntervalSeconds,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.ExecuteAsync(async () =>
        {
            if (_isMonitoring)
            {
                return;
            }

            _isMonitoring = true;
            _scanIntervalSeconds = Math.Clamp(scanIntervalSeconds, 1, 3600);

            // Initial scan to populate known ports
            IEnumerable<SerialPortInfo> currentPorts = await _discoveryService.ScanAvailablePortsAsync(
                includeUsbPorts,
                includeAcmPorts,
                includeStandardPorts,
                maxScanPorts,
                cancellationToken).ConfigureAwait(false);

            foreach (SerialPortInfo port in currentPorts)
            {
                _lastKnownPorts[port.PortPath] = port;
            }

            // Start self-rescheduling timer with initial delay
            // The timer will reschedule itself after each execution to support dynamic interval updates
            _monitoringTimer!.Change(TimeSpan.FromSeconds(_scanIntervalSeconds), Timeout.InfiniteTimeSpan);

            _logger.LogInformation("Started port monitoring with {Interval}s interval (dynamic updates enabled)", _scanIntervalSeconds);
        }, cancellationToken);
    }

    /// <summary>
    /// Stops monitoring serial ports.
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        await _semaphore.ExecuteAsync(async () =>
        {
            if (!_isMonitoring)
            {
                return;
            }

            _isMonitoring = false;
            // Stop monitoring timer (keep instance for reuse)
            _monitoringTimer!.Change(Timeout.Infinite, Timeout.Infinite);
            _lastKnownPorts.Clear();

            _logger.LogInformation("Stopped port monitoring");
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Updates the scan interval for monitoring.
    /// </summary>
    /// <param name="scanIntervalSeconds">New scan interval in seconds.</param>
    public void UpdateScanInterval(int scanIntervalSeconds)
    {
        _scanIntervalSeconds = Math.Clamp(scanIntervalSeconds, 1, 3600);
        _logger.LogDebug("Updated scan interval to {Interval}s", _scanIntervalSeconds);
    }

    #region Private Methods

    /// <summary>
    /// Monitors port changes and raises events.
    /// </summary>
    private async Task MonitorPortChangesAsync()
    {
        if (!_isMonitoring)
        {
            return;
        }

        try
        {
            // Scan with current settings - Note: We need to get these from somewhere
            // For now, we'll assume all types enabled with max 32 ports
            IEnumerable<SerialPortInfo> currentPorts = await _discoveryService.ScanAvailablePortsAsync(
                includeUsbPorts: true,
                includeAcmPorts: true,
                includeStandardPorts: true,
                maxScanPorts: 32).ConfigureAwait(false);

            var currentPortPaths = currentPorts.ToDictionary(p => p.PortPath, p => p);

            // Check for removed ports
            var removedPorts = _lastKnownPorts.Keys.Except(currentPortPaths.Keys).ToList();
            foreach (string? removedPortPath in removedPorts)
            {
                SerialPortInfo removedPort = _lastKnownPorts[removedPortPath];
                _lastKnownPorts.Remove(removedPortPath);
                PortRemoved?.Invoke(this, new SerialPortEventArgs(removedPort));
                _logger.LogDebug("Port removed: {PortPath}", removedPortPath);
            }

            // Check for added ports
            var addedPorts = currentPortPaths.Keys.Except(_lastKnownPorts.Keys).ToList();
            foreach (string? addedPortPath in addedPorts)
            {
                SerialPortInfo addedPort = currentPortPaths[addedPortPath];
                _lastKnownPorts[addedPortPath] = addedPort;
                PortAdded?.Invoke(this, new SerialPortEventArgs(addedPort));
                _logger.LogDebug("Port added: {PortPath}", addedPortPath);
            }

            // Check for status changes
            foreach (string? portPath in currentPortPaths.Keys.Intersect(_lastKnownPorts.Keys))
            {
                SerialPortInfo currentPort = currentPortPaths[portPath];
                SerialPortInfo lastKnownPort = _lastKnownPorts[portPath];

                if (currentPort.IsAccessible != lastKnownPort.IsAccessible)
                {
                    _lastKnownPorts[portPath] = currentPort;
                    PortStatusChanged?.Invoke(this, new SerialPortStatusChangedEventArgs(portPath, lastKnownPort.IsAccessible, currentPort.IsAccessible));
                    _logger.LogDebug("Port status changed: {PortPath} - {OldStatus} -> {NewStatus}", portPath, lastKnownPort.IsAccessible, currentPort.IsAccessible);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during port monitoring");
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}
