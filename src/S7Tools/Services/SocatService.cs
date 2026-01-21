using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using System.Net.Sockets;
using S7Tools.Extensions;

namespace S7Tools.Services;

/// <summary>
/// Service for socat (Serial-to-TCP Proxy) operations including process management, command generation, and status monitoring.
/// This service provides comprehensive socat management capabilities for serial-to-TCP bridge functionality.
/// </summary>
public partial class SocatService : ISocatService, IDisposable
{
#pragma warning disable CS0067 // Events may be declared for external subscriptions; not used in this assembly
    private readonly ILogger<SocatService> _logger;
    private readonly IApplicationSettingsService _settingsService;

    // Specialized Socat services (facade pattern)
    private readonly Socat.SocatCommandBuilder _commandBuilder;
    private readonly Socat.SocatProcessManager _processManager;
    private readonly Socat.SocatPortManager _portManager;
    private readonly Socat.SocatConfigurationService _configService;

    // Facade state management (coordination across specialized services)
    private readonly ISerialPortService _serialPortService;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<int, Process> _managedProcesses = [];
    private readonly Dictionary<int, Timer> _monitoringTimers = [];

    // State management (coordinated by facade)
    private readonly Dictionary<int, SocatProcessInfo> _runningProcesses = [];
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the SocatService class (Facade/Orchestrator).
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="settingsService">The application settings service for runtime configuration.</param>
    /// <param name="commandBuilder">Service for command generation and validation.</param>
    /// <param name="processManager">Service for process lifecycle management.</param>
    /// <param name="portManager">Service for port checking and connection testing.</param>
    /// <param name="configService">Service for serial device configuration.</param>
    /// <param name="serialPortService">Service for serial port communications.</param>
    /// <param name="timeProvider">Provider for time-related operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public SocatService(
        ILogger<SocatService> logger,
        IApplicationSettingsService settingsService,
        Socat.SocatCommandBuilder commandBuilder,
        Socat.SocatProcessManager processManager,
        Socat.SocatPortManager portManager,
        Socat.SocatConfigurationService configService,
        ISerialPortService serialPortService,
        ITimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(processManager);
        ArgumentNullException.ThrowIfNull(portManager);
        ArgumentNullException.ThrowIfNull(configService);
        ArgumentNullException.ThrowIfNull(serialPortService);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _logger = logger;
        _settingsService = settingsService;
        _commandBuilder = commandBuilder;
        _processManager = processManager;
        _portManager = portManager;
        _configService = configService;
        _serialPortService = serialPortService;
        _timeProvider = timeProvider;

        _processManager.ProcessExited += OnProcessExited;

