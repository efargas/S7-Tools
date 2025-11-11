---
title: "Deprecated Property Migration Guide"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "migration", "deprecated", "properties"]
related:
  - docs/guides/migration/deprecated-patterns.md
  - docs/guides/versioning-guide.md
---

# Deprecated Property Migration Guide

## Overview

This document provides guidance for migrating away from deprecated properties in the S7Tools codebase.

---

## JobProfile.MemoryRegion Property

### Status
**DEPRECATED** as of version 1.0.0 (2025-11-10)
**Planned Removal**: Version 2.0.0 (Q2 2026)

### Summary
The `MemoryRegion` property in `JobProfile` is being replaced by the `MemoryRegionProfileId` property to support the new Memory Region Profile Management system.

### Details

**Deprecated Property:**
```csharp
[Required(ErrorMessage = "Memory region configuration is required")]
public MemoryRegionProfile MemoryRegion { get; set; }
```

**Replacement Property:**
```csharp
public int MemoryRegionProfileId { get; set; }
```

### Migration Path

#### Why the Change?

1. **Profile Management**: The new system uses centralized memory region profiles that can be reused across multiple jobs
2. **Separation of Concerns**: Profiles are managed independently from job instances
3. **Improved Maintainability**: Updates to memory region configurations can be made in one place
4. **Better User Experience**: Users can create, edit, and manage memory region profiles separately from jobs

#### How to Migrate

**Step 1: Convert Inline MemoryRegion to Profile**

If you have code that creates jobs with inline `MemoryRegion` objects:

```csharp
// OLD (Deprecated)
var job = new JobProfile
{
    Name = "My Job",
    MemoryRegion = new MemoryRegionProfile(0x20000000, 0x1000)
};
```

Convert to:

```csharp
// NEW (Recommended)
// 1. Create or select a memory region profile
var memoryProfile = await _memoryRegionService.CreateAsync(new MemoryMappingProfile
{
    Name = "Default User Memory",
    Segments = new List<MemorySegment>
    {
        new MemorySegment
        {
            Name = "User Memory",
            StartAddress = 0x20000000,
            EndAddress = 0x20001000,
            IsSelected = true
        }
    }
});

// 2. Reference the profile in the job
var job = new JobProfile
{
    Name = "My Job",
    MemoryRegionProfileId = memoryProfile.Id
};
```

**Step 2: Update Existing Job Profiles**

For existing job profiles stored in JSON:

```json
// OLD format
{
  "Name": "My Job",
  "MemoryRegion": {
    "StartAddress": 536870912,
    "EndAddress": 536875008
  }
}
```

Convert to:

```json
// NEW format
{
  "Name": "My Job",
  "MemoryRegionProfileId": 1
}
```

**Step 3: Use Memory Region Profile Service**

Access memory region data through the profile service:

```csharp
// OLD (Deprecated)
uint startAddress = job.MemoryRegion.StartAddress;
uint endAddress = job.MemoryRegion.EndAddress;

// NEW (Recommended)
var memoryProfile = await _memoryRegionService.GetByIdAsync(job.MemoryRegionProfileId);
var selectedSegments = memoryProfile.Segments.Where(s => s.IsSelected).ToList();
uint startAddress = selectedSegments.First().StartAddress;
uint totalSize = selectedSegments.Sum(s => s.Size);
```

### Timeline

| Version | Date | Action |
|---------|------|--------|
| 1.0.0 | 2025-11-10 | Property marked as deprecated, warning added |
| 1.5.0 | Q1 2026 | Compiler warning elevated to error in new code |
| 2.0.0 | Q2 2026 | Property removed (BREAKING CHANGE) |

### Compatibility Notes

**Current Behavior (v1.0.0 - v1.x.x):**
- Both `MemoryRegion` and `MemoryRegionProfileId` are supported
- If `MemoryRegionProfileId` is set, it takes precedence
- If only `MemoryRegion` is set, a temporary profile is created at runtime
- Compiler warning generated when using `MemoryRegion`

**Future Behavior (v2.0.0+):**
- `MemoryRegion` property will be removed
- Only `MemoryRegionProfileId` will be supported
- Jobs without a valid `MemoryRegionProfileId` will fail validation

### Migration Tools

A migration utility is planned for v1.5.0 that will:
1. Scan existing job profiles
2. Convert inline `MemoryRegion` objects to managed profiles
3. Update job references to use `MemoryRegionProfileId`
4. Preserve existing job configurations

### Support

For questions or assistance with migration:
- Review the Memory Region Profile Management documentation: `docs/MEMORY_REGION_PROFILES.md`
- Check the Job Wizard implementation: `src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs`
- Refer to pattern documentation: `PATTERNS_REFERENCE.md` (Memory Region Profile Management section)

---

## Future Deprecations

No additional deprecations are currently planned.

---

**Last Updated**: 2025-11-10
**Maintainer**: S7Tools Development Team

## Related Documentation

- [Deprecated Patterns](guides/migration/deprecated-patterns.md)
- [Versioning Guide](guides/versioning-guide.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
