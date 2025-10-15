namespace S7Tools.Core.Models;

/// <summary>
/// Defines the types of power supply devices that can be controlled.
/// </summary>
/// <remarks>
/// This enumeration supports multiple power supply device types for extensibility.
/// Additional types can be added in the future without breaking existing configurations.
/// </remarks>
public enum PowerSupplyType
{
    /// <summary>
    /// Modbus TCP/IP power supply device.
    /// </summary>
    /// <remarks>
    /// Communicates with power supply via TCP/IP using Modbus protocol.
    /// Default port is 502. Supports standard Modbus coil read/write operations.
    /// </remarks>
    ModbusTcp = 0,

    /// <summary>
    /// Serial RS232 power supply device.
    /// </summary>
    /// <remarks>
    /// Communicates with power supply via RS-232 serial port.
    /// Requires serial port configuration.
    /// </remarks>
    SerialRs232 = 1,

    /// <summary>
    /// Serial RS485 power supply device.
    /// </summary>
    /// <remarks>
    /// Communicates with power supply via RS-485 serial port.
    /// Requires serial port configuration and RS-485 adapters.
    /// </remarks>
    SerialRs485 = 2,

    /// <summary>
    /// Ethernet IP power supply device.
    /// </summary>
    /// <remarks>
    /// Communicates with power supply via Ethernet/IP protocol.
    /// Industrial automation protocol for Ethernet-based devices.
    /// </remarks>
    EthernetIp = 3
}
