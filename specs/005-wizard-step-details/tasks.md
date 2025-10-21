````markdown
# Tasks: Enhanced Wizard Step Profile Details

**Input**: Design documents from `/specs/005-wizard-step-details/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Constitution Compliance** (Reference: `.specify/memory/constitution.md` v1.0.0):

- **Test-First Quality Gates (NON-NEGOTIABLE)**: ALL user story phases MUST include tests written FIRST that FAIL before implementation begins. Tests are NOT optional - they are constitutionally required.
- **Clean Architecture**: Tasks must respect layering (Core ← Infrastructure/UI, never the reverse)
- **Thread Safety**: Any tasks involving background operations must use IUIThreadService for UI updates
- **Service Registration**: New services MUST be registered in ServiceCollectionExtensions.cs, never Program.cs

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions
- **Source**: `src/S7Tools/` for UI/ViewModels, `src/S7Tools.Core/` for domain models
- **Tests**: `tests/S7Tools.Tests/` for unit tests, `tests/S7Tools.Core.Tests/` for core tests

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Verify existing JobWizardViewModel structure in src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [X] T002 [P] Verify existing JobWizardView.axaml structure in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [X] T003 [P] Verify existing JobInfoDisplayView.axaml patterns in src/S7Tools/Views/Jobs/JobInfoDisplayView.axaml

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Analyze existing profile models in src/S7Tools.Core/Models/ for property access patterns
- [X] T005 [P] Review existing reactive property patterns in JobWizardViewModel for extension approach
- [X] T006 [P] Examine existing XAML styling patterns in JobWizardView.axaml for consistency requirements

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - View Complete Profile Configuration in Wizard Steps (Priority: P1) 🎯 MVP

**Goal**: Display comprehensive profile configuration details within existing wizard step profile details sections for all three profile types (Serial, Socat, Power Supply)

**Independent Test**: Navigate to any wizard step, select different profiles from dropdown, and verify that comprehensive profile details are displayed immediately within the existing profile details sections, showing all configuration properties, flags, options, and metadata exactly as shown in the main job management view.

### Tests for User Story 1 (REQUIRED - Constitution Article III) ⚠️

**CONSTITUTIONAL REQUIREMENT**: Write these tests FIRST, ensure they FAIL before implementation begins

- [ ] T007 [P] [US1] Create unit tests for serial profile computed properties in tests/S7Tools.Tests/ViewModels/Jobs/JobWizardViewModelTests.cs
- [ ] T008 [P] [US1] Create unit tests for socat profile computed properties in tests/S7Tools.Tests/ViewModels/Jobs/JobWizardViewModelTests.cs
- [ ] T009 [P] [US1] Create unit tests for power profile computed properties in tests/S7Tools.Tests/ViewModels/Jobs/JobWizardViewModelTests.cs
- [ ] T010 [P] [US1] Create integration tests for profile selection UI updates in tests/S7Tools.Tests/Views/Jobs/JobWizardViewTests.cs

### Implementation for User Story 1

#### Serial Profile Detail Properties
- [ ] T011 [P] [US1] Add serial profile basic settings computed properties (BaudRate, CharacterSize, Parity, StopBits) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T012 [P] [US1] Add serial profile control flags computed properties (EnableReceiver, DisableHardwareFlowControl, ParityEnabled, OddParity) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T013 [P] [US1] Add serial profile input flags computed properties (IgnoreBreak, DisableBreakInterrupt, DisableMapCRtoNL, DisableBellOnQueueFull, DisableXonXoffFlowControl) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T014 [P] [US1] Add serial profile output flags computed properties (DisableOutputProcessing, DisableMapNLtoCRNL) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T015 [P] [US1] Add serial profile local flags computed properties (DisableCanonicalMode, DisableSignalGeneration, DisableExtendedProcessing, DisableEcho, DisableEchoErase, DisableEchoKill, DisableEchoControl, DisableEchoKillErase) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T016 [P] [US1] Add serial profile special modes computed properties (RawMode) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T017 [P] [US1] Add serial profile metadata computed properties (Version, CreatedAt, ModifiedAt, IsReadOnly, IsDefault) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs

#### Socat Profile Detail Properties
- [ ] T018 [P] [US1] Add socat profile TCP settings computed properties (TcpPort, TcpHost, EnableFork, EnableReuseAddr) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T019 [P] [US1] Add socat profile flags computed properties (Verbose, HexDump, BlockSize, DebugLevel) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T020 [P] [US1] Add socat profile serial device settings computed properties (SerialRawMode, SerialDisableEcho) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T021 [P] [US1] Add socat profile process management computed properties (AutoConfigureSerial, ConnectionTimeout, AutoRestart) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T022 [P] [US1] Add socat profile metadata computed properties (Version, CreatedAt, ModifiedAt, IsReadOnly, IsDefault) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs

#### Power Supply Profile Detail Properties
- [ ] T023 [P] [US1] Add power profile connection settings computed properties (Host, Port, DeviceId) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T024 [P] [US1] Add power profile Modbus configuration computed properties (AddressingMode, ConnectionTimeoutMs, ReadTimeoutMs, WriteTimeoutMs) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T025 [P] [US1] Add power profile control settings computed properties (OnOffCoil, EnableAutoReconnect, MaxRetryAttempts) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T026 [P] [US1] Add power profile metadata computed properties (Version, CreatedAt, ModifiedAt, IsReadOnly, IsDefault) to src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs

#### XAML Enhancement for Profile Details
- [ ] T027 [US1] Enhance serial profile details section in JobWizardView.axaml with comprehensive property display including Basic Settings, Control Flags, Input Flags, Output Flags, Local Flags, Special Modes, and Metadata sections in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T028 [US1] Enhance socat profile details section in JobWizardView.axaml with comprehensive property display including TCP Settings, Socat Flags, Serial Device Settings, Process Management, and Metadata sections in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T029 [US1] Enhance power profile details section in JobWizardView.axaml with comprehensive property display including Connection Settings, Modbus Configuration, Control Settings, and Metadata sections in src/S7Tools/Views/Jobs/JobWizardView.axaml

#### Integration and Validation
- [ ] T030 [US1] Verify all computed properties handle null profile selections with appropriate fallback values ("N/A") in src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T031 [US1] Validate profile detail updates occur within 100ms performance requirement when changing profile selections
- [ ] T032 [US1] Ensure all profile detail sections use ScrollViewer for handling extensive property lists in src/S7Tools/Views/Jobs/JobWizardView.axaml

**Checkpoint**: At this point, User Story 1 should be fully functional - users can see complete profile configuration details within existing wizard step details sections

---

## Phase 4: User Story 2 - Consistent Detail Display Formatting (Priority: P2)

**Goal**: Ensure the detailed profile information shown inline within wizard step profile details sections uses consistent formatting, styling, and organization as the main job management view

**Independent Test**: Compare the visual presentation of profile details between wizard step details sections and the main job information display to verify identical styling, organization, and completeness while maintaining the wizard's existing layout structure.

### Tests for User Story 2 (REQUIRED - Constitution Article III) ⚠️

- [ ] T033 [P] [US2] Create visual consistency tests comparing wizard profile details with JobInfoDisplayView formatting in tests/S7Tools.Tests/Views/Jobs/JobWizardViewStyleTests.cs
- [ ] T034 [P] [US2] Create property organization tests verifying logical grouping matches main view patterns in tests/S7Tools.Tests/ViewModels/Jobs/JobWizardViewModelOrganizationTests.cs

### Implementation for User Story 2

#### Styling Consistency
- [ ] T035 [P] [US2] Apply consistent Border styling (#2D2D2D background, #464647 borders) to all profile detail sections in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T036 [P] [US2] Apply consistent Grid layout (150px label column, * value column) to all property displays in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T037 [P] [US2] Apply consistent TextBlock styling (#CCCCCC foreground, appropriate FontWeight) to all property labels and values in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T038 [P] [US2] Apply consistent spacing (8px section spacing, 4px property spacing) throughout all profile detail sections in src/S7Tools/Views/Jobs/JobWizardView.axaml

#### Section Organization
- [ ] T039 [US2] Organize serial profile properties into logical sections matching JobInfoDisplayView patterns (Basic Settings, Control Flags, Input Flags, Output Flags, Local Flags, Special Modes, Metadata) in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T040 [US2] Organize socat profile properties into logical sections matching JobInfoDisplayView patterns (TCP Settings, Socat Flags, Serial Device Settings, Process Management, Metadata) in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T041 [US2] Organize power profile properties into logical sections matching JobInfoDisplayView patterns (Connection Settings, Modbus Configuration, Control Settings, Metadata) in src/S7Tools/Views/Jobs/JobWizardView.axaml

#### Property Label and Value Consistency
- [ ] T042 [P] [US2] Ensure property labels match exactly with JobInfoDisplayView terminology in src/S7Tools/Views/Jobs/JobWizardView.axaml
- [ ] T043 [P] [US2] Ensure value formatting (Yes/No for booleans, yyyy-MM-dd HH:mm for dates) matches JobInfoDisplayView patterns in src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T044 [P] [US2] Validate section headers use consistent styling and naming with JobInfoDisplayView in src/S7Tools/Views/Jobs/JobWizardView.axaml

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - profile details display with complete information AND consistent formatting

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T045 [P] Add comprehensive XML documentation for all new computed properties in src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs
- [ ] T046 [P] Add performance logging for profile detail update operations using ILogger<JobWizardViewModel>
- [ ] T047 [P] Validate XAML structure follows Avalonia best practices for ScrollViewer and Grid layouts
- [ ] T048 [P] Run quickstart.md validation scenarios to ensure feature works as expected
- [ ] T049 Code cleanup and refactoring of computed properties for maintainability
- [ ] T050 Performance optimization validation ensuring <100ms update requirement is met across all scenarios

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May reference US1 styling but should be independently testable

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Computed properties before XAML binding
- Basic properties before complex properties
- Profile-specific sections before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, both user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Computed properties for different profile types marked [P] can run in parallel
- XAML styling tasks marked [P] can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Create unit tests for serial profile computed properties"
Task: "Create unit tests for socat profile computed properties"
Task: "Create unit tests for power profile computed properties"
Task: "Create integration tests for profile selection UI updates"

# Launch all serial profile computed properties together:
Task: "Add serial profile basic settings computed properties"
Task: "Add serial profile control flags computed properties"
Task: "Add serial profile input flags computed properties"
Task: "Add serial profile output flags computed properties"
Task: "Add serial profile local flags computed properties"
Task: "Add serial profile special modes computed properties"
Task: "Add serial profile metadata computed properties"

# Similar parallel groups for socat and power profile properties
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (comprehensive profile details)
   - Developer B: User Story 2 (formatting consistency)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Focus on reusing existing JobInfoDisplayView patterns for consistency
- Maintain existing wizard navigation flow while enhancing detail display
- All computed properties must handle null profiles gracefully with "N/A" fallbacks
- Performance requirement: profile detail updates must complete within 100ms

---

## Task Summary

**Total Tasks**: 50
- **Setup Phase**: 3 tasks
- **Foundational Phase**: 3 tasks
- **User Story 1**: 26 tasks (4 tests + 22 implementation)
- **User Story 2**: 12 tasks (2 tests + 10 implementation)
- **Polish Phase**: 6 tasks

**Parallel Opportunities**: 35 tasks marked [P] can run in parallel within their phases

**Independent Test Criteria**:
- **US1**: Complete profile configuration details visible in all wizard steps with immediate updates
- **US2**: Visual consistency with main job information display formatting and organization

**Suggested MVP Scope**: User Story 1 only - provides core functionality of displaying comprehensive profile details in wizard steps

**Performance Validation**: All profile detail updates must complete within 100ms requirement as specified in success criteria
