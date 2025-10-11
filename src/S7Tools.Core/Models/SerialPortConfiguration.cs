using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents the complete configuration for a serial port using Linux stty command parameters.
/// This model maps directly to stty command flags and options for precise control over serial port behavior.
/// </summary>
public class SerialPortConfiguration
{
    #region Basic Settings

    /// <summary>
    /// Gets or sets the baud rate for serial communication.
    /// </summary>
    /// <value>The baud rate in bits per second. Default is 38400.</value>
    [Range(50, 4000000, ErrorMessage = "Baud rate must be between 50 and 4,000,000")]
    public int BaudRate { get; set; } = 38400;

    /// <summary>
    /// Gets or sets the character size in bits.
    /// </summary>
    /// <value>The number of bits per character (5-8). Default is 8 (cs8).</value>
    [Range(5, 8, ErrorMessage = "Character size must be between 5 and 8 bits")]
    public int CharacterSize { get; set; } = 8;

    /// <summary>
    /// Gets or sets the parity checking mode.
    /// </summary>
    /// <value>The parity mode. Default is Even with parity enabled.</value>
    public ParityMode Parity { get; set; } = ParityMode.Even;

    /// <summary>
    /// Gets or sets the number of stop bits.
    /// </summary>
    /// <value>The number of stop bits. Default is One.</value>
    public StopBits StopBits { get; set; } = StopBits.One;

    #endregion

    #region Control Flags (c_cflag)

