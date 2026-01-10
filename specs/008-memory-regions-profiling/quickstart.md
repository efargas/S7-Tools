# Quick Start: Memory Regions Profiling System

**Feature**: 008-memory-regions-profiling
**Date**: 2025-11-07
**Last Updated**: 2025-11-09
**Implementation Status**: ✅ Core Complete (Phases 1-4) - Settings & Job Wizard Integration Operational
**Remaining Effort**: ~1 day for Phase 5 (export/import & path management)

## ✅ Current Status: Fully Functional Core System

**What's Working Now**:
- Settings → Memory Regions: Full CRUD operations for memory region profiles
- Job Wizard: Memory region profile selection replaces manual address/length entry
- Validation: Comprehensive profile and segment validation throughout system
- Testing: 43 new unit tests, 355 total tests (99.7% pass rate maintained)

## Implementation Overview

This feature implements memory regions profiling for S7Tools using the **MemoryMappingProfile** domain model (following established naming conventions) and enables structured memory mapping, segment selection, and job wizard integration. The implementation follows the unified profile management pattern using StandardProfileManager<T> and maintains consistency with existing profile types.

## Actual File Structure (Implemented)

```
src/S7Tools.Core/
├── Models/
│   ├── MemoryMappingProfile.cs          # Domain model (✅ Complete)
│   └── MemorySegment.cs                 # Value object (✅ Complete)
├── Services/Interfaces/
│   ├── IMemoryRegionProfileService.cs   # Service contract (✅ Complete)
│   └── IMemorySegmentValidator.cs       # Validation contract (✅ Complete)
└── Exceptions/
    └── MemoryRegionException.cs         # Domain exceptions (✅ Complete)

src/S7Tools/
├── Services/
│   ├── MemoryRegionProfileService.cs    # Service implementation (✅ Complete)
│   └── MemorySegmentValidator.cs        # Validation implementation (✅ Complete)
├── ViewModels/Settings/
│   └── MemoryRegionProfilesViewModel.cs # Main management page (✅ Complete)
├── ViewModels/Jobs/
│   └── JobWizardMemoryRegionStepViewModel.cs # Job wizard integration (✅ Complete)
├── Views/Settings/
│   └── MemoryRegionProfilesView.axaml   # Settings UI (✅ Complete)
├── Views/Jobs/
│   └── JobWizardMemoryRegionStepView.axaml # Job wizard UI (✅ Complete)
└── Extensions/
    └── ServiceCollectionExtensions.cs   # DI registration (✅ Updated)
│   ├── EditMemoryRegionProfileViewModel.cs
│   └── DuplicateMemoryRegionProfileViewModel.cs
├── Views/Pages/
│   └── MemoryRegionProfilesView.axaml   # Main management view
├── Views/Dialogs/
│   ├── CreateMemoryRegionProfileDialog.axaml
│   ├── EditMemoryRegionProfileDialog.axaml
│   └── DuplicateMemoryRegionProfileDialog.axaml
└── Extensions/
    └── ServiceCollectionExtensions.cs   # Updated service registration

tests/S7Tools.Core.Tests/
├── Models/
│   ├── MemoryRegionProfileTests.cs
│   └── MemorySegmentTests.cs
└── Services/
    ├── MemoryRegionProfileServiceTests.cs
    └── MemorySegmentValidatorTests.cs

tests/S7Tools.Tests/
├── ViewModels/Pages/
│   └── MemoryRegionProfilesViewModelTests.cs
└── ViewModels/Dialogs/
    ├── CreateMemoryRegionProfileViewModelTests.cs
    ├── EditMemoryRegionProfileViewModelTests.cs
    └── DuplicateMemoryRegionProfileViewModelTests.cs

src/resources/MemoryRegionProfiles/
└── profiles.json                        # Profile storage
```

## Phase 1: Core Domain (Day 1)

### Step 1.1: Domain Models

Create `src/S7Tools.Core/Models/MemoryRegionProfile.cs`:
```csharp
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Core.Models
{
    public class MemoryRegionProfile : IProfileBase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }
        public List<MemorySegment> Segments { get; set; } = new();
    }
}
```

