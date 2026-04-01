using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Services.SerialPort;

namespace S7Tools.Services;

/// <summary>
/// Facade service for serial port operations. Delegates to specialized services for discovery, configuration, and monitoring.
/// This service maintains the ISerialPortService interface for backward compatibility while providing improved separation of concerns.
/// </summary>
public sealed class SerialPortService : ISerialPortService, IDisposable
{
    private readonly ILogger<SerialPortService> _logger;
    private readonly IApplicationSettingsService _settingsService;

    // Specialized services
    private readonly SerialPortDiscoveryService _discoveryService;
    private readonly SerialPortConfigurationService _configService;
    private readonly SerialPortMonitoringService _monitoringService;

    /// <summary>
    /// Initializes a new instance of the SerialPortService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="settingsService">The application settings service for runtime configuration.</param>
    /// <param name="discoveryService">The discovery service for port scanning.</param>
    /// <param name="configService">The configuration service for stty operations.</param>
    /// <param name="monitoringService">The monitoring service for change detection.</param>
    public SerialPortService(
        ILogger<SerialPortService> logger,
        IApplicationSettingsService settingsService,
        SerialPortDiscoveryService discoveryService,
        SerialPortConfigurationService configService,
        SerialPortMonitoringService monitoringService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));

        // Wire up events from monitoring service
        _monitoringService.PortAdded += OnPortAdded;
        _monitoringService.PortRemoved += OnPortRemoved;
        _monitoringService.PortStatusChanged += OnPortStatusChanged;

        _logger.LogDebug("SerialPortService facade initialized with specialized services");
    }

    private void OnPortAdded(object? sender, SerialPortEventArgs e) => PortAdded?.Invoke(this, e);
    private void OnPortRemoved(object? sender, SerialPortEventArgs e) => PortRemoved?.Invoke(this, e);
    private void OnPortStatusChanged(object? sender, SerialPortStatusChangedEventArgs e) => PortStatusChanged?.Invoke(this, e);

    #region Events

    /// <inheritdoc />
    public event EventHandler<SerialPortEventArgs>? PortAdded;

    /// <inheritdoc />
    public event EventHandler<SerialPortEventArgs>? PortRemoved;

    /// <inheritdoc />
    public event EventHandler<SerialPortStatusChangedEventArgs>? PortStatusChanged;

    #endregion

    #region Port Discovery and Monitoring

    /// <inheritdoc />
    public async Task<IEnumerable<SerialPortInfo>> ScanAvailablePortsAsync(CancellationToken cancellationToken = default)
    {
        // Get settings from application settings service
        bool includeUsbPorts = _settingsService.Current.Serial.IncludeUsbPorts;
        bool includeAcmPorts = _settingsService.Current.Serial.IncludeAcmPorts;
        bool includeStandardPorts = _settingsService.Current.Serial.IncludeStandardPorts;
        int maxScanPorts = _settingsService.Current.Serial.MaxScanPorts;

        return await _discoveryService.ScanAvailablePortsAsync(
            includeUsbPorts,
            includeAcmPorts,
            includeStandardPorts,
            maxScanPorts,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SerialPortInfo?> GetPortInfoAsync(string portPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        // Get port test timeout from settings and clamp to a safe range
        int configuredTimeoutMs = _settingsService.Current.Serial.PortTestTimeoutMs;
        int portTestTimeoutMs = Math.Clamp(configuredTimeoutMs, 100, 10_000);
        if (portTestTimeoutMs != configuredTimeoutMs)
        {
            _logger.LogWarning("Adjusted 'serial.portTestTimeoutMs' from {Configured} to safe value {Effective}", configuredTimeoutMs, portTestTimeoutMs);
        }

        return await _discoveryService.GetPortInfoAsync(portPath, portTestTimeoutMs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> IsPortAccessibleAsync(string portPath, int timeoutMs = 1000, CancellationToken cancellationToken = default)
        => _discoveryService.IsPortAccessibleAsync(portPath, timeoutMs, cancellationToken);

    /// <inheritdoc />
    public async Task StartPortMonitoringAsync(CancellationToken cancellationToken = default)
    {
        // Get settings from application settings service
        bool includeUsbPorts = _settingsService.Current.Serial.IncludeUsbPorts;
        bool includeAcmPorts = _settingsService.Current.Serial.IncludeAcmPorts;
        bool includeStandardPorts = _settingsService.Current.Serial.IncludeStandardPorts;
        int maxScanPorts = _settingsService.Current.Serial.MaxScanPorts;

        // Get scan interval from settings and clamp to a safe range
        int configuredInterval = _settingsService.Current.Serial.ScanIntervalSeconds;
        int scanIntervalSeconds = Math.Clamp(configuredInterval, 1, 3600);
        if (scanIntervalSeconds != configuredInterval)
        {
            _logger.LogWarning("Adjusted 'serial.scanIntervalSeconds' from {Configured} to safe value {Effective}", configuredInterval, scanIntervalSeconds);
        }

        await _monitoringService.StartMonitoringAsync(
            includeUsbPorts,
            includeAcmPorts,
            includeStandardPorts,
            maxScanPorts,
            scanIntervalSeconds,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopPortMonitoringAsync()
        => _monitoringService.StopMonitoringAsync();

    #endregion

    #region Configuration Management

    /// <inheritdoc />
    public Task<SerialPortConfiguration?> ReadPortConfigurationAsync(string portPath, CancellationToken cancellationToken = default)
        => _configService.ReadPortConfigurationAsync(portPath, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ApplyConfigurationAsync(
        string portPath,
        SerialPortConfiguration configuration,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        CancellationToken cancellationToken = default)
        => _configService.ApplyConfigurationAsync(portPath, configuration, taskLogger, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ApplyProfileAsync(
        string portPath,
        SerialPortProfile profile,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        CancellationToken cancellationToken = default)
        => _configService.ApplyProfileAsync(portPath, profile, taskLogger, cancellationToken);

    /// <inheritdoc />
    public Task<SerialPortConfiguration?> BackupPortConfigurationAsync(string portPath, CancellationToken cancellationToken = default)
        => _configService.BackupPortConfigurationAsync(portPath, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RestorePortConfigurationAsync(string portPath, SerialPortConfiguration backupConfiguration, CancellationToken cancellationToken = default)
        => _configService.RestorePortConfigurationAsync(portPath, backupConfiguration, cancellationToken);

    #endregion

    #region stty Command Generation and Execution

    /// <inheritdoc />
    public string GenerateSttyCommand(string portPath, SerialPortConfiguration configuration)
        => _configService.GenerateSttyCommand(portPath, configuration);

    /// <inheritdoc />
    public string GenerateSttyCommandForProfile(string portPath, SerialPortProfile profile)
        => _configService.GenerateSttyCommandForProfile(portPath, profile);

    /// <inheritdoc />
    public Task<SttyCommandResult> ExecuteSttyCommandAsync(string command, CancellationToken cancellationToken = default)
        => _configService.ExecuteSttyCommandAsync(command, cancellationToken);

    /// <inheritdoc />
    public SttyCommandValidationResult ValidateSttyCommand(string command)
        => _configService.ValidateSttyCommand(command);

    #endregion

    #region Port Type Detection

    /// <inheritdoc />
    public SerialPortType GetPortType(string portPath)
        => _discoveryService.GetPortType(portPath);

    /// <inheritdoc />
    public Task<UsbDeviceInfo?> GetUsbDeviceInfoAsync(string portPath, CancellationToken cancellationToken = default)
        => _discoveryService.GetUsbDeviceInfoAsync(portPath, cancellationToken);

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_monitoringService != null)
        {
            _monitoringService.PortAdded -= OnPortAdded;
            _monitoringService.PortRemoved -= OnPortRemoved;
            _monitoringService.PortStatusChanged -= OnPortStatusChanged;
        }

        _monitoringService?.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}
