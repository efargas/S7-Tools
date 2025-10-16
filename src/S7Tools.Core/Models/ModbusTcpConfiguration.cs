using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace S7Tools.Core.Models;

/// <summary>
/// Configuration for Modbus TCP power supply devices.
/// </summary>
/// <remarks>
/// This configuration supports communication with power supply devices via Modbus TCP/IP protocol.
/// Modbus TCP uses standard TCP/IP sockets (default port 502) for communication.
/// </remarks>
public class ModbusTcpConfiguration : PowerSupplyConfiguration
{
    #region TCP Connection Settings

    /// <summary>
    /// Gets or sets the host address (IP address or hostname) of the Modbus TCP device.
    /// </summary>
    /// <value>The host address. Can be an IPv4 address (e.g., "192.168.1.100") or hostname (e.g., "power-supply.local").</value>
    [Required(ErrorMessage = "Host address is required")]
    [StringLength(255, ErrorMessage = "Host address cannot exceed 255 characters")]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TCP port number for Modbus communication.
    /// </summary>
    /// <value>The TCP port number. Default is 502 (standard Modbus TCP port).</value>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 502;

    #endregion

    #region Modbus Protocol Settings

    /// <summary>
    /// Gets or sets the Modbus device ID (slave address).
    /// </summary>
    /// <value>The device ID. Valid range is 0-247. Default is 1.</value>
    /// <remarks>
    /// Also known as Unit Identifier in Modbus TCP.
    /// Value 0 is typically reserved for broadcast (not recommended for power control).
    /// Value 255 is reserved and should not be used.
    /// Common values: 1 for single device, 1-247 for multi-drop configurations.
    /// </remarks>
    [Range(0, 247, ErrorMessage = "Device ID must be between 0 and 247")]
    public byte DeviceId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the coil address for power on/off control.
    /// </summary>
    /// <value>The coil address. Range depends on addressing mode.</value>
    /// <remarks>
    /// This coil address is used to control the power supply on/off state.
    /// - Base-0 mode: Range 0-65535 (protocol address)
    /// - Base-1 mode: Range 1-65536 (display address, internally converted to 0-based)
    /// Writing TRUE (0xFF00) turns power ON, FALSE (0x0000) turns power OFF.
    /// </remarks>
    public ushort OnOffCoil { get; set; }

    /// <summary>
    /// Gets or sets the addressing mode for coil addresses.
    /// </summary>
    /// <value>The addressing mode. Default is Base0 (0-based indexing).</value>
    /// <remarks>
    /// <para>Base-0: Coil addresses start at 0 (matches Modbus protocol).</para>
    /// <para>Base-1: Coil addresses start at 1 (user-friendly, converted to 0-based internally).</para>
    /// </remarks>
    public ModbusAddressingMode AddressingMode { get; set; } = ModbusAddressingMode.Base0;

    #endregion

    #region Connection Settings

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    /// <value>The connection timeout. Default is 5000ms (5 seconds).</value>
    [Range(100, 60000, ErrorMessage = "Connection timeout must be between 100ms and 60000ms")]
    public int ConnectionTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the read timeout in milliseconds.
    /// </summary>
    /// <value>The read timeout. Default is 3000ms (3 seconds).</value>
    [Range(100, 30000, ErrorMessage = "Read timeout must be between 100ms and 30000ms")]
    public int ReadTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the write timeout in milliseconds.
    /// </summary>
    /// <value>The write timeout. Default is 3000ms (3 seconds).</value>
    [Range(100, 30000, ErrorMessage = "Write timeout must be between 100ms and 30000ms")]
    public int WriteTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets whether to enable automatic reconnection on connection loss.
    /// </summary>
    /// <value>True to enable auto-reconnect, false otherwise. Default is true.</value>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for operations.
    /// </summary>
    /// <value>The maximum retry attempts. Default is 3.</value>
    [Range(0, 10, ErrorMessage = "Retry attempts must be between 0 and 10")]
    public int MaxRetryAttempts { get; set; } = 3;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ModbusTcpConfiguration"/> class.
    /// </summary>
    public ModbusTcpConfiguration()
    {
        Type = PowerSupplyType.ModbusTcp;
    }

