using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Defines a memory region configuration for dumping operations.
/// Specifies the start address and length of memory to extract.
/// </summary>
/// <param name="Start">Starting memory address (in bytes).</param>
/// <param name="Length">Length of memory region to dump (in bytes).</param>
public sealed record MemoryRegionProfile(
    [property: Display(Name = "Start Address (bytes)", Order = 1)]
    uint Start,
    [property: Display(Name = "Length (bytes)", Order = 2)]
    uint Length
);
