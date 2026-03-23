using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents a single memory segment with address range, type classification, and selection state.
/// </summary>
/// <remarks>
/// Memory segments define contiguous memory regions within a firmware memory map.
/// They support overlap detection, address validation, and provide formatted display properties.
/// </remarks>
public class MemorySegment : INotifyPropertyChanged
{
    #region INotifyPropertyChanged Implementation

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
    #region Fields

    private bool _isSelected;
    private string _name = string.Empty;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the segment identifier.
    /// </summary>
    /// <value>A unique name within the profile (e.g., ".text", ".data", ".bss").</value>
    [Required(ErrorMessage = "Segment name is required")]
    [StringLength(50, ErrorMessage = "Segment name cannot exceed 50 characters")]
    [Display(Name = "Segment Name")]
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>
    /// Gets or sets the hexadecimal start address.
    /// </summary>
    /// <value>The start address in hexadecimal format (e.g., "0x08000000" or "08000000").</value>
    [Required(ErrorMessage = "Start address is required")]
    [Display(Name = "Start Address")]
    public string StartAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the segment size in bytes.
    /// </summary>
    /// <value>The size of the memory segment in bytes. Must be non-negative (0 or positive).</value>
    [Range(0, long.MaxValue, ErrorMessage = "Size must be non-negative")]
    [Display(Name = "Size (bytes)")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the memory type classification.
    /// </summary>
    /// <value>The type of memory (Flash, RAM, EEPROM, ROM).</value>
    [Display(Name = "Memory Type")]
    public MemorySegmentType Type { get; set; } = MemorySegmentType.Flash;

    /// <summary>
    /// Gets or sets a value indicating whether this segment is selected for operations.
    /// </summary>
    /// <value>True if the segment is selected for memory operations, false otherwise.</value>
    [Display(Name = "Selected")]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    /// <summary>
    /// Gets or sets an optional description of the segment.
    /// </summary>
    /// <value>A description explaining the purpose or contents of this segment.</value>
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the calculated end address of this segment.
    /// </summary>
    /// <value>The end address (StartAddress + Size - 1).</value>
    [JsonIgnore]
    [Browsable(false)]
    public long EndAddress
    {
        get
        {
            try
            {
                return ParseAddress(StartAddress) + Size - 1;
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Gets the human-readable size representation.
    /// </summary>
    /// <value>The size formatted as "128 KB", "16 MB", etc.</value>
    [JsonIgnore]
    [Browsable(false)]
    public string SizeFormatted
    {
        get
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            return Size switch
            {
                >= GB => $"{Size / (double)GB:F1} GB",
                >= MB => $"{Size / (double)MB:F1} MB",
                >= KB => $"{Size / (double)KB:F1} KB",
                _ => $"{Size} bytes"
            };
        }
    }

    /// <summary>
    /// Gets the display representation of the address range.
    /// </summary>
    /// <value>The range formatted as "0x08000000 - 0x0801FFFF".</value>
    [JsonIgnore]
    [Browsable(false)]
    public string AddressRange
    {
        get
        {
            try
            {
                long start = ParseAddress(StartAddress);
                long end = start + Size - 1;
                return $"0x{start:X8} - 0x{end:X8}";
            }
            catch
            {
                return "Invalid address";
            }
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Determines whether this segment overlaps with another segment.
    /// </summary>
    /// <param name="other">The other segment to check against.</param>
    /// <returns>True if the segments overlap, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when address parsing fails.</exception>
    /// <remarks>
    /// Segments with size 0 never overlap (they are markers/placeholders).
    /// Overlap check uses exclusive end boundary: [start, start+size)
    /// </remarks>
    public bool OverlapsWith(MemorySegment other)
    {
        ArgumentNullException.ThrowIfNull(other);

        try
        {
            // Size 0 segments never overlap - they are just markers/placeholders
            if (Size == 0 || other.Size == 0)
            {
                return false;
            }

            long thisStart = ParseAddress(StartAddress);
            long thisEnd = thisStart + Size;
            long otherStart = ParseAddress(other.StartAddress);
            long otherEnd = otherStart + other.Size;

            return thisStart < otherEnd && otherStart < thisEnd;
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to compare segment '{Name}' with '{other.Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates that this segment has valid properties.
    /// </summary>
    /// <returns>True if the segment is valid, false otherwise.</returns>
    public bool IsValid()
    {
        try
        {
            // Check required properties
            if (string.IsNullOrWhiteSpace(Name))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(StartAddress))
            {
                return false;
            }

            if (Size < 0) // Size can be 0 (empty segment) or positive
            {
                return false;
            }

            // Check address format
            ParseAddress(StartAddress);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses a hexadecimal address string into a numeric value.
    /// </summary>
    /// <param name="address">The address string to parse (supports "0x" prefix).</param>
    /// <returns>The numeric address value.</returns>
    /// <exception cref="ArgumentException">Thrown when the address format is invalid.</exception>
    /// <exception cref="FormatException">Thrown when the hexadecimal parsing fails.</exception>
    public static long ParseAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address cannot be null or empty", nameof(address));
        }

        // Remove 0x prefix if present
        string cleanAddress = address.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? address[2..]
            : address;

        if (string.IsNullOrEmpty(cleanAddress))
        {
            throw new ArgumentException("Address cannot be empty after removing prefix", nameof(address));
        }

        try
        {
            return Convert.ToInt64(cleanAddress, 16);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Invalid hexadecimal address format: '{address}'", ex);
        }
    }

    /// <summary>
    /// Creates a formatted address string from a numeric value.
    /// </summary>
    /// <param name="address">The numeric address value.</param>
    /// <returns>The formatted address string with "0x" prefix.</returns>
    public static string FormatAddress(long address)
    {
        return $"0x{address:X8}";
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of this memory segment.
    /// </summary>
    /// <returns>A formatted string describing the segment.</returns>
    public override string ToString()
    {
        return $"{Name}: {AddressRange} ({SizeFormatted}, {Type})";
    }

    /// <summary>
    /// Determines whether the specified object is equal to this segment.
    /// </summary>
    /// <param name="obj">The object to compare with this segment.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return obj is MemorySegment other &&
               string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(StartAddress, other.StartAddress, StringComparison.OrdinalIgnoreCase) &&
               Size == other.Size &&
               Type == other.Type;
    }

    /// <summary>
    /// Returns a hash code for this segment.
    /// </summary>
    /// <returns>A hash code based on the segment's key properties.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name?.ToLowerInvariant(),
            StartAddress?.ToLowerInvariant(),
            Size,
            Type);
    }

    /// <summary>
    /// Creates a deep copy of this memory segment.
    /// </summary>
    /// <returns>A new MemorySegment instance with the same values.</returns>
    public MemorySegment Clone()
    {
        return new MemorySegment
        {
            Name = Name,
            StartAddress = StartAddress,
            Size = Size,
            Type = Type,
            IsSelected = IsSelected,
            Description = Description
        };
    }

    #endregion
}
