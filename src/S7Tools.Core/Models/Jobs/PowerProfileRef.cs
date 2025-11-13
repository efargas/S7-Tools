using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// References a power supply control configuration profile for job execution.
/// Configures power cycling parameters for PLC reset with complete configuration for execution.
/// </summary>
/// <param name="Host">Power supply controller host address (for display).</param>
/// <param name="Port">Power supply controller port (for display).</param>
/// <param name="Coil">Coil/relay number controlling the PLC power (for display).</param>
/// <param name="DelaySeconds">Delay in seconds between power off and power on.</param>
/// <param name="Configuration">Complete power supply configuration with all parameters.</param>
/// <param name="DeviceId">Modbus device ID (default 1) (for display).</param>
/// <param name="AddressingMode">Modbus addressing mode (Base0 or Base1) (for display).</param>
public sealed record PowerProfileRef(
    [property: Display(Name = "Host Address", Order = 1)]
    string Host,
    [property: Display(Name = "TCP Port", Order = 2)]
    int Port,
    [property: Display(Name = "Coil Number", Order = 3)]
    int Coil,
    [property: Display(Name = "Power Cycle Delay (seconds)", Order = 4)]
    int DelaySeconds,
    PowerSupplyConfiguration Configuration,
    byte DeviceId = 1,
    ModbusAddressingMode AddressingMode = ModbusAddressingMode.Base0
)
{
    /// <summary>
    /// Creates a PowerProfileRef from a complete PowerSupplyProfile.
    /// </summary>
    /// <param name="profile">The source power supply profile.</param>
    /// <param name="delaySeconds">The power cycle delay in seconds.</param>
    /// <returns>A new PowerProfileRef with complete configuration.</returns>
    public static PowerProfileRef FromProfile(PowerSupplyProfile profile, int delaySeconds)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profile.Configuration);

        // Extract display properties from ModbusTcpConfiguration if available
        var modbusTcp = profile.Configuration as ModbusTcpConfiguration;

        return new PowerProfileRef(
            Host: modbusTcp?.Host ?? "Unknown",
            Port: modbusTcp?.Port ?? 502,
            Coil: (int)(modbusTcp?.OnOffCoil ?? 0),
            DelaySeconds: delaySeconds,
            Configuration: profile.Configuration,
            DeviceId: modbusTcp?.DeviceId ?? 1,
            AddressingMode: modbusTcp?.AddressingMode ?? ModbusAddressingMode.Base0
        );
    }
};
