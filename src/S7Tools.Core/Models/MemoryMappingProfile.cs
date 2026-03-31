using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using S7Tools.Core.Constants;
using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Core.Models;

/// <summary>
/// Represents a collection of memory segments for PLC firmware memory mapping with metadata and validation.
/// </summary>
/// <remarks>
/// Memory mapping profiles provide reusable memory configurations for different PLC models and firmware versions.
/// They implement the unified profile management interface for consistent CRUD operations.
/// </remarks>
public class MemoryMappingProfile : IProfileBase
{
    #region IProfileBase Implementation

    /// <inheritdoc/>
    [Browsable(false)]
    public int Id { get; set; }

    /// <inheritdoc/>
    [Required(ErrorMessage = "Profile name is required")]
    [StringLength(100, ErrorMessage = "Profile name cannot exceed 100 characters")]
    [Display(Name = "Profile Name", Order = 1)]
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    [StringLength(500, ErrorMessage = "Profile description cannot exceed 500 characters")]
    [Display(Name = "Description", Order = 2)]
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc/>
    [Display(Name = "Default Profile", Order = 3)]
    public bool IsDefault { get; set; }

    /// <inheritdoc/>
    [Display(Name = "Read-Only", Order = 4)]
    public bool IsReadOnly { get; set; }

    /// <inheritdoc/>
    [Browsable(false)]
    public string Options { get; set; } = string.Empty;

    /// <inheritdoc/>
    [Browsable(false)]
    public string Flags { get; set; } = string.Empty;

