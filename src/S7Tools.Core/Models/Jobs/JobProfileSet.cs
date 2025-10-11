namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Contains all configuration profiles required for a bootloader job.
/// Aggregates serial, network, power, memory, and payload settings.
/// </summary>
/// <param name="Serial">Serial port configuration for PLC communication.</param>
/// <param name="Socat">Socat bridge configuration for serial-to-TCP forwarding.</param>
/// <param name="Power">Power supply control configuration.</param>
/// <param name="Memory">Memory region configuration for dumping.</param>
/// <param name="Payloads">Payload file configuration.</param>
/// <param name="OutputPath">Output directory path for dump files.</param>
public sealed record JobProfileSet(
    SerialProfileRef Serial,
    SocatProfileRef Socat,
    PowerProfileRef Power,
    MemoryRegionProfile Memory,
    PayloadSetProfile Payloads,
    string OutputPath
);