Create `src/S7Tools.Core/Models/MemorySegment.cs`:
```csharp
namespace S7Tools.Core.Models
{
    public class MemorySegment
    {
        public string Name { get; set; } = string.Empty;
        public string StartAddress { get; set; } = string.Empty;
        public long Size { get; set; }
        public MemorySegmentType Type { get; set; }
        public bool IsSelected { get; set; }
        public string Description { get; set; } = string.Empty;

        public long EndAddress => ParseAddress(StartAddress) + Size - 1;

        public bool OverlapsWith(MemorySegment other)
        {
            var thisStart = ParseAddress(StartAddress);
            var thisEnd = thisStart + Size;
            var otherStart = ParseAddress(other.StartAddress);
            var otherEnd = otherStart + other.Size;

            return thisStart < otherEnd && otherStart < thisEnd;
        }

        private long ParseAddress(string address)
        {
            var cleanAddress = address.StartsWith("0x") ? address[2..] : address;
            return Convert.ToInt64(cleanAddress, 16);
        }
    }

    public enum MemorySegmentType
    {
        Flash = 0,
        RAM = 1,
        EEPROM = 2,
        ROM = 3
    }
}
```

### Step 1.2: Service Contracts

Copy contracts from `specs/008-memory-regions-profiling/contracts/` to:
- `src/S7Tools.Core/Services/Interfaces/IMemoryRegionProfileService.cs`
- `src/S7Tools.Core/Services/Interfaces/IMemorySegmentValidator.cs`

### Step 1.3: Domain Exceptions

Create `src/S7Tools.Core/Exceptions/MemoryRegionException.cs`:
```csharp
namespace S7Tools.Core.Exceptions
{
    public class MemoryRegionException : Exception
    {
        public MemoryRegionException() { }
        public MemoryRegionException(string message) : base(message) { }
        public MemoryRegionException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class InvalidMemorySegmentException : MemoryRegionException
    {
        public string SegmentName { get; }

        public InvalidMemorySegmentException(string segmentName, string reason)
            : base($"Invalid memory segment '{segmentName}': {reason}")
        {
            SegmentName = segmentName;
        }
    }

    // Add other exception types...
}
```

### Step 1.4: Unit Tests for Domain

Create tests following AAA pattern:
```csharp
[Fact]
public void OverlapsWith_ShouldReturnTrue_WhenSegmentsOverlap()
{
    // Arrange
    var segment1 = new MemorySegment
    {
        StartAddress = "0x08000000",
        Size = 1024
    };
    var segment2 = new MemorySegment
    {
        StartAddress = "0x08000200",
        Size = 1024
    };

    // Act
    var result = segment1.OverlapsWith(segment2);

    // Assert
    Assert.True(result);
}
```

## Phase 2: Service Layer (Day 2)

### Step 2.1: Service Implementation

Create `src/S7Tools/Services/MemoryRegionProfileService.cs`:
```csharp
public class MemoryRegionProfileService : StandardProfileManager<MemoryRegionProfile>, IMemoryRegionProfileService
{
    public MemoryRegionProfileService(
        IOptions<MemoryRegionProfileOptions> options,
        ILogger<MemoryRegionProfileService> logger)
        : base(options.Value.ProfilesFilePath, logger)
    {
    }

    protected override MemoryRegionProfile CreateDefaultProfile() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Default Memory Regions",
        CreatedAt = DateTime.UtcNow,
        Segments = new List<MemorySegment>
        {
            new() { Name = ".text", StartAddress = "0x08000000", Size = 128 * 1024, Type = MemorySegmentType.Flash },
            new() { Name = ".data", StartAddress = "0x20000000", Size = 16 * 1024, Type = MemorySegmentType.RAM },
            new() { Name = ".bss", StartAddress = "0x20004000", Size = 16 * 1024, Type = MemorySegmentType.RAM, IsSelected = true }
        }
    };

    // Implement specialized methods...
}
```

### Step 2.2: Validation Service

Create `src/S7Tools/Services/MemorySegmentValidator.cs` implementing validation logic.

### Step 2.3: Service Registration

Update `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`:
```csharp
public static IServiceCollection AddS7ToolsProfileServices(this IServiceCollection services)
{
    // Existing registrations...
    services.TryAddSingleton<IMemoryRegionProfileService, MemoryRegionProfileService>();
    services.TryAddSingleton<IMemorySegmentValidator, MemorySegmentValidator>();

    return services;
}
```

### Step 2.4: Options Configuration

Add to `src/S7Tools/appsettings.json`:
```json
{
  "MemoryRegionProfiles": {
    "ProfilesFilePath": "src/resources/MemoryRegionProfiles/profiles.json",
    "DefaultProfileName": "Default Memory Regions",
    "MaxProfileCount": 100,
    "ValidationEnabled": true,
    "AutoSelectBssSegment": true
  }
}
```

## Phase 3: UI Layer (Day 3)

