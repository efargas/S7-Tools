namespace S7Tools.Core.Models;

/// <summary>
/// Defines the addressing mode for Modbus coil addresses.
/// </summary>
/// <remarks>
/// Different Modbus systems use different addressing conventions.
/// This enumeration allows users to select their preferred addressing mode.
/// </remarks>
public enum ModbusAddressingMode
{
    /// <summary>
    /// Base-0 addressing mode (0-based indexing).
    /// </summary>
    /// <remarks>
    /// <para>Coil addresses start at 0.</para>
    /// <para>First coil = address 0, second coil = address 1, etc.</para>
    /// <para>No conversion needed - matches Modbus protocol specification.</para>
    /// <para>Used by: Most PLCs, Modbus specifications, industrial control systems.</para>
    /// <para>Address range: 0-65535</para>
    /// </remarks>
    Base0 = 0,

    /// <summary>
    /// Base-1 addressing mode (1-based indexing).
    /// </summary>
    /// <remarks>
    /// <para>Coil addresses start at 1 for display purposes.</para>
    /// <para>First coil = display address 1 (internally converted to 0).</para>
    /// <para>Requires conversion: protocol_address = display_address - 1.</para>
    /// <para>Used by: Some HMI systems, SCADA software, user-friendly interfaces.</para>
    /// <para>Display address range: 1-65536</para>
    /// </remarks>
    Base1 = 1
}
