using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents the complete configuration for socat (Serial-to-TCP Proxy) command parameters.
/// This model maps directly to socat command flags and options for precise control over serial-to-TCP bridge behavior.
/// </summary>
public class SocatConfiguration
{
    #region TCP Settings

    /// <summary>
    /// Gets or sets the TCP port number for listening.
    /// </summary>
    /// <value>The TCP port number. Default is 1238.</value>
    [Range(1, 65535, ErrorMessage = "TCP port must be between 1 and 65535")]
    public int TcpPort { get; set; } = 1238;

    /// <summary>
    /// Gets or sets the TCP host/interface to bind to.
    /// </summary>
    /// <value>The host interface to bind to. Empty means all interfaces.</value>
    [StringLength(255, ErrorMessage = "Host cannot exceed 255 characters")]
    public string TcpHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to enable fork mode for multiple concurrent connections.
    /// </summary>
    /// <value>True to enable fork mode (allows multiple connections), false otherwise. Default is true.</value>
    public bool EnableFork { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable address reuse.
    /// </summary>
    /// <value>True to enable reuseaddr option, false otherwise. Default is true.</value>
    public bool EnableReuseAddr { get; set; } = true;

    #endregion

    #region socat Flags

    /// <summary>
    /// Gets or sets whether to enable verbose logging.
    /// </summary>
    /// <value>True to enable verbose mode (-v flag), false otherwise. Default is true.</value>
    public bool Verbose { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable hex dump of transferred data.
    /// </summary>
    /// <value>True to enable hex dump (-x flag), false otherwise. Default is true.</value>
    public bool HexDump { get; set; } = true;

    /// <summary>
    /// Gets or sets the block size for data transfers.
    /// </summary>
    /// <value>The block size in bytes. Default is 4.</value>
    [Range(1, 65536, ErrorMessage = "Block size must be between 1 and 65536 bytes")]
    public int BlockSize { get; set; } = 4;

    /// <summary>
    /// Gets or sets the debug level (number of -d flags).
    /// </summary>
    /// <value>The debug level (0-3). 0 = no debug, 1 = -d, 2 = -d -d, 3 = -d -d -d. Default is 2.</value>
    [Range(0, 3, ErrorMessage = "Debug level must be between 0 and 3")]
    public int DebugLevel { get; set; } = 2;

    #endregion

    #region Serial Device Settings

    /// <summary>
    /// Gets or sets whether to enable raw mode for the serial device.
    /// </summary>
    /// <value>True to enable raw mode, false otherwise. Default is true.</value>
    /// <remarks>
    /// Raw mode disables all input and output processing, making the serial device behave like a simple I/O channel.
    /// This is essential for binary data transmission and precise control over serial communication.
    /// </remarks>
    public bool SerialRawMode { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to disable echo on the serial device.
    /// </summary>
    /// <value>True to disable echo (echo=0), false to enable. Default is true.</value>
    public bool SerialDisableEcho { get; set; } = true;

    #endregion

    #region Process Management

    /// <summary>
    /// Gets or sets whether to automatically configure the serial port with stty before starting socat.
    /// </summary>
    /// <value>True to run stty configuration first, false to skip. Default is true.</value>
    public bool AutoConfigureSerial { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for TCP connections in seconds.
    /// </summary>
    /// <value>The connection timeout in seconds. 0 means no timeout. Default is 0.</value>
    [Range(0, 3600, ErrorMessage = "Timeout must be between 0 and 3600 seconds")]
    public int ConnectionTimeout { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to restart socat automatically if it terminates unexpectedly.
    /// </summary>
    /// <value>True to enable auto-restart, false otherwise. Default is false.</value>
    public bool AutoRestart { get; set; }

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
    /// Creates a default configuration that matches the required socat command for S7Tools.
    /// </summary>
    /// <returns>A SocatConfiguration with settings that generate the exact required socat command.</returns>
    /// <remarks>
    /// This configuration generates the command:
    /// socat -d -d -v -b 4 -x TCP-LISTEN:1238,fork,reuseaddr /dev/device,raw,echo=0
    /// </remarks>
    public static SocatConfiguration CreateDefault()
    {
        return new SocatConfiguration
        {
            // TCP settings
            TcpPort = 1238,
            TcpHost = string.Empty, // All interfaces
            EnableFork = true, // fork
            EnableReuseAddr = true, // reuseaddr

            // socat flags
            Verbose = true, // -v
            HexDump = true, // -x
            BlockSize = 4, // -b 4
            DebugLevel = 2, // -d -d

            // Serial device settings
            SerialRawMode = true, // raw
            SerialDisableEcho = true, // echo=0

            // Process management
            AutoConfigureSerial = true,
            ConnectionTimeout = 0, // No timeout
            AutoRestart = false,

            // Metadata
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a configuration optimized for high-speed data transfer.
    /// </summary>
    /// <returns>A SocatConfiguration suitable for high-speed communication.</returns>
    public static SocatConfiguration CreateHighSpeed()
    {
        var config = CreateDefault();
        config.Verbose = false; // Disable verbose for performance
        config.HexDump = false; // Disable hex dump for performance
        config.BlockSize = 8192; // Larger block size for better throughput
        config.DebugLevel = 0; // No debug output
        return config;
    }

    /// <summary>
    /// Creates a configuration optimized for debugging with detailed logging.
    /// </summary>
    /// <returns>A SocatConfiguration suitable for debugging and troubleshooting.</returns>
    public static SocatConfiguration CreateDebug()
    {
        var config = CreateDefault();
        config.Verbose = true; // Enable verbose
        config.HexDump = true; // Enable hex dump
        config.BlockSize = 1; // Smaller block size for detailed logging
        config.DebugLevel = 3; // Maximum debug level (-d -d -d)
        return config;
    }

    /// <summary>
    /// Creates a configuration for minimal logging and simple operation.
    /// </summary>
    /// <returns>A SocatConfiguration with minimal logging.</returns>
    public static SocatConfiguration CreateMinimal()
    {
        var config = CreateDefault();
        config.Verbose = false; // No verbose output
        config.HexDump = false; // No hex dump
        config.DebugLevel = 0; // No debug output
        return config;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    /// <returns>A new SocatConfiguration instance with identical settings.</returns>
    public SocatConfiguration Clone()
    {
        return new SocatConfiguration
        {
            // TCP settings
            TcpPort = TcpPort,
            TcpHost = TcpHost,
            EnableFork = EnableFork,
            EnableReuseAddr = EnableReuseAddr,

            // socat flags
            Verbose = Verbose,
            HexDump = HexDump,
            BlockSize = BlockSize,
            DebugLevel = DebugLevel,

            // Serial device settings
            SerialRawMode = SerialRawMode,
            SerialDisableEcho = SerialDisableEcho,

            // Process management
            AutoConfigureSerial = AutoConfigureSerial,
            ConnectionTimeout = ConnectionTimeout,
            AutoRestart = AutoRestart,

            // Metadata
            Version = Version,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null,
            CreatedAt = CreatedAt,
            ModifiedAt = DateTime.UtcNow // Update modification time for clone
        };
    }

    /// <summary>
    /// Generates the socat command string based on this configuration.
    /// </summary>
    /// <param name="serialDevice">The serial device path (e.g., "/dev/ttyUSB0").</param>
    /// <returns>The complete socat command string.</returns>
    /// <remarks>
    /// The generated command follows the pattern:
    /// socat [debug flags] [verbose] [-b blocksize] [hexdump] TCP-LISTEN:port[,options] device[,options]
    /// </remarks>
    public string GenerateCommand(string serialDevice)
    {
        var command = new StringBuilder("socat");

        // Add debug flags
        for (int i = 0; i < DebugLevel; i++)
        {
            command.Append(" -d");
        }

        // Add verbose flag
        if (Verbose)
        {
            command.Append(" -v");
        }

        // Add block size
        command.Append($" -b {BlockSize}");

        // Add hex dump flag
        if (HexDump)
        {
            command.Append(" -x");
        }

        // Build TCP-LISTEN part
        var tcpOptions = new List<string>();
        if (EnableFork)
        {
            tcpOptions.Add("fork");
        }
        if (EnableReuseAddr)
        {
            tcpOptions.Add("reuseaddr");
        }

        var tcpPart = $"TCP-LISTEN:{TcpPort}";
        if (tcpOptions.Count > 0)
        {
            tcpPart += "," + string.Join(",", tcpOptions);
        }

        command.Append($" {tcpPart}");

        // Build serial device part
        var serialOptions = new List<string>();
        if (SerialRawMode)
        {
            serialOptions.Add("raw");
        }
        if (SerialDisableEcho)
        {
            serialOptions.Add("echo=0");
        }

        var serialPart = serialDevice;
        if (serialOptions.Count > 0)
        {
            serialPart += "," + string.Join(",", serialOptions);
        }

        command.Append($" {serialPart}");

        return command.ToString();
    }

    /// <summary>
    /// Validates this configuration and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (TcpPort < 1 || TcpPort > 65535)
        {
            errors.Add("TCP port must be between 1 and 65535");
        }

        if (!string.IsNullOrEmpty(TcpHost) && TcpHost.Length > 255)
        {
            errors.Add("Host cannot exceed 255 characters");
        }

        if (BlockSize < 1 || BlockSize > 65536)
        {
            errors.Add("Block size must be between 1 and 65536 bytes");
        }

        if (DebugLevel < 0 || DebugLevel > 3)
        {
            errors.Add("Debug level must be between 0 and 3");
        }

        if (ConnectionTimeout < 0 || ConnectionTimeout > 3600)
        {
            errors.Add("Timeout must be between 0 and 3600 seconds");
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
        var features = new List<string>();

        if (Verbose)
        {
            features.Add("Verbose");
        }
        if (HexDump)
        {
            features.Add("HexDump");
        }
        if (EnableFork)
        {
            features.Add("Fork");
        }
        if (SerialRawMode)
        {
            features.Add("Raw");
        }

        var featuresStr = features.Count > 0 ? $" ({string.Join(", ", features)})" : "";

        return $"SocatConfiguration: TCP:{TcpPort}, Block:{BlockSize}, Debug:{DebugLevel}{featuresStr}";
    }

    #endregion
}
