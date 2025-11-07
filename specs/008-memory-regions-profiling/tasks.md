---
description: "Task list for Memory Regions Profiling System implementation"
---

# Tasks: Memory Regions Profiling System

**Input**: Design documents from `/home/kali/WS/S7-Tools/specs/008-memory-regions-profiling/`
**Prerequisites**: spec.md (required), data-model.md (required), research.md (completed), contracts/ (defined), quickstart.md (guidance)

**Constitution Compliance** (Reference: `.copilot-tracking/memory-bank/systemPatterns.md` v1.2.0):

- **Clean Architecture (NON-NEGOTIABLE)**: Core domain has no external dependencies. Infrastructure and UI depend on Core only. Services extend StandardProfileManager<T> pattern.
- **MVVM Pattern (NON-NEGOTIABLE)**: All ViewModels use ReactiveUI with RaiseAndSetIfChanged. Follow ProfileManagementViewModelBase<T> pattern.
- **Thread Safety (NON-NEGOTIABLE)**: Use Internal Method Pattern for semaphore safety. All UI updates via IUIThreadService.
- **Service Registration (NON-NEGOTIABLE)**: Register in ServiceCollectionExtensions.cs only, never Program.cs.
- **Terminal Commands Only (NON-NEGOTIABLE)**: Use `dotnet build`, `dotnet test`, `dotnet format` commands only.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story following established profile management patterns.

## Current Implementation Status (Updated: November 7, 2025)

🟢 **Completed Phases:**

- ✅ **Phase 1 (Setup)**: All directory structures created
- ✅ **Phase 2 (Foundation)**: Complete domain models, services, and unit tests implemented
- ✅ **Phase 3 (User Story 1)**: Full Settings UI with CRUD dialog operations (17/17 tasks complete)
    - ✅ Service layer and ViewModels complete
    - ✅ Complete XAML UI and Settings integration working
    - ✅ Full dialog implementations with template selection, segment editing, and duplication

🔴 **Pending:**

- ⭕ **Phase 4 (User Story 2)**: Job Wizard integration (0/5 tasks started)
    - Memory region profile selection in job creation
    - Segment validation and profile details display
    - Integration with existing job workflow

 **Overall Progress**: 32/59 tasks complete (54.2%)
📊 **MVP Progress**: 32/32 MVP tasks complete (100% - Setup + Foundation + US1 complete!)

**Build Status**: ✅ Compiles successfully, 0 warnings, 99.7% test pass rate maintained (286 pass, 1 skip, 0 fail)

