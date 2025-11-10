# Data Model: Memory Regions Profiling System

**Feature**: 008-memory-regions-profiling
**Date**: 2025-11-07
**Status**: Complete

## Core Entities

### MemoryRegionProfile

Represents a collection of memory segments for PLC firmware memory mapping with metadata and validation.

**Fields**:
- `Id: Guid` - Unique identifier for the profile
- `Name: string` - User-friendly name for the profile
- `Description: string` - Optional detailed description
- `CreatedAt: DateTime` - Creation timestamp
- `UpdatedAt: DateTime` - Last modification timestamp
- `Segments: List<MemorySegment>` - Collection of memory segments
- `IsActive: bool` - Whether profile is currently active/selected

**Validation Rules**:
- Name must be non-empty and unique across all profiles
- Segments collection must contain at least one valid segment
- No two segments can have overlapping memory ranges
- Profile must have at least one selected segment for job execution

**Relationships**:
- Implements `IProfileBase` interface for unified profile management
- Used by `JobProfile` for memory region configuration
- Managed by `MemoryRegionProfileService` using `StandardProfileManager<T>`

**Business Rules**:
- Default profile created with standard firmware segments (.text, .data, .bss)
- Profile can be duplicated with auto-incremented naming (e.g., "Profile Copy", "Profile Copy (2)")
- Deletion requires confirmation if profile is referenced by existing jobs

### MemorySegment

Represents a single memory segment with address range, type classification, and selection state.

**Fields**:
- `Name: string` - Segment identifier (e.g., ".text", ".data", ".bss")
- `StartAddress: string` - Hexadecimal start address (e.g., "0x08000000")
- `Size: long` - Segment size in bytes
- `Type: MemorySegmentType` - Classification of memory type
- `IsSelected: bool` - Whether segment is selected for operations
- `Description: string` - Optional segment description

**Derived Properties**:
- `EndAddress: long` - Calculated end address (StartAddress + Size - 1)
- `SizeFormatted: string` - Human-readable size (e.g., "128 KB", "16 MB")
- `AddressRange: string` - Display format (e.g., "0x08000000 - 0x0801FFFF")

**Validation Rules**:
- Name must be non-empty and unique within profile
- StartAddress must be valid hexadecimal with optional 0x prefix
- Size must be positive and within realistic bounds (1 byte to 1 GB)
- Segments cannot overlap with other segments in the same profile

**Business Rules**:
- Address parsing supports both "0x08000000" and "08000000" formats
- Size validation prevents unrealistic memory allocations
- Overlap detection ensures memory map integrity

### MemorySegmentType

Enumeration defining the classification of memory segments based on hardware characteristics.

**Values**:
- `Flash = 0` - Non-volatile flash memory (program code, constants)
- `RAM = 1` - Volatile random access memory (data, stack, heap)
- `EEPROM = 2` - Electrically erasable programmable read-only memory
- `ROM = 3` - Read-only memory (firmware, boot code)

**Business Rules**:
- Default templates use Flash for .text segment, RAM for .data/.bss
- Type affects validation rules (e.g., Flash typically larger than EEPROM)
- Used for display styling and operation filtering

### MemoryRegionProfileOptions

Configuration options for memory region profile management and storage.

**Fields**:
- `ProfilesFilePath: string` - Path to profiles storage JSON file
- `DefaultProfileName: string` - Name for auto-created default profile
- `MaxProfileCount: int` - Maximum allowed profiles (default: 100)
- `ValidationEnabled: bool` - Whether to enforce segment validation
- `AutoSelectBssSegment: bool` - Whether to auto-select .bss segment by default

**Validation Rules**:
- ProfilesFilePath must be a valid, writable file path
- MaxProfileCount must be positive and reasonable (1-1000)

**Default Values**:
- ProfilesFilePath: "src/resources/MemoryRegionProfiles/profiles.json"
- DefaultProfileName: "Default Memory Regions"
- MaxProfileCount: 100
- ValidationEnabled: true
- AutoSelectBssSegment: true

## State Transitions

### Profile Lifecycle

```
[New] → [Draft] → [Valid] → [Active]
   ↓        ↓         ↓        ↓
   ↓        ↓         ↓     [Inactive]
   ↓        ↓         ↓        ↓
   ↓        ↓      [Invalid] ←--
   ↓        ↓         ↓
   ↓     [Deleted] ←---
   ↓        ↓
[Deleted] ←--
```