    /// <summary>
    /// Gets or sets whether to enable receiver (CREAD flag).
    /// </summary>
    /// <value>True to enable receiver, false otherwise. Default is true.</value>
    public bool EnableReceiver { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use hardware flow control (CRTSCTS flag).
    /// </summary>
    /// <value>True to disable hardware flow control (-crtscts), false to enable. Default is true (disabled).</value>
    public bool DisableHardwareFlowControl { get; set; } = true;

    /// <summary>
    /// Gets or sets whether parity is enabled (PARENB flag).
    /// </summary>
    /// <value>True to enable parity checking, false otherwise. Default is true.</value>
    public bool ParityEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use odd parity (PARODD flag).
    /// </summary>
    /// <value>True for odd parity, false for even parity. Default is false (even parity, -parodd).</value>
    public bool OddParity { get; set; }

    #endregion

    #region Input Flags (c_iflag)

    /// <summary>
    /// Gets or sets whether to ignore break conditions (IGNBRK flag).
    /// </summary>
    /// <value>True to ignore break conditions, false otherwise. Default is true.</value>
    public bool IgnoreBreak { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to signal interrupt on break (BRKINT flag).
    /// </summary>
    /// <value>True to disable break interrupt (-brkint), false to enable. Default is true (disabled).</value>
    public bool DisableBreakInterrupt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to map CR to NL on input (ICRNL flag).
    /// </summary>
    /// <value>True to disable CR to NL mapping (-icrnl), false to enable. Default is true (disabled).</value>
    public bool DisableMapCRtoNL { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to ring bell on input queue full (IMAXBEL flag).
    /// </summary>
    /// <value>True to disable bell on queue full (-imaxbel), false to enable. Default is true (disabled).</value>
    public bool DisableBellOnQueueFull { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable XON/XOFF flow control (IXON flag).
    /// </summary>
    /// <value>True to disable XON/XOFF flow control (-ixon), false to enable. Default is true (disabled).</value>
    public bool DisableXonXoffFlowControl { get; set; } = true;

    #endregion

    #region Output Flags (c_oflag)

    /// <summary>
    /// Gets or sets whether to enable output processing (OPOST flag).
    /// </summary>
    /// <value>True to disable output processing (-opost), false to enable. Default is true (disabled).</value>
    public bool DisableOutputProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to map NL to CR-NL on output (ONLCR flag).
    /// </summary>
    /// <value>True to disable NL to CR-NL mapping (-onlcr), false to enable. Default is true (disabled).</value>
    public bool DisableMapNLtoCRNL { get; set; } = true;

    #endregion

    #region Local Flags (c_lflag)

    /// <summary>
    /// Gets or sets whether to enable canonical input processing (ICANON flag).
    /// </summary>
    /// <value>True to disable canonical mode (-icanon), false to enable. Default is true (disabled for raw mode).</value>
    public bool DisableCanonicalMode { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable signal generation (ISIG flag).
    /// </summary>
    /// <value>True to disable signal generation (-isig), false to enable. Default is true (disabled).</value>
    public bool DisableSignalGeneration { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable extended input processing (IEXTEN flag).
    /// </summary>
    /// <value>True to disable extended processing (-iexten), false to enable. Default is true (disabled).</value>
    public bool DisableExtendedProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to echo input characters (ECHO flag).
    /// </summary>
    /// <value>True to disable echo (-echo), false to enable. Default is true (disabled).</value>
    public bool DisableEcho { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to echo erase characters (ECHOE flag).
    /// </summary>
    /// <value>True to disable echo erase (-echoe), false to enable. Default is true (disabled).</value>
    public bool DisableEchoErase { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to echo kill characters (ECHOK flag).
    /// </summary>
    /// <value>True to disable echo kill (-echok), false to enable. Default is true (disabled).</value>
    public bool DisableEchoKill { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to echo control characters (ECHOCTL flag).
    /// </summary>
    /// <value>True to disable echo control (-echoctl), false to enable. Default is true (disabled).</value>
    public bool DisableEchoControl { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to echo kill with erase (ECHOKE flag).
    /// </summary>
    /// <value>True to disable echo kill erase (-echoke), false to enable. Default is true (disabled).</value>
    public bool DisableEchoKillErase { get; set; } = true;

    #endregion

    #region Special Modes

    /// <summary>
    /// Gets or sets whether to enable raw mode.
    /// </summary>
    /// <value>True to enable raw mode, false otherwise. Default is true.</value>
    /// <remarks>
    /// Raw mode disables all input and output processing, making the terminal behave like a simple I/O channel.
    /// This is essential for binary data transmission and precise control over serial communication.
    /// </remarks>
    public bool RawMode { get; set; } = true;

    #endregion

    #region Metadata

    /// <summary>
    /// Gets or sets the version of this configuration format.
    /// </summary>
    /// <value>The configuration version. Default is "1.0".</value>
    [StringLength(10, ErrorMessage = "Version cannot exceed 10 characters")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets additional metadata for this configuration.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs.</value>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this configuration was created.
    /// </summary>
    /// <value>The creation timestamp in UTC.</value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this configuration was last modified.
    /// </summary>
    /// <value>The last modification timestamp in UTC.</value>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a default configuration that matches the required stty command for S7Tools.
    /// </summary>
    /// <returns>A SerialPortConfiguration with settings that generate the exact required stty command.</returns>
    /// <remarks>
    /// This configuration generates the command:
    /// stty -F ${SERIAL_DEV} cs8 38400 ignbrk -brkint -icrnl -imaxbel -opost -onlcr -isig -icanon -iexten -echo -echoe -echok -echoctl -echoke -ixon -crtscts -parodd parenb raw
    /// </remarks>
    public static SerialPortConfiguration CreateDefault()
    {
        return new SerialPortConfiguration
        {
            // Basic settings
            BaudRate = 38400,
            CharacterSize = 8, // cs8
            Parity = ParityMode.Even, // parenb -parodd (even parity)
            StopBits = StopBits.One,

            // Control flags
            EnableReceiver = true,
            DisableHardwareFlowControl = true, // -crtscts
            ParityEnabled = true, // parenb
            OddParity = false, // -parodd (even parity)

            // Input flags
            IgnoreBreak = true, // ignbrk
            DisableBreakInterrupt = true, // -brkint
            DisableMapCRtoNL = true, // -icrnl
            DisableBellOnQueueFull = true, // -imaxbel
            DisableXonXoffFlowControl = true, // -ixon

            // Output flags
            DisableOutputProcessing = true, // -opost
            DisableMapNLtoCRNL = true, // -onlcr

            // Local flags
            DisableCanonicalMode = true, // -icanon
            DisableSignalGeneration = true, // -isig
            DisableExtendedProcessing = true, // -iexten
            DisableEcho = true, // -echo
            DisableEchoErase = true, // -echoe
            DisableEchoKill = true, // -echok
            DisableEchoControl = true, // -echoctl
            DisableEchoKillErase = true, // -echoke

            // Special modes
            RawMode = true, // raw

            // Metadata
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a configuration optimized for text-based communication.
    /// </summary>
    /// <returns>A SerialPortConfiguration suitable for text communication.</returns>
    public static SerialPortConfiguration CreateTextMode()
    {
        var config = CreateDefault();
        config.DisableCanonicalMode = false; // Enable canonical mode for line-based input
        config.DisableEcho = false; // Enable echo for interactive communication
        config.RawMode = false; // Disable raw mode for text processing
        return config;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    /// <returns>A new SerialPortConfiguration instance with identical settings.</returns>
    public SerialPortConfiguration Clone()
    {
        return new SerialPortConfiguration
        {
            // Basic settings
            BaudRate = BaudRate,
            CharacterSize = CharacterSize,
            Parity = Parity,
            StopBits = StopBits,

            // Control flags
            EnableReceiver = EnableReceiver,
            DisableHardwareFlowControl = DisableHardwareFlowControl,
            ParityEnabled = ParityEnabled,
            OddParity = OddParity,

            // Input flags
            IgnoreBreak = IgnoreBreak,
            DisableBreakInterrupt = DisableBreakInterrupt,
            DisableMapCRtoNL = DisableMapCRtoNL,
            DisableBellOnQueueFull = DisableBellOnQueueFull,
            DisableXonXoffFlowControl = DisableXonXoffFlowControl,

            // Output flags
            DisableOutputProcessing = DisableOutputProcessing,
            DisableMapNLtoCRNL = DisableMapNLtoCRNL,

            // Local flags
            DisableCanonicalMode = DisableCanonicalMode,
            DisableSignalGeneration = DisableSignalGeneration,
            DisableExtendedProcessing = DisableExtendedProcessing,
            DisableEcho = DisableEcho,
            DisableEchoErase = DisableEchoErase,
            DisableEchoKill = DisableEchoKill,
            DisableEchoControl = DisableEchoControl,
            DisableEchoKillErase = DisableEchoKillErase,

            // Special modes
            RawMode = RawMode,

            // Metadata
            Version = Version,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null,
            CreatedAt = CreatedAt,
            ModifiedAt = DateTime.UtcNow // Update modification time for clone
        };
    }

    /// <summary>
    /// Validates this configuration and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (BaudRate < 50 || BaudRate > 4000000)
        {
            errors.Add("Baud rate must be between 50 and 4,000,000");
        }

        if (CharacterSize < 5 || CharacterSize > 8)
        {
            errors.Add("Character size must be between 5 and 8 bits");
        }

        if (!string.IsNullOrEmpty(Version) && Version.Length > 10)
        {
            errors.Add("Version cannot exceed 10 characters");
        }

        return errors;
    }

    /// <summary>
    /// Returns a string representation of this configuration.
    /// </summary>
    /// <returns>A string describing the key configuration parameters.</returns>
    public override string ToString()
    {
        return $"SerialPortConfiguration: {BaudRate} baud, {CharacterSize} bits, {Parity} parity, {StopBits} stop bit(s), Raw={RawMode}";
    }

    #endregion
}

/// <summary>
/// Defines the parity checking modes for serial communication.
/// </summary>
public enum ParityMode
{
    /// <summary>
    /// No parity checking.
    /// </summary>
    None,

    /// <summary>
    /// Even parity checking.
    /// </summary>
    Even,

    /// <summary>
    /// Odd parity checking.
    /// </summary>
    Odd,

    /// <summary>
    /// Mark parity (always 1).
    /// </summary>
    Mark,

    /// <summary>
    /// Space parity (always 0).
    /// </summary>
    Space
}

/// <summary>
/// Defines the number of stop bits for serial communication.
/// </summary>
public enum StopBits
{
    /// <summary>
    /// One stop bit.
    /// </summary>
    One,

    /// <summary>
    /// One and a half stop bits.
    /// </summary>
    OnePointFive,

    /// <summary>
    /// Two stop bits.
    /// </summary>
    Two
}