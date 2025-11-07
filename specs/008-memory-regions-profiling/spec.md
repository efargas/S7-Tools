# Feature Specification: Memory Regions Profiling System

**Feature Branch**: `008-memory-regions-profiling`
**Created**: 2025-11-07
**Status**: Draft
**Input**: User description: "we have to implement the memory regions profiling views, viewmodels, menu, settings, ... as the other profilers in the aplication. this profiles has to be populated also in the jobwizard corresponding step."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Manage Memory Region Profiles (Priority: P1)

Users can create, edit, duplicate, and delete memory region profiles with predefined memory ranges, address configurations, and descriptive information through a dedicated Memory Regions settings page, enabling reusable memory dump configurations for different PLC scenarios.

**Why this priority**: Essential foundation for memory region profiling - users need to define and manage reusable memory configurations before they can use them in job creation workflows.

**Independent Test**: Navigate to Settings → Memory Regions, create a new profile with name "Flash Memory", set start address 0x08000000, length 512KB, verify profile appears in list and can be edited/duplicated/deleted independently of other features.

**Acceptance Scenarios**:

1. **Given** user is on Memory Regions settings page, **When** they click Create button, **Then** a dialog opens with options to create from template (showing available PLC models/firmware versions) or create custom profile, with fields for profile name, description, PLC model, firmware version, and a list to add/edit named memory segments (segment name, start address, length, flags)
2. **Given** user fills profile dialog with valid data (name="ROM Dump", start=0x08000000, length=1024), **When** they save, **Then** profile appears in the profiles list with formatted display
3. **Given** user selects an existing memory region profile, **When** they click Edit button, **Then** dialog opens pre-populated with current profile data for modification
4. **Given** user selects a profile and clicks Duplicate, **When** they provide new name "ROM Dump Copy", **Then** new profile is created with identical configuration but unique name
5. **Given** user selects a non-default profile, **When** they click Delete and confirm, **Then** profile is removed from list and file storage

---

### User Story 2 - Select Memory Region Profiles in Job Wizard (Priority: P2)

Users can select from available memory region profiles during job creation in the Memory Region step of the job wizard, with immediate display of profile details (start address, length, calculated end address) for verification and confidence in their selection.

**Why this priority**: Enables integration with existing job workflow - users can select predefined memory configurations during job creation instead of manually entering addresses.

**Independent Test**: Create a job in the wizard, navigate to Memory Region step, select a memory region profile from dropdown, verify profile details display shows complete configuration and wizard continues to next step.

**Acceptance Scenarios**:

1. **Given** user is in job wizard Memory Region step, **When** the step loads, **Then** dropdown shows all available memory region profiles plus "Custom Range" option
2. **Given** user selects a memory region profile from dropdown, **When** selection changes, **Then** profile details section immediately updates with PLC model, firmware version, and a table showing all memory segments with .bss segment pre-selected by default, segment names, start addresses, lengths, end addresses, and flags
3. **Given** user selects individual memory segments, **When** selections are non-correlative (not contiguous), **Then** system displays validation error and prevents proceeding until correlative segments are selected
4. **Given** user has selected valid correlative segments or "Select All", **When** they click Next, **Then** wizard proceeds to next step with memory configuration using lowest segment start address and calculated total size
5. **Given** user selects "Custom Range" option, **When** they switch to custom mode, **Then** manual input fields for start address and length become available and editable
6. **Given** user navigates to Review step, **When** they view job summary, **Then** memory region configuration shows either selected profile name with selected segments or "Custom Range" with address details

---

### User Story 3 - Memory Region Profile Settings Management (Priority: P3)

Users can configure memory region profiles file storage location, export/import profiles for backup and sharing, and access profile management commands through the Settings → Memory Regions interface with consistent behavior matching other profiler settings.

**Why this priority**: Provides administrative capabilities - users need to manage profile storage and sharing for team collaboration and system maintenance.

**Independent Test**: Navigate to Memory Regions settings, browse for custom profiles path, export profiles to JSON file, import profiles from another file, verify all operations work independently and profiles are properly synchronized.

**Acceptance Scenarios**:

