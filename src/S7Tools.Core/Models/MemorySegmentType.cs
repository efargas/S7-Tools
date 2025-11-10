using System.ComponentModel;

namespace S7Tools.Core.Models;

/// <summary>
/// Enumeration defining the classification of memory segments based on hardware characteristics.
/// </summary>
/// <remarks>
/// Memory segment types affect validation rules, display styling, and operation filtering.
/// The enumeration values correspond to common PLC firmware memory classifications.
/// </remarks>
public enum MemorySegmentType
{
    /// <summary>
    /// Non-volatile flash memory (program code, constants).
    /// Typically used for .text segments containing executable code.
    /// </summary>
    [Description("Flash Memory")]
    Flash = 0,

    /// <summary>
    /// Volatile random access memory (data, stack, heap).
    /// Typically used for .data and .bss segments containing variables.
    /// </summary>
    [Description("RAM Memory")]
    RAM = 1,

    /// <summary>
    /// Electrically erasable programmable read-only memory.
    /// Used for configuration data and persistent settings.
    /// </summary>
    [Description("EEPROM Memory")]
    EEPROM = 2,

    /// <summary>
    /// Read-only memory (firmware, boot code).
    /// Contains immutable code and data segments.
    /// </summary>
    [Description("ROM Memory")]
    ROM = 3
}