**States**:
- **New**: Profile created but not yet saved
- **Draft**: Profile saved but may have validation issues
- **Valid**: Profile passes all validation rules
- **Active**: Profile selected for job operations
- **Inactive**: Valid profile not currently selected
- **Invalid**: Profile has validation errors that must be resolved
- **Deleted**: Profile marked for removal (soft delete)

### Segment Selection

```
[Unselected] ↔ [Selected]
      ↓            ↓
   [Hidden] ← [Validated] → [Error]
```

**States**:
- **Unselected**: Segment available but not chosen for operations
- **Selected**: Segment marked for memory operations
- **Validated**: Selected segment passes overlap and range validation
- **Error**: Selected segment has validation issues
- **Hidden**: Segment temporarily hidden from display (filtering)

## Integration Points

### Unified Profile Management

**Interface Implementation**:
```csharp
public class MemoryRegionProfile : IProfileBase
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Service Registration**:
```csharp
services.TryAddSingleton<IMemoryRegionProfileService, MemoryRegionProfileService>();
services.Configure<MemoryRegionProfileOptions>(configuration.GetSection("MemoryRegionProfiles"));
```

### Job System Integration

**JobProfile Extension**:
```csharp
public class JobProfile
{
    // Existing fields...
    public Guid? MemoryRegionProfileId { get; set; }

    // Navigation property (not serialized)
    [JsonIgnore]
    public MemoryRegionProfile? MemoryRegionProfile { get; set; }
}
```

**Job Execution Context**:
- Memory region profile resolved during job preparation
- Selected segments determine memory dump scope
- Address validation prevents invalid memory access

### ViewModels Integration

**Profile Management**:
```csharp
public class MemoryRegionProfilesViewModel : ProfileManagementViewModelBase<MemoryRegionProfile>
{
    public ObservableCollection<MemorySegment> SelectedSegments =>
        SelectedProfile?.Segments?.Where(s => s.IsSelected).ToObservableCollection();

