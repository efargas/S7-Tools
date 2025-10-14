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
    /// Modbus RTU (serial) power supply device.
    /// </summary>
    /// <remarks>
    /// Communicates with power supply via RS-232/RS-485 serial port using Modbus RTU protocol.
    /// Requires serial port configuration and Modbus RTU framing.
    /// </remarks>
    ModbusRtu = 1,

    /// <summary>
    /// SNMP-based power supply device.
    /// </summary>
    /// <remarks>
    /// Communicates with power supply via SNMP (Simple Network Management Protocol).
    /// Common in network-attached power distribution units (PDUs).
    /// </remarks>
    Snmp = 2,

    /// <summary>
    /// HTTP REST API power supply device.
    /// </summary>
    /// <remarks>
    /// Communicates with power supply via HTTP/HTTPS REST API.
    /// Modern IoT power supplies often provide REST APIs for control.
    /// </remarks>
    HttpRest = 3,

    /// <summary>
    /// Custom or proprietary protocol power supply device.
    /// </summary>
    /// <remarks>
    /// For power supplies with vendor-specific or custom protocols.
    /// Requires custom implementation of communication logic.
    /// </remarks>
    Custom = 99
}
