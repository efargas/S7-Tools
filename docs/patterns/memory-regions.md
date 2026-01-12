---
title: "Memory Region Profiling Pattern"
version: "1.0.1"
created: "2025-11-12"
last-updated: "2025-11-22"
status: "current"
tags: ["memory", "profiling", "plc", "bootloader", "job-wizard", "pattern"]
related:
  - "docs/patterns/profile-management.md"
  - "docs/guides/development-workflow.md"
---

# Memory Region Profiling Pattern

## Overview

Memory Region Profiling provides a flexible system for defining, managing, and selecting PLC memory dump configurations. It enables users to create reusable memory region templates with segment definitions, validation, and profile-based selection.

## Purpose

The Memory Region Profile system addresses the need for:

1. **Reusable Memory Configurations**: Define memory layouts once, use in multiple jobs
2. **Segment-Based Organization**: Organize memory dumps by logical segments (.text, .data, .bss, etc.)
3. **Validation**: Prevent overlapping segments and invalid address ranges
4. **Template Library**: Pre-built templates for common S7-1200 memory layouts
5. **Job Wizard Integration**: Seamless memory region selection during job creation

## Architecture

### Core Components

```
┌─────────────────────────────────────────┐
│   MemoryMappingProfile (Domain Model)   │
│  - Segments: List<MemorySegment>        │
│  - Validation: Overlap detection        │
│  - Templates: Factory methods           │
└─────────────────────────────────────────┘
              ↓ (uses)
┌─────────────────────────────────────────┐
│  IMemoryMappingProfileService           │
│  - CRUD operations                      │
│  - Import/Export                        │
│  - Validation                           │
└─────────────────────────────────────────┘
              ↓ (managed by)
┌─────────────────────────────────────────┐
│  StandardProfileManager<T>              │
│  - Thread-safe operations               │
│  - JSON persistence                     │
│  - ID gap filling                       │
└─────────────────────────────────────────┘
```

### Data Model

```csharp
public class MemoryMappingProfile : IProfileBase
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<MemorySegment> Segments { get; set; } = new();
    public bool IsDefault { get; set; }
    public bool IsReadOnly { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public class MemorySegment
{
    public string Name { get; set; }          // e.g., ".text", ".data"
    public string StartAddress { get; set; }  // Hex format: "0x20000000"
    public string Size { get; set; }          // Hex format: "0x800"
    public string Description { get; set; }   // User-friendly description
}
```

## Usage Patterns

### Creating a Memory Region Profile

#### 1. Using Template Factory Methods

```csharp
// Create standard S7-1200 template
var profile = MemoryMappingProfile.CreateS7Template();
// Profile includes: .text, .data, .bss segments with standard addresses

// Customize template
profile.Name = "Custom S7-1200 Layout";
profile.Description = "Modified layout for specific project";
await _profileService.CreateAsync(profile);
```

#### 2. Manual Profile Creation

```csharp
var profile = new MemoryMappingProfile
{
    Name = "Custom Memory Layout",
    Description = "Application-specific memory regions",
    Segments = new List<MemorySegment>
    {
        new()
        {
            Name = ".text",
            StartAddress = "0x20000000",
            Size = "0x1000",
            Description = "Code segment"
        },
        new()
        {
            Name = ".data",
            StartAddress = "0x20001000",
            Size = "0x800",
            Description = "Initialized data"
        }
    }
};

await _profileService.CreateAsync(profile);
```

### Segment Management

#### Adding Segments

```csharp
var segment = new MemorySegment
{
    Name = ".rodata",
    StartAddress = "0x20001800",
    Size = "0x400",
    Description = "Read-only data"
};

profile.Segments.Add(segment);
await _profileService.UpdateAsync(profile);
```

#### Validation

```csharp
// Segment overlap detection
var hasOverlap = DetectSegmentOverlap(profile.Segments);
if (hasOverlap)
{
    throw new ValidationException("Segments", "Memory segments overlap");
}

// Address format validation
foreach (var segment in profile.Segments)
{
    if (!IsValidHexAddress(segment.StartAddress))
    {
        throw new ValidationException(
            "StartAddress",
            $"Invalid hex address: {segment.StartAddress}");
    }
}
```

## Job Wizard Integration

### Memory Region Selection Workflow

1. **Profile Selection** (Optional Step)
   - User selects existing memory mapping profile OR
   - Chooses "Manual Configuration" for custom regions

2. **Fallback Mechanism**
   - If profile selected: Use profile segments
   - If manual: Use StartAddress, EndAddress, SegmentSelection from job

3. **Validation**
   - Verify address ranges are valid
   - Check for segment overlaps (if using profile)
   - Ensure memory size is reasonable (<64KB warning)

### Example Job Wizard Usage

