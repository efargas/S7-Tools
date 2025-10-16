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
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Models;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Service for socat (Serial-to-TCP Proxy) operations including process management, command generation, and status monitoring.
/// This service provides comprehensive socat management capabilities for serial-to-TCP bridge functionality.
/// </summary>
public class SocatService : ISocatService, IDisposable
{
    #pragma warning disable CS0067 // Events may be declared for external subscriptions; not used in this assembly
    private readonly ILogger<SocatService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly ISerialPortService _serialPortService;
    private readonly Dictionary<int, SocatProcessInfo> _runningProcesses = new();
    private readonly Dictionary<int, Process> _activeProcesses = new(); // Keep actual Process objects alive
    private readonly Dictionary<int, Timer> _processMonitors = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the SocatService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="settingsService">The settings service for accessing application settings.</param>
    /// <param name="serialPortService">The serial port service for device validation and configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public SocatService(
        ILogger<SocatService> logger,
        ISettingsService settingsService,
        ISerialPortService serialPortService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));

        _logger.LogDebug("SocatService initialized");
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
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        return configuration.GenerateCommand(serialDevice);
    }

    /// <inheritdoc />
    public string GenerateSocatCommandForProfile(SocatProfile profile, string serialDevice)
    {
        ArgumentNullException.ThrowIfNull(profile, nameof(profile));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        return profile.Configuration.GenerateCommand(serialDevice);
    }

    /// <inheritdoc />
    public SocatCommandValidationResult ValidateSocatCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var result = new SocatCommandValidationResult
        {
            ValidatedCommand = command.Trim()
        };

        try
        {
            // Basic command structure validation
            if (!command.TrimStart().StartsWith("socat", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Command must start with 'socat'");
                result.IsValid = false;
                return result;
            }

            // Check for required TCP-LISTEN part
            Match tcpListenMatch = Regex.Match(command, @"TCP-LISTEN:(\d+)", RegexOptions.IgnoreCase);
            if (!tcpListenMatch.Success)
            {
                result.Errors.Add("Command must contain TCP-LISTEN:port specification");
                result.IsValid = false;
            }
            else
            {
                if (int.TryParse(tcpListenMatch.Groups[1].Value, out int port))
                {
                    if (port < 1 || port > 65535)
                    {
                        result.Errors.Add($"TCP port {port} is not in valid range (1-65535)");
                        result.IsValid = false;
                    }
                    else
                    {
                        result.DetectedTcpPort = port;
                    }
                }
            }

            // Check for serial device specification
            Match deviceMatch = Regex.Match(command, @"(/dev/[^\s,]+)");
            if (!deviceMatch.Success)
            {
                result.Errors.Add("Command must contain a serial device path (/dev/...)");
                result.IsValid = false;
            }
            else
            {
                result.DetectedSerialDevice = deviceMatch.Groups[1].Value;
            }

            // Check for potentially dangerous flags
            if (command.Contains("-r", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add("Command contains raw mode flag (-r) which may affect performance");
            }

            // Check if command requires root privileges (ports < 1024)
            if (result.DetectedTcpPort.HasValue && result.DetectedTcpPort.Value < 1024)
            {
                result.RequiresRoot = true;
                result.Warnings.Add($"TCP port {result.DetectedTcpPort.Value} may require root privileges");
            }

            // If no errors, command is valid
            if (!result.Errors.Any())
            {
                result.IsValid = true;
            }

            _logger.LogDebug("Validated socat command: {IsValid}, Port: {Port}, Device: {Device}",
                result.IsValid, result.DetectedTcpPort, result.DetectedSerialDevice);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating socat command: {Command}", command);
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
            return result;
        }
    }

    #endregion

    #region Process Management

    /// <inheritdoc />
    public async Task<SocatProcessInfo> StartSocatAsync(SocatConfiguration configuration, string serialDevice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        SocatSettings settings = _settingsService.Settings.Socat;

        // Check concurrent instances limit
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_runningProcesses.Count >= settings.MaxConcurrentInstances)
            {
                throw new InvalidOperationException($"Maximum number of socat instances ({settings.MaxConcurrentInstances}) already running");
            }

            // Validate serial device exists before starting socat
            if (!File.Exists(serialDevice))
            {
                _logger.LogError("Serial device {Device} does not exist. Please verify the device path and ensure it is connected.", serialDevice);
                throw new InvalidOperationException($"Serial device '{serialDevice}' does not exist. Please check the device connection and try scanning for devices again.");
            }

            // Check if TCP port is already in use (internal check - semaphore already held)
            if (await IsPortInUseInternalAsync(configuration.TcpPort, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"TCP port {configuration.TcpPort} is already in use");
            }

            // Prepare serial device if configured
            if (settings.AutoConfigureSerialDevice && configuration.AutoConfigureSerial)
            {
                _logger.LogDebug("Preparing serial device {Device} for socat", serialDevice);
                bool prepared = await PrepareSerialDeviceAsync(serialDevice, configuration, cancellationToken).ConfigureAwait(false);
                if (!prepared)
                {
                    throw new InvalidOperationException($"Failed to prepare serial device {serialDevice}");
                }
            }

            // Generate and validate command
            string command = GenerateSocatCommand(configuration, serialDevice);
            SocatCommandValidationResult validation = ValidateSocatCommand(command);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Invalid socat command: {string.Join(", ", validation.Errors)}");
            }

            // Start socat process
            SocatProcessInfo processInfo = await StartSocatProcessAsync(command, configuration, serialDevice, null, cancellationToken).ConfigureAwait(false);

            _runningProcesses[processInfo.ProcessId] = processInfo;

            _logger.LogInformation("Started socat process {ProcessId} for device {Device} on TCP port {Port}",
                processInfo.ProcessId, serialDevice, configuration.TcpPort);

            // Raise event
            ProcessStarted?.Invoke(this, new SocatProcessEventArgs(processInfo));

            return processInfo;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SocatProcessInfo> StartSocatWithProfileAsync(SocatProfile profile, string serialDevice, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀🚀🚀 ENTERED StartSocatWithProfileAsync - Profile: {ProfileName}, Device: {Device}",
            profile?.Name ?? "NULL", serialDevice ?? "NULL");

        ArgumentNullException.ThrowIfNull(profile, nameof(profile));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            _logger.LogError("❌ Serial device is null or empty");
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        _logger.LogInformation("📋 Getting settings...");
        SocatSettings settings = _settingsService.Settings.Socat;
        _logger.LogInformation("📋 Settings obtained - MaxConcurrentInstances: {Max}", settings.MaxConcurrentInstances);

        // Check concurrent instances limit
        _logger.LogInformation("🔒 Waiting for semaphore...");
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("🔓 Semaphore acquired");
        try
        {
            _logger.LogInformation("📊 Checking concurrent instances: Current={Current}, Max={Max}",
                _runningProcesses.Count, settings.MaxConcurrentInstances);
            if (_runningProcesses.Count >= settings.MaxConcurrentInstances)
            {
                _logger.LogError("❌ Too many concurrent instances");
                throw new InvalidOperationException($"Maximum number of socat instances ({settings.MaxConcurrentInstances}) already running");
            }

            // Validate serial device exists before starting socat
            _logger.LogInformation("🔍 Checking if serial device exists: {Device}", serialDevice);
            if (!File.Exists(serialDevice))
            {
                _logger.LogError("❌ Serial device {Device} does not exist. Please verify the device path and ensure it is connected.", serialDevice);
                throw new InvalidOperationException($"Serial device '{serialDevice}' does not exist. Please check the device connection and try scanning for devices again.");
            }
            _logger.LogInformation("✅ Serial device exists");

            // Check if TCP port is already in use (internal check - semaphore already held)
            _logger.LogInformation("🌐 Checking if TCP port {Port} is available...", profile.Configuration.TcpPort);
            if (await IsPortInUseInternalAsync(profile.Configuration.TcpPort, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogError("❌ TCP port {Port} is already in use", profile.Configuration.TcpPort);
                throw new InvalidOperationException($"TCP port {profile.Configuration.TcpPort} is already in use");
            }
            _logger.LogInformation("✅ TCP port {Port} is available", profile.Configuration.TcpPort);

            // Prepare serial device if configured
            if (settings.AutoConfigureSerialDevice && profile.Configuration.AutoConfigureSerial)
            {
                _logger.LogInformation("🔧 Preparing serial device {Device} for socat profile '{Profile}'", serialDevice, profile.Name);
                bool prepared = await PrepareSerialDeviceAsync(serialDevice, profile.Configuration, cancellationToken).ConfigureAwait(false);
                if (!prepared)
                {
                    _logger.LogError("❌ Failed to prepare serial device {Device}", serialDevice);
                    throw new InvalidOperationException($"Failed to prepare serial device {serialDevice}");
                }
                _logger.LogInformation("✅ Serial device prepared");
            }
            else
            {
                _logger.LogInformation("⏭️ Skipping serial device preparation (AutoConfigure={Auto}, ProfileAuto={ProfileAuto})",
                    settings.AutoConfigureSerialDevice, profile.Configuration.AutoConfigureSerial);
            }

            // Generate and validate command
            _logger.LogInformation("📝 Generating socat command...");
            string command = GenerateSocatCommandForProfile(profile, serialDevice);
            _logger.LogInformation("📝 Generated command: {Command}", command);

            _logger.LogInformation("✅ Validating command...");
            SocatCommandValidationResult validation = ValidateSocatCommand(command);
            if (!validation.IsValid)
            {
                _logger.LogError("❌ Invalid socat command: {Errors}", string.Join(", ", validation.Errors));
                throw new InvalidOperationException($"Invalid socat command: {string.Join(", ", validation.Errors)}");
            }
            _logger.LogInformation("✅ Command validation passed");

            // Start socat process
            _logger.LogInformation("🚀 Calling StartSocatProcessAsync...");
            SocatProcessInfo processInfo = await StartSocatProcessAsync(command, profile.Configuration, serialDevice, profile, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("🎉 StartSocatProcessAsync SUCCESS - ProcessId: {ProcessId}", processInfo.ProcessId);

            _logger.LogInformation("📝 Adding process to _runningProcesses...");
            _runningProcesses[processInfo.ProcessId] = processInfo;
            _logger.LogInformation("📝 Process added. Total running processes: {Count}", _runningProcesses.Count);

            _logger.LogInformation("🎉 Started socat process {ProcessId} with profile '{Profile}' for device {Device} on TCP port {Port}",
                processInfo.ProcessId, profile.Name, serialDevice, profile.Configuration.TcpPort);

            // Raise event
            _logger.LogInformation("📢 Raising ProcessStarted event...");
            ProcessStarted?.Invoke(this, new SocatProcessEventArgs(processInfo));
            _logger.LogInformation("📢 ProcessStarted event raised");

            _logger.LogInformation("🏁 StartSocatWithProfileAsync SUCCESSFUL EXIT");
            return processInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 EXCEPTION in StartSocatWithProfileAsync: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _logger.LogInformation("🔓 Releasing semaphore...");
            _semaphore.Release();
            _logger.LogInformation("🔓 Semaphore released");
        }
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

            SocatSettings settings = _settingsService.Settings.Socat;
            int timeoutMs = settings.ProcessShutdownTimeoutSeconds * 1000;

            try
            {
                Process? process = null;

                // Try to get the process from our stored references first
                if (_activeProcesses.TryGetValue(processId, out process))
                {
                    // Use our stored process reference
                }
                else
                {
                    // Fallback to system process lookup if not in our dictionary
                    try
                    {
                        process = Process.GetProcessById(processId);
                    }
                    catch (ArgumentException)
                    {
                        // Process doesn't exist anymore
                        processInfo.Status = SocatProcessStatus.Stopped;
                        processInfo.IsRunning = false;
                        _logger.LogDebug("Socat process {ProcessId} was already stopped", processId);
                        return true;
                    }
                }

                bool exited = false;

                // Try SIGTERM on Unix-like systems
                if (!process.HasExited)
                {
                    try
                    {
                        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                        {
                            // Send SIGTERM
                            (bool _, int _, string _, string _) = await ExecuteCommandAsync($"kill -TERM {processId}", timeoutMs / 2, cancellationToken).ConfigureAwait(false);
                            exited = await WaitForProcessExitAsync(process, timeoutMs / 2, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // On Windows, try CloseMainWindow if possible (unlikely headless)
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                process.CloseMainWindow();
                                exited = await WaitForProcessExitAsync(process, timeoutMs / 2, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore and escalate below
                    }
                }

                if (!exited && !process.HasExited)
                {
                    _logger.LogWarning("Socat process {ProcessId} did not exit after SIGTERM, forcing termination", processId);
                    process.Kill();
                    await WaitForProcessExitAsync(process, timeoutMs / 2, cancellationToken).ConfigureAwait(false);
                }

                processInfo.Status = SocatProcessStatus.Stopped;
                processInfo.IsRunning = false;

                _logger.LogInformation("Stopped socat process {ProcessId}", processId);

                ProcessStopped?.Invoke(this, new SocatProcessEventArgs(processInfo));
                return true;
            }
            catch (ArgumentException)
            {
                processInfo.Status = SocatProcessStatus.Stopped;
                processInfo.IsRunning = false;
                _logger.LogDebug("Socat process {ProcessId} was already stopped", processId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop socat process {ProcessId}", processId);
                return false;
            }
            finally
            {
                _runningProcesses.Remove(processId);
                // Clean up the stored process reference and dispose it
                if (_activeProcesses.TryGetValue(processId, out Process? storedProcess))
                {
                    _activeProcesses.Remove(processId);
                    try
                    {
                        storedProcess.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing process {ProcessId}", processId);
                    }
                }
                if (_processMonitors.TryGetValue(processId, out Timer? monitor))
                {
                    monitor.Dispose();
                    _processMonitors.Remove(processId);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> StopAllSocatProcessesAsync(CancellationToken cancellationToken = default)
    {
        var processIds = new List<int>();
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            processIds = _runningProcesses.Keys.ToList();
        }
        finally
        {
            _semaphore.Release();
        }

        int stoppedCount = 0;
        IEnumerable<Task> tasks = processIds.Select(async processId =>
        {
            if (await StopSocatByIdAsync(processId, cancellationToken).ConfigureAwait(false))
            {
                Interlocked.Increment(ref stoppedCount);
            }
        });
        await Task.WhenAll(tasks).ConfigureAwait(false);

        _logger.LogInformation("Stopped {StoppedCount} of {TotalCount} socat processes", stoppedCount, processIds.Count);
        return stoppedCount;
    }

    #endregion

    #region Process Monitoring and Status

    /// <inheritdoc />
    public async Task<IEnumerable<SocatProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 GetRunningProcessesAsync ENTRY");

        _logger.LogInformation("🔒 Waiting for semaphore...");
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("🔓 Semaphore acquired");

        try
        {
            _logger.LogInformation("📊 Current _runningProcesses count before update: {Count}", _runningProcesses.Count);

            // Update process status before returning
            _logger.LogInformation("🔄 Calling UpdateProcessStatusesAsync...");
            await UpdateProcessStatusesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("✅ UpdateProcessStatusesAsync completed");

            _logger.LogInformation("📊 Final _runningProcesses count: {Count}", _runningProcesses.Count);
            var result = _runningProcesses.Values.ToList();
            _logger.LogInformation("📊 Returning {Count} processes", result.Count);

            return result;
        }
        finally
        {
            _logger.LogInformation("🔓 Releasing semaphore...");
            _semaphore.Release();
            _logger.LogInformation("🏁 GetRunningProcessesAsync EXIT");
        }
    }

    /// <inheritdoc />
    public async Task<SocatProcessInfo?> GetProcessInfoAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processId <= 0)
        {
            throw new ArgumentException("Process ID must be greater than zero", nameof(processId));
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
            {
                await UpdateProcessStatusAsync(processInfo, cancellationToken).ConfigureAwait(false);
                return processInfo;
            }

            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Internal port check method that doesn't acquire semaphore (assumes already held).
    /// Used when semaphore is already acquired to avoid deadlock.
    /// </summary>
    private async Task<bool> IsPortInUseInternalAsync(int tcpPort, CancellationToken cancellationToken = default)
    {
        if (tcpPort < 1 || tcpPort > 65535)
        {
            throw new ArgumentException("TCP port must be between 1 and 65535", nameof(tcpPort));
        }

        try
        {
            // Check our managed processes (semaphore already held)
            SocatProcessInfo? managedProcess = _runningProcesses.Values.FirstOrDefault(p => p.TcpPort == tcpPort && p.IsRunning);
            if (managedProcess != null)
            {
                _logger.LogDebug("Port {Port} is in use by managed socat process {ProcessId}", tcpPort, managedProcess.ProcessId);
                return true;
            }

            // Then, attempt to bind to the port to detect external usage
            try
            {
                using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, tcpPort);
                listener.Start();
                listener.Stop();
                _logger.LogDebug("Port {Port} is available (bind test successful)", tcpPort);
                return false; // successfully bound -> port not in use
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogDebug("Port {Port} is in use (bind failed: {Error})", tcpPort, ex.Message);
                return true; // bind failed -> port in use or insufficient privileges
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if TCP port {Port} is in use (internal)", tcpPort);
            // Be conservative: assume port is in use on error to avoid collisions
            return true;
        }
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
        if (tcpPort < 1 || tcpPort > 65535)
        {
            throw new ArgumentException("TCP port must be between 1 and 65535", nameof(tcpPort));
        }

        try
        {
            // First, check our managed processes under lock
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                SocatProcessInfo? managedProcess = _runningProcesses.Values.FirstOrDefault(p => p.TcpPort == tcpPort && p.IsRunning);
                if (managedProcess != null)
                {
                    return true;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            // Then, attempt to bind to the port to detect external usage
            try
            {
                using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, tcpPort);
                listener.Start();
                listener.Stop();
                return false; // successfully bound -> port not in use
            }
            catch (System.Net.Sockets.SocketException)
            {
                return true; // bind failed -> port in use or insufficient privileges
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if TCP port {Port} is in use", tcpPort);
            // Be conservative: assume port is in use on error to avoid collisions
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<SocatProcessInfo?> GetProcessByPortAsync(int tcpPort, CancellationToken cancellationToken = default)
    {
        if (tcpPort < 1 || tcpPort > 65535)
        {
            throw new ArgumentException("TCP port must be between 1 and 65535", nameof(tcpPort));
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await UpdateProcessStatusesAsync(cancellationToken).ConfigureAwait(false);

            return _runningProcesses.Values.FirstOrDefault(p => p.TcpPort == tcpPort && p.IsRunning);
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

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Stop existing monitoring for this process
            if (_processMonitors.TryGetValue(processInfo.ProcessId, out Timer? existingMonitor))
            {
                existingMonitor.Dispose();
                _processMonitors.Remove(processInfo.ProcessId);
            }

            // Start new monitoring
            SocatSettings settings = _settingsService.Settings.Socat;
            var monitorInterval = TimeSpan.FromSeconds(settings.StatusRefreshIntervalSeconds);

            var monitor = new Timer(async _ =>
            {
                try
                {
                    await UpdateProcessStatusAsync(processInfo, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring socat process {ProcessId}", processInfo.ProcessId);
                }
            }, null, monitorInterval, monitorInterval);

            _processMonitors[processInfo.ProcessId] = monitor;

            _logger.LogDebug("Started monitoring socat process {ProcessId}", processInfo.ProcessId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopProcessMonitoringAsync(int processId)
    {
        if (processId <= 0)
        {
            throw new ArgumentException("Process ID must be greater than zero", nameof(processId));
        }

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_processMonitors.TryGetValue(processId, out Timer? monitor))
            {
                monitor.Dispose();
                _processMonitors.Remove(processId);
                _logger.LogDebug("Stopped monitoring socat process {ProcessId}", processId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
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

        try
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
                {
                    return Enumerable.Empty<SocatConnectionInfo>();
                }

                // Use netstat to get connections for this TCP port
                string command = $"netstat -n | grep ':{processInfo.TcpPort} '";
                (bool success, int exitCode, string? output, string _) = await ExecuteCommandAsync(command, 5000, cancellationToken).ConfigureAwait(false);

                if (!success || string.IsNullOrWhiteSpace(output))
                {
                    return Enumerable.Empty<SocatConnectionInfo>();
                }

                var connections = new List<SocatConnectionInfo>();
                string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    SocatConnectionInfo? connection = ParseNetstatConnection(line);
                    if (connection != null)
                    {
                        connections.Add(connection);
                    }
                }

                return connections;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active connections for process {ProcessId}", processId);
            return Enumerable.Empty<SocatConnectionInfo>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestTcpConnectionAsync(string tcpHost, int tcpPort, int timeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tcpHost))
        {
            throw new ArgumentException("TCP host cannot be null or empty", nameof(tcpHost));
        }

        if (tcpPort < 1 || tcpPort > 65535)
        {
            throw new ArgumentException("TCP port must be between 1 and 65535", nameof(tcpPort));
        }

        try
        {
            // Use nc (netcat) to test the connection
            string command = $"timeout {timeoutMs / 1000} nc -z {tcpHost} {tcpPort}";
            (bool success, int exitCode, string _, string _) = await ExecuteCommandAsync(command, timeoutMs + 1000, cancellationToken).ConfigureAwait(false);

            bool connectionSuccessful = success && exitCode == 0;
            _logger.LogDebug("TCP connection test to {Host}:{Port}: {Result}", tcpHost, tcpPort, connectionSuccessful ? "Success" : "Failed");

            return connectionSuccessful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test TCP connection to {Host}:{Port}", tcpHost, tcpPort);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<SocatTransferStats?> GetTransferStatsAsync(int processId, CancellationToken cancellationToken = default)
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
                return null;
            }

            // Update transfer statistics from process monitoring
            await UpdateProcessStatusAsync(processInfo, cancellationToken).ConfigureAwait(false);

            return processInfo.TransferStats;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Serial Device Management

    /// <inheritdoc />
    public async Task<bool> PrepareSerialDeviceAsync(string serialDevice, SocatConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        try
        {
            // Validate device accessibility first
            SerialDeviceValidationResult validation = await ValidateSerialDeviceAsync(serialDevice, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                _logger.LogError("Serial device validation failed for {Device}: {Errors}",
                    serialDevice, string.Join(", ", validation.Errors));
                return false;
            }

            // For socat, we typically want the device in raw mode
            // We can create a default serial port configuration for socat use
            var serialConfig = new SerialPortConfiguration
            {
                BaudRate = 9600, // Default, will be overridden by socat raw mode
                CharacterSize = 8,
                Parity = ParityMode.None,
                StopBits = StopBits.One,
                DisableHardwareFlowControl = true,
                RawMode = true
            };

            // Apply the configuration using the serial port service
            bool applied = await _serialPortService.ApplyConfigurationAsync(serialDevice, serialConfig, cancellationToken).ConfigureAwait(false);

            if (applied)
            {
                _logger.LogDebug("Successfully prepared serial device {Device} for socat", serialDevice);
            }
            else
            {
                _logger.LogWarning("Failed to prepare serial device {Device} for socat", serialDevice);
            }

            return applied;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing serial device {Device} for socat", serialDevice);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<SerialDeviceValidationResult> ValidateSerialDeviceAsync(string serialDevice, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        var result = new SerialDeviceValidationResult();

        try
        {
            // Check if device exists
            result.Exists = File.Exists(serialDevice);
            if (!result.Exists)
            {
                result.Errors.Add($"Serial device {serialDevice} does not exist");
                result.IsValid = false;
                return result;
            }

            // Check device accessibility using serial port service
            bool accessible = await _serialPortService.IsPortAccessibleAsync(serialDevice, 1000, cancellationToken).ConfigureAwait(false);
            result.IsAccessible = accessible;

            if (!accessible)
            {
                result.Errors.Add($"Serial device {serialDevice} is not accessible");
                result.IsValid = false;
            }

            // Get additional device information
            SerialPortInfo? portInfo = await _serialPortService.GetPortInfoAsync(serialDevice, cancellationToken).ConfigureAwait(false);
            if (portInfo != null)
            {
                result.DeviceInfo = $"Type: {portInfo.PortType}, Description: {portInfo.Description}";
                result.IsInUse = portInfo.IsInUse;

                if (portInfo.IsInUse)
                {
                    result.Warnings.Add($"Serial device {serialDevice} appears to be in use by another process");
                }
            }
            else
            {
                result.DeviceInfo = "Unable to retrieve device information";
            }

            // If we reach here with no errors, the device is valid
            if (!result.Errors.Any())
            {
                result.IsValid = true;
            }

            _logger.LogDebug("Validated serial device {Device}: {IsValid}", serialDevice, result.IsValid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating serial device {Device}", serialDevice);
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
            return result;
        }
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
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Process information for the started socat process.</returns>
    private async Task<SocatProcessInfo> StartSocatProcessAsync(
        string command,
        SocatConfiguration configuration,
        string serialDevice,
        SocatProfile? profile,
        CancellationToken cancellationToken)
    {
        SocatSettings settings = _settingsService.Settings.Socat;

        try
        {
            // Prefer invoking socat directly; reject unsupported commands to avoid injection
            string fileName = "socat";
            string arguments;
            string trimmed = command.Trim();
            if (trimmed.StartsWith("socat ", StringComparison.OrdinalIgnoreCase))
            {
                arguments = trimmed.Substring(5).TrimStart();
            }
            else if (string.Equals(trimmed, "socat", StringComparison.OrdinalIgnoreCase))
            {
                arguments = string.Empty;
            }
            else
            {
                throw new InvalidOperationException("Only socat commands are allowed to be executed.");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = settings.CaptureProcessOutput,
                RedirectStandardError = settings.CaptureProcessOutput,
                CreateNoWindow = true
            };

            // Create process but DON'T use 'using' - we need to keep it alive!
            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true // Enable events for proper lifecycle management
            };

            StringBuilder? outputBuilder = null;
            StringBuilder? errorBuilder = null;
            if (settings.CaptureProcessOutput)
            {
                outputBuilder = new StringBuilder();
                errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder!.AppendLine(e.Data);
                        _logger.LogTrace("Socat output: {Output}", e.Data);
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder!.AppendLine(e.Data);
                        _logger.LogWarning("Socat error: {Error}", e.Data);
                    }
                };
            }

            // Set up process exit handler before starting
            process.Exited += (sender, args) =>
            {
                Task.Run(async () =>
                {
                    await _semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (_runningProcesses.TryGetValue(process.Id, out SocatProcessInfo? processInfo))
                        {
                            processInfo.IsRunning = false;
                            processInfo.Status = SocatProcessStatus.Stopped;
                            _logger.LogInformation("Socat process {ProcessId} exited with code {ExitCode}",
                                process.Id, process.ExitCode);

                            ProcessStopped?.Invoke(this, new SocatProcessEventArgs(processInfo));
                        }

                        // Clean up references
                        _runningProcesses.Remove(process.Id);
                        _activeProcesses.Remove(process.Id);

                        // Dispose the process now that it's finished
                        process.Dispose();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            };

            process.Start();

            if (settings.CaptureProcessOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            await Task.Delay(150, cancellationToken).ConfigureAwait(false);

            if (process.HasExited)
            {
                int exitCode = process.ExitCode;
                string? stderr = settings.CaptureProcessOutput ? errorBuilder?.ToString() : string.Empty;
                throw new InvalidOperationException($"Socat process exited immediately with code {exitCode}. {stderr}");
            }

            var processInfo = new SocatProcessInfo
            {
                ProcessId = process.Id,
                TcpPort = configuration.TcpPort,
                TcpHost = configuration.TcpHost,
                SerialDevice = serialDevice,
                Configuration = configuration.Clone(),
                Profile = profile?.Clone(),
                CommandLine = $"{fileName} {arguments}",
                StartTime = DateTime.UtcNow,
                IsRunning = true,
                Status = SocatProcessStatus.Running,
                ActiveConnections = 0,
                TransferStats = new SocatTransferStats
                {
                    BytesSerialToTcp = 0,
                    BytesTcpToSerial = 0,
                    TotalConnections = 0,
                    ActiveConnections = 0,
                    LastUpdated = DateTime.UtcNow,
                    Uptime = TimeSpan.Zero
                },
                LastUpdated = DateTime.UtcNow
            };

            // Store the actual Process object to keep it alive
            _activeProcesses[process.Id] = process;

            return processInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start socat process with command: {Command}", command);
            throw new InvalidOperationException($"Failed to start socat process: {ex.Message}", ex);
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
    /// Waits for a process to exit with a timeout.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the process exited within the timeout, false otherwise.</returns>
    private async Task<bool> WaitForProcessExitAsync(Process process, int timeoutMs, CancellationToken cancellationToken)
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
    private async Task UpdateProcessStatusesAsync(CancellationToken cancellationToken)
    {
        var processIds = _runningProcesses.Keys.ToList();

        foreach (int processId in processIds)
        {
            if (_runningProcesses.TryGetValue(processId, out SocatProcessInfo? processInfo))
            {
                await UpdateProcessStatusAsync(processInfo, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Updates the status of a specific process.
    /// </summary>
    /// <param name="processInfo">The process information to update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task UpdateProcessStatusAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken)
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
            string[] parts = netstatLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

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
            foreach (Process process in _activeProcesses.Values)
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
            _activeProcesses.Clear();

            // Dispose all monitors
            foreach (Timer monitor in _processMonitors.Values)
            {
                monitor.Dispose();
            }
            _processMonitors.Clear();

            _semaphore?.Dispose();
            _disposed = true;

            _logger.LogDebug("SocatService disposed");
        }
    }

    #endregion
}
