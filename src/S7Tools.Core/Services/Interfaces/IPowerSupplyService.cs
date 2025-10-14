using System;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for power supply control operations using configuration-based approach.
/// Supports multiple power supply device types (Modbus TCP, Modbus RTU, SNMP, HTTP REST) with unified interface.
/// </summary>
public interface IPowerSupplyService
{
    #region Connection Management

    /// <summary>
    /// Connects to the power supply device using the specified configuration.
    /// </summary>
    /// <param name="configuration">The power supply configuration containing connection parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the connection was successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when already connected or connection fails.</exception>
    Task<bool> ConnectAsync(PowerSupplyConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the currently connected power supply device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the power supply device without establishing a persistent connection.
    /// </summary>
    /// <param name="configuration">The power supply configuration to test.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the connection test was successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    Task<bool> TestConnectionAsync(PowerSupplyConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the service is currently connected to a power supply device.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current configuration being used for the connection.
    /// </summary>
    PowerSupplyConfiguration? CurrentConfiguration { get; }

    #endregion

    #region Power Control Operations

    /// <summary>
    /// Turns the power supply on.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the operation was successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to a power supply device.</exception>
    Task<bool> TurnOnAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns the power supply off.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the operation was successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to a power supply device.</exception>
    Task<bool> TurnOffAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the current power state from the power supply device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the power is currently on (true) or off (false).</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to a power supply device.</exception>
    Task<bool> ReadPowerStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a power cycle operation (turn off, wait, turn on).
    /// </summary>
    /// <param name="delayMs">The delay in milliseconds between power off and power on. Default is 5000ms (5 seconds).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result indicates whether the operation was successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to a power supply device.</exception>
    Task<bool> PowerCycleAsync(int delayMs = 5000, CancellationToken cancellationToken = default);

    #endregion

    #region Legacy Compatibility Methods (Deprecated)

    /// <summary>
    /// Performs a power cycle operation on the PLC.
    /// </summary>
    /// <param name="host">Power supply controller host address.</param>
    /// <param name="port">Power supply controller port.</param>
    /// <param name="coil">Coil/relay number to control.</param>
    /// <param name="delaySeconds">Delay in seconds between power off and power on.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>This method is deprecated. Use ConnectAsync() with configuration and PowerCycleAsync() instead.</remarks>
    [Obsolete("Use ConnectAsync() with PowerSupplyConfiguration and PowerCycleAsync() instead. This method will be removed in a future version.")]
    Task PowerCycleAsync(
        string host,
        int port,
        int coil,
        int delaySeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the power state of the PLC.
    /// </summary>
    /// <param name="host">Power supply controller host address.</param>
    /// <param name="port">Power supply controller port.</param>
    /// <param name="coil">Coil/relay number to control.</param>
    /// <param name="on">True to power on, false to power off.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>This method is deprecated. Use ConnectAsync() with configuration and TurnOnAsync()/TurnOffAsync() instead.</remarks>
    [Obsolete("Use ConnectAsync() with PowerSupplyConfiguration and TurnOnAsync()/TurnOffAsync() instead. This method will be removed in a future version.")]
    Task SetPowerAsync(
        string host,
        int port,
        int coil,
        bool on,
        CancellationToken cancellationToken = default);

    #endregion
}
