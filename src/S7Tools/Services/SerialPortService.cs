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
/// Service for serial port operations including port discovery, configuration management, and Linux stty command integration.
/// This service provides comprehensive serial port management capabilities optimized for Linux systems.
/// </summary>
public sealed class SerialPortService : ISerialPortService, IDisposable
{
    private readonly ILogger<SerialPortService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly Timer? _monitoringTimer;
    private readonly Dictionary<string, SerialPortInfo> _lastKnownPorts = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isMonitoring;

    /// <summary>
    /// Initializes a new instance of the SerialPortService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="settingsService">The settings service for accessing application settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or settingsService is null.</exception>
    public SerialPortService(ILogger<SerialPortService> logger, ISettingsService settingsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        _logger.LogDebug("SerialPortService initialized");
    }

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
        _logger.LogDebug("Starting serial port scan");

        var settings = _settingsService.Settings.SerialPorts;
        var ports = new List<SerialPortInfo>();

        try
        {
            // Scan USB ports
            if (settings.IncludeUsbPorts)
            {
                var usbPorts = await ScanPortTypeAsync("/dev/ttyUSB", SerialPortType.Usb, settings.MaxScanPorts, cancellationToken).ConfigureAwait(false);
                ports.AddRange(usbPorts);
            }

            // Scan ACM ports
            if (settings.IncludeAcmPorts)
            {
                var acmPorts = await ScanPortTypeAsync("/dev/ttyACM", SerialPortType.Acm, settings.MaxScanPorts, cancellationToken).ConfigureAwait(false);
                ports.AddRange(acmPorts);
            }

            // Scan standard ports
            if (settings.IncludeStandardPorts)
            {
                var standardPorts = await ScanPortTypeAsync("/dev/ttyS", SerialPortType.Standard, settings.MaxScanPorts, cancellationToken).ConfigureAwait(false);
                ports.AddRange(standardPorts);
            }

            _logger.LogInformation("Found {Count} serial ports", ports.Count);
            return ports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan serial ports");
            throw new InvalidOperationException("Port scanning failed due to system issues", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SerialPortInfo?> GetPortInfoAsync(string portPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            if (!File.Exists(portPath))
            {
                return null;
            }

            var portType = GetPortType(portPath);
            var isAccessible = await IsPortAccessibleAsync(portPath, _settingsService.Settings.SerialPorts.PortTestTimeoutMs, cancellationToken).ConfigureAwait(false);

            var portInfo = new SerialPortInfo
            {
                PortPath = portPath,
                DisplayName = Path.GetFileName(portPath),
                PortType = portType,
                IsAccessible = isAccessible,
                IsInUse = await IsPortInUseAsync(portPath, cancellationToken).ConfigureAwait(false),
                Description = GetPortDescription(portPath, portType),
                LastUpdated = DateTime.UtcNow
            };

            // Get USB device info if it's a USB port
            if (portType == SerialPortType.Usb || portType == SerialPortType.Acm)
            {
                portInfo.UsbInfo = await GetUsbDeviceInfoAsync(portPath, cancellationToken).ConfigureAwait(false);
            }

            return portInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get port info for {PortPath}", portPath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsPortAccessibleAsync(string portPath, int timeoutMs = 1000, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            if (!File.Exists(portPath))
            {
                return false;
            }

            // Test accessibility by trying to read port status with stty
            var command = $"stty -F {portPath} -a";
            var result = await ExecuteCommandAsync(command, timeoutMs, cancellationToken).ConfigureAwait(false);

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Port accessibility test failed for {PortPath}", portPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task StartPortMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isMonitoring)
            {
                return;
            }

            _isMonitoring = true;
            var settings = _settingsService.Settings.SerialPorts;

            // Initial scan to populate known ports
            var currentPorts = await ScanAvailablePortsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var port in currentPorts)
            {
                _lastKnownPorts[port.PortPath] = port;
            }

            // Start monitoring timer
            var timer = new Timer(async _ => await MonitorPortChangesAsync().ConfigureAwait(false),
                null, TimeSpan.Zero, TimeSpan.FromSeconds(settings.ScanIntervalSeconds));

            _logger.LogInformation("Started port monitoring with {Interval}s interval", settings.ScanIntervalSeconds);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopPortMonitoringAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_isMonitoring)
            {
                return;
            }

            _isMonitoring = false;
            _monitoringTimer?.Dispose();
            _lastKnownPorts.Clear();

            _logger.LogInformation("Stopped port monitoring");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Configuration Management

    /// <inheritdoc />
    public async Task<SerialPortConfiguration?> ReadPortConfigurationAsync(string portPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            if (!await IsPortAccessibleAsync(portPath, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Port {portPath} is not accessible");
            }

            var command = $"stty -F {portPath} -a";
            var result = await ExecuteSttyCommandAsync(command, cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to read port configuration: {result.StandardError}");
            }

            return ParseSttyOutput(result.StandardOutput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read configuration for port {PortPath}", portPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ApplyConfigurationAsync(string portPath, SerialPortConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        try
        {
            if (!await IsPortAccessibleAsync(portPath, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Port {portPath} is not accessible");
            }

            var command = GenerateSttyCommand(portPath, configuration);
            var validationResult = ValidateSttyCommand(command);

            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Invalid stty command: {string.Join(", ", validationResult.Errors)}");
            }

            var result = await ExecuteSttyCommandAsync(command, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                _logger.LogInformation("Applied configuration to port {PortPath}", portPath);
                return true;
            }
            else
            {
                _logger.LogError("Failed to apply configuration to port {PortPath}: {Error}", portPath, result.StandardError);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply configuration to port {PortPath}", portPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ApplyProfileAsync(string portPath, SerialPortProfile profile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        return await ApplyConfigurationAsync(portPath, profile.Configuration, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SerialPortConfiguration?> BackupPortConfigurationAsync(string portPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            var configuration = await ReadPortConfigurationAsync(portPath, cancellationToken).ConfigureAwait(false);
            if (configuration != null)
            {
                _logger.LogDebug("Backed up configuration for port {PortPath}", portPath);
            }
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup configuration for port {PortPath}", portPath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestorePortConfigurationAsync(string portPath, SerialPortConfiguration backupConfiguration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(backupConfiguration, nameof(backupConfiguration));

        try
        {
            var success = await ApplyConfigurationAsync(portPath, backupConfiguration, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                _logger.LogInformation("Restored configuration for port {PortPath}", portPath);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore configuration for port {PortPath}", portPath);
            throw;
        }
    }

    #endregion

    #region stty Command Generation and Execution

    /// <inheritdoc />
    public string GenerateSttyCommand(string portPath, SerialPortConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var sb = new StringBuilder();
        sb.Append($"stty -F {portPath}");

        // Character size
        sb.Append($" cs{configuration.CharacterSize}");

        // Baud rate
        sb.Append($" {configuration.BaudRate}");

        // Input flags
        sb.Append(configuration.IgnoreBreak ? " ignbrk" : " -ignbrk");
        sb.Append(configuration.DisableBreakInterrupt ? " -brkint" : " brkint");
        sb.Append(configuration.DisableMapCRtoNL ? " -icrnl" : " icrnl");
        sb.Append(configuration.DisableBellOnQueueFull ? " -imaxbel" : " imaxbel");
        sb.Append(configuration.DisableXonXoffFlowControl ? " -ixon" : " ixon");

        // Output flags
        sb.Append(configuration.DisableOutputProcessing ? " -opost" : " opost");
        sb.Append(configuration.DisableMapNLtoCRNL ? " -onlcr" : " onlcr");

        // Local flags
        sb.Append(configuration.DisableSignalGeneration ? " -isig" : " isig");
        sb.Append(configuration.DisableCanonicalMode ? " -icanon" : " icanon");
        sb.Append(configuration.DisableExtendedProcessing ? " -iexten" : " iexten");
        sb.Append(configuration.DisableEcho ? " -echo" : " echo");
        sb.Append(configuration.DisableEchoErase ? " -echoe" : " echoe");
        sb.Append(configuration.DisableEchoKill ? " -echok" : " echok");
        sb.Append(configuration.DisableEchoControl ? " -echoctl" : " echoctl");
        sb.Append(configuration.DisableEchoKillErase ? " -echoke" : " echoke");

        // Control flags
        sb.Append(configuration.DisableHardwareFlowControl ? " -crtscts" : " crtscts");
        sb.Append(configuration.OddParity ? " parodd" : " -parodd");
        sb.Append(configuration.ParityEnabled ? " parenb" : " -parenb");

        // Special modes
        if (configuration.RawMode)
        {
            sb.Append(" raw");
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string GenerateSttyCommandForProfile(string portPath, SerialPortProfile profile)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        return GenerateSttyCommand(portPath, profile.Configuration);
    }

    /// <inheritdoc />
    public async Task<SttyCommandResult> ExecuteSttyCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await ExecuteCommandAsync(command, 5000, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return new SttyCommandResult
            {
                Success = result.Success,
                ExitCode = result.ExitCode,
                StandardOutput = result.StandardOutput,
                StandardError = result.StandardError,
                ExecutionTime = stopwatch.Elapsed,
                Command = command
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to execute stty command: {Command}", command);

            return new SttyCommandResult
            {
                Success = false,
                ExitCode = -1,
                StandardOutput = "",
                StandardError = ex.Message,
                ExecutionTime = stopwatch.Elapsed,
                Command = command
            };
        }
    }

    /// <inheritdoc />
    public SttyCommandValidationResult ValidateSttyCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var result = new SttyCommandValidationResult
        {
            ValidatedCommand = command.Trim()
        };

        // Basic validation
        if (!command.TrimStart().StartsWith("stty", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("Command must start with 'stty'");
            return result;
        }

        // Check for dangerous commands
        var dangerousPatterns = new[]
        {
            @"rm\s+", @"del\s+", @"format\s+", @"mkfs\s+",
            @";\s*dd\s+", @"&&\s*dd\s+", @"\|\s*dd\s+", @"^\s*dd\s+",  // Only dangerous dd usage (standalone dd command)
            @">\s*/dev/", @";\s*rm\s+", @"&&\s*rm\s+", @"\|\s*rm\s+"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
            {
                result.Errors.Add($"Command contains potentially dangerous pattern: {pattern}");
            }
        }

        // Check for required -F flag
        if (!Regex.IsMatch(command, @"-F\s+/dev/tty", RegexOptions.IgnoreCase))
        {
            result.Warnings.Add("Command should specify a device with -F flag");
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    #endregion

    #region Port Type Detection

    /// <inheritdoc />
    public SerialPortType GetPortType(string portPath)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        var fileName = Path.GetFileName(portPath).ToLowerInvariant();

        if (fileName.StartsWith("ttyusb"))
        {
            return SerialPortType.Usb;
        }
        else if (fileName.StartsWith("ttyacm"))
        {
            return SerialPortType.Acm;
        }
        else if (fileName.StartsWith("ttys"))
        {
            return SerialPortType.Standard;
        }
        else if (fileName.Contains("virtual") || fileName.Contains("pty"))
        {
            return SerialPortType.Virtual;
        }

        return SerialPortType.Unknown;
    }

    /// <inheritdoc />
    public async Task<UsbDeviceInfo?> GetUsbDeviceInfoAsync(string portPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            // Try to get USB device information from sysfs
            var deviceName = Path.GetFileName(portPath);
            var sysfsPath = $"/sys/class/tty/{deviceName}/device";

            if (!Directory.Exists(sysfsPath))
            {
                return null;
            }

            var usbInfo = new UsbDeviceInfo
            {
                DevicePath = portPath
            };

            // Try to read vendor and product IDs
            await TryReadSysfsFileAsync(Path.Combine(sysfsPath, "../idVendor"), value => usbInfo.VendorId = value, cancellationToken).ConfigureAwait(false);
            await TryReadSysfsFileAsync(Path.Combine(sysfsPath, "../idProduct"), value => usbInfo.ProductId = value, cancellationToken).ConfigureAwait(false);
            await TryReadSysfsFileAsync(Path.Combine(sysfsPath, "../manufacturer"), value => usbInfo.VendorName = value, cancellationToken).ConfigureAwait(false);
            await TryReadSysfsFileAsync(Path.Combine(sysfsPath, "../product"), value => usbInfo.ProductName = value, cancellationToken).ConfigureAwait(false);
            await TryReadSysfsFileAsync(Path.Combine(sysfsPath, "../serial"), value => usbInfo.SerialNumber = value, cancellationToken).ConfigureAwait(false);

            return usbInfo;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get USB device info for {PortPath}", portPath);
            return null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Scans for ports of a specific type.
    /// </summary>
    /// <param name="basePattern">The base pattern for port paths (e.g., "/dev/ttyUSB").</param>
    /// <param name="portType">The type of ports to scan for.</param>
    /// <param name="maxPorts">The maximum number of ports to scan.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of found ports.</returns>
    private async Task<IEnumerable<SerialPortInfo>> ScanPortTypeAsync(string basePattern, SerialPortType portType, int maxPorts, CancellationToken cancellationToken)
    {
        var ports = new List<SerialPortInfo>();
        var settings = _settingsService.Settings.SerialPorts;

        for (int i = 0; i < maxPorts; i++)
        {
            var portPath = $"{basePattern}{i}";
            var portInfo = await GetPortInfoAsync(portPath, cancellationToken).ConfigureAwait(false);

            if (portInfo != null)
            {
                ports.Add(portInfo);
            }
        }

        return ports;
    }

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
            var currentPorts = await ScanAvailablePortsAsync().ConfigureAwait(false);
            var currentPortPaths = currentPorts.ToDictionary(p => p.PortPath, p => p);

            // Check for removed ports
            var removedPorts = _lastKnownPorts.Keys.Except(currentPortPaths.Keys).ToList();
            foreach (var removedPortPath in removedPorts)
            {
                var removedPort = _lastKnownPorts[removedPortPath];
                _lastKnownPorts.Remove(removedPortPath);
                PortRemoved?.Invoke(this, new SerialPortEventArgs(removedPort));
                _logger.LogDebug("Port removed: {PortPath}", removedPortPath);
            }

            // Check for added ports
            var addedPorts = currentPortPaths.Keys.Except(_lastKnownPorts.Keys).ToList();
            foreach (var addedPortPath in addedPorts)
            {
                var addedPort = currentPortPaths[addedPortPath];
                _lastKnownPorts[addedPortPath] = addedPort;
                PortAdded?.Invoke(this, new SerialPortEventArgs(addedPort));
                _logger.LogDebug("Port added: {PortPath}", addedPortPath);
            }

            // Check for status changes
            foreach (var portPath in currentPortPaths.Keys.Intersect(_lastKnownPorts.Keys))
            {
                var currentPort = currentPortPaths[portPath];
                var lastKnownPort = _lastKnownPorts[portPath];

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

    /// <summary>
    /// Checks if a port is currently in use.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the port is in use, false otherwise.</returns>
    private async Task<bool> IsPortInUseAsync(string portPath, CancellationToken cancellationToken)
    {
        try
        {
            // Try to use lsof to check if port is in use
            var command = $"lsof {portPath}";
            var result = await ExecuteCommandAsync(command, 2000, cancellationToken).ConfigureAwait(false);
            return result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a description for a port based on its type.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="portType">The type of the port.</param>
    /// <returns>A description of the port.</returns>
    private static string GetPortDescription(string portPath, SerialPortType portType)
    {
        return portType switch
        {
            SerialPortType.Usb => "USB Serial Port",
            SerialPortType.Acm => "USB ACM Device",
            SerialPortType.Standard => "Built-in Serial Port",
            SerialPortType.Virtual => "Virtual Serial Port",
            _ => "Serial Port"
        };
    }

    /// <summary>
    /// Parses stty output to create a SerialPortConfiguration.
    /// </summary>
    /// <param name="sttyOutput">The output from stty command.</param>
    /// <returns>A SerialPortConfiguration parsed from the output.</returns>
    private SerialPortConfiguration ParseSttyOutput(string sttyOutput)
    {
        var config = new SerialPortConfiguration();

        try
        {
            // Parse baud rate
            var baudMatch = Regex.Match(sttyOutput, @"speed (\d+) baud");
            if (baudMatch.Success && int.TryParse(baudMatch.Groups[1].Value, out var baud))
            {
                config.BaudRate = baud;
            }

            // Parse character size
            var csMatch = Regex.Match(sttyOutput, @"cs(\d)");
            if (csMatch.Success && int.TryParse(csMatch.Groups[1].Value, out var cs))
            {
                config.CharacterSize = cs;
            }

            // Parse flags (simplified parsing - would need more comprehensive implementation)
            config.ParityEnabled = sttyOutput.Contains("parenb");
            config.OddParity = sttyOutput.Contains("parodd") && !sttyOutput.Contains("-parodd");
            config.DisableHardwareFlowControl = sttyOutput.Contains("-crtscts");
            config.IgnoreBreak = sttyOutput.Contains("ignbrk");
            config.DisableBreakInterrupt = sttyOutput.Contains("-brkint");
            config.DisableMapCRtoNL = sttyOutput.Contains("-icrnl");
            config.DisableBellOnQueueFull = sttyOutput.Contains("-imaxbel");
            config.DisableXonXoffFlowControl = sttyOutput.Contains("-ixon");
            config.DisableOutputProcessing = sttyOutput.Contains("-opost");
            config.DisableMapNLtoCRNL = sttyOutput.Contains("-onlcr");
            config.DisableSignalGeneration = sttyOutput.Contains("-isig");
            config.DisableCanonicalMode = sttyOutput.Contains("-icanon");
            config.DisableExtendedProcessing = sttyOutput.Contains("-iexten");
            config.DisableEcho = sttyOutput.Contains("-echo");
            config.DisableEchoErase = sttyOutput.Contains("-echoe");
            config.DisableEchoKill = sttyOutput.Contains("-echok");
            config.DisableEchoControl = sttyOutput.Contains("-echoctl");
            config.DisableEchoKillErase = sttyOutput.Contains("-echoke");

            config.ModifiedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse stty output completely");
        }

        return config;
    }

    /// <summary>
    /// Executes a command and returns the result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The command execution result.</returns>
    private async Task<(bool Success, int ExitCode, string StandardOutput, string StandardError)> ExecuteCommandAsync(string command, int timeoutMs, CancellationToken cancellationToken)
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

            process.OutputDataReceived += (_, e) => { if (e.Data != null) { outputBuilder.AppendLine(e.Data); } };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) { errorBuilder.AppendLine(e.Data); } };

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

            var success = process.ExitCode == 0;
            return (success, process.ExitCode, outputBuilder.ToString().Trim(), errorBuilder.ToString().Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {Command}", command);
            return (false, -1, "", ex.Message);
        }
    }

    /// <summary>
    /// Tries to read a sysfs file and apply the value using the provided action.
    /// </summary>
    /// <param name="filePath">The path to the sysfs file.</param>
    /// <param name="setValue">The action to apply the read value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task TryReadSysfsFileAsync(string filePath, Action<string> setValue, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var value = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                setValue(value.Trim());
            }
        }
        catch
        {
            // Ignore errors reading sysfs files
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
