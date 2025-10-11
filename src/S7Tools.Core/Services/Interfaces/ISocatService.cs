using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for socat (Serial-to-TCP Proxy) operations including process management, command generation, and status monitoring.
/// This service provides comprehensive socat management capabilities for serial-to-TCP bridge functionality.
/// </summary>
public interface ISocatService
{
    #region Command Generation

    /// <summary>
    /// Generates the socat command string for a given configuration and serial device.
    /// </summary>
    /// <param name="configuration">The socat configuration to generate the command for.</param>
    /// <param name="serialDevice">The serial device path (e.g., "/dev/ttyUSB0").</param>
    /// <returns>The complete socat command string that can be executed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="ArgumentException">Thrown when serialDevice is null or empty.</exception>
    /// <remarks>
    /// This method generates the exact socat command required by S7Tools:
    /// socat -d -d -v -b 4 -x TCP-LISTEN:1238,fork,reuseaddr /dev/device,raw,echo=0
    /// </remarks>
    string GenerateSocatCommand(SocatConfiguration configuration, string serialDevice);

    /// <summary>
    /// Generates the socat command string for a given profile and serial device.
    /// </summary>
    /// <param name="profile">The socat profile to generate the command for.</param>
    /// <param name="serialDevice">The serial device path (e.g., "/dev/ttyUSB0").</param>
    /// <returns>The complete socat command string that can be executed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="ArgumentException">Thrown when serialDevice is null or empty.</exception>
    string GenerateSocatCommandForProfile(SocatProfile profile, string serialDevice);

    /// <summary>
    /// Validates a socat command for syntax and safety before execution.
    /// </summary>
    /// <param name="command">The socat command to validate.</param>
    /// <returns>A validation result indicating whether the command is safe to execute and any issues found.</returns>
    /// <exception cref="ArgumentException">Thrown when command is null or empty.</exception>
    SocatCommandValidationResult ValidateSocatCommand(string command);

    #endregion

    #region Process Management