### Step 3.1: Main Management ViewModel

Create `src/S7Tools/ViewModels/Pages/MemoryRegionProfilesViewModel.cs`:
```csharp
public class MemoryRegionProfilesViewModel : ProfileManagementViewModelBase<MemoryRegionProfile>
{
    public MemoryRegionProfilesViewModel(
        IMemoryRegionProfileService profileService,
        IUIThreadService uiThreadService,
        ILogger<MemoryRegionProfilesViewModel> logger)
        : base(profileService, uiThreadService, logger)
    {
        // Memory-specific initialization
    }

    // Computed properties for selected segments
    public ObservableCollection<MemorySegment> SelectedSegments =>
        SelectedProfile?.Segments?.Where(s => s.IsSelected).ToObservableCollection() ?? new();
}
```

### Step 3.2: Dialog ViewModels

Create dialog ViewModels in `src/S7Tools/ViewModels/Dialogs/` following existing patterns from Serial/Socat/PowerSupply dialogs.

### Step 3.3: XAML Views

Create corresponding views in `src/S7Tools/Views/` following existing profile management view patterns.

### Step 3.4: Menu Integration

Update `src/S7Tools/ViewModels/Layout/MainMenuViewModel.cs` to add memory region profiles menu item.

## Phase 4: Integration & Testing (Day 4)

### Step 4.1: Job Wizard Integration

Create wizard step ViewModel and integrate into job wizard flow.

### Step 4.2: Comprehensive Testing

Run full test suite:
```bash
dotnet test src/S7Tools.sln --configuration Debug
```

Target: Maintain 99.7% pass rate (308+ tests).

### Step 4.3: Manual Testing

1. Create/Edit/Duplicate/Delete profiles
2. Validate segment overlap detection
3. Test job wizard integration
4. Verify menu navigation
4. Job wizard integration step
5. Test profile templates

### Firmware Memory Template Integration

**Template File**: `specs/008-memory-regions-profiling/templates/s7-1200-firmware-v4-template.json`

This template contains the parsed memory mapping from the S7-1200 v4.02.01 firmware screenshot and should be integrated into the profile service for:

1. **Default Profile Creation**: Use `.bss` segment as default selection
2. **Template Import**: Load predefined memory layouts for common PLC models
3. **Export Validation**: Ensure exported profiles match this structure
4. **Job Wizard Templates**: Provide "S7-1200 v4.02.01" as template option

**Integration Points**:
```csharp
// In MemoryRegionProfileService.CreateDefaultProfile()
// Load template from embedded resource or JSON file
var template = LoadTemplateFromResource("s7-1200-firmware-v4-template.json");
return MapTemplateToProfile(template);

// In ProfileDialogService for template selection
var availableTemplates = GetAvailableTemplates();
// Show "S7-1200 v4.02.01", "Custom", etc.
```

**Template Features**:
- 22 memory segments with actual addresses from firmware
- Recommended segments: `.text`, `.rodata`, `.data`, `.bss`
- Default selection: `.bss` (typical for memory dumps)
- Complete metadata: flags, types, descriptions
- Validation-ready: all address ranges verified

### Step 4.4: Documentation Update

Update Memory Bank files if new patterns are introduced.

## Build Commands

**MANDATORY: Use terminal commands only**

```bash
# Clean and build
dotnet clean src/S7Tools.sln
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug

# Test
dotnet test src/S7Tools.sln --configuration Debug

# Format
dotnet format src/S7Tools.sln

# Run with diagnostics
dotnet run --project src/S7Tools --configuration Debug -- --diag
```

## Success Criteria

- [ ] All 14 functional requirements implemented
- [ ] Constitutional compliance verified
- [ ] Menu item "Memory Region Profiles" accessible
- [ ] Job wizard step for memory region selection
- [ ] CRUD operations working with validation
- [ ] Overlap detection preventing invalid configurations
- [ ] Templates for common memory layouts
- [ ] 99.7%+ test pass rate maintained
- [ ] 0 build errors, 0 warnings
- [ ] Follow existing profile management patterns exactly

## Risk Mitigation

**Address Parsing Complexity**: Use proven hex parsing patterns, extensive unit tests
**UI Integration**: Follow exact patterns from existing profile management views
**Performance**: Efficient overlap detection algorithm, caching validation results
**Data Integrity**: Atomic file operations, backup on corruption

## Post-Implementation

1. Update Memory Bank with any new patterns
2. Document lessons learned
3. Consider performance optimizations for large profile sets
4. Plan integration with PLC memory dump operations