    /// <inheritdoc />
    public override string GenerateConnectionString()
    {
        // Format: modbus-tcp://host:port/device/coil?mode=baseN
        var modeStr = AddressingMode == ModbusAddressingMode.Base0 ? "base0" : "base1";
        return $"modbus-tcp://{Host}:{Port}/device/{DeviceId}/coil/{OnOffCoil}?mode={modeStr}";
    }

    /// <inheritdoc />
    public override async Task<bool> TestConnectionAsync()
    {
        // This is a placeholder - actual implementation will be in the service layer
        // The service layer will use NModbus to test the connection
        await Task.CompletedTask;
        return false; // Return false until service implementation is complete
    }

    /// <inheritdoc />
    public override List<string> Validate()
    {
        var errors = new List<string>();

        // Validate host
        if (string.IsNullOrWhiteSpace(Host))
        {
            errors.Add("Host address is required");
        }
        else
        {
            // Try to parse as IP address or validate as hostname
            if (!IPAddress.TryParse(Host, out _) && !IsValidHostname(Host))
            {
                errors.Add("Host must be a valid IP address or hostname");
            }
        }

        // Validate port
        if (Port < 1 || Port > 65535)
        {
            errors.Add("Port must be between 1 and 65535");
        }

        // Validate device ID
        if (DeviceId > 247)
        {
            errors.Add("Device ID must be between 0 and 247");
        }

        // Validate coil address based on addressing mode
        if (AddressingMode == ModbusAddressingMode.Base1 && OnOffCoil == 0)
        {
            errors.Add("Coil address must be at least 1 in Base-1 addressing mode");
        }

        return errors;
    }

    /// <inheritdoc />
    public override PowerSupplyConfiguration Clone()
    {
        return new ModbusTcpConfiguration
        {
            Type = Type,
            Host = Host,
            Port = Port,
            DeviceId = DeviceId,
            OnOffCoil = OnOffCoil,
            AddressingMode = AddressingMode,
            ConnectionTimeoutMs = ConnectionTimeoutMs,
            ReadTimeoutMs = ReadTimeoutMs,
            WriteTimeoutMs = WriteTimeoutMs,
            EnableAutoReconnect = EnableAutoReconnect,
            MaxRetryAttempts = MaxRetryAttempts
        };
    }

    /// <summary>
    /// Converts the display address to the protocol address based on the addressing mode.
    /// </summary>
    /// <param name="displayAddress">The address as displayed to the user.</param>
    /// <returns>The actual protocol address to use in Modbus operations.</returns>
    /// <remarks>
    /// - Base-0 mode: Returns the address as-is (no conversion needed).
    /// - Base-1 mode: Subtracts 1 to convert from 1-based to 0-based addressing.
    /// </remarks>
    public ushort ConvertToProtocolAddress(ushort displayAddress)
    {
        return AddressingMode == ModbusAddressingMode.Base1
            ? (ushort)(displayAddress - 1)
            : displayAddress;
    }

    /// <summary>
    /// Converts the protocol address to the display address based on the addressing mode.
    /// </summary>
    /// <param name="protocolAddress">The actual Modbus protocol address.</param>
    /// <returns>The address to display to the user.</returns>
    /// <remarks>
    /// - Base-0 mode: Returns the address as-is (no conversion needed).
    /// - Base-1 mode: Adds 1 to convert from 0-based to 1-based addressing.
    /// </remarks>
    public ushort ConvertToDisplayAddress(ushort protocolAddress)
    {
        return AddressingMode == ModbusAddressingMode.Base1
            ? (ushort)(protocolAddress + 1)
            : protocolAddress;
    }

    private static bool IsValidHostname(string hostname)
    {
        // Simple hostname validation
        if (string.IsNullOrWhiteSpace(hostname) || hostname.Length > 253)
        {
            return false;
        }

        // Hostname can contain alphanumeric characters, hyphens, and dots
        foreach (char c in hostname)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '.')
            {
                return false;
            }
        }

        return true;
    }
}
