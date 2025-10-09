using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines a contract for managing connections to an S7 PLC with modern async patterns and connection state management.
/// </summary>
public interface IS7ConnectionProvider
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Gets the connection configuration.
    /// </summary>
    S7ConnectionConfig Configuration { get; }

    /// <summary>
    /// Event raised when the connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Asynchronously establishes a connection to the PLC.
    /// </summary>
    /// <param name="config">The connection configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> ConnectAsync(S7ConnectionConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously disconnects from the PLC.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously tests the connection without establishing a persistent connection.
    /// </summary>
    /// <param name="config">The connection configuration to test.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating if the connection test was successful.</returns>
    Task<Result> TestConnectionAsync(S7ConnectionConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets information about the connected PLC.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing PLC information or an error.</returns>
    Task<Result<PlcInfo>> GetPlcInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reconnects to the PLC using the current configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> ReconnectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the connection state of the PLC.
/// </summary>
public enum ConnectionState
{
    /// <summary>Not connected.</summary>
    Disconnected,
    /// <summary>Attempting to connect.</summary>
    Connecting,
    /// <summary>Successfully connected.</summary>
    Connected,
    /// <summary>Connection lost or error occurred.</summary>
    Error,
    /// <summary>Attempting to reconnect.</summary>
    Reconnecting
}

/// <summary>
/// Event arguments for connection state changes.
/// </summary>
/// <param name="PreviousState">The previous connection state.</param>
/// <param name="CurrentState">The current connection state.</param>
/// <param name="Error">Optional error message if the state change was due to an error.</param>
public sealed record ConnectionStateChangedEventArgs(
    ConnectionState PreviousState,
    ConnectionState CurrentState,
    string? Error = null);

/// <summary>
/// Configuration for S7 PLC connections.
/// </summary>
/// <param name="IpAddress">The IP address of the PLC.</param>
/// <param name="Port">The port number (default 102 for S7).</param>
/// <param name="Rack">The rack number (typically 0).</param>
/// <param name="Slot">The slot number (typically 1 for CPU).</param>
/// <param name="ConnectionTimeout">Connection timeout in milliseconds.</param>
/// <param name="ReadTimeout">Read operation timeout in milliseconds.</param>
/// <param name="WriteTimeout">Write operation timeout in milliseconds.</param>
public sealed record S7ConnectionConfig(
    string IpAddress,
    int Port = 102,
    int Rack = 0,
    int Slot = 1,
    int ConnectionTimeout = 5000,
    int ReadTimeout = 3000,
    int WriteTimeout = 3000)
{
    /// <summary>
    /// Creates a connection configuration with validation.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="port">The port number.</param>
    /// <param name="rack">The rack number.</param>
    /// <param name="slot">The slot number.</param>
    /// <param name="connectionTimeout">Connection timeout in milliseconds.</param>
    /// <param name="readTimeout">Read timeout in milliseconds.</param>
    /// <param name="writeTimeout">Write timeout in milliseconds.</param>
    /// <returns>A Result containing the configuration or validation errors.</returns>
    public static Result<S7ConnectionConfig> Create(
        string ipAddress,
        int port = 102,
        int rack = 0,
        int slot = 1,
        int connectionTimeout = 5000,
        int readTimeout = 3000,
        int writeTimeout = 3000)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return Result<S7ConnectionConfig>.Failure("IP address cannot be null or empty");
        }

        if (port is <= 0 or > 65535)
        {
            return Result<S7ConnectionConfig>.Failure("Port must be between 1 and 65535");
        }

        if (rack is < 0 or > 7)
        {
            return Result<S7ConnectionConfig>.Failure("Rack must be between 0 and 7");
        }

        if (slot is < 0 or > 31)
        {
            return Result<S7ConnectionConfig>.Failure("Slot must be between 0 and 31");
        }

        if (connectionTimeout <= 0)
        {
            return Result<S7ConnectionConfig>.Failure("Connection timeout must be positive");
        }

        if (readTimeout <= 0)
        {
            return Result<S7ConnectionConfig>.Failure("Read timeout must be positive");
        }

        if (writeTimeout <= 0)
        {
            return Result<S7ConnectionConfig>.Failure("Write timeout must be positive");
        }

        var config = new S7ConnectionConfig(
            ipAddress.Trim(),
            port,
            rack,
            slot,
            connectionTimeout,
            readTimeout,
            writeTimeout);

        return Result<S7ConnectionConfig>.Success(config);
    }

    /// <summary>
    /// Returns a string representation of the connection configuration.
    /// </summary>
    public override string ToString() => $"{IpAddress}:{Port} (Rack:{Rack}, Slot:{Slot})";
}

/// <summary>
/// Information about a connected PLC.
/// </summary>
/// <param name="CpuType">The CPU type/model.</param>
/// <param name="SerialNumber">The serial number of the CPU.</param>
/// <param name="ModuleName">The module name.</param>
/// <param name="ModuleTypeName">The module type name.</param>
/// <param name="FirmwareVersion">The firmware version.</param>
public sealed record PlcInfo(
    string CpuType,
    string SerialNumber,
    string ModuleName,
    string ModuleTypeName,
    string FirmwareVersion)
{
    /// <summary>
    /// Returns a formatted string representation of the PLC information.
    /// </summary>
    public override string ToString() =>
        $"{CpuType} - {ModuleName} (S/N: {SerialNumber}, FW: {FirmwareVersion})";
}
