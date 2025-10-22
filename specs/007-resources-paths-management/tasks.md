````markdown
# Tasks: Resources and Settings Paths Management

**Feature**: 007-resources-paths-management
**Input**: Design documents from `/specs/007-resources-paths-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Constitution Compliance** (Reference: `.specify/memory/constitution.md` v1.0.0):

- **Test-First Quality Gates (MODIFIED)**: No testing projects initially - focus on building solid base implementation first
- **Clean Architecture**: Tasks respect layering (Core ← Infrastructure/UI, never the reverse)
- **Thread Safety**: File operations use async/await patterns, no semaphore usage required
- **Service Registration**: New services registered in ServiceCollectionExtensions.cs, never Program.cs

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure for path management

- [ ] T001 Create path management models directory structure in src/S7Tools.Core/Models/Configuration/
- [ ] T002 Create path management service interfaces directory structure in src/S7Tools.Core/Interfaces/Services/
- [ ] T003 [P] Create path management exceptions directory in src/S7Tools.Core/Exceptions/
- [ ] T004 [P] Setup ResourcePathConstants from contracts in src/S7Tools.Core/Constants/ResourcePathConstants.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Create PathConfiguration model in src/S7Tools.Core/Models/Configuration/PathConfiguration.cs
- [ ] T006 [P] Create ApplicationSettings model in src/S7Tools.Core/Models/Configuration/ApplicationSettings.cs
- [ ] T007 [P] Create ResourceManifest model in src/S7Tools.Core/Models/Configuration/ResourceManifest.cs
- [ ] T008 [P] Create DirectoryInfo model in src/S7Tools.Core/Models/Configuration/DirectoryInfo.cs
- [ ] T009 [P] Create FileInfo model in src/S7Tools.Core/Models/Configuration/FileInfo.cs
- [ ] T010 [P] Create ResourceInfo model in src/S7Tools.Core/Models/Configuration/ResourceInfo.cs
- [ ] T011 [P] Create ResourceType enum in src/S7Tools.Core/Models/Configuration/ResourceType.cs
- [ ] T012 [P] Create CreationStrategy enum in src/S7Tools.Core/Models/Configuration/CreationStrategy.cs
- [ ] T013 Create IPathService interface in src/S7Tools.Core/Interfaces/Services/IPathService.cs
- [ ] T014 [P] Create IApplicationSettingsService interface in src/S7Tools.Core/Interfaces/Services/IApplicationSettingsService.cs
- [ ] T015 [P] Create IResourceManagerService interface in src/S7Tools.Core/Interfaces/Services/IResourceManagerService.cs
- [ ] T016 [P] Create path management specific exceptions in src/S7Tools.Core/Exceptions/PathResolutionException.cs
- [ ] T017 [P] Create settings specific exceptions in src/S7Tools.Core/Exceptions/SettingsLoadException.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Dynamic Path Management (Priority: P1) 🎯 MVP

**Goal**: Implement dynamic path resolution for resources and settings based on executable location

**Independent Test**: Verify that all resource paths are dynamically resolved and files are created in the correct locations relative to the executable

### Implementation for User Story 1

- [ ] T018 [US1] Implement PathService in src/S7Tools/Services/PathService.cs with executable-relative path resolution
- [ ] T019 [US1] Add path validation and directory creation logic to PathService
- [ ] T020 [US1] Add structured logging for all path operations in PathService
- [ ] T021 [US1] Implement ResourceManagerService in src/S7Tools/Services/ResourceManagerService.cs for resource initialization
- [ ] T022 [US1] Add manifest loading and resource creation logic to ResourceManagerService
- [ ] T023 [US1] Add error recovery and validation to ResourceManagerService
- [ ] T024 [US1] Register path management services in src/S7Tools/Extensions/ServiceCollectionExtensions.cs
- [ ] T025 [US1] Add path service initialization during app startup in src/S7Tools/App.axaml.cs
- [ ] T026 [US1] Verify dynamic folder structure creation on first run

**Checkpoint**: At this point, User Story 1 should be fully functional - dynamic path resolution works

---

## Phase 4: User Story 2 - Settings Hierarchy (Priority: P1)

**Goal**: Implement user settings that take precedence over default settings without overwriting defaults

**Independent Test**: Verify that user settings override defaults without modifying the default settings file

### Implementation for User Story 2

- [ ] T027 [US2] Implement ApplicationSettingsService in src/S7Tools/Services/ApplicationSettingsService.cs with layered configuration
- [ ] T028 [US2] Add default settings loading logic to ApplicationSettingsService
- [ ] T029 [US2] Add user settings override logic to ApplicationSettingsService
- [ ] T030 [US2] Add settings merging and effective settings computation to ApplicationSettingsService
- [ ] T031 [US2] Add change detection and notifications to ApplicationSettingsService
- [ ] T032 [US2] Add settings persistence with atomic write operations to ApplicationSettingsService
- [ ] T033 [US2] Register ApplicationSettingsService in src/S7Tools/Extensions/ServiceCollectionExtensions.cs
- [ ] T034 [US2] Update existing services to use new settings resolution instead of hardcoded paths
- [ ] T035 [US2] Verify user settings override behavior with test scenarios

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - settings hierarchy functional

---

## Phase 5: User Story 3 - Error Handling and Logging (Priority: P2)

**Goal**: Implement robust error handling with detailed logging for path resolution issues

**Independent Test**: Verify that all path resolution errors are logged with sufficient detail

### Implementation for User Story 3

- [ ] T036 [US3] Add comprehensive error handling to PathService for permission issues
- [ ] T037 [US3] Add detailed error logging to PathService with file paths and error reasons
- [ ] T038 [US3] Add graceful degradation for read-only environments to PathService
- [ ] T039 [US3] Add error handling to ApplicationSettingsService for corrupted settings files
- [ ] T040 [US3] Add error recovery to ResourceManagerService for creation failures
- [ ] T041 [US3] Add error notifications to status bar for critical path failures
- [ ] T042 [US3] Add error metrics collection for observability
- [ ] T043 [US3] Verify error scenarios with permission testing and invalid path testing

**Checkpoint**: All user stories should now be independently functional with robust error handling

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and finalization

- [ ] T044 [P] Update existing hardcoded paths throughout codebase to use new path services
- [ ] T045 [P] Remove any remaining hardcoded string constants and replace with dynamic resolution
- [ ] T046 [P] Add XML documentation to all public APIs for path management services
- [ ] T047 [P] Performance optimization - cache resolved paths and minimize file system calls
- [ ] T048 [P] Add cross-platform path handling validation for Windows/Linux/macOS
- [ ] T049 [P] Update application initialization to ensure proper service order during startup
- [ ] T050 Verify build process completes successfully with 30-second wait verification
- [ ] T051 Run quickstart.md validation scenarios to ensure all requirements met

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P1 → P2)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - May integrate with US1 path services but should be independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Enhances US1 and US2 with error handling but should be independently testable

### Within Each User Story

- Core models before services
- Services before registration
- Registration before initialization
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational model tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- Within each story, tasks marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# These can run together after T018 completes:
Task: "Add path validation and directory creation logic to PathService"
Task: "Add structured logging for all path operations in PathService"

# These can run together after ResourceManagerService is created:
Task: "Add manifest loading and resource creation logic to ResourceManagerService"
Task: "Add error recovery and validation to ResourceManagerService"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Dynamic Path Management)
4. **STOP and VALIDATE**: Test User Story 1 independently - verify paths resolve correctly
5. Deploy/demo if ready - application works with dynamic paths

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (Settings hierarchy)
4. Add User Story 3 → Test independently → Deploy/Demo (Error handling)
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Dynamic paths)
   - Developer B: User Story 2 (Settings hierarchy)
   - Developer C: User Story 3 (Error handling)
3. Stories complete and integrate independently

---

## Specific Folder Structure to Create

The implementation will create this exact structure from executable location:

```
Resources/
├── AppSettings/
│   └── AppSettings.json
├── Profiles/
│   ├── Serial/
│   │   └── SerialProfiles.json
│   ├── Socat/
│   │   └── SocatProfiles.json
│   ├── PowerSupply/
│   │   └── PowerSupplyProfiles.json
│   └── MemoryRegions/
│       └── **/* (various memory region files)
├── Logs/
│   ├── Main/
│   │   └── MainLog_{timestamp}_{RollingNumber}.json
│   └── Exported/
│       ├── CSV/
│       ├── TXT/
│       └── JSON/
├── Jobs/
│   └── Jobs.json
├── Tasks/
│   └── Tasks.json
├── Payloads/
│   └── **/* (various payload files)
└── Dumps/
    └── **/* (various dump files)
```

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Manual verification used instead of unit tests initially
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Focus on eliminating all hardcoded paths from existing codebase
- Ensure cross-platform compatibility for Windows, Linux, and macOS