```csharp
public class JobWizardViewModel : ReactiveObject
{
    // Memory Region Step
    public MemoryMappingProfile? SelectedMemoryProfile { get; set; }
    public bool UseManualConfiguration { get; set; }

    // Manual configuration fallback
    public string ManualStartAddress { get; set; } = "0x20000000";
    public string ManualEndAddress { get; set; } = "0x20001000";

    private async Task CreateJobAsync()
    {
        var job = new JobProfile
        {
            // ... other properties

            // Memory configuration
            MemoryMappingProfileId = SelectedMemoryProfile?.Id,

            // Fallback for manual configuration
            StartAddress = UseManualConfiguration
                ? ManualStartAddress
                : SelectedMemoryProfile?.Segments.First().StartAddress,
            EndAddress = UseManualConfiguration
                ? ManualEndAddress
                : CalculateEndAddress(SelectedMemoryProfile?.Segments)
        };

        await _jobService.CreateAsync(job);
    }
}
```

## Standard Memory Layouts

### S7-1200 Standard Layout

```
┌──────────────────────────────────────┐
│ .text (Code Segment)                 │
│ Start: 0x20000000                    │
│ Size:  0x800 (2KB)                   │
└──────────────────────────────────────┘
┌──────────────────────────────────────┐
│ .data (Initialized Data)             │
│ Start: 0x20000800                    │
│ Size:  0x400 (1KB)                   │
└──────────────────────────────────────┘
┌──────────────────────────────────────┐
│ .bss (Uninitialized Data)            │
│ Start: 0x20000C00                    │
│ Size:  0x200 (512B)                  │
└──────────────────────────────────────┘
```

### Memory Presets

Available in Job Wizard:

1. **4KB Boot Sector** (`0x20000000` - `0x20001000`)
   - Minimal bootloader region
   - Fast dump time (~5 seconds)

2. **64KB Full Dump** (`0x20000000` - `0x20010000`)
   - Complete user memory
   - Standard S7-1200 configuration
   - Dump time ~2 minutes

3. **Custom Range**
   - User-defined start/end addresses
   - Flexible segment selection

## Best Practices

### Profile Organization

1. **Naming Convention**
   - Use descriptive names: "S7-1200 Standard", "Minimal Boot Sector"
   - Avoid generic names: "Profile 1", "Test"

2. **Segment Naming**
   - Follow ELF conventions: `.text`, `.data`, `.bss`, `.rodata`
   - Use descriptive names for custom segments

3. **Documentation**
   - Always fill Description field for profiles and segments
   - Document why custom layouts were created

### Validation Rules

1. **Address Format**
   - Always use hex format with `0x` prefix
   - Example: `0x20000000` (not `20000000`)

2. **Segment Overlap**
   - System detects overlaps automatically
   - Fix overlaps before saving profile

3. **Memory Size**
   - Keep individual segments < 16KB for performance
   - Total dump size < 64KB recommended

### Import/Export

```csharp
// Export profile for sharing
var json = await _profileService.ExportToJsonAsync(profile);
File.WriteAllText("my-layout.json", json);

// Import profile from file
var json = File.ReadAllText("my-layout.json");
var profile = await _profileService.ImportFromJsonAsync(json);
```

## Troubleshooting

### Common Issues

**Issue**: Segment overlap detected
**Solution**: Adjust segment addresses to eliminate overlap. Use validation tool to identify conflicting regions.

**Issue**: Invalid hex address format
**Solution**: Ensure addresses use `0x` prefix and valid hex characters (0-9, A-F)

**Issue**: Job wizard doesn't use profile segments
**Solution**: Verify profile has at least one segment. Check fallback configuration is disabled.

**Issue**: Memory dump fails with invalid address
**Solution**: Verify addresses are within S7-1200 user memory range (`0x20000000` - `0x20010000`)

## Migration from Manual Configuration

### Before (Manual Configuration)

```csharp
var job = new JobProfile
{
    StartAddress = "0x20000000",
    EndAddress = "0x20001000",
    SegmentSelection = SegmentSelection.All
};
```

### After (Profile-Based)

```csharp
// Create profile once
var profile = MemoryMappingProfile.CreateS7Template();
await _profileService.CreateAsync(profile);

// Use in multiple jobs
var job = new JobProfile
{
    MemoryMappingProfileId = profile.Id,
    // StartAddress, EndAddress derived from profile
};
```

## API Reference

### IMemoryMappingProfileService

```csharp
public interface IMemoryMappingProfileService
{
    Task<MemoryMappingProfile> CreateAsync(MemoryMappingProfile profile);
    Task<MemoryMappingProfile> UpdateAsync(MemoryMappingProfile profile);
    Task<bool> DeleteAsync(int profileId);
    Task<IEnumerable<MemoryMappingProfile>> GetAllAsync();
    Task<MemoryMappingProfile?> GetByIdAsync(int profileId);
    Task<bool> ValidateAsync(MemoryMappingProfile profile);
    Task<string> ExportToJsonAsync(MemoryMappingProfile profile);
    Task<MemoryMappingProfile> ImportFromJsonAsync(string json);
}
```

## Related Documentation

- [Profile Management Pattern](profile-management.md)
- [Development Workflow](../guides/development-workflow.md)
- [Architecture Overview](../architecture/overview.md)
- [System Patterns](system-patterns.md)