    public bool HasValidSelection =>
        SelectedProfile?.Segments?.Any(s => s.IsSelected) == true;
}
```

**Dialog ViewModels**:
- CreateMemoryRegionProfileViewModel: Profile creation with template selection
- EditMemoryRegionProfileViewModel: Profile modification with validation
- DuplicateMemoryRegionProfileViewModel: Profile duplication with name conflict resolution

## Data Persistence

### JSON Storage Format

```json
{
  "profiles": [
    {
      "id": "12345678-1234-1234-1234-123456789abc",
      "name": "STM32F4 Memory Map",
      "description": "Standard memory layout for STM32F4 series",
      "createdAt": "2025-11-07T10:00:00Z",
      "updatedAt": "2025-11-07T10:00:00Z",
      "isActive": true,
      "segments": [
        {
          "name": ".text",
          "startAddress": "0x08000000",
          "size": 131072,
          "type": 0,
          "isSelected": true,
          "description": "Program code section"
        },
        {
          "name": ".data",
          "startAddress": "0x20000000",
          "size": 16384,
          "type": 1,
          "isSelected": false,
          "description": "Initialized data section"
        },
        {
          "name": ".bss",
          "startAddress": "0x20004000",
          "size": 16384,
          "type": 1,
          "isSelected": true,
          "description": "Uninitialized data section"
        }
      ]
    }
  ]
}
```

### Firmware Memory Mapping Template (S7-1200 v4.02.01)

**Reference Template for Import/Export**: Based on actual S7-1200 firmware memory mapping from 0x000439C0

```json
{
  "profileTemplate": {
    "id": "s7-1200-v4-template",
    "name": "S7-1200 Firmware v4.02.01 Complete",
    "description": "Complete firmware memory mapping for S7-1200 v4.02.01 (6ES7212-1AE40-OXBO)",
    "plcModel": "6ES7212-1AE40-OXBO",
    "firmwareVersion": "v4.02.01",
    "isTemplate": true,
    "segments": [
      {
        "name": ".exec_in_lomem",
        "startAddress": "0x00000000",
        "size": 30132,
        "type": 0,
        "flags": 1,
        "description": "Executive code in low memory"
      },
      {
        "name": ".bitable",
        "startAddress": "0x00040000",
        "size": 64,
        "type": 0,
        "flags": 1,
        "description": "Binary table segment"
      },
      {
        "name": ".th_initial",
        "startAddress": "0x00041040",
        "size": 10584,
        "type": 0,
        "flags": 33,
        "description": "Thread initialization segment"
      },
      {
        "name": ".text",
        "startAddress": "0x00043d00",
        "size": 14139040,
        "type": 0,
        "flags": 33,
        "description": "Code/text segment",
        "isSelected": true
      },
      {
        "name": ".rodata",
        "startAddress": "0x000defdc0",
        "size": 3871660,
        "type": 1,
        "flags": 34,
        "description": "Read-only data segment",
        "isSelected": true
      },
      {
        "name": ".data",
        "startAddress": "0x0111a1f80",
        "size": 133012,
        "type": 1,
        "flags": 42,
        "description": "Initialized data segment",
        "isSelected": true
      },
      {
        "name": ".bss",
        "startAddress": "0x01fe01040",
        "size": 8519448,
        "type": 1,
        "flags": 12,
        "description": "Uninitialized data segment (BSS)",
        "isSelected": true
      },
      {
        "name": ".uninitialized",
        "startAddress": "0x03c41040",
        "size": 54186228,
        "type": 1,
        "flags": 12,
        "description": "Uninitialized memory pool"
      },
      {
        "name": "MAP_MAC_MEM",
        "startAddress": "0x07ff0000",
        "size": 1172,
        "type": 2,
        "flags": 12,
        "description": "MAC memory mapping"
      },
      {
        "name": ".iram0",
        "startAddress": "0x10030000",
        "size": 31392,
        "type": 2,
        "flags": 12,
        "description": "Internal RAM 0"
      },
      {
        "name": ".iram1",
        "startAddress": "0x10040000",
        "size": 49756,
        "type": 2,
        "flags": 12,
        "description": "Internal RAM 1"
      }
    ],
    "defaultSegments": [".bss"],
    "recommendedSegments": [".text", ".rodata", ".data", ".bss"],
    "memoryLayout": {
      "description": "Standard S7-1200 firmware layout with executable and data regions",
      "totalAddressSpace": "0x10012e70",
      "notes": "Default .bss segment selection for typical memory dumps"
    }
  }
}
```

### File Management

**Storage Strategy**:
- Single JSON file per configuration (default: profiles.json)
- Atomic write operations with backup during updates
- Automatic directory creation if path doesn't exist
- File locking during read/write operations

**Error Handling**:
- Graceful degradation if file doesn't exist (create default)
- Validation on load with detailed error reporting
- Backup restoration on corruption detection
- Structured logging for all file operations

## Validation Framework

### Profile Validation

**Rules Engine**:
```csharp
public class MemoryRegionProfileValidator
{
    public ValidationResult Validate(MemoryRegionProfile profile)
    {
        var result = new ValidationResult();

        ValidateName(profile.Name, result);
        ValidateSegments(profile.Segments, result);
        ValidateSelection(profile.Segments, result);

        return result;
    }

    private void ValidateSegments(List<MemorySegment> segments, ValidationResult result)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            for (int j = i + 1; j < segments.Count; j++)
            {
                if (segments[i].OverlapsWith(segments[j]))
                {
                    result.AddError($"Segments '{segments[i].Name}' and '{segments[j].Name}' overlap");
                }
            }
        }
    }
}
```

**Validation Points**:
- Profile creation/update (immediate validation)
- Job wizard step validation (ensure selection)
- File load validation (corruption detection)
- UI input validation (real-time feedback)

## Performance Considerations

### Memory Management

**Profile Loading**:
- Lazy loading of profiles (load on demand)
- Caching of frequently accessed profiles
- Disposal pattern for large profile collections

**Segment Operations**:
- Efficient overlap detection using sorted address ranges
- Batch validation for multiple segment changes
- Reactive updates for UI responsiveness

### Scalability

**Limits**:
- Maximum 100 profiles per configuration (configurable)
- Maximum 50 segments per profile
- Address range validation for 64-bit address space
- File size monitoring (warn if JSON exceeds 10 MB)

**Optimization**:
- Index profiles by ID for fast lookup
- Cache validation results until profile changes
- Debounce UI updates during rapid changes

## Security Considerations

### Data Validation

**Input Sanitization**:
- Hex address validation prevents code injection
- File path validation prevents directory traversal
- Size limits prevent memory exhaustion attacks

**Access Control**:
- File permissions validation before write operations
- User confirmation for destructive operations (delete, overwrite)
- Audit logging for profile management operations

### Data Integrity

**Consistency**:
- Atomic file operations prevent partial writes
- Checksum validation for critical data
- Backup and recovery mechanisms

**Concurrency**:
- File locking prevents concurrent modification
- Optimistic concurrency for UI updates
- Transaction boundaries for multi-step operations
