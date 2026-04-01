using System.ComponentModel.DataAnnotations;

namespace S7Tools.Core.Models.Jobs;

/// <summary>
/// Defines a memory region configuration for dumping operations.
/// Specifies the start address and size of memory to extract.
/// </summary>
/// <param name="StartAddress">Starting memory address as hex string (e.g., "0x01e01040").</param>
/// <param name="Size">Size of memory region to dump (in bytes).</param>
public sealed record MemoryRegionProfile(
    [property: Display(Name = "Start Address", Order = 1)]
    string StartAddress,
    [property: Display(Name = "Size (bytes)", Order = 2)]
    uint Size
)
{
    /// <summary>
    /// Gets the start address as a uint value (parsed from hex string).
    /// </summary>
    public uint Start
    {
        get
        {
            try
            {
                string addressStr = StartAddress.Replace("0x", "").Replace("0X", "");
                return Convert.ToUInt32(addressStr, 16);
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Gets the length (alias for Size for backward compatibility).
    /// </summary>
    public uint Length => Size;
};
