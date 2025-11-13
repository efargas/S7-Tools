using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// References a socat bridge configuration profile for job execution.
/// Configures the serial-to-TCP bridge for PLC communication.
/// </summary>
/// <param name="Port">TCP port for socat bridge listening (for display).</param>
/// <param name="Ephemeral">Indicates if the port is ephemeral (temporary).</param>
/// <param name="Configuration">Complete socat configuration with all parameters.</param>
public sealed record SocatProfileRef(
    [property: Display(Name = "TCP Port", Order = 1)]
    int Port,
    [property: Display(Name = "Ephemeral Port", Order = 2)]
    bool Ephemeral = true,
    SocatConfiguration? Configuration = null
)
{
    /// <summary>
    /// Creates a SocatProfileRef from a complete SocatProfile.
    /// </summary>
    /// <param name="profile">The source socat profile.</param>
    /// <param name="ephemeral">Whether the port assignment is ephemeral.</param>
    /// <returns>A new SocatProfileRef with complete configuration.</returns>
    public static SocatProfileRef FromProfile(SocatProfile profile, bool ephemeral = true)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profile.Configuration);

        return new SocatProfileRef(
            Port: profile.Configuration.TcpPort,
            Ephemeral: ephemeral,
            Configuration: profile.Configuration
        );
    }
};
