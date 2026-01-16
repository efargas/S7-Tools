using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Validation;

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
    /// <param name="taskLogger">Optional logger for main task operations and workflow steps.</param>
    /// <param name="processLogger">Optional logger for capturing socat process stdout/stderr output.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result containing dump data and saved file paths.</returns>
    Task<BootloaderResult> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        Microsoft.Extensions.Logging.ILogger? processLogger = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates profile set configuration before workflow execution.
    /// Checks: serial port accessible, TCP port available, modbus reachable, payloads exist, memory region valid.
    /// </summary>
    /// <param name="profiles">Configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with errors if configuration invalid.</returns>
    Task<ValidationResult> ValidateProfileSetAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates workflow duration based on memory region size and historical performance.
    /// </summary>
    /// <param name="memoryRegion">Memory region to dump.</param>
    /// <returns>Estimated duration (range: 5-300s per SC-001).</returns>
    TimeSpan EstimateDuration(MemoryRegionProfile memoryRegion);
}
