using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Services.Shell;

namespace S7Tools.Services.SerialPort;

/// <summary>
/// Service responsible for serial port discovery and information retrieval.
/// This service provides stateless port scanning and USB device information without dependencies on settings or timers.
/// </summary>
public sealed class SerialPortDiscoveryService
{
    private readonly ILogger<SerialPortDiscoveryService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IShellCommandExecutor _shellExecutor;

    /// <summary>
    /// Initializes a new instance of the SerialPortDiscoveryService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="timeProvider">The time provider for abstracting time operations.</param>
    /// <param name="shellExecutor">The shell command executor service.</param>
    public SerialPortDiscoveryService(
        ILogger<SerialPortDiscoveryService> logger,
        ITimeProvider timeProvider,
        IShellCommandExecutor shellExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _shellExecutor = shellExecutor ?? throw new ArgumentNullException(nameof(shellExecutor));
    }

    /// <summary>
    /// Scans for available serial ports based on configuration settings.
    /// </summary>
    /// <param name="includeUsbPorts">Whether to include USB ports in the scan.</param>
    /// <param name="includeAcmPorts">Whether to include ACM ports in the scan.</param>
    /// <param name="includeStandardPorts">Whether to include standard ports in the scan.</param>
    /// <param name="maxScanPorts">Maximum number of ports to scan per type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of discovered serial ports.</returns>
    public async Task<IEnumerable<SerialPortInfo>> ScanAvailablePortsAsync(
        bool includeUsbPorts,
        bool includeAcmPorts,
        bool includeStandardPorts,
        int maxScanPorts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting serial port scan");

        List<SerialPortInfo> ports = [];

        try
        {
            // Scan USB ports
            if (includeUsbPorts)
            {
                IEnumerable<SerialPortInfo> usbPorts = await ScanPortTypeAsync("/dev/ttyUSB", maxScanPorts, cancellationToken).ConfigureAwait(false);
                ports.AddRange(usbPorts);
            }

            // Scan ACM ports
            if (includeAcmPorts)
            {
                IEnumerable<SerialPortInfo> acmPorts = await ScanPortTypeAsync("/dev/ttyACM", maxScanPorts, cancellationToken).ConfigureAwait(false);
                ports.AddRange(acmPorts);
            }

            // Scan standard ports
            if (includeStandardPorts)
            {
                IEnumerable<SerialPortInfo> standardPorts = await ScanPortTypeAsync("/dev/ttyS", maxScanPorts, cancellationToken).ConfigureAwait(false);
                ports.AddRange(standardPorts);
            }

            _logger.LogInformation("Found {Count} serial ports", ports.Count);
            return ports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan serial ports");
            throw new ValidationException(
                "PortScan",
                "Port scanning failed due to system issues");
        }
    }

    /// <summary>
    /// Gets detailed information about a specific serial port.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="portTestTimeoutMs">Timeout for port accessibility test.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Port information if found, null otherwise.</returns>
    public async Task<SerialPortInfo?> GetPortInfoAsync(
        string portPath,
        int portTestTimeoutMs,
        CancellationToken cancellationToken = default)
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

            SerialPortType portType = GetPortType(portPath);
            bool isAccessible = await IsPortAccessibleAsync(portPath, portTestTimeoutMs, cancellationToken).ConfigureAwait(false);

            var portInfo = new SerialPortInfo
            {
                PortPath = portPath,
                DisplayName = Path.GetFileName(portPath),
                PortType = portType,
                IsAccessible = isAccessible,
                IsInUse = await IsPortInUseAsync(portPath, cancellationToken).ConfigureAwait(false),
                Description = GetPortDescription(portType),
                LastUpdated = _timeProvider.GetLocalNow()
            };

            // Get USB device info if it's a USB port
            if (portType is SerialPortType.Usb or SerialPortType.Acm)
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

    /// <summary>
    /// Tests whether a port is accessible.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="timeoutMs">Timeout for the test.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the port is accessible, false otherwise.</returns>
    public async Task<bool> IsPortAccessibleAsync(
        string portPath,
        int timeoutMs = 1000,
        CancellationToken cancellationToken = default)
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
            var result = await _shellExecutor.ExecuteDirectAsync("stty", ["-F", portPath, "-a"], timeoutMs, cancellationToken).ConfigureAwait(false);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Port accessibility test failed for {PortPath}", portPath);
            return false;
        }
    }

    /// <summary>
    /// Determines the type of a serial port based on its path.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <returns>The port type.</returns>
    public SerialPortType GetPortType(string portPath)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        string fileName = Path.GetFileName(portPath).ToLowerInvariant();
        string normalizedPath = portPath.ToLowerInvariant();

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
        else if (normalizedPath.Contains("/dev/pts/") || fileName.Contains("virtual") || fileName.Contains("pty"))
        {
            return SerialPortType.Virtual;
        }

        return SerialPortType.Unknown;
    }

    /// <summary>
    /// Retrieves USB device information for a USB serial port.
    /// </summary>
    /// <param name="portPath">The path to the USB port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>USB device information if available, null otherwise.</returns>
    public async Task<UsbDeviceInfo?> GetUsbDeviceInfoAsync(
        string portPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            // Try to get USB device information from sysfs
            string deviceName = Path.GetFileName(portPath);
            string sysfsPath = $"/sys/class/tty/{deviceName}/device";

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

    #region Private Methods

    /// <summary>
    /// Scans for ports of a specific type.
    /// </summary>
    /// <param name="basePattern">The base pattern for port paths (e.g., "/dev/ttyUSB").</param>
    /// <param name="maxPorts">The maximum number of ports to scan.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of found ports.</returns>
    private async Task<IEnumerable<SerialPortInfo>> ScanPortTypeAsync(
        string basePattern,
        int maxPorts,
        CancellationToken cancellationToken)
    {
        List<SerialPortInfo> ports = [];

        for (int i = 0; i < maxPorts; i++)
        {
            string portPath = $"{basePattern}{i}";
            SerialPortInfo? portInfo = await GetPortInfoAsync(portPath, 1000, cancellationToken).ConfigureAwait(false);

            if (portInfo != null)
            {
                ports.Add(portInfo);
            }
        }

        return ports;
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
            var result = await _shellExecutor.ExecuteDirectAsync("lsof", [portPath], 2000, cancellationToken).ConfigureAwait(false);
            return result.Success && !string.IsNullOrWhiteSpace(result.Output);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a description for a port based on its type.
    /// </summary>
    /// <param name="portType">The type of the port.</param>
    /// <returns>A description of the port.</returns>
    private static string GetPortDescription(SerialPortType portType)
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
    /// Tries to read a sysfs file and apply the value using the provided action.
    /// </summary>
    /// <param name="filePath">The path to the sysfs file.</param>
    /// <param name="setValue">The action to apply the read value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private static async Task TryReadSysfsFileAsync(
        string filePath,
        Action<string> setValue,
        CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string value = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                setValue(value.Trim());
            }
        }
        catch
        {
            // Ignore errors reading sysfs files
        }
    }

    #endregion
}
