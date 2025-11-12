namespace S7Tools.Core.Exceptions;

/// <summary>
/// Exception thrown when memory dump operation fails.
/// </summary>
public class MemoryDumpException : BootloaderException
{
    /// <summary>
    /// Gets the memory start address that failed to dump.
    /// </summary>
    public uint? StartAddress { get; init; }

    /// <summary>
    /// Gets the length in bytes that was attempted to dump.
    /// </summary>
    public uint? Length { get; init; }

    /// <summary>
    /// Gets the number of bytes successfully dumped before failure (0 if none).
    /// </summary>
    public long BytesDumped { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDumpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public MemoryDumpException(string message) : base(message)
    {
        Stage = "memory_dump";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDumpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MemoryDumpException(string message, Exception innerException) : base(message, innerException)
    {
        Stage = "memory_dump";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDumpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="startAddress">The start address.</param>
    /// <param name="length">The length in bytes.</param>
    /// <param name="bytesDumped">The bytes successfully dumped.</param>
    public MemoryDumpException(string message, uint? startAddress, uint? length, long bytesDumped) : base(message)
    {
        Stage = "memory_dump";
        StartAddress = startAddress;
        Length = length;
        BytesDumped = bytesDumped;
    }
}
