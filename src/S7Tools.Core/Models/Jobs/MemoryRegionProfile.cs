namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Defines a memory region configuration for dumping operations.
/// Specifies the start address and length of memory to extract.
/// </summary>
/// <param name="Start">Starting memory address (in bytes).</param>
/// <param name="Length">Length of memory region to dump (in bytes).</param>
public sealed record MemoryRegionProfile(
    uint Start,
    uint Length
);