    /// <summary>
    /// Starts a socat process with the specified configuration.
    /// </summary>
    /// <param name="configuration">The socat configuration to use.</param>
    /// <param name="serialDevice">The serial device path to bridge.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about the started process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="ArgumentException">Thrown when serialDevice is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started or port is already in use.</exception>
    Task<SocatProcessInfo> StartSocatAsync(SocatConfiguration configuration, string serialDevice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a socat process with the specified profile.
    /// </summary>
    /// <param name="profile">The socat profile to use.</param>
    /// <param name="serialDevice">The serial device path to bridge.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about the started process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="ArgumentException">Thrown when serialDevice is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started or port is already in use.</exception>
    Task<SocatProcessInfo> StartSocatWithProfileAsync(SocatProfile profile, string serialDevice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a running socat process.
    /// </summary>
    /// <param name="processInfo">Information about the socat process to stop.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the process was stopped successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown when processInfo is null.</exception>
    Task<bool> StopSocatAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a socat process by its process ID.
    /// </summary>
    /// <param name="processId">The process ID of the socat process to stop.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the process was stopped successfully.</returns>
    /// <exception cref="ArgumentException">Thrown when processId is less than or equal to zero.</exception>
    Task<bool> StopSocatByIdAsync(int processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all running socat processes started by this service.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of processes stopped.</returns>
    Task<int> StopAllSocatProcessesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Process Monitoring and Status

    /// <summary>
    /// Gets information about all currently running socat processes managed by this service.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about running processes.</returns>
    Task<IEnumerable<SocatProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific socat process.
    /// </summary>
    /// <param name="processId">The process ID to get information for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains process information if found, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when processId is less than or equal to zero.</exception>
    Task<SocatProcessInfo?> GetProcessInfoAsync(int processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a TCP port is currently in use by a socat process.
    /// </summary>
    /// <param name="tcpPort">The TCP port to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the port is in use.</returns>
    /// <exception cref="ArgumentException">Thrown when tcpPort is not in valid range (1-65535).</exception>
    Task<bool> IsPortInUseAsync(int tcpPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the socat process that is using a specific TCP port.
    /// </summary>
    /// <param name="tcpPort">The TCP port to find the process for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains process information if found, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when tcpPort is not in valid range (1-65535).</exception>
    Task<SocatProcessInfo?> GetProcessByPortAsync(int tcpPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors a socat process and provides real-time status updates.
    /// </summary>
    /// <param name="processInfo">The process to monitor.</param>
    /// <param name="cancellationToken">Token to cancel the monitoring operation.</param>
    /// <returns>A task that represents the asynchronous monitoring operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when processInfo is null.</exception>
    Task StartProcessMonitoringAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring a socat process.
    /// </summary>
    /// <param name="processId">The process ID to stop monitoring.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when processId is less than or equal to zero.</exception>
    Task StopProcessMonitoringAsync(int processId);

    #endregion

    #region Connection Management

    /// <summary>
    /// Gets information about active TCP connections to socat processes.
    /// </summary>
    /// <param name="processId">The socat process ID to get connections for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains information about active connections.</returns>
    /// <exception cref="ArgumentException">Thrown when processId is less than or equal to zero.</exception>
    Task<IEnumerable<SocatConnectionInfo>> GetActiveConnectionsAsync(int processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the TCP connection to a socat process.
    /// </summary>
    /// <param name="tcpHost">The host to connect to.</param>
    /// <param name="tcpPort">The port to connect to.</param>
    /// <param name="timeoutMs">The connection timeout in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the connection was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when tcpHost is null/empty or tcpPort is not in valid range.</exception>
    Task<bool> TestTcpConnectionAsync(string tcpHost, int tcpPort, int timeoutMs = 5000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about data transfer for a socat process.
    /// </summary>
    /// <param name="processId">The process ID to get statistics for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains transfer statistics if available, otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when processId is less than or equal to zero.</exception>
    Task<SocatTransferStats?> GetTransferStatsAsync(int processId, CancellationToken cancellationToken = default);

    #endregion

    #region Serial Device Management

    /// <summary>
    /// Prepares a serial device for use with socat by applying necessary stty configuration.
    /// </summary>
    /// <param name="serialDevice">The serial device path to prepare.</param>
    /// <param name="configuration">The socat configuration that may affect serial settings.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the preparation was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when serialDevice is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the device is not accessible or stty configuration fails.</exception>
    Task<bool> PrepareSerialDeviceAsync(string serialDevice, SocatConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a serial device is compatible and accessible for use with socat.
    /// </summary>
    /// <param name="serialDevice">The serial device path to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains validation information.</returns>
    /// <exception cref="ArgumentException">Thrown when serialDevice is null or empty.</exception>
    Task<SerialDeviceValidationResult> ValidateSerialDeviceAsync(string serialDevice, CancellationToken cancellationToken = default);

    #endregion

    #region Events

    /// <summary>
    /// Occurs when a socat process starts.
    /// </summary>
    event EventHandler<SocatProcessEventArgs>? ProcessStarted;

    /// <summary>
    /// Occurs when a socat process stops or exits.
    /// </summary>
    event EventHandler<SocatProcessEventArgs>? ProcessStopped;

    /// <summary>
    /// Occurs when a socat process encounters an error.
    /// </summary>
    event EventHandler<SocatProcessErrorEventArgs>? ProcessError;

    /// <summary>
    /// Occurs when a new TCP connection is established to a socat process.
    /// </summary>
    event EventHandler<SocatConnectionEventArgs>? ConnectionEstablished;

    /// <summary>
    /// Occurs when a TCP connection to a socat process is closed.
    /// </summary>
    event EventHandler<SocatConnectionEventArgs>? ConnectionClosed;

    /// <summary>
    /// Occurs when data is transferred through a socat process (for monitoring purposes).
    /// </summary>
    event EventHandler<SocatDataTransferEventArgs>? DataTransferred;

    #endregion
}

/// <summary>
/// Represents information about a running socat process.
/// </summary>
public class SocatProcessInfo
{
    /// <summary>
    /// Gets or sets the process ID of the socat process.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the TCP port being used by this socat process.
    /// </summary>
    public int TcpPort { get; set; }

    /// <summary>
    /// Gets or sets the TCP host/interface being used.
    /// </summary>
    public string TcpHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serial device path being bridged.
    /// </summary>
    public string SerialDevice { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the socat configuration used for this process.
    /// </summary>
    public SocatConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the profile used to start this process (if any).
    /// </summary>
    public SocatProfile? Profile { get; set; }

    /// <summary>
    /// Gets or sets the command line used to start the process.
    /// </summary>
    public string CommandLine { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the process was started.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the process is currently running.
    /// </summary>
    public bool IsRunning { get; set; } = true;

    /// <summary>
    /// Gets or sets the current status of the process.
    /// </summary>
    public SocatProcessStatus Status { get; set; } = SocatProcessStatus.Starting;

    /// <summary>
    /// Gets or sets the number of active TCP connections.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Gets or sets transfer statistics for this process.
    /// </summary>
    public SocatTransferStats? TransferStats { get; set; }

    /// <summary>
    /// Gets or sets the last error message (if any).
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this information was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the status of a socat process.
/// </summary>
public enum SocatProcessStatus
{
    /// <summary>
    /// Process is starting up.
    /// </summary>
    Starting,

    /// <summary>
    /// Process is running and listening for connections.
    /// </summary>
    Running,

    /// <summary>
    /// Process is stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Process has stopped normally.
    /// </summary>
    Stopped,

    /// <summary>
    /// Process has encountered an error.
    /// </summary>
    Error,

    /// <summary>
    /// Process status is unknown.
    /// </summary>
    Unknown
}

/// <summary>
/// Represents information about a TCP connection to a socat process.
/// </summary>
public class SocatConnectionInfo
{
    /// <summary>
    /// Gets or sets the remote IP address of the connection.
    /// </summary>
    public string RemoteAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remote port of the connection.
    /// </summary>
    public int RemotePort { get; set; }

    /// <summary>
    /// Gets or sets the local IP address of the connection.
    /// </summary>
    public string LocalAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local port of the connection.
    /// </summary>
    public int LocalPort { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the connection was established.
    /// </summary>
    public DateTime EstablishedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of bytes sent through this connection.
    /// </summary>
    public long BytesSent { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes received through this connection.
    /// </summary>
    public long BytesReceived { get; set; }
}

/// <summary>
/// Represents transfer statistics for a socat process.
/// </summary>
public class SocatTransferStats
{
    /// <summary>
    /// Gets or sets the total number of bytes transferred from serial to TCP.
    /// </summary>
    public long BytesSerialToTcp { get; set; }

    /// <summary>
    /// Gets or sets the total number of bytes transferred from TCP to serial.
    /// </summary>
    public long BytesTcpToSerial { get; set; }

    /// <summary>
    /// Gets or sets the total number of TCP connections handled.
    /// </summary>
    public int TotalConnections { get; set; }

    /// <summary>
    /// Gets or sets the current number of active connections.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when statistics were last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the uptime of the socat process.
    /// </summary>
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Represents the result of validating a socat command.
/// </summary>
public class SocatCommandValidationResult
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

    /// <summary>
    /// Gets or sets whether the command requires root privileges.
    /// </summary>
    public bool RequiresRoot { get; set; }

    /// <summary>
    /// Gets or sets the detected TCP port from the command.
    /// </summary>
    public int? DetectedTcpPort { get; set; }

    /// <summary>
    /// Gets or sets the detected serial device from the command.
    /// </summary>
    public string? DetectedSerialDevice { get; set; }
}

/// <summary>
/// Represents the result of validating a serial device for socat use.
/// </summary>
public class SerialDeviceValidationResult
{
    /// <summary>
    /// Gets or sets whether the device is valid and accessible.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets whether the device exists.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// Gets or sets whether the device is accessible for reading/writing.
    /// </summary>
    public bool IsAccessible { get; set; }

    /// <summary>
    /// Gets or sets whether the device is currently in use by another process.
    /// </summary>
    public bool IsInUse { get; set; }

    /// <summary>
    /// Gets or sets validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warning messages.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets additional information about the device.
    /// </summary>
    public string DeviceInfo { get; set; } = string.Empty;
}

/// <summary>
/// Provides data for socat process events.
/// </summary>
public class SocatProcessEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the SocatProcessEventArgs class.
    /// </summary>
    /// <param name="processInfo">Information about the socat process.</param>
    public SocatProcessEventArgs(SocatProcessInfo processInfo)
    {
        ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
    }

    /// <summary>
    /// Gets the information about the socat process.
    /// </summary>
    public SocatProcessInfo ProcessInfo { get; }
}

/// <summary>
/// Provides data for socat process error events.
/// </summary>
public class SocatProcessErrorEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the SocatProcessErrorEventArgs class.
    /// </summary>
    /// <param name="processInfo">Information about the socat process.</param>
    /// <param name="error">The error that occurred.</param>
    public SocatProcessErrorEventArgs(SocatProcessInfo processInfo, Exception error)
    {
        ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Gets the information about the socat process.
    /// </summary>
    public SocatProcessInfo ProcessInfo { get; }

    /// <summary>
    /// Gets the error that occurred.
    /// </summary>
    public Exception Error { get; }
}

/// <summary>
/// Provides data for socat connection events.
/// </summary>
public class SocatConnectionEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the SocatConnectionEventArgs class.
    /// </summary>
    /// <param name="processInfo">Information about the socat process.</param>
    /// <param name="connectionInfo">Information about the connection.</param>
    public SocatConnectionEventArgs(SocatProcessInfo processInfo, SocatConnectionInfo connectionInfo)
    {
        ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
        ConnectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
    }

    /// <summary>
    /// Gets the information about the socat process.
    /// </summary>
    public SocatProcessInfo ProcessInfo { get; }

    /// <summary>
    /// Gets the information about the connection.
    /// </summary>
    public SocatConnectionInfo ConnectionInfo { get; }
}

/// <summary>
/// Provides data for socat data transfer events.
/// </summary>
public class SocatDataTransferEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the SocatDataTransferEventArgs class.
    /// </summary>
    /// <param name="processInfo">Information about the socat process.</param>
    /// <param name="direction">The direction of data transfer.</param>
    /// <param name="bytesTransferred">The number of bytes transferred.</param>
    public SocatDataTransferEventArgs(SocatProcessInfo processInfo, DataTransferDirection direction, long bytesTransferred)
    {
        ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
        Direction = direction;
        BytesTransferred = bytesTransferred;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the information about the socat process.
    /// </summary>
    public SocatProcessInfo ProcessInfo { get; }

    /// <summary>
    /// Gets the direction of data transfer.
    /// </summary>
    public DataTransferDirection Direction { get; }

    /// <summary>
    /// Gets the number of bytes transferred.
    /// </summary>
    public long BytesTransferred { get; }

    /// <summary>
    /// Gets the timestamp when the transfer occurred.
    /// </summary>
    public DateTime Timestamp { get; }
}

/// <summary>
/// Represents the direction of data transfer in a socat process.
/// </summary>
public enum DataTransferDirection
{
    /// <summary>
    /// Data transferred from serial device to TCP connection.
    /// </summary>
    SerialToTcp,

    /// <summary>
    /// Data transferred from TCP connection to serial device.
    /// </summary>
    TcpToSerial
}
