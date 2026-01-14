using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text;

namespace S7Tools.Core.Models.MemoryDump;

/// <summary>
/// Represents a contiguous block of PLC memory with its address and data.
/// This is an immutable, lightweight structure optimized for high-frequency allocation in memory dump scenarios.
/// </summary>
public readonly struct MemoryBlock : IEquatable<MemoryBlock>
{
    private const int BytesPerLine = 16;

    /// <summary>
    /// Gets the starting memory address of this block.
    /// </summary>
    public uint Address { get; }

    /// <summary>
    /// Gets the raw memory data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBlock"/> struct.
    /// </summary>
    /// <param name="address">Starting memory address.</param>
    /// <param name="data">Raw memory data.</param>
    public MemoryBlock(uint address, byte[] data)
    {
        Address = address;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Gets the hexadecimal representation of the data, formatted with spaces between bytes.
    /// </summary>
    /// <remarks>
    /// This property performs formatting on-demand. For high-frequency scenarios with repeated access,
    /// consider caching the result at the consumer level.
    /// </remarks>
    public string HexPreview => BitConverter.ToString(Data).Replace("-", " ");

    /// <summary>
    /// Gets the ASCII representation of the data, with non-printable characters shown as '.'.
    /// </summary>
    public string AsciiPreview => new(Data.Select(b => b >= 32 && b <= 126 ? (char)b : '.').ToArray());

    /// <summary>
    /// Gets the size of this memory block in bytes.
    /// </summary>
    public int Size => Data.Length;

    /// <summary>
    /// Gets the ending address (exclusive) of this memory block.
    /// </summary>
    public uint EndAddress => Address + (uint)Data.Length;

    /// <summary>
    /// Reads a 32-bit unsigned integer from the specified offset in little-endian format.
    /// </summary>
    /// <param name="offset">Offset within the data array.</param>
    /// <returns>The 32-bit unsigned integer value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when offset is invalid.</exception>
    public uint ReadUInt32LE(int offset)
    {
        if (offset < 0 || offset + 4 > Data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        return BinaryPrimitives.ReadUInt32LittleEndian(Data.AsSpan(offset, 4));
    }

    /// <summary>
    /// Reads a 32-bit unsigned integer from the specified offset in big-endian format (common in PLC protocols).
    /// </summary>
    /// <param name="offset">Offset within the data array.</param>
    /// <returns>The 32-bit unsigned integer value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when offset is invalid.</exception>
    public uint ReadUInt32BE(int offset)
    {
        if (offset < 0 || offset + 4 > Data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        return BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(offset, 4));
    }

    /// <summary>
    /// Creates a formatted string representation suitable for hex viewer display.
    /// </summary>
    /// <returns>Formatted string with address, hex data, and ASCII representation.</returns>
    public override string ToString()
    {
        return $"0x{Address:X8}: {HexPreview}  |{AsciiPreview}|";
    }

    /// <inheritdoc/>
    public bool Equals(MemoryBlock other)
    {
        return Address == other.Address && Data.SequenceEqual(other.Data);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is MemoryBlock other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Address, Data.Length);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(MemoryBlock left, MemoryBlock right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(MemoryBlock left, MemoryBlock right)
    {
        return !left.Equals(right);
    }
}
