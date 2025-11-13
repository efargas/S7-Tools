using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// References a serial port configuration profile for job execution.
/// Contains essential serial communication parameters for display and full configuration for execution.
/// </summary>
/// <param name="Device">Serial device path (e.g., "/dev/ttyUSB0").</param>
/// <param name="Baud">Baud rate for serial communication (for display).</param>
/// <param name="Parity">Parity setting (e.g., "None", "Even", "Odd") (for display).</param>
/// <param name="DataBits">Number of data bits (typically 8) (for display).</param>
/// <param name="StopBits">Stop bits setting (e.g., "One", "Two") (for display).</param>
/// <param name="Configuration">Complete serial port configuration with all parameters.</param>
/// <param name="Options">Additional stty options/flags (e.g., "-F", "raw", "-echo").</param>
/// <param name="Flags">Additional profile-specific flags (e.g., "auto-configure=true").</param>
public sealed record SerialProfileRef(
    [property: Display(Name = "Serial Device", Order = 1)]
    string Device,
    [property: Display(Name = "Baud Rate", Order = 2)]
    int Baud,
    [property: Display(Name = "Parity", Order = 3)]
    string Parity,
    [property: Display(Name = "Data Bits", Order = 4)]
    int DataBits,
    [property: Display(Name = "Stop Bits", Order = 5)]
    string StopBits,
    SerialPortConfiguration Configuration,
    string Options = "",
    string Flags = ""
)
{
    /// <summary>
    /// Creates a SerialProfileRef from a complete SerialPortProfile.
    /// </summary>
    /// <param name="profile">The source serial port profile.</param>
    /// <param name="device">The serial device path to use.</param>
    /// <returns>A new SerialProfileRef with complete configuration.</returns>
    public static SerialProfileRef FromProfile(SerialPortProfile profile, string device)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profile.Configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(device);

        return new SerialProfileRef(
            Device: device,
            Baud: profile.Configuration.BaudRate,
            Parity: profile.Configuration.Parity.ToString(),
            DataBits: profile.Configuration.CharacterSize,
            StopBits: profile.Configuration.StopBits.ToString(),
            Configuration: profile.Configuration,
            Options: profile.Options,
            Flags: profile.Flags
        );
    }
};
