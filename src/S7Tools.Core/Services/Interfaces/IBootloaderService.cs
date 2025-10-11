using S7Tools.Core.Models.Jobs;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Defines the contract for bootloader orchestration operations.
/// Coordinates the complete memory dump workflow including socat bridge, power cycling, and PLC communication.
/// </summary>
public interface IBootloaderService
{
    /// <summary>
    /// Performs a complete memory dump operation on the PLC.
    /// Orchestrates socat bridge setup, power cycling, handshake, stager installation, and memory dumping.
    /// </summary>
    /// <param name="profiles">Job profile set containing all configuration parameters.</param>
    /// <param name="progress">Progress reporter providing stage name and completion percentage.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The dumped memory data.</returns>
    Task<byte[]> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent)> progress,
        CancellationToken cancellationToken = default);
}
