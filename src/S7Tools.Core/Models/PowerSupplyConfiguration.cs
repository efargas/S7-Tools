using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S7Tools.Core.Models;

/// <summary>
/// Abstract base class for power supply device configurations.
/// </summary>
/// <remarks>
/// This base class provides a polymorphic foundation for different power supply types.
/// Each power supply type (Modbus TCP, Modbus RTU, SNMP, HTTP REST) implements this base class
/// with its specific configuration properties and connection logic.
/// </remarks>
[JsonDerivedType(typeof(ModbusTcpConfiguration), typeDiscriminator: "modbus-tcp")]
public abstract class PowerSupplyConfiguration
{
    /// <summary>
    /// Gets or sets the type of power supply device.
    /// </summary>
    /// <value>The power supply device type.</value>
    public PowerSupplyType Type { get; set; }

    /// <summary>
    /// Generates a connection string representation of the configuration.
    /// </summary>
    /// <returns>A human-readable connection string.</returns>
    /// <remarks>
    /// This method should return a string that uniquely identifies the connection configuration.
    /// Example formats:
    /// - Modbus TCP: "modbus-tcp://192.168.1.100:502/device/1"
    /// - Modbus RTU: "modbus-rtu:COM3:9600:8N1/device/1"
    /// - SNMP: "snmp://192.168.1.100:161/outlet/1"
    /// - HTTP REST: "https://api.power.local/v1/outlet/1"
    /// </remarks>
    public abstract string GenerateConnectionString();

    /// <summary>
    /// Tests the connection to the power supply device.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the connection test succeeds, false otherwise.</returns>
    /// <remarks>
    /// This method should attempt to establish a connection to the device and verify basic communication.
    /// It should not modify any device state (e.g., turning power on/off).
    /// Implementations should include appropriate timeouts and error handling.
    /// </remarks>
    public abstract Task<bool> TestConnectionAsync();

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    /// <returns>A list of validation error messages. Empty list if configuration is valid.</returns>
    /// <remarks>
    /// This method should validate all configuration properties and return descriptive error messages
    /// for any invalid settings. This allows for comprehensive validation before attempting to connect.
    /// </remarks>
    public abstract System.Collections.Generic.List<string> Validate();

    /// <summary>
    /// Creates a deep copy of this configuration.
    /// </summary>
    /// <returns>A new instance with the same configuration values.</returns>
    public abstract PowerSupplyConfiguration Clone();
}
