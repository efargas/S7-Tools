using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for serial port operations including port discovery, configuration management, and Linux stty command integration.
/// This service provides comprehensive serial port management capabilities optimized for Linux systems.
/// </summary>
public interface ISerialPortService
{
    #region Port Discovery and Monitoring

    /// <summary>
    /// Scans for available serial ports on the system.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about available ports.</returns>
    /// <exception cref="InvalidOperationException">Thrown when port scanning fails due to system issues.</exception>
    Task<IEnumerable<SerialPortInfo>> ScanAvailablePortsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific serial port.
    /// </summary>
    /// <param name="portPath">The path to the serial port (e.g., "/dev/ttyUSB0").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains detailed port information if the port exists, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null or empty.</exception>
    Task<SerialPortInfo?> GetPortInfoAsync(string portPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests whether a serial port is accessible and can be opened.
    /// </summary>
    /// <param name="portPath">The path to the serial port to test.</param>
    /// <param name="timeoutMs">The timeout in milliseconds for the accessibility test.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the port is accessible.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null or empty.</exception>
    Task<bool> IsPortAccessibleAsync(string portPath, int timeoutMs = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors serial port availability changes and raises events when ports are added or removed.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the monitoring operation.</param>
    /// <returns>A task that represents the asynchronous monitoring operation.</returns>
    Task StartPortMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring serial port availability changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopPortMonitoringAsync();

    #endregion

    #region Configuration Management

    /// <summary>
    /// Reads the current configuration of a serial port using stty command.
    /// </summary>
    /// <param name="portPath">The path to the serial port to read configuration from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current port configuration if successful, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the port is not accessible or stty command fails.</exception>
    Task<SerialPortConfiguration?> ReadPortConfigurationAsync(string portPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a configuration profile to a serial port using stty command.
    /// </summary>
    /// <param name="portPath">The path to the serial port to configure.</param>
    /// <param name="configuration">The configuration to apply to the port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the configuration was applied successfully.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null/empty or configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the port is not accessible or stty command fails.</exception>
    Task<bool> ApplyConfigurationAsync(string portPath, SerialPortConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a profile to a serial port.
    /// </summary>
    /// <param name="portPath">The path to the serial port to configure.</param>
    /// <param name="profile">The profile to apply to the port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the profile was applied successfully.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null/empty or profile is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the port is not accessible or stty command fails.</exception>
    Task<bool> ApplyProfileAsync(string portPath, SerialPortProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup of the current port configuration before applying changes.
    /// </summary>
    /// <param name="portPath">The path to the serial port to backup.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the backup configuration if successful, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null or empty.</exception>
    Task<SerialPortConfiguration?> BackupPortConfigurationAsync(string portPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a previously backed up configuration to a serial port.
    /// </summary>
    /// <param name="portPath">The path to the serial port to restore configuration to.</param>
    /// <param name="backupConfiguration">The backup configuration to restore.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the restoration was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null/empty or backupConfiguration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the port is not accessible or stty command fails.</exception>
    Task<bool> RestorePortConfigurationAsync(string portPath, SerialPortConfiguration backupConfiguration, CancellationToken cancellationToken = default);

    #endregion

    #region stty Command Generation and Execution

    /// <summary>
    /// Generates the stty command string for a given configuration.
    /// </summary>
    /// <param name="portPath">The path to the serial port.</param>
    /// <param name="configuration">The configuration to generate the command for.</param>
    /// <returns>The complete stty command string that can be executed to apply the configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null/empty or configuration is null.</exception>
    /// <remarks>
    /// This method generates the exact stty command required by S7Tools:
    /// stty -F ${SERIAL_DEV} cs8 38400 ignbrk -brkint -icrnl -imaxbel -opost -onlcr -isig -icanon -iexten -echo -echoe -echok -echoctl -echoke -ixon -crtscts -parodd parenb raw
    /// </remarks>
    string GenerateSttyCommand(string portPath, SerialPortConfiguration configuration);

    /// <summary>
    /// Generates the stty command string for a given profile.
    /// </summary>
    /// <param name="portPath">The path to the serial port.</param>
    /// <param name="profile">The profile to generate the command for.</param>
    /// <returns>The complete stty command string that can be executed to apply the profile configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null/empty or profile is null.</exception>
    string GenerateSttyCommandForProfile(string portPath, SerialPortProfile profile);

    /// <summary>
    /// Executes a stty command and returns the result.
    /// </summary>
    /// <param name="command">The stty command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command execution result.</returns>
    /// <exception cref="ArgumentException">Thrown when command is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when command execution fails.</exception>
    Task<SttyCommandResult> ExecuteSttyCommandAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a stty command for syntax and safety before execution.
    /// </summary>
    /// <param name="command">The stty command to validate.</param>
    /// <returns>A validation result indicating whether the command is safe to execute and any issues found.</returns>
    /// <exception cref="ArgumentException">Thrown when command is null or empty.</exception>
    SttyCommandValidationResult ValidateSttyCommand(string command);

    #endregion

    #region Port Type Detection

    /// <summary>
    /// Determines the type of a serial port based on its path and characteristics.
    /// </summary>
    /// <param name="portPath">The path to the serial port.</param>
    /// <returns>The detected port type (USB, ACM, Standard, etc.).</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null or empty.</exception>
    SerialPortType GetPortType(string portPath);

    /// <summary>
    /// Gets the vendor and product information for a USB serial port.
    /// </summary>
    /// <param name="portPath">The path to the USB serial port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains USB device information if available, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when portPath is null or empty.</exception>
    Task<UsbDeviceInfo?> GetUsbDeviceInfoAsync(string portPath, CancellationToken cancellationToken = default);

    #endregion

    #region Events

    /// <summary>
    /// Occurs when a serial port is detected as added to the system.
    /// </summary>
    event EventHandler<SerialPortEventArgs>? PortAdded;

    /// <summary>
    /// Occurs when a serial port is detected as removed from the system.
    /// </summary>
    event EventHandler<SerialPortEventArgs>? PortRemoved;

    /// <summary>
    /// Occurs when a serial port's status changes (accessible/inaccessible).
    /// </summary>
    event EventHandler<SerialPortStatusChangedEventArgs>? PortStatusChanged;

    #endregion
}

/// <summary>
/// Represents information about a serial port.
/// </summary>
public class SerialPortInfo
{
    /// <summary>
    /// Gets or sets the path to the serial port (e.g., "/dev/ttyUSB0").
    /// </summary>
    public string PortPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the port.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the serial port.
    /// </summary>
    public SerialPortType PortType { get; set; }

    /// <summary>
    /// Gets or sets whether the port is currently accessible.
    /// </summary>
    public bool IsAccessible { get; set; }

    /// <summary>
    /// Gets or sets whether the port is currently in use.
    /// </summary>
    public bool IsInUse { get; set; }

    /// <summary>
    /// Gets or sets additional information about the port.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets USB device information if this is a USB serial port.
    /// </summary>
    public UsbDeviceInfo? UsbInfo { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this information was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the type of a serial port.
/// </summary>
public enum SerialPortType
{
    /// <summary>
    /// Unknown or unidentified port type.
    /// </summary>
    Unknown,

    /// <summary>
    /// USB serial port (/dev/ttyUSB*).
    /// </summary>
    Usb,

    /// <summary>
    /// ACM (Abstract Control Model) serial port (/dev/ttyACM*).
    /// </summary>
    Acm,

    /// <summary>
    /// Standard built-in serial port (/dev/ttyS*).
    /// </summary>
    Standard,

    /// <summary>
    /// Virtual or emulated serial port.
    /// </summary>
    Virtual
}

/// <summary>
/// Represents USB device information for USB serial ports.
/// </summary>
public class UsbDeviceInfo
{
    /// <summary>
    /// Gets or sets the USB vendor ID.
    /// </summary>
    public string VendorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the USB product ID.
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vendor name.
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device serial number.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the USB device path.
    /// </summary>
    public string DevicePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of executing a stty command.
/// </summary>
public class SttyCommandResult
{
    /// <summary>
    /// Gets or sets whether the command executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the exit code of the command.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the standard output from the command.
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the standard error output from the command.
    /// </summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution time of the command.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the command that was executed.
    /// </summary>
    public string Command { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of validating a stty command.
/// </summary>
public class SttyCommandValidationResult
{
    /// <summary>
    /// Gets or sets whether the command is valid and safe to execute.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warning messages.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the validated command (potentially with corrections).
    /// </summary>
    public string ValidatedCommand { get; set; } = string.Empty;
}

/// <summary>
/// Provides data for serial port events.
/// </summary>
public class SerialPortEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the SerialPortEventArgs class.
    /// </summary>
    /// <param name="portInfo">Information about the serial port.</param>
    public SerialPortEventArgs(SerialPortInfo portInfo)
    {
        PortInfo = portInfo ?? throw new ArgumentNullException(nameof(portInfo));
    }

    /// <summary>
    /// Gets the information about the serial port.
    /// </summary>
    public SerialPortInfo PortInfo { get; }
}

/// <summary>
/// Provides data for serial port status change events.
/// </summary>
public class SerialPortStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the SerialPortStatusChangedEventArgs class.
    /// </summary>
    /// <param name="portPath">The path to the serial port.</param>
    /// <param name="oldStatus">The previous accessibility status.</param>
    /// <param name="newStatus">The new accessibility status.</param>
    public SerialPortStatusChangedEventArgs(string portPath, bool oldStatus, bool newStatus)
    {
        PortPath = portPath ?? throw new ArgumentNullException(nameof(portPath));
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }

    /// <summary>
    /// Gets the path to the serial port.
    /// </summary>
    public string PortPath { get; }

    /// <summary>
    /// Gets the previous accessibility status.
    /// </summary>
    public bool OldStatus { get; }

    /// <summary>
    /// Gets the new accessibility status.
    /// </summary>
    public bool NewStatus { get; }
}