**Next Priority**: Complete remaining 5 dialog tasks (T019-T021, T023-T025) for full User Story 1 functionality

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions
- Domain models: `src/S7Tools.Core/Models/`
- Service interfaces: `src/S7Tools.Core/Services/Interfaces/`
- Service implementations: `src/S7Tools/Services/`
- ViewModels: `src/S7Tools/ViewModels/{Category}/`
- Views: `src/S7Tools/Views/{Category}/`
- Tests: `tests/S7Tools.Core.Tests/` and `tests/S7Tools.Tests/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and directory structure following Clean Architecture

- [x] T001 Create directory structure for memory region profiling in src/S7Tools.Core/Models/
- [x] T002 Create directory structure for service interfaces in src/S7Tools.Core/Services/Interfaces/
- [x] T003 [P] Create directory structure for service implementations in src/S7Tools/Services/
- [x] T004 [P] Create directory structure for ViewModels in src/S7Tools/ViewModels/Settings/
- [x] T005 [P] Create directory structure for Views in src/S7Tools/Views/Settings/
- [x] T006 [P] Create directory structure for profile dialogs in src/S7Tools/ViewModels/Dialogs/ and src/S7Tools/Views/Dialogs/
- [x] T007 Create resources directory in src/resources/MemoryRegionProfiles/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain models and service contracts that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T008 [P] Create MemorySegment domain model in src/S7Tools.Core/Models/MemorySegment.cs implementing address parsing, validation, and overlap detection
- [x] T009 [P] Create MemorySegmentType enumeration in src/S7Tools.Core/Models/MemorySegmentType.cs with Flash, RAM, EEPROM, ROM values
- [x] T010 Create MemoryRegionProfile domain model in src/S7Tools.Core/Models/MemoryRegionProfile.cs implementing IProfileBase with segments collection
- [x] T011 [P] Create IMemoryRegionProfileService interface in src/S7Tools.Core/Services/Interfaces/IMemoryRegionProfileService.cs extending StandardProfileManager<T> pattern
- [x] T012 [P] Create IMemorySegmentValidator interface in src/S7Tools.Core/Services/Interfaces/IMemorySegmentValidator.cs for validation operations
- [x] T013 [P] Create MemoryRegionException hierarchy in src/S7Tools.Core/Exceptions/MemoryRegionException.cs with domain-specific exceptions
- [x] T014 Create MemoryRegionProfile unit tests in tests/S7Tools.Core.Tests/Models/MemoryRegionProfileTests.cs following AAA pattern
- [x] T015 Create MemorySegment unit tests in tests/S7Tools.Core.Tests/Models/MemorySegmentTests.cs testing overlap detection and validation

**Checkpoint**: ✅ Foundation ready - domain models and contracts established, user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Create and Manage Memory Region Profiles (Priority: P1) 🎯 MVP

**Goal**: Users can create, edit, duplicate, and delete memory region profiles through Settings → Memory Regions page with CRUD operations, templates, and validation

**Independent Test**: Navigate to Settings → Memory Regions, create profile "Flash Memory" with address 0x08000000 and 512KB size, verify profile appears in list and can be edited/duplicated/deleted independently

### Implementation for User Story 1

- [x] T016 [P] [US1] Create MemoryRegionProfileService in src/S7Tools/Services/MemoryRegionProfileService.cs extending StandardProfileManager<T> with default S7-1200 template
- [x] T017 [P] [US1] Create MemorySegmentValidator in src/S7Tools/Services/MemorySegmentValidator.cs implementing overlap detection and address validation
- [x] T018 [US1] Create MemoryRegionSettingsViewModel in src/S7Tools/ViewModels/Settings/MemoryRegionSettingsViewModel.cs extending ProfileManagementViewModelBase<T>
- [x] T019 [P] [US1] Create CreateMemoryRegionProfileDialogViewModel in src/S7Tools/ViewModels/Dialogs/CreateMemoryRegionProfileDialogViewModel.cs with template selection
- [x] T020 [P] [US1] Create EditMemoryRegionProfileDialogViewModel in src/S7Tools/ViewModels/Dialogs/EditMemoryRegionProfileDialogViewModel.cs with segment editing
- [x] T021 [P] [US1] Create DuplicateMemoryRegionProfileDialogViewModel in src/S7Tools/ViewModels/Dialogs/DuplicateMemoryRegionProfileDialogViewModel.cs with name conflict resolution
- [x] T022 [US1] Create MemoryRegionSettingsView in src/S7Tools/Views/Settings/MemoryRegionSettingsView.axaml following PowerSupplySettingsView pattern with DataGrid and button layout
- [x] T023 [P] [US1] Create CreateMemoryRegionProfileDialog in src/S7Tools/Views/Dialogs/CreateMemoryRegionProfileDialog.axaml with template dropdown and custom segment input
- [x] T024 [P] [US1] Create EditMemoryRegionProfileDialog in src/S7Tools/Views/Dialogs/EditMemoryRegionProfileDialog.axaml with segment collection editor and validation
- [x] T025 [P] [US1] Create DuplicateMemoryRegionProfileDialog in src/S7Tools/Views/Dialogs/DuplicateMemoryRegionProfileDialog.axaml with name input and conflict handling
- [x] T026 [US1] Register memory region services in src/S7Tools/Extensions/ServiceCollectionExtensions.cs following existing profile service patterns
- [x] T027 [US1] Add MemoryRegionProfiles configuration section to src/S7Tools/appsettings.json with default paths and options
- [x] T028 [US1] Update SettingsViewModel in src/S7Tools/ViewModels/Settings/SettingsViewModel.cs to include "Memory Regions" category
- [x] T029 [US1] Implement S7-1200 firmware template integration in MemoryRegionProfileService using specs/008-memory-regions-profiling/templates/s7-1200-firmware-v4-template.json
- [x] T030 [US1] Create MemoryRegionProfileService unit tests in tests/S7Tools.Core.Tests/Services/MemoryRegionProfileServiceTests.cs testing CRUD operations and template loading
- [x] T031 [US1] Create MemorySegmentValidator unit tests in tests/S7Tools.Core.Tests/Services/MemorySegmentValidatorTests.cs testing validation rules and overlap detection
- [x] T032 [US1] Create MemoryRegionSettingsViewModel unit tests in tests/S7Tools.Tests/ViewModels/Settings/MemoryRegionSettingsViewModelTests.cs testing profile management operations

**Checkpoint**: ✅ User Story 1 complete - Full Settings UI with CRUD dialog operations implemented, all async method warnings resolved, ready for User Story 2 (Job Wizard integration)

---

## Phase 4: User Story 2 - Select Memory Region Profiles in Job Wizard (Priority: P2)

**Goal**: Users can select memory region profiles during job creation with segment selection, validation, and profile details display in Memory Region wizard step

**Independent Test**: Create job in wizard, navigate to Memory Region step, select profile from dropdown, verify profile details display and segment selection works, proceed to next step

### Implementation for User Story 2

- [ ] T033 [P] [US2] Create JobWizardMemoryRegionStepViewModel in src/S7Tools/ViewModels/Jobs/JobWizardMemoryRegionStepViewModel.cs extending StepViewModel with profile selection and segment validation
- [ ] T034 [P] [US2] Create JobWizardMemoryRegionStepView in src/S7Tools/Views/Jobs/JobWizardMemoryRegionStepView.axaml with profile dropdown, segment selection grid, and details panel
- [ ] T035 [US2] Update JobProfile model in src/S7Tools.Core/Models/Jobs/JobProfile.cs to include MemoryRegionProfileId and selected segments collection
- [ ] T036 [US2] Integrate memory region step into JobWizardViewModel in src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs following existing step registration pattern
- [ ] T037 [US2] Update job wizard navigation in src/S7Tools/Views/Jobs/JobWizardView.axaml to include Memory Region step after existing steps
- [ ] T038 [US2] Implement correlative segment validation in JobWizardMemoryRegionStepViewModel preventing non-contiguous segment selections
- [ ] T039 [US2] Add "Custom Range" option handling in JobWizardMemoryRegionStepViewModel with manual address/size input fields
- [ ] T040 [US2] Update job review step to display memory region configuration in src/S7Tools/ViewModels/Jobs/JobReviewStepViewModel.cs
- [ ] T041 [US2] Create JobWizardMemoryRegionStepViewModel unit tests in tests/S7Tools.Tests/ViewModels/Jobs/JobWizardMemoryRegionStepViewModelTests.cs testing validation and selection logic
- [ ] T042 [US2] Update JobProfile unit tests in tests/S7Tools.Core.Tests/Models/Jobs/JobProfileTests.cs to include memory region integration testing

**Checkpoint**: User Story 2 complete - memory region profiles integrated into job wizard with validation and selection capabilities

---

## Phase 5: User Story 3 - Memory Region Profile Settings Management (Priority: P3)

**Goal**: Users can configure storage paths, export/import profiles, and access profile management commands through Settings → Memory Regions interface matching other profiler settings

**Independent Test**: Navigate to Memory Regions settings, browse for custom profiles path, export profiles to JSON, import from another file, verify all operations work independently

### Implementation for User Story 3

- [ ] T043 [P] [US3] Add path management commands to MemoryRegionSettingsViewModel in src/S7Tools/ViewModels/Settings/MemoryRegionSettingsViewModel.cs (BrowseProfilesPath, OpenProfilesPath, ResetProfilesPath)
- [ ] T044 [P] [US3] Add export/import commands to MemoryRegionSettingsViewModel (ExportProfilesCommand, ImportProfilesCommand) with JSON serialization
- [ ] T045 [US3] Update MemoryRegionSettingsView in src/S7Tools/Views/Settings/MemoryRegionSettingsView.axaml to include path management and export/import buttons following PowerSupplySettingsView pattern
- [ ] T046 [US3] Implement export functionality in MemoryRegionProfileService with JSON serialization matching template format from specs/008-memory-regions-profiling/templates/s7-1200-firmware-v4-template.json
- [ ] T047 [US3] Implement import functionality in MemoryRegionProfileService with validation, conflict resolution, and profile merging
- [ ] T048 [US3] Add IFileDialogService integration for browse/export/import operations following existing profiler patterns
- [ ] T049 [US3] Add settings refresh pattern to MemoryRegionSettingsViewModel following PowerSupplySettingsViewModel RefreshFromSettings() implementation
- [ ] T050 [US3] Update ApplicationSettings in src/S7Tools.Core/Models/Configuration/ApplicationSettings.cs to include memoryRegion.* configuration keys
- [ ] T051 [US3] Create export/import unit tests in tests/S7Tools.Core.Tests/Services/MemoryRegionProfileServiceTests.cs testing serialization, validation, and conflict handling
- [ ] T052 [US3] Create path management unit tests in tests/S7Tools.Tests/ViewModels/Settings/MemoryRegionSettingsViewModelTests.cs testing browse, open, and reset operations

**Checkpoint**: User Story 3 complete - memory region profiles support full administrative capabilities with path management and export/import

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements affecting multiple user stories and final integration validation

- [ ] T053 [P] Update ViewLocator in src/S7Tools/ViewLocator.cs to include memory region views if needed (automatic resolution should work with established patterns)
- [ ] T054 [P] Add memory region profile integration tests in tests/S7Tools.Tests/Integration/ testing end-to-end workflows
- [ ] T055 [P] Update Memory Bank systemPatterns.md if new patterns were introduced during implementation
- [ ] T056 Run comprehensive validation using quickstart.md success criteria and build commands
- [ ] T057 Verify constitutional compliance checklist for Clean Architecture, MVVM, Thread Safety, and Service Registration patterns
- [ ] T058 [P] Performance testing with large profile collections (100+ profiles) following scalability requirements from data-model.md
- [ ] T059 [P] Security validation for file operations, input sanitization, and access control following security requirements

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed) or sequentially by priority
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - Foundation for other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Integrates with US1 profiles but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Extends US1 functionality but independently testable

### Within Each User Story

- Service layer before ViewModels
- ViewModels before Views
- Dialog ViewModels in parallel with main ViewModels
- Views follow their corresponding ViewModels
- Service registration after service implementation
- Unit tests can run in parallel with implementation (TDD approach)

### Parallel Opportunities

- **Phase 1**: All directory creation tasks T003-T007 can run in parallel
- **Phase 2**: All contract tasks T008, T009, T011, T012, T013 can run in parallel
- **User Story 1**: Dialog ViewModels T019-T021 and Views T023-T025 can run in parallel
- **User Story 2**: JobWizard ViewModel/View T033-T034 can run in parallel
- **User Story 3**: Path management and export/import commands T043-T044 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all dialog ViewModels in parallel:
Task T019: "Create CreateMemoryRegionProfileDialogViewModel"
Task T020: "Create EditMemoryRegionProfileDialogViewModel"
Task T021: "Create DuplicateMemoryRegionProfileDialogViewModel"

# Launch all dialog Views in parallel:
Task T023: "Create CreateMemoryRegionProfileDialog"
Task T024: "Create EditMemoryRegionProfileDialog"
Task T025: "Create DuplicateMemoryRegionProfileDialog"

# Launch validation and service tests in parallel:
Task T030: "Create MemoryRegionProfileService unit tests"
Task T031: "Create MemorySegmentValidator unit tests"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (directory structure)
2. Complete Phase 2: Foundational (domain models, contracts) - CRITICAL blocking phase
3. Complete Phase 3: User Story 1 (CRUD operations and Settings page)
4. **STOP and VALIDATE**: Test memory region profile creation, editing, duplication, deletion independently
5. Deploy/demo Settings → Memory Regions functionality

### Incremental Delivery

1. **Foundation** (Phases 1-2) → Core domain ready
2. **MVP** (Phase 3) → Memory region profile management via Settings
3. **Job Integration** (Phase 4) → Job wizard memory region selection
4. **Administration** (Phase 5) → Export/import and path management
5. Each increment adds value without breaking previous functionality

### Parallel Team Strategy

With multiple developers after Foundational phase complete:

1. **Developer A**: User Story 1 (Settings and CRUD operations)
2. **Developer B**: User Story 2 (Job wizard integration)
3. **Developer C**: User Story 3 (Export/import and path management)

All stories integrate through shared service layer established in Foundation phase.

---

## Build Commands (Constitutional Requirement)

**MANDATORY: Use terminal commands only - VS Code tasks are FORBIDDEN**

```bash
# Clean and build (MANDATORY pattern for all implementations)
dotnet clean src/S7Tools.sln
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug

