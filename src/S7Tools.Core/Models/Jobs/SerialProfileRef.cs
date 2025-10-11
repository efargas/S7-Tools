namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// References a serial port configuration profile for job execution.
/// Contains essential serial communication parameters.
/// </summary>
/// <param name="Device">Serial device path (e.g., "/dev/ttyUSB0").</param>
/// <param name="Baud">Baud rate for serial communication.</param>
/// <param name="Parity">Parity setting (e.g., "None", "Even", "Odd").</param>
/// <param name="DataBits">Number of data bits (typically 8).</param>
/// <param name="StopBits">Stop bits setting (e.g., "One", "Two").</param>
public sealed record SerialProfileRef(
    string Device,
    int Baud,
    string Parity,
    int DataBits,
    string StopBits
);
