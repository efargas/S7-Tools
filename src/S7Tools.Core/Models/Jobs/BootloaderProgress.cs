namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Progress record for bootloader operations.
/// </summary>
/// <param name="Stage">The current bootloader stage (e.g., "socat_setup", "handshake", "memory_dump").</param>
/// <param name="Percentage">The progress percentage (0.0 to 100.0).</param>
/// <param name="CurrentOperation">User-friendly description of current operation.</param>
/// <param name="Data">Additional stage-specific data (e.g., bytes transferred, bootloader version).</param>
public record BootloaderProgress(
    string Stage,
    double Percentage,
    string CurrentOperation,
    Dictionary<string, object>? Data = null);
