namespace S7Tools.Core.Constants;

/// <summary>
/// Memory addressing constants for Siemens S7-1200 PLC memory operations.
/// </summary>
/// <remarks>
/// These constants define default memory regions and sizes for PLC dump operations.
/// The S7-1200 user memory typically starts at 0x20000000 in the memory map.
/// </remarks>
public static class MemoryConstants
{
    /// <summary>
    /// Default start address for user memory in S7-1200 PLC.
    /// Value: 0x20000000 (536,870,912 decimal)
    /// </summary>
    /// <remarks>
    /// This is the standard base address for user-accessible RAM in S7-1200 controllers.
    /// Boot sectors and firmware code typically reside at this address.
    /// </remarks>
    public const uint DefaultUserMemoryStart = 0x20000000;

    /// <summary>
    /// Default dump size for memory operations (4KB).
    /// Value: 0x1000 (4,096 bytes)
    /// </summary>
    /// <remarks>
    /// 4KB is a reasonable default size that captures boot sector data
    /// without overwhelming the system with large dumps.
    /// </remarks>
    public const uint DefaultDumpSize = 0x1000;

    /// <summary>
    /// Extended dump size for full memory region captures (64KB).
    /// Value: 0x10000 (65,536 bytes)
    /// </summary>
    /// <remarks>
    /// 64KB provides comprehensive memory coverage for debugging and analysis,
    /// capturing a significant portion of user memory for detailed inspection.
    /// </remarks>
    public const uint ExtendedDumpSize = 0x10000;

    /// <summary>
    /// Default start address as a hexadecimal string for UI display.
    /// Value: "0x20000000"
    /// </summary>
    public const string DefaultUserMemoryStartHex = "0x20000000";
}
