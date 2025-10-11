namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for power supply control operations.
/// Manages PLC power cycling via Modbus or similar protocols.
/// </summary>
public interface IPowerSupplyService
{
    /// <summary>
    /// Performs a power cycle operation on the PLC.
    /// </summary>
    /// <param name="host">Power supply controller host address.</param>
    /// <param name="port">Power supply controller port.</param>
    /// <param name="coil">Coil/relay number to control.</param>
    /// <param name="delaySeconds">Delay in seconds between power off and power on.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
    Task SetPowerAsync(
        string host,
        int port,
        int coil,
        bool on,
        CancellationToken cancellationToken = default);
}
