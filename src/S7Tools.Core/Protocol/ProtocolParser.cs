using System.Buffers;
using S7Tools.Core.Models;

namespace S7Tools.Core.Protocol;

/// <summary>
/// High-performance protocol parser for PLC memory dump streams.
/// Uses System.Buffers for zero-copy parsing of fragmented network data.
/// </summary>
public static class ProtocolParser
{
    /// <summary>
    /// Default block size for memory dump parsing (16 bytes for hex viewer alignment).
    /// </summary>
    public const int DefaultBlockSize = 16;

    /// <summary>
    /// Parses raw binary memory dump data into MemoryBlock instances.
    /// </summary>
    /// <param name="buffer">The input buffer containing potentially fragmented PLC data.</param>
    /// <param name="currentAddress">Reference to current memory address (updated as blocks are parsed).</param>
    /// <param name="blockSize">Size of each memory block in bytes.</param>
    /// <param name="parsedBlocks">Collection to receive parsed memory blocks.</param>
    /// <returns>The sequence position up to which data was successfully consumed.</returns>
    /// <remarks>
    /// This method is designed to handle fragmented network streams where complete blocks
    /// may arrive split across multiple TCP segments. It uses SequenceReader for efficient
    /// traversal of ReadOnlySequence without copying data until necessary.
    /// </remarks>
    public static SequencePosition ParseMemoryDump(
        ReadOnlySequence<byte> buffer,
        ref uint currentAddress,
        int blockSize,
        ICollection<MemoryBlock> parsedBlocks)
    {
        ArgumentNullException.ThrowIfNull(parsedBlocks);

        if (blockSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be positive");
        }

        SequenceReader<byte> reader = new(buffer);

        // Parse complete blocks while we have enough data
        while (reader.Remaining >= blockSize)
        {
            // Extract the block data
            ReadOnlySequence<byte> blockSequence = reader.Sequence.Slice(reader.Position, blockSize);

            // Convert to array for storage (this is the boundary where we accept one allocation
            // per block for UI consumption - the data must be owned independently)
            byte[] blockData = blockSequence.ToArray();

            MemoryBlock block = new(currentAddress, blockData);
            parsedBlocks.Add(block);

            // Update state
            currentAddress += (uint)blockSize;
            reader.Advance(blockSize);
        }

        // Return the position up to which we successfully consumed data
        return reader.Position;
    }

    /// <summary>
    /// Parses memory dump data with a protocol header.
    /// </summary>
    /// <param name="buffer">Input buffer containing header and data.</param>
    /// <param name="currentAddress">Reference to current memory address.</param>
    /// <param name="parsedBlocks">Collection to receive parsed blocks.</param>
    /// <param name="headerPattern">Optional magic header bytes to validate (e.g., [0xAA, 0x55]).</param>
    /// <returns>Sequence position up to which data was consumed.</returns>
    /// <remarks>
    /// For more complex protocols that include framing headers, this variant validates
    /// header patterns before processing the payload.
    /// </remarks>
    public static SequencePosition ParseMemoryDumpWithHeader(
        ReadOnlySequence<byte> buffer,
        ref uint currentAddress,
        ICollection<MemoryBlock> parsedBlocks,
        ReadOnlySpan<byte> headerPattern)
    {
        SequenceReader<byte> reader = new(buffer);

        // Simple header validation (can be extended for more complex protocols)
        if (!headerPattern.IsEmpty)
        {
            if (reader.Remaining < headerPattern.Length)
            {
                return reader.Position; // Not enough data for header
            }

            Span<byte> headerBuffer = stackalloc byte[headerPattern.Length];
            if (!reader.TryCopyTo(headerBuffer) || !headerBuffer.SequenceEqual(headerPattern))
            {
                // Header mismatch - skip this byte and try to resync
                reader.Advance(1);
                return reader.Position;
            }

            reader.Advance(headerPattern.Length);
        }

        // After header validation, parse the data portion
        ReadOnlySequence<byte> dataSequence = reader.Sequence.Slice(reader.Position);
        SequencePosition consumed = ParseMemoryDump(dataSequence, ref currentAddress, DefaultBlockSize, parsedBlocks);

        // Adjust consumed position relative to original buffer
        return buffer.GetPosition(reader.Consumed + dataSequence.GetOffset(consumed));
    }
}
