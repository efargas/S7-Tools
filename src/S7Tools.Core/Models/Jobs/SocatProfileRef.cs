namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// References a socat bridge configuration profile for job execution.
/// Configures the serial-to-TCP bridge for PLC communication.
/// </summary>
/// <param name="Port">TCP port for socat bridge listening.</param>
/// <param name="Ephemeral">Indicates if the port is ephemeral (temporary).</param>
public sealed record SocatProfileRef(
    int Port,
    bool Ephemeral = true
);