# Testing (maintain 99.7%+ passing rate: 308+ tests, 1 intentionally skipped)
dotnet test src/S7Tools.sln --configuration Debug

# Code formatting (REQUIRED before any commit)
dotnet format src/S7Tools.sln

# Run application with diagnostic mode
dotnet run --project src/S7Tools --configuration Debug -- --diag
```

---

## Success Criteria Validation

- [ ] All 14 functional requirements from spec.md implemented
- [ ] Constitutional compliance verified (Clean Architecture, MVVM, Thread Safety, Service Registration)
- [ ] Menu item "Memory Region Profiles" accessible under Settings
- [ ] Job wizard includes Memory Region selection step with validation
- [ ] CRUD operations working with overlap detection and validation
- [ ] S7-1200 firmware template integration with .bss default selection
- [ ] Export/import functionality matching JSON format from template
- [ ] Path management commands consistent with other profiler settings
- [ ] 99.7%+ test pass rate maintained (308+ tests total)
- [ ] 0 build errors, 0 warnings achieved
- [ ] All patterns follow existing profile management architecture exactly

## Risk Mitigation

**Domain Model Complexity**: Follow established MemorySegment structure from data-model.md with proven overlap detection algorithm
**Service Integration**: Use StandardProfileManager<T> pattern proven by PowerSupplyProfileService, SerialPortProfileService
**UI Consistency**: Follow ProfileManagementViewModelBase<T> and existing Settings ViewModel patterns exactly
**Job Wizard Integration**: Use established StepViewModel pattern from existing job wizard steps
**Thread Safety**: Apply Internal Method Pattern consistently throughout service layer
**Constitutional Compliance**: Validate each task against systemPatterns.md requirements

## Summary

**Total Tasks**: 59 tasks across 6 phases
**Task Breakdown**:
- Phase 1 (Setup): 7 tasks
- Phase 2 (Foundation): 8 tasks (BLOCKING)
- Phase 3 (US1 - MVP): 17 tasks
- Phase 4 (US2 - Job Integration): 10 tasks
- Phase 5 (US3 - Administration): 10 tasks
- Phase 6 (Polish): 7 tasks

**Parallel Opportunities**: 25 tasks marked [P] can run in parallel within their phases

**MVP Scope**: Phases 1-3 deliver complete memory region profile management through Settings interface

**Independent Test Criteria**: Each user story has specific independent test scenarios ensuring standalone functionality

**Implementation follows established patterns**: All tasks leverage proven S7Tools architecture patterns for consistency and quality