    /// <inheritdoc/>
    [Browsable(false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    [Browsable(false)]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    [Browsable(false)]
    [StringLength(10, ErrorMessage = "Version cannot exceed 10 characters")]
    public string Version { get; set; } = "1.0";

    /// <inheritdoc/>
    [Browsable(false)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }

    #endregion

    #region Memory Region Specific Properties

    /// <summary>
    /// Gets or sets the collection of memory segments that define this profile.
    /// </summary>
    /// <value>A list of memory segments with their address ranges and properties.</value>
    /// <remarks>
    /// Segments must not overlap with each other and must have valid address ranges.
    /// At least one segment must be present for the profile to be valid.
    /// </remarks>
    [Required(ErrorMessage = "At least one memory segment is required")]
    [Display(Name = "Memory Segments")]
    public List<MemorySegment> Segments { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this profile is currently active for job operations.
    /// </summary>
    /// <value>True if the profile is active and selected for operations, false otherwise.</value>
    [Display(Name = "Active Profile")]
    public bool IsActive { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the segments that are currently selected for operations.
    /// </summary>
    /// <value>A collection of segments where IsSelected is true.</value>
    [JsonIgnore]
    [Browsable(false)]
    public IEnumerable<MemorySegment> SelectedSegments
    {
        get { return Segments.Where(s => s.IsSelected); }
    }

    /// <summary>
    /// Gets whether this profile has any selected segments.
    /// </summary>
    /// <value>True if at least one segment is selected, false otherwise.</value>
    [JsonIgnore]
    [Browsable(false)]
    public bool HasSelectedSegments
    {
        get { return Segments.Any(s => s.IsSelected); }
    }

    /// <summary>
    /// Gets the total size of all selected segments in bytes.
    /// </summary>
    /// <value>The sum of sizes for all selected segments.</value>
    [JsonIgnore]
    [Browsable(false)]
    public long TotalSelectedSize
    {
        get { return Segments.Where(s => s.IsSelected).Sum(s => s.Size); }
    }

    /// <summary>
    /// Gets the number of segments in this profile.
    /// </summary>
    /// <value>The total count of segments.</value>
    [JsonIgnore]
    [Browsable(false)]
    public int SegmentCount
    {
        get { return Segments.Count; }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates the default S7Tools memory mapping profile with comprehensive S7-1200 firmware v4.02.01 segments.
    /// </summary>
    /// <returns>A new MemoryMappingProfile instance configured with comprehensive S7-1200 memory mapping.</returns>
    /// <remarks>
    /// The default profile includes complete S7-1200 firmware v4.02.01 segments based on actual firmware analysis.
    /// The .bss segment is pre-selected for typical memory dump operations, with .text, .rodata, and .data
    /// also recommended for comprehensive firmware analysis.
    /// </remarks>
    public static MemoryMappingProfile CreateDefaultProfile()
    {
        return new MemoryMappingProfile
        {
            Id = 1, // Default profile always has ID 1
            Name = "S7-1200 Firmware v4.02.01 Complete",
            Description = "Complete firmware memory mapping for Siemens S7-1200 v4.02.01 (6ES7212-1AE40-OXBO).",
            Segments = new List<MemorySegment>
            {
                // Executive and System Segments
                new()
                {
                    Name = ".exec_in_lomem",
                    StartAddress = "0x00000000",
                    Size = 30132,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Executive code in low memory"
                },
                new()
                {
                    Name = ".bitable",
                    StartAddress = "0x00040000",
                    Size = 64,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Binary table segment"
                },
                new()
                {
                    Name = ".sdramexec",
                    StartAddress = "0x00040040",
                    Size = 1232,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "SDRAM executive segment"
                },
                new()
                {
                    Name = ".syscall",
                    StartAddress = "0x00040540",
                    Size = 8,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "System call segment"
                },
                new()
                {
                    Name = ".th_initial",
                    StartAddress = "0x00041040",
                    Size = 10584,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Thread initialization segment"
                },
                new()
                {
                    Name = ".secinfo",
                    StartAddress = "0x000439c0",
                    Size = 828,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Security information segment"
                },
                new()
                {
                    Name = ".fixaddr",
                    StartAddress = "0x00043d00",
                    Size = 0,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Fixed address segment"
                },
                new()
                {
                    Name = ".fixtype",
                    StartAddress = "0x00043d00",
                    Size = 0,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Fixed type segment"
                },

                // Core Firmware Segments (Recommended for analysis)
                new()
                {
                    Name = ".text",
                    StartAddress = "0x00043d00",
                    Size = 14139040, // ~13.5 MB
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Code/text segment - main executable code (RECOMMENDED)"
                },
                new()
                {
                    Name = ".rodata",
                    StartAddress = "0x00defdc0",
                    Size = 3871660, // ~3.7 MB
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Read-only data segment - constants and strings (RECOMMENDED)"
                },
                new()
                {
                    Name = ".data",
                    StartAddress = "0x011a1180",
                    Size = 133012, // ~130 KB
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Initialized data segment - global variables (RECOMMENDED)"
                },
                new()
                {
                    Name = ".bss",
                    StartAddress = "0x01e01040",
                    Size = 8519448, // ~8.1 MB
                    Type = MemorySegmentType.RAM,
                    IsSelected = true,
                    Description = "Uninitialized data segment (BSS) - zero-initialized variables (DEFAULT SELECTION)"
                },

                // Memory Pool and Cache Segments
                new()
                {
                    Name = ".cc_memory",
                    StartAddress = "0x03641040",
                    Size = 0,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "Cache coherent memory"
                },
                new()
                {
                    Name = ".uninitialized",
                    StartAddress = "0x03c41040",
                    Size = 54186228, // ~51.7 MB
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Uninitialized memory pool - heap space"
                },
                new()
                {
                    Name = "CLSI_CACHED_MEM_POOL",
                    StartAddress = "0x06fac940",
                    Size = 0,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "CLSI cached memory pool"
                },
                new()
                {
                    Name = ".dram_uncache",
                    StartAddress = "0x07ff0000",
                    Size = 0,
                    Type = MemorySegmentType.Flash,
                    IsSelected = false,
                    Description = "DRAM uncached segment"
                },

                // Hardware Interface Segments
                new()
                {
                    Name = "MAP_MAC_MEM",
                    StartAddress = "0x07ff0000",
                    Size = 1172,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "MAC memory mapping - network interface"
                },

                // Internal RAM Segments
                new()
                {
                    Name = ".iram0",
                    StartAddress = "0x10030000",
                    Size = 31392,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Internal RAM 0 - fast access memory"
                },
                new()
                {
                    Name = ".iram1",
                    StartAddress = "0x10040000",
                    Size = 49756,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Internal RAM 1 - additional fast memory"
                },
                new()
                {
                    Name = ".crctable",
                    StartAddress = "0x10041400",
                    Size = 1024,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "CRC table segment"
                },

                // Boot Segments
                new()
                {
                    Name = ".softboot",
                    StartAddress = "0x10041800",
                    Size = 1792,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Soft boot segment - boot loader code"
                },
                new()
                {
                    Name = ".bootinfo",
                    StartAddress = "0x1004ff00",
                    Size = 28,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Boot information segment - boot parameters"
                },
                new()
                {
                    Name = ".dtcm",
                    StartAddress = "0x10010000",
                    Size = 11376,
                    Type = MemorySegmentType.RAM,
                    IsSelected = false,
                    Description = "Data tightly coupled memory - processor cache"
                }
            },
            IsDefault = true,
            IsReadOnly = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "System",
                ["Purpose"] = "S7Tools Memory Region Management",
                ["Author"] = "S7Tools",
                ["Modifiable"] = "False",
                ["DefaultSegment"] = ".bss",
                ["PLCModel"] = "6ES7212-1AE40-OXBO",
                ["FirmwareVersion"] = "v4.02.01",
                ["TotalSegments"] = "23",
                ["RecommendedSegments"] = ".text,.rodata,.data,.bss",
                ["Source"] = "Actual S7-1200 firmware analysis"
            }
        };
    }

    /// <summary>
    /// Creates a new user profile with basic firmware segments.
    /// </summary>
    /// <param name="name">The name for the new profile.</param>
    /// <param name="description">Optional description for the profile.</param>
    /// <returns>A new MemoryMappingProfile instance with default firmware segments.</returns>
    public static MemoryMappingProfile CreateUserProfile(string name, string description = "")
    {
        return new MemoryMappingProfile
        {
            Id = 0, // Will be assigned by the service
            Name = name,
            Description = description,
            Segments = new List<MemorySegment>
            {
                new()
                {
                    Name = ".text",
                    StartAddress = "0x08000000",
                    Size = 128 * 1024,
                    Type = MemorySegmentType.Flash,
                    Description = "Program code section"
                },
                new()
                {
                    Name = ".data",
                    StartAddress = MemoryConstants.DefaultUserMemoryStartHex,
                    Size = 16 * 1024,
                    Type = MemorySegmentType.RAM,
                    Description = "Initialized data section"
                },
                new()
                {
                    Name = ".bss",
                    StartAddress = "0x20004000",
                    Size = 16 * 1024,
                    Type = MemorySegmentType.RAM,
                    IsSelected = true,
                    Description = "Uninitialized data section"
                }
            },
            IsDefault = false,
            IsReadOnly = false,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = "1.0",
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "User",
                ["Author"] = Environment.UserName
            }
        };
    }

    #endregion

    #region Validation and Business Logic

    /// <summary>
    /// Validates this memory region profile and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Basic profile validation
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Profile name is required");
        }
        else if (Name.Length > 100)
        {
            errors.Add("Profile name cannot exceed 100 characters");
        }

        if (!string.IsNullOrEmpty(Description) && Description.Length > 500)
        {
            errors.Add("Profile description cannot exceed 500 characters");
        }

        // Memory segment validation
        if (Segments == null || Segments.Count == 0)
        {
            errors.Add("At least one memory segment is required");
        }
        else
        {
            // Validate individual segments
            for (int i = 0; i < Segments.Count; i++)
            {
                MemorySegment segment = Segments[i];
                if (!segment.IsValid())
                {
                    errors.Add($"Segment {i + 1} ('{segment.Name}') has invalid properties");
                }
            }

            // Check for overlapping segments
            for (int i = 0; i < Segments.Count; i++)
            {
                for (int j = i + 1; j < Segments.Count; j++)
                {
                    try
                    {
                        if (Segments[i].OverlapsWith(Segments[j]))
                        {
                            errors.Add($"Segments '{Segments[i].Name}' and '{Segments[j].Name}' have overlapping address ranges");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to validate overlap between '{Segments[i].Name}' and '{Segments[j].Name}': {ex.Message}");
                    }
                }
            }

            // Check for duplicate segment names
            IEnumerable<string> duplicateNames = Segments
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (string duplicateName in duplicateNames)
            {
                errors.Add($"Duplicate segment name: '{duplicateName}'");
            }
        }

        return errors;
    }

    /// <summary>
    /// Determines whether any selected segments form a contiguous (correlative) memory range.
    /// </summary>
    /// <returns>True if selected segments are contiguous, false otherwise.</returns>
    /// <remarks>
    /// This validation is used in job wizard to ensure correlative segment selection
    /// for certain memory dump operations that require contiguous address ranges.
    /// </remarks>
    public bool HasContiguousSelection()
    {
        var selected = Segments.Where(s => s.IsSelected).ToList();

        if (selected.Count <= 1)
        {
            return true; // Single or no selection is always contiguous
        }

        try
        {
            // Sort by start address
            selected.Sort((a, b) => MemorySegment.ParseAddress(a.StartAddress).CompareTo(MemorySegment.ParseAddress(b.StartAddress)));

            // Check if each segment ends where the next begins
            for (int i = 0; i < selected.Count - 1; i++)
            {
                long currentEnd = MemorySegment.ParseAddress(selected[i].StartAddress) + selected[i].Size;
                long nextStart = MemorySegment.ParseAddress(selected[i + 1].StartAddress);

                if (currentEnd != nextStart)
                {
                    return false; // Gap found
                }
            }

            return true;
        }
        catch
        {
            return false; // Address parsing failed
        }
    }

    #endregion

    #region IProfileBase Implementation

    /// <inheritdoc/>
    public bool CanModify()
    {
        return !IsReadOnly;
    }

    /// <inheritdoc/>
    public bool CanDelete()
    {
        return !IsReadOnly && !IsDefault;
    }

    /// <inheritdoc/>
    public void Touch()
    {
        ModifiedAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public string GetSummary()
    {
        string summary = $"{Name}: {SegmentCount} segments";

        int selectedCount = Segments.Count(s => s.IsSelected);
        if (selectedCount > 0)
        {
            summary += $" ({selectedCount} selected)";
        }

        if (IsDefault)
        {
            summary += " (Default)";
        }

        if (IsReadOnly)
        {
            summary += " (Read-Only)";
        }

        if (IsActive)
        {
            summary += " (Active)";
        }

        return summary;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a deep copy of this profile while preserving the profile Id and flags.
    /// </summary>
    /// <returns>A new MemoryMappingProfile instance with identical settings including Id.</returns>
    public MemoryMappingProfile ClonePreserveId()
    {
        return new MemoryMappingProfile
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Segments = Segments.Select(s => s.Clone()).ToList(),
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            IsActive = IsActive,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Version = Version,
            Options = Options,
            Flags = Flags,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Creates a copy of this profile with a new name.
    /// </summary>
    /// <param name="newName">The name for the duplicated profile.</param>
    /// <returns>A new MemoryMappingProfile instance with the specified name.</returns>
    public MemoryMappingProfile Duplicate(string newName)
    {
        return new MemoryMappingProfile
        {
            Id = 0, // Will be assigned by the service
            Name = newName,
            Description = Description,
            Segments = Segments.Select(s => s.Clone()).ToList(),
            IsDefault = false, // Duplicates are never default
            IsReadOnly = false, // Duplicates are never read-only
            IsActive = false, // Duplicates start inactive
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            Version = Version,
            Options = Options,
            Flags = Flags,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    /// <summary>
    /// Creates a clone of this profile.
    /// </summary>
    /// <returns>A new MemoryMappingProfile instance with new ID to be assigned.</returns>
    public MemoryMappingProfile Clone()
    {
        return new MemoryMappingProfile
        {
            Id = 0, // Will be assigned by the service
            Name = Name,
            Description = Description,
            Segments = Segments.Select(s => s.Clone()).ToList(),
            IsDefault = IsDefault,
            IsReadOnly = IsReadOnly,
            IsActive = IsActive,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Version = Version,
            Options = Options,
            Flags = Flags,
            Metadata = Metadata != null ? new Dictionary<string, string>(Metadata) : null
        };
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of this memory region profile.
    /// </summary>
    /// <returns>A formatted string describing the profile.</returns>
    public override string ToString()
    {
        return GetSummary();
    }

    #endregion
}
