using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// References a power supply control configuration profile for job execution.
/// Configures power cycling parameters for PLC reset.
/// </summary>
/// <param name="Host">Power supply controller host address.</param>
/// <param name="Port">Power supply controller port.</param>
/// <param name="Coil">Coil/relay number controlling the PLC power.</param>
/// <param name="DelaySeconds">Delay in seconds between power off and power on.</param>
public sealed record PowerProfileRef(
    [property: Display(Name = "Host Address", Order = 1)]
    string Host,
    [property: Display(Name = "TCP Port", Order = 2)]
    int Port,
    [property: Display(Name = "Coil Number", Order = 3)]
    int Coil,
    [property: Display(Name = "Power Cycle Delay (seconds)", Order = 4)]
    int DelaySeconds
);