1. **Given** user is on Memory Regions settings page, **When** they click Browse Path button, **Then** folder browser dialog opens to select custom profiles storage location
2. **Given** user has memory region profiles, **When** they click Export Profiles, **Then** file save dialog opens and profiles are exported to selected JSON file
3. **Given** user has a profiles JSON file, **When** they click Import Profiles and select file, **Then** profiles are loaded and merged with existing profiles, handling name conflicts appropriately
4. **Given** user clicks Open Profiles Path, **When** command executes, **Then** file explorer opens to the current profiles directory
5. **Given** user clicks Reset Profiles Path, **When** they confirm the action, **Then** profiles path returns to default application location

### Edge Cases

- What happens when user tries to create memory region profile with address range that overlaps with known PLC memory constraints?
- How does system handle invalid hexadecimal addresses or extremely large memory ranges in profile creation?
- What occurs when imported memory region profiles have duplicate names or invalid data structures?
- How does job wizard behave when selected memory region profile is deleted after selection but before job completion?
- What happens when profiles file becomes corrupted or inaccessible during profile management operations?
- How does system respond when user attempts to select non-correlative (non-contiguous) memory segments in job wizard?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a Memory Region profile management interface in Settings following the unified profile management patterns
- **FR-002**: Memory Region profiles MUST implement IProfileBase interface with standard metadata (name, description, options, flags, timestamps)
- **FR-003**: Memory Region profiles MUST store PLC model, firmware version, and a collection of named memory segments, where each segment contains name, start address (uint), length (uint), and optional flags for flash ROM memory areas
- **FR-004**: System MUST support CRUD operations (Create, Read, Update, Delete) for Memory Region profiles with dialog-based editing, allowing users to create profiles from scratch with custom memory region entries or from predefined templates for common PLC models
- **FR-005**: Job wizard MUST include Memory Region selection step with dropdown of available profiles, individual segment selection with correlative validation, "Select All" option, plus "Custom Range" option
- **FR-006**: Job wizard MUST display selected memory region profile details including PLC model, firmware version, and a table showing all memory segments with selection checkboxes (defaulting .bss segment selected), names, start addresses, lengths, end addresses, and flags
- **FR-007**: System MUST validate memory region addresses and lengths to prevent invalid configurations, and MUST enforce correlative (contiguous) segment selection - non-correlative segment combinations not allowed, with job processing using lowest segment start address and calculated total size
- **FR-008**: Memory Region profile storage MUST use JSON persistence with configurable file path following existing profile management patterns
- **FR-009**: System MUST support export/import of memory region profiles for backup and sharing capabilities
- **FR-010**: Memory Region settings page MUST provide path management commands (Browse, Open, Reset) consistent with other profiler settings
- **FR-011**: System MUST handle memory region profile deletion gracefully when profile is referenced in existing jobs with appropriate fallback behavior
- **FR-012**: Memory Region profiles MUST support default profile designation with automatic selection in job wizard
- **FR-013**: System MUST register Memory Region profile service and ViewModels in dependency injection container following established patterns
- **FR-014**: System MUST provide predefined memory region profile templates for common PLC models and firmware versions (e.g., 6ES7212-1AE40-OXBO with Firmware v4.02.01) with complete firmware memory mappings

### Key Entities *(include if feature involves data)*

- **MemoryRegionProfile**: Manages firmware memory dump configurations with PLC model, firmware version, multiple memory segments (name, start address, length), and descriptive metadata implementing IProfileBase
- **MemoryRegionProfileService**: Handles CRUD operations, validation, and persistence for memory region profiles extending StandardProfileManager<T>
- **MemoryRegionSettingsViewModel**: Settings page ViewModel managing profile collection, file operations, and path configuration following ProfileManagementViewModelBase pattern
- **MemoryRegionProfileViewModel**: Dialog ViewModel for creating/editing individual memory region profiles with validation and user input handling

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create and manage memory region profiles within 2 minutes of accessing Memory Regions settings page with successful save/load operations
- **SC-002**: Job wizard Memory Region step completes in under 30 seconds including profile selection and detail verification for improved workflow efficiency
- **SC-003**: Memory region profile operations (create, edit, duplicate, delete) complete within 500ms with immediate UI feedback and proper error handling
- **SC-004**: Export/import operations handle files up to 1MB with 100+ memory region profiles without performance degradation or data corruption
- **SC-005**: System maintains 99%+ profile data integrity during file operations, path changes, and concurrent access scenarios
- **SC-006**: Memory region profile integration reduces manual address entry errors by 90% through predefined, validated configurations