        _logger.LogDebug("SocatService initialized as facade with 4 specialized services and 2 injected dependencies");
    }

    private void OnProcessExited(object? sender, Socat.ProcessExitedEventArgs e)
    {
        // Handle process exit event from ProcessManager
        Task.Run(async () =>
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_runningProcesses.TryGetValue(e.ProcessId, out SocatProcessInfo? processInfo))
                {
                    processInfo.IsRunning = false;
                    processInfo.Status = SocatProcessStatus.Stopped;
                    _runningProcesses.Remove(e.ProcessId);

                    ProcessStopped?.Invoke(this, new SocatProcessEventArgs(processInfo));
                }
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    #region Events

    // Some events may be wired by consumers at runtime; suppress unused warnings in this assembly
    /// <summary>
    /// Occurs when a socat process starts.
    /// </summary>
    public event EventHandler<SocatProcessEventArgs>? ProcessStarted;

    /// <summary>
    /// Occurs when a socat process stops or exits.
    /// </summary>
    public event EventHandler<SocatProcessEventArgs>? ProcessStopped;

    /// <summary>
    /// Occurs when a socat process encounters an error.
    /// </summary>
    public event EventHandler<SocatProcessErrorEventArgs>? ProcessError;

    /// <summary>
    /// Occurs when a new TCP connection is established to a socat process.
    /// </summary>
    public event EventHandler<SocatConnectionEventArgs>? ConnectionEstablished;

    /// <summary>
    /// Occurs when a TCP connection to a socat process is closed.
    /// </summary>
    public event EventHandler<SocatConnectionEventArgs>? ConnectionClosed;

    /// <summary>
    /// Occurs when data is transferred through a socat process (for monitoring purposes).
    /// </summary>
    public event EventHandler<SocatDataTransferEventArgs>? DataTransferred;
#pragma warning restore CS0067

    #endregion

    #region Command Generation

    /// <inheritdoc />
    public string GenerateSocatCommand(SocatConfiguration configuration, string serialDevice)
    {
        // Delegate to CommandBuilder service
        return _commandBuilder.GenerateCommand(configuration, serialDevice);
    }

    /// <inheritdoc />
    public string GenerateSocatCommandForProfile(SocatProfile profile, string serialDevice)
    {
        // Delegate to CommandBuilder service
        return _commandBuilder.GenerateCommandForProfile(profile, serialDevice);
    }

    /// <inheritdoc />
    public SocatCommandValidationResult ValidateSocatCommand(string command)
    {
        // Delegate to CommandBuilder service
        return _commandBuilder.Validate(command);
    }

    #endregion

    #region Process Management

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<SocatProcessInfo> StartSocatAsync(SocatConfiguration configuration, string serialDevice, Microsoft.Extensions.Logging.ILogger? processLogger = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        // Get settings from application settings service
        int maxConcurrentInstances = _settingsService.GetSetting("socat.maxConcurrentInstances", 5);
        bool autoConfigureSerialDevice = _settingsService.GetSetting("socat.autoConfigureSerialDevice", true);

        // Check concurrent instances limit
        return await _semaphore.ExecuteAsync(async () =>
        {
            if (_runningProcesses.Count >= maxConcurrentInstances)
            {
                throw new ConfigurationException(
                    "MaxConcurrentInstances",
                    $"Maximum number of socat instances ({maxConcurrentInstances}) already running");
            }

            // Validate serial device exists before starting socat
            if (!File.Exists(serialDevice))
            {
                _logger.LogError("Serial device {Device} does not exist. Please verify the device path and ensure it is connected.", serialDevice);
                throw new ValidationException(
                    "SerialDevice",
                    $"Serial device '{serialDevice}' does not exist. Please check the device connection and try scanning for devices again.");
            }

            // Check if port is already in use (delegate to PortManager)
            bool portInUse = _portManager.IsPortUsedByManagedProcess(configuration.TcpPort, _runningProcesses.Values);
            if (!portInUse)
            {
                portInUse = await _portManager.IsPortInUseAsync(configuration.TcpPort, cancellationToken).ConfigureAwait(false);
            }

            if (portInUse)
            {
                throw new ConnectionException(
                    $"0.0.0.0:{configuration.TcpPort}",
                    "TCP",
                    $"TCP port {configuration.TcpPort} is already in use");
            }

            // Prepare serial device if configured
            if (autoConfigureSerialDevice && configuration.AutoConfigureSerial)
            {
                _logger.LogDebug("Preparing serial device {Device} for socat", serialDevice);
                bool prepared = await PrepareSerialDeviceAsync(serialDevice, configuration, cancellationToken).ConfigureAwait(false);
                if (!prepared)
                {
                    throw new ValidationException(
                        "SerialDevice",
                        $"Failed to prepare serial device {serialDevice}");
                }
            }

            // Generate and validate command
            string command = GenerateSocatCommand(configuration, serialDevice);
            SocatCommandValidationResult validation = ValidateSocatCommand(command);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }

            // Start socat process
            SocatProcessInfo processInfo = await StartSocatProcessAsync(command, configuration, serialDevice, null, processLogger, cancellationToken).ConfigureAwait(false);

            _runningProcesses[processInfo.ProcessId] = processInfo;

            _logger.LogInformation("Started socat process {ProcessId} for device {Device} on TCP port {Port}",
                processInfo.ProcessId, serialDevice, configuration.TcpPort);

            // Raise event
            ProcessStarted?.Invoke(this, new SocatProcessEventArgs(processInfo));

            return processInfo;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SocatProcessInfo> StartSocatWithProfileAsync(SocatProfile profile, string serialDevice, Microsoft.Extensions.Logging.ILogger? processLogger = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Entering StartSocatWithProfileAsync - Profile: {ProfileName}, Device: {Device}",
            profile?.Name ?? "NULL", serialDevice ?? "NULL");

        ArgumentNullException.ThrowIfNull(profile, nameof(profile));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            _logger.LogError("Serial device is null or empty");
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        _logger.LogDebug("Getting socat settings - MaxConcurrentInstances query");
        // Get settings from application settings service
        int maxConcurrentInstances = _settingsService.GetSetting("socat.maxConcurrentInstances", 5);
        bool autoConfigureSerialDevice = _settingsService.GetSetting("socat.autoConfigureSerialDevice", true);

        // PERFORM VALIDATIONS BEFORE ACQUIRING SEMAPHORE to reduce lock duration

        // The pre-check for port availability has been removed to avoid a race condition.
        // The check is now performed atomically inside the semaphore lock.

        // Validate serial device exists before starting socat
        _logger.LogDebug("Checking if serial device exists: {Device}", serialDevice);
        if (!File.Exists(serialDevice))
        {
            _logger.LogError("Serial device {Device} does not exist. Please verify the device path and ensure it is connected.", serialDevice);
            throw new ValidationException(
                "SerialDevice",
                $"Serial device '{serialDevice}' does not exist. Please check the device connection and try scanning for devices again.");
        }

        // Prepare serial device if configured (before semaphore to reduce critical section)
        if (autoConfigureSerialDevice && profile.Configuration.AutoConfigureSerial)
        {
            _logger.LogDebug("Preparing serial device {Device} for socat profile '{Profile}'", serialDevice, profile.Name);
            bool prepared = await PrepareSerialDeviceAsync(serialDevice, profile.Configuration, cancellationToken).ConfigureAwait(false);
            if (!prepared)
            {
                _logger.LogError("Failed to prepare serial device {Device}", serialDevice);
                throw new ValidationException(
                    "SerialDevice",
                    $"Failed to prepare serial device {serialDevice}");
            }
        }

        // Generate and validate command (before semaphore)
        _logger.LogDebug("Generating socat command");
        string command = GenerateSocatCommandForProfile(profile, serialDevice);

        _logger.LogDebug("Validating socat command");
        SocatCommandValidationResult validation = ValidateSocatCommand(command);
        if (!validation.IsValid)
        {
            _logger.LogError("Invalid socat command: {Errors}", string.Join(", ", validation.Errors));
            throw new ValidationException(validation.Errors);
        }

        // NOW ACQUIRE SEMAPHORE - only protect shared state access
        _logger.LogDebug("Acquiring semaphore for socat start operation");
        return await _semaphore.ExecuteAsync(async () =>
        {
            _logger.LogDebug("Checking concurrent instances: Current={Current}, Max={Max}",
                _runningProcesses.Count, maxConcurrentInstances);
            if (_runningProcesses.Count >= maxConcurrentInstances)
            {
                _logger.LogError("Too many concurrent socat instances");
                throw new ConfigurationException(
                    "MaxConcurrentInstances",
                    $"Maximum number of socat instances ({maxConcurrentInstances}) already running");
            }

            // Check if TCP port is already in use (must be inside semaphore to avoid race)
            _logger.LogDebug("Checking if TCP port {Port} is available", profile.Configuration.TcpPort);
            // Check if port is already in use (delegate to PortManager)
            bool portInUse = _portManager.IsPortUsedByManagedProcess(profile.Configuration.TcpPort, _runningProcesses.Values);
            if (!portInUse)
            {
                portInUse = await _portManager.IsPortInUseAsync(profile.Configuration.TcpPort, cancellationToken).ConfigureAwait(false);
            }

            if (portInUse)
            {
                _logger.LogError("TCP port {Port} is already in use", profile.Configuration.TcpPort);
                throw new ConnectionException(
                    $"0.0.0.0:{profile.Configuration.TcpPort}",
                    "TCP",
                    $"TCP port {profile.Configuration.TcpPort} is already in use");
            }

            // Start socat process (now all validation is done, minimize time in lock)
            _logger.LogDebug("Starting socat process with profile '{Profile}'", profile.Name);
            try
            {
                SocatProcessInfo processInfo = await StartSocatProcessAsync(command, profile.Configuration, serialDevice, profile, processLogger, cancellationToken).ConfigureAwait(false);

                _runningProcesses[processInfo.ProcessId] = processInfo;

                _logger.LogInformation("Started socat process {ProcessId} with profile '{Profile}' for device {Device} on TCP port {Port}",
                    processInfo.ProcessId, profile.Name, serialDevice, profile.Configuration.TcpPort);

                // Raise event
                ProcessStarted?.Invoke(this, new SocatProcessEventArgs(processInfo));

                return processInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in StartSocatWithProfileAsync: {Message}", ex.Message);
                throw;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> StopSocatAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processInfo, nameof(processInfo));

        return await StopSocatByIdAsync(processInfo.ProcessId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> StopSocatByIdAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processId <= 0)
        {
            throw new ArgumentException("Process ID must be greater than zero", nameof(processId));
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
            {
                _logger.LogWarning("Attempted to stop unknown socat process {ProcessId}", processId);
                return false;
            }

            // Get shutdown timeout from settings
            int configuredShutdownSeconds = _settingsService.GetSetting("socat.processShutdownTimeoutSeconds", 5);
            int timeoutMs = Math.Clamp(configuredShutdownSeconds, 1, 120) * 1000;

            // Delegate process stop to ProcessManager
            bool stopped = await _processManager.StopProcessAsync(processId, timeoutMs, cancellationToken).ConfigureAwait(false);

            if (stopped)
            {
                // Update facade state
                processInfo.Status = SocatProcessStatus.Stopped;
                processInfo.IsRunning = false;
                _runningProcesses.Remove(processId);

                _logger.LogInformation("Stopped socat process {ProcessId}", processId);
                ProcessStopped?.Invoke(this, new SocatProcessEventArgs(processInfo));
            }

            return stopped;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> StopAllSocatProcessesAsync(CancellationToken cancellationToken = default)
    {
        // Discover external socat processes first
        await DiscoverExternalSocatProcessesAsync(cancellationToken).ConfigureAwait(false);

        List<int> processIds = await _semaphore.ExecuteAsync(() =>
            Task.FromResult<List<int>>([.. _runningProcesses.Keys]), cancellationToken);

        int stoppedCount = 0;
        await Task.WhenAll(processIds.Select(async processId =>
        {
            if (await StopSocatByIdAsync(processId, cancellationToken).ConfigureAwait(false))
            {
                Interlocked.Increment(ref stoppedCount);
            }
        }));

        return stoppedCount;
    }

    #endregion

    #region Process Monitoring and Status

    /// <inheritdoc />
    public async Task<IEnumerable<SocatProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default)
    {
        // Discover external socat processes first (outside lock to avoid long hold)
        await DiscoverExternalSocatProcessesAsync(cancellationToken).ConfigureAwait(false);

        return await _semaphore.ExecuteAsync(async () =>
        {
            // Update process status before returning
            await UpdateProcessStatusesAsync(cancellationToken).ConfigureAwait(false);
            return (IEnumerable<SocatProcessInfo>)_runningProcesses.Values.ToList();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SocatProcessInfo?> GetProcessInfoAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processId <= 0)
        {
            throw new ArgumentException("Process ID must be greater than zero", nameof(processId));
        }

        return await _semaphore.ExecuteAsync(async () =>
        {
            if (_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
            {
                await UpdateProcessStatusAsync(processInfo).ConfigureAwait(false);
                return processInfo;
            }

            return null;
        }, cancellationToken);
    }

    /// <inheritdoc />
    /// <summary>
    /// Checks if a TCP port is currently in use by either a managed socat process or any external process.
    /// </summary>
    /// <param name="tcpPort">The TCP port to check (1-65535).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the port is in use, false otherwise.</returns>
    public async Task<bool> IsPortInUseAsync(int tcpPort, CancellationToken cancellationToken = default)
    {
        // Check managed processes first
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        bool managedUsage;
        try
        {
            managedUsage = _portManager.IsPortUsedByManagedProcess(tcpPort, _runningProcesses.Values);
        }
        finally
        {
            _semaphore.Release();
        }

        if (managedUsage)
        {
            return true;
        }

        // Delegate systemwide check to PortManager
        return await _portManager.IsPortInUseAsync(tcpPort, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SocatProcessInfo?> GetProcessByPortAsync(int tcpPort, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Delegate to PortManager for lookup
            return _portManager.GetProcessByPort(tcpPort, _runningProcesses.Values);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task StartProcessMonitoringAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processInfo, nameof(processInfo));

        await _semaphore.ExecuteAsync(async () =>
        {
            // Stop existing monitoring for this process
            if (_monitoringTimers.TryGetValue(processInfo.ProcessId, out Timer? existingMonitor))
            {
                existingMonitor.Dispose();
                _monitoringTimers.Remove(processInfo.ProcessId);
            }

            // Get status refresh interval from settings and clamp to a safe range
            int configuredInterval = _settingsService.GetSetting("socat.statusRefreshIntervalSeconds", 2);
            int statusRefreshIntervalSeconds = Math.Clamp(configuredInterval, 1, 3600);
            if (statusRefreshIntervalSeconds != configuredInterval)
            {
                _logger.LogWarning("Adjusted 'socat.statusRefreshIntervalSeconds' from {Configured} to safe value {Effective}", configuredInterval, statusRefreshIntervalSeconds);
            }

            var monitorInterval = TimeSpan.FromSeconds(statusRefreshIntervalSeconds);
            int isRunning = 0;

            // Start self-rescheduling monitoring with overlap protection (immediate first run)
            // Timer will reschedule itself after each execution to support dynamic interval updates
            Timer? monitor = null;
            monitor = new Timer(async _ =>
            {
                if (Interlocked.Exchange(ref isRunning, 1) == 1)
                {
                    // Skip overlapping executions
                    return;
                }

                try
                {
                    await UpdateProcessStatusAsync(processInfo).ConfigureAwait(false);

                    // Re-read the setting to get the latest value for dynamic updates
                    int updatedConfiguredInterval = _settingsService.GetSetting("socat.statusRefreshIntervalSeconds", 2);
                    int updatedInterval = Math.Clamp(updatedConfiguredInterval, 1, 3600);

                    // Only reschedule if this timer is still the active one for the process
                    if (_monitoringTimers.TryGetValue(processInfo.ProcessId, out Timer? activeTimer) && ReferenceEquals(activeTimer, monitor))
                    {
                        try
                        {
                            monitor.Change(TimeSpan.FromSeconds(updatedInterval), Timeout.InfiniteTimeSpan);
                        }
                        catch (ObjectDisposedException)
                        {
                            // Timer disposed during shutdown; ignore
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring socat process {ProcessId}", processInfo.ProcessId);

                    // Still reschedule even on error if timer is still active
                    if (_monitoringTimers.TryGetValue(processInfo.ProcessId, out Timer? activeTimer) && ReferenceEquals(activeTimer, monitor))
                    {
                        try
                        {
                            int updatedConfiguredInterval = _settingsService.GetSetting("socat.statusRefreshIntervalSeconds", 2);
                            int updatedInterval = Math.Clamp(updatedConfiguredInterval, 1, 3600);
                            monitor.Change(TimeSpan.FromSeconds(updatedInterval), Timeout.InfiniteTimeSpan);
                        }
                        catch (ObjectDisposedException)
                        {
                            // Timer disposed during error recovery; ignore
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref isRunning, 0);
                }
            }, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);

            _monitoringTimers[processInfo.ProcessId] = monitor;

            _logger.LogDebug("Started monitoring socat process {ProcessId}", processInfo.ProcessId);
            await Task.CompletedTask;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopProcessMonitoringAsync(int processId)
    {
        if (processId <= 0)
        {
            throw new ArgumentException("Process ID must be greater than zero", nameof(processId));
        }

        await _semaphore.ExecuteAsync(async () =>
        {
            if (_monitoringTimers.TryGetValue(processId, out Timer? monitor))
            {
                monitor.Dispose();
                _monitoringTimers.Remove(processId);
                _logger.LogDebug("Stopped monitoring socat process {ProcessId}", processId);
            }
            await Task.CompletedTask;
        });
    }

    #endregion

    #region Connection Management

    /// <inheritdoc />
    public async Task<IEnumerable<SocatConnectionInfo>> GetActiveConnectionsAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processId <= 0)
        {
            throw new ArgumentException("Process ID must be greater than zero", nameof(processId));
        }

        return await _semaphore.ExecuteAsync(async () =>
        {
            if (_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
            {
                // Update to get latest connection info
                await UpdateProcessStatusAsync(processInfo).ConfigureAwait(false);

                // Connection enumeration not yet implemented
                // Future: Could parse netstat/ss output filtered by port
                return Enumerable.Empty<SocatConnectionInfo>();
            }

            return Enumerable.Empty<SocatConnectionInfo>();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TestTcpConnectionAsync(string tcpHost, int tcpPort, int timeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        // Delegate to PortManager
        return await _portManager.TestConnectionAsync(tcpHost, tcpPort, timeoutMs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SocatTransferStats?> GetTransferStatsAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processId <= 0)
        {
            throw new ArgumentException("Process ID must be greater than zero", nameof(processId));
        }

        return await _semaphore.ExecuteAsync(async () =>
        {
            if (_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
            {
                // Update status to get latest stats
                await UpdateProcessStatusAsync(processInfo).ConfigureAwait(false);
                return processInfo.TransferStats;
            }
            return null;
        }, cancellationToken);
    }

    #endregion

    #region Serial Device Management

    /// <inheritdoc />
    public async Task<bool> PrepareSerialDeviceAsync(string serialDevice, SocatConfiguration configuration, CancellationToken cancellationToken = default)
    {
        // Delegate to ConfigurationService
        return await _configService.PrepareSerialDeviceAsync(serialDevice, configuration, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SerialDeviceValidationResult> ValidateSerialDeviceAsync(string serialDevice, CancellationToken cancellationToken = default)
    {
        // Delegate to ConfigurationService
        return await _configService.ValidateSerialDeviceAsync(serialDevice, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Starts a socat process with the specified parameters.
    /// </summary>
    /// <param name="command">The socat command to execute.</param>
    /// <param name="configuration">The socat configuration.</param>
    /// <param name="serialDevice">The serial device path.</param>
    /// <param name="profile">The profile used (if any).</param>
    /// <param name="protocolLogger">Optional logger for capturing protocol-level communication logs.</param>
    /// <param name="processLogger">Optional logger for capturing process stdout/stderr output.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Process information for the started socat process.</returns>
    private async Task<SocatProcessInfo> StartSocatProcessAsync(
        string command,
        SocatConfiguration configuration,
        string serialDevice,
        SocatProfile? profile,
        Microsoft.Extensions.Logging.ILogger? processLogger,
        CancellationToken cancellationToken)
    {
        // Get settings from application settings service
        bool captureProcessOutput = _settingsService.GetSetting("socat.captureProcessOutput", true);

        try
        {
            // Prefer invoking socat directly; reject unsupported commands to avoid injection
            string fileName = "socat";
            string arguments;
            string trimmed = command.Trim();
            if (trimmed.StartsWith("socat ", StringComparison.OrdinalIgnoreCase))
            {
                arguments = trimmed[5..].TrimStart();
            }
            else if (string.Equals(trimmed, "socat", StringComparison.OrdinalIgnoreCase))
            {
                arguments = string.Empty;
            }
            else
            {
                throw new ValidationException(
                    "Command",
                    "Only socat commands are allowed to be executed.");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = captureProcessOutput,
                RedirectStandardError = captureProcessOutput,
                CreateNoWindow = true
            };

            // Create process but DON'T use 'using' - we need to keep it alive!
            _logger.LogInformation("Executing: {Command}", command);
            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true // Enable events for proper lifecycle management
            };

            int processId = 0;

            StringBuilder? outputBuilder = null;
            StringBuilder? errorBuilder = null;
            if (captureProcessOutput)
            {
                outputBuilder = new StringBuilder();
                errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder!.AppendLine(e.Data);
                        _logger.LogTrace("Socat output: {Output}", e.Data);

                        // Log to task-specific process logger if provided
                        processLogger?.LogDebug("socat[{ProcessId}] {Output}", processId, e.Data);
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder!.AppendLine(e.Data);

                        // Clean up the log message by removing the timestamp if present
                        // Format: 2026/01/09 03:05:33 socat[113280] N ...
                        string cleanMessage = SocatLogTimestampRegex().Replace(e.Data, string.Empty);

                        // Only log to the task-specific logger, NOT the main application logger
                        processLogger?.LogDebug("socat[{ProcessId}] {Message}", processId, cleanMessage);
                    }
                };
            }

            // Set up process exit handler before starting
            process.Exited += (sender, args) => Task.Run(async () => await _semaphore.ExecuteAsync(async () =>
            {
                if (_runningProcesses.TryGetValue(process.Id, out SocatProcessInfo? processInfo))
                {
                    processInfo.IsRunning = false;
                    processInfo.Status = SocatProcessStatus.Stopped;
                    _logger.LogInformation("Socat process {ProcessId} exited with code {ExitCode}",
                        process.Id, process.ExitCode);

                    ProcessStopped?.Invoke(this, new SocatProcessEventArgs(processInfo));
                }

                // Clean up references - but DO NOT dispose process here
                // Disposal is handled by StopSocatByIdAsync or the Exited handler cleanup
                // Disposing here creates a race condition if StopSocatByIdAsync is running concurrently
                _runningProcesses.Remove(process.Id);
                _managedProcesses.Remove(process.Id);

                // NOTE: Process will be disposed either by:
                // 1. StopSocatByIdAsync when explicitly stopped
                // 2. Garbage collection after all references are removed
                await Task.CompletedTask;
            }));

            process.Start();
            processId = process.Id;

            if (captureProcessOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            await Task.Delay(150, cancellationToken).ConfigureAwait(false);

            if (process.HasExited)
            {
                int exitCode = process.ExitCode;
                string? stderr = captureProcessOutput ? errorBuilder?.ToString() : string.Empty;
                throw new ConnectionException(
                    $"{configuration.TcpHost}:{configuration.TcpPort}",
                    "Socat",
                    $"Socat process exited immediately with code {exitCode}. {stderr}");
            }

            var processInfo = new SocatProcessInfo
            {
                ProcessId = process.Id,
                TcpHost = string.IsNullOrEmpty(configuration.TcpHost) ? "127.0.0.1" : configuration.TcpHost,
                TcpPort = configuration.TcpPort,
                SerialDevice = serialDevice,
                Configuration = configuration.Clone(),
                Profile = profile?.Clone(),
                CommandLine = $"{fileName} {arguments}",
                StartTime = _timeProvider.GetLocalNow(),
                IsRunning = true,
                Status = SocatProcessStatus.Running,
                ActiveConnections = 0,
                TransferStats = new SocatTransferStats
                {
                    BytesSerialToTcp = 0,
                    BytesTcpToSerial = 0,
                    TotalConnections = 0,
                    ActiveConnections = 0,
                    LastUpdated = _timeProvider.GetLocalNow(),
                    Uptime = TimeSpan.Zero
                },
                LastUpdated = _timeProvider.GetLocalNow()
            };

            // Store the actual Process object to keep it alive
            _managedProcesses[process.Id] = process;

            return processInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start socat process with command: {Command}", command);
            throw new ConnectionException(
                $"{configuration.TcpHost}:{configuration.TcpPort}",
                "Socat",
                $"Failed to start socat process: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Executes a system command and returns the result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The command execution result.</returns>
    private async Task<(bool Success, int ExitCode, string StandardOutput, string StandardError)> ExecuteCommandAsync(
        string command,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{command}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(combinedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
                throw;
            }

            string output = outputBuilder.ToString().Trim();
            string error = errorBuilder.ToString().Trim();

            return (true, process.ExitCode, output, error);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Command execution timed out: {Command}", command);
            return (false, -1, string.Empty, "Command timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {Command}", command);
            return (false, -1, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a list of child process IDs for a given parent process.
    /// </summary>
    /// <param name="parentId">The parent process ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of child process IDs.</returns>
    private async Task<List<int>> GetChildProcessesAsync(int parentId, CancellationToken cancellationToken)
    {
        if (parentId <= 0 || !(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
        {
            return [];
        }

        try
        {
            // Use pgrep to find child processes (portable enough on modern Linux/macOS)
            // -P matches Parent Process ID
            (bool success, int _, string stdout, string _) = await ExecuteCommandAsync($"pgrep -P {parentId}", 1000, cancellationToken).ConfigureAwait(false);

            if (!success || string.IsNullOrWhiteSpace(stdout))
            {
                // Fallback to ps if pgrep fails or returns distinct exit code for "no matches"
                (success, _, stdout, _) = await ExecuteCommandAsync($"ps -o pid --ppid {parentId} --no-headers", 1000, cancellationToken).ConfigureAwait(false);

                if (!success || string.IsNullOrWhiteSpace(stdout))
                {
                    return [];
                }
            }

            List<int> childPids = [];
            string[] lines = stdout.Split(NewLines, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (int.TryParse(line.Trim(), out int pid))
                {
                    childPids.Add(pid);
                }
            }

            return childPids;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get child processes for PID {ParentId}", parentId);
            return [];
        }
    }

    /// <summary>
    /// Waits for a process to exit with a timeout.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the process exited within the timeout, false otherwise.</returns>
    private static async Task<bool> WaitForProcessExitAsync(Process process, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await process.WaitForExitAsync(combinedCts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Updates the status of all running processes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task UpdateProcessStatusesAsync(CancellationToken cancellationToken = default)
    {
        var processIds = _runningProcesses.Keys.ToList();

        foreach (int processId in processIds)
        {
            if (_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
            {
                await UpdateProcessStatusAsync(processInfo).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Discovers external socat processes not started by this service and merges them into tracking.
    /// </summary>
    private async Task DiscoverExternalSocatProcessesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
            {
                return; // discovery implemented for Unix-like systems only
            }

            // Delegate to PortManager for system-wide process discovery
            List<int> socatPids = await _portManager.DiscoverSocatProcessIdsAsync(cancellationToken).ConfigureAwait(false);

            if (socatPids.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Discovered {Count} socat processes: {Pids}", socatPids.Count, string.Join(", ", socatPids));

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Add discovered processes that we're not already tracking
                foreach (int pid in socatPids)
                {
                    if (!_runningProcesses.ContainsKey(pid))
                    {
                        _logger.LogInformation("Discovered external socat process {ProcessId}", pid);
                        // Create basic process info for external process
                        var processInfo = new SocatProcessInfo
                        {
                            ProcessId = pid,
                            IsRunning = true,
                            Status = SocatProcessStatus.Running,
                            StartTime = _timeProvider.GetUtcNow() // We don't know actual start time
                        };
                        _runningProcesses[pid] = processInfo;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover external socat processes");
        }
    }

    /// <summary>
    /// Updates the status of a specific process.
    /// </summary>
    /// <param name="processInfo">The process information to update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private Task UpdateProcessStatusAsync(SocatProcessInfo processInfo)
    {
        try
        {
            // Check if process is still running
            try
            {
                var process = Process.GetProcessById(processInfo.ProcessId);
                processInfo.IsRunning = !process.HasExited;

                if (process.HasExited)
                {
                    processInfo.Status = SocatProcessStatus.Stopped;
                    processInfo.IsRunning = false;

                    // Remove from our tracking
                    _runningProcesses.Remove(processInfo.ProcessId);

                    // Raise stopped event
                    ProcessStopped?.Invoke(this, new SocatProcessEventArgs(processInfo));
                }
                else
                {
                    processInfo.Status = SocatProcessStatus.Running;

                    // Update uptime
                    if (processInfo.TransferStats != null)
                    {
                        processInfo.TransferStats.Uptime = DateTime.UtcNow - processInfo.StartTime;
                        processInfo.TransferStats.LastUpdated = DateTime.UtcNow;
                    }
                }
            }
            catch (ArgumentException)
            {
                // Process no longer exists
                processInfo.IsRunning = false;
                processInfo.Status = SocatProcessStatus.Stopped;
                _runningProcesses.Remove(processInfo.ProcessId);

                // Raise stopped event
                ProcessStopped?.Invoke(this, new SocatProcessEventArgs(processInfo));
            }

            processInfo.LastUpdated = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for socat process {ProcessId}", processInfo.ProcessId);
            processInfo.Status = SocatProcessStatus.Error;
            processInfo.LastError = ex.Message;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Parses a netstat connection line into connection information.
    /// </summary>
    /// <param name="netstatLine">The netstat output line to parse.</param>
    /// <returns>Connection information if parsed successfully, null otherwise.</returns>
    private SocatConnectionInfo? ParseNetstatConnection(string netstatLine)
    {
        try
        {
            // Parse netstat line format: Proto Recv-Q Send-Q Local-Address Foreign-Address State
            string[] parts = netstatLine.Split(NetstatSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 6 || !parts[0].StartsWith("tcp", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string localAddress = parts[3];
            string remoteAddress = parts[4];
            string state = parts[5];

            // Only include established connections
            if (!string.Equals(state, "ESTABLISHED", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Parse local address
            string[] localParts = localAddress.Split(':');
            if (localParts.Length < 2)
            {
                return null;
            }

            string localHost = string.Join(":", localParts.Take(localParts.Length - 1));
            if (!int.TryParse(localParts.Last(), out int localPort))
            {
                return null;
            }

            // Parse remote address
            string[] remoteParts = remoteAddress.Split(':');
            if (remoteParts.Length < 2)
            {
                return null;
            }

            string remoteHost = string.Join(":", remoteParts.Take(remoteParts.Length - 1));
            if (!int.TryParse(remoteParts.Last(), out int remotePort))
            {
                return null;
            }

            return new SocatConnectionInfo
            {
                LocalAddress = localHost,
                LocalPort = localPort,
                RemoteAddress = remoteHost,
                RemotePort = remotePort,
                EstablishedTime = DateTime.UtcNow, // Approximate
                BytesSent = 0, // Not available from netstat
                BytesReceived = 0 // Not available from netstat
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse netstat line: {Line}", netstatLine);
            return null;
        }
    }

    [GeneratedRegex("TCP-LISTEN:(\\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TcpListenRegex();

    [GeneratedRegex("(/dev/[^\\s,]+)")]
    private static partial Regex SerialDeviceRegex();

    [GeneratedRegex(@"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} ")]
    private static partial Regex SocatLogTimestampRegex();

    private static readonly char[] NetstatSeparators = [' ', '\t'];
    private static readonly char[] NewLines = ['\n', '\r'];

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Releases all resources used by the SocatService.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the SocatService and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Stop all running processes
            try
            {
                Task<int> stopTask = StopAllSocatProcessesAsync(CancellationToken.None);
                stopTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping socat processes during disposal");
            }

            // Dispose all stored processes
            foreach (Process process in _managedProcesses.Values)
            {
                try
                {
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing process during cleanup");
                }
            }
            _managedProcesses.Clear();

            // Dispose all monitors
            foreach (Timer monitor in _monitoringTimers.Values)
            {
                monitor.Dispose();
            }
            _monitoringTimers.Clear();

            _semaphore?.Dispose();
            _disposed = true;

            _logger.LogDebug("SocatService disposed");
        }
    }

    #endregion


}