## Constitution Check

**Reference**: `.specify/memory/constitution.md` v1.0.0

**Impacted Principles**:
- **Cross-cutting Concerns**: New memory region profiling system affects DI registration, settings management, job wizard integration, and profile persistence
- **Public Contracts**: Introduction of new IMemoryRegionProfileService interface and MemoryRegionProfile model following established IProfileBase contract
- **Service Architecture**: Addition of memory region management services extending StandardProfileManager pattern

**Compliance Assessment**: **✅ COMPLIANT**

**Rationale**: Implementation follows existing unified profile management architecture patterns established in TASK008. All new components (service interfaces, ViewModels, profile models) conform to existing abstractions and dependency injection patterns. No breaking changes to public contracts - extends system capabilities through established extension points.

**Mitigations**: None required - implementation leverages proven architectural patterns and maintains consistency with existing codebase standards.

## Clarifications

### Session 2025-11-07

- Q: Memory Type Classification - What types are supported or how they're used? → A: No memory type classification - all memory areas reference PLC flash ROM firmware. Profiles target specific PLC models and firmware versions (e.g., Firmware v4.02.01 on 6ES7212-1AE40-OXBO PLC). Users can create profiles from scratch with custom memory region entries.
- Q: Memory Region Structure - How should the profile structure handle multiple memory segments? → A: Collection of named segments with individual ranges
- Q: Memory Segment Selection for Jobs - How should users choose which segments to dump? → A: Individual segment selection with correlative requirement - selected segments must be correlative (contiguous), non-correlative selections not allowed. Job processing uses lowest segment start address and calculates total size. "Select All" option dumps entire memory range.
- Q: Default Memory Segment Selection - What should be the default selection state for individual segments? → A: Default .bss segment selected - this application is normally used to dump the .bss segment
- Q: Profile Template Creation - Should the system provide pre-built profile templates? → A: Include predefined templates for common PLC models

## Firmware Memory Mapping Reference

**Screenshot Reference**: Firmware Memory Mapping on S7-1200 v4, from 0x000439C0
![Firmware Memory Mapping](../../../screenshots/firmware-memory-mapping-s7-1200-v4.png)

**Parsed Memory Region Template (S7-1200 v4.02.01)**:

```json
{
  "profileName": "S7-1200_Firmware_v4.02.01_Complete",
  "description": "Complete firmware memory mapping for S7-1200 v4.02.01 starting from 0x000439C0",
  "plcModel": "6ES7212-1AE40-OXBO",
  "firmwareVersion": "v4.02.01",
  "baseAddress": "0x000439C0",
  "segments": [
    {
      "name": ".exec_in_lomem",
      "startAddress": "0x00000000",
      "endAddress": "0x000075b4",
      "size": "0x000075b4",
      "flags": 1,
      "description": "Executive code in low memory"
    },
    {
      "name": ".bitable",
      "startAddress": "0x00040000",
      "endAddress": "0x00040040",
      "size": "0x00000040",
      "flags": 1,
      "description": "Binary table segment"
    },
    {
      "name": ".sdramexec",
      "startAddress": "0x00040040",
      "endAddress": "0x00040510",
      "size": "0x000004d0",
      "flags": 1,
      "description": "SDRAM executive segment"
    },
    {
      "name": ".syscall",
      "startAddress": "0x00040540",
      "endAddress": "0x00040548",
      "size": "0x00000008",
      "flags": 1,
      "description": "System call segment"
    },
    {
      "name": ".th_initial",
      "startAddress": "0x00041040",
      "endAddress": "0x00043998",
      "size": "0x00002958",
      "flags": 33,
      "description": "Thread initialization segment"
    },
    {
      "name": ".secinfo",
      "startAddress": "0x000439c0",
      "endAddress": "0x00043cfc",
      "size": "0x0000033c",
      "flags": 34,
      "description": "Security information segment"
    },
    {
      "name": ".fixaddr",
      "startAddress": "0x00043d00",
      "endAddress": "0x00043d00",
      "size": "0x00000000",
      "flags": 4,
      "description": "Fixed address segment"
    },
    {
      "name": ".fixtype",
      "startAddress": "0x00043d00",
      "endAddress": "0x00043d00",
      "size": "0x00000000",
      "flags": 4,
      "description": "Fixed type segment"
    },
    {
      "name": ".text",
      "startAddress": "0x00043d00",
      "endAddress": "0x000defda0",
      "size": "0x0dac0a0",
      "flags": 33,
      "description": "Code/text segment"
    },
    {
      "name": ".rodata",
      "startAddress": "0x000defdc0",
      "endAddress": "0x0111a1f6c",
      "size": "0x003b13ac",
      "flags": 34,
      "description": "Read-only data segment"
    },
    {
      "name": ".data",
      "startAddress": "0x0111a1f80",
      "endAddress": "0x011fc1df4",
      "size": "0x00020b94",
      "flags": 42,
      "description": "Initialized data segment"
    },
    {
      "name": ".bss",
      "startAddress": "0x01fe01040",
      "endAddress": "0x02620f58",
      "size": "0x0081ff18",
      "flags": 12,
      "description": "Uninitialized data segment (BSS)"
    },
    {
      "name": ".cc_memory",
      "startAddress": "0x03641040",
      "endAddress": "0x03641040",
      "size": "0x00000000",
      "flags": 4,
      "description": "Cache coherent memory"
    },
    {
      "name": ".uninitialized",
      "startAddress": "0x03c41040",
      "endAddress": "0x06fac934",
      "size": "0x0336b8f4",
      "flags": 12,
      "description": "Uninitialized memory pool"
    },
    {
      "name": "CLSI_CACHED_MEM_POOL",
      "startAddress": "0x06fac940",
      "endAddress": "0x06fac940",
      "size": "0x00000000",
      "flags": 4,
      "description": "CLSI cached memory pool"
    },
    {
      "name": ".dram_uncache",
      "startAddress": "0x07ff0000",
      "endAddress": "0x07ff0000",
      "size": "0x00000000",
      "flags": 4,
      "description": "DRAM uncached segment"
    },
    {
      "name": "MAP_MAC_MEM",
      "startAddress": "0x07ff0000",
      "endAddress": "0x07ff0494",
      "size": "0x00000494",
      "flags": 12,
      "description": "MAC memory mapping"
    },
    {
      "name": ".iram0",
      "startAddress": "0x10030000",
      "endAddress": "0x10037aa0",
      "size": "0x00007aa0",
      "flags": 12,
      "description": "Internal RAM 0"
    },
    {
      "name": ".iram1",
      "startAddress": "0x10040000",
      "endAddress": "0x1004c35c",
      "size": "0x0000c35c",
      "flags": 12,
      "description": "Internal RAM 1"
    },
    {
      "name": ".qrdtable",
      "startAddress": "0x10041400",
      "endAddress": "0x10041800",
      "size": "0x00000400",
      "flags": 12,
      "description": "QRD table segment"
    },
    {
      "name": ".softboot",
      "startAddress": "0x10041800",
      "endAddress": "0x10041f00",
      "size": "0x00000700",
      "flags": 12,
      "description": "Soft boot segment"
    },
    {
      "name": ".bootinfo",
      "startAddress": "0x10041f00",
      "endAddress": "0x10041f1c",
      "size": "0x0000001c",
      "flags": 12,
      "description": "Boot information segment"
    },
    {
      "name": ".dtcm",
      "startAddress": "0x10010000",
      "endAddress": "0x10012e70",
      "size": "0x00002e70",
      "flags": 12,
      "description": "Data tightly coupled memory"
    }
  ],
  "defaultSegments": [".bss"],
  "recommendedSegments": [".text", ".rodata", ".data", ".bss"],
  "memoryLayout": {
    "totalAddressSpace": "0x10012e70",
    "executableRegions": [".exec_in_lomem", ".th_initial", ".text"],
    "dataRegions": [".rodata", ".data", ".bss", ".uninitialized"],
    "systemRegions": [".syscall", ".secinfo", ".bootinfo"]
  }
}
```

