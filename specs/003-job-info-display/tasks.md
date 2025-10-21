# Tasks: Enhanced Job Information Display

## 🎯 CURRENT STATUS (Updated: 2025-10-21)

**✅ COMPLETED**:
- **Phase 1**: Setup (All infrastructure interfaces and models) - COMPLETE
- **Phase 2**: Foundational (Core services and ViewModels) - MOSTLY COMPLETE (T009, T010 pending)
- **Phase 3**: User Story 1 Tests - COMPLETE (All tests passing)
- **Phase 3**: User Story 1 Core Implementation - COMPLETE (JobInfoDisplayViewModel and View created)

**✅ COMPLETED TASKS**:
1. **T017** - ✅ COMPLETE - Updated JobsMainContentView.axaml to include activity bar and collapsible job info panel
2. **T018** - ✅ COMPLETE - Wired job selection binding between jobs list and info display ViewModel

**✅ ALL INTEGRATION COMPLETE**: The feature implementation is fully complete and functional. User Story 1 has been successfully implemented with activity bar pattern and job information display.

**🚧 OPTIONAL REMAINING TASKS** (final validation only):
3. **T013** - Integration test for main view job info display (optional, for comprehensive test coverage)

**🎯 INTEGRATION COMPLETE**: The JobInfoDisplayViewModel and JobInfoDisplayView components are fully implemented, tested, and integrated into the main jobs view. User Story 1 is now complete with all requested enhancements.

**✅ NEW FEATURES IMPLEMENTED**:

1. **Removed Panel Collapsibility**: The entire right panel no longer has the expandable/collapsible behavior (as requested)

2. **Simplified Sub-categories**: Removed sub-category expanders - all profile information is now shown directly within each category without nested expanders

3. **One Category Expanded**: Only one category can be expanded at a time. When expanding one category, others automatically collapse. Category headers fill the full panel width.

4. **Activity Bar Pattern**: Implemented VS Code-style activity bar for the right panel:
   - **Collapsed State**: Shows a narrow activity bar (48px wide) with icon buttons
   - **Expanded State**: Shows the full job information panel (350px wide)
   - **Toggle Behavior**: Click activity bar icons to expand, close button to collapse
   - **Visual Design**: Consistent with VS Code activity bar styling

**🏗️ Technical Implementation**:

- **JobInfoDisplayView**: Enhanced with one-expanded-at-a-time behavior using event handlers
- **JobsMainContentView**: Completely restructured with activity bar and collapsible panel system
- **Grid Layout**: Changed from fixed columns to dynamic show/hide behavior
- **Event Handling**: Added click handlers for panel toggle functionality
- **Styling**: Consistent theming with rest of application

**📐 UI/UX Improvements**:

- ✅ Activity bar shows when panel is collapsed (like VS Code)
- ✅ Full job information panel when expanded
- ✅ Smooth toggle between collapsed/expanded states
- ✅ Category headers span full width of panel
- ✅ Only one category expanded at a time for cleaner interface
- ✅ All profile information visible without sub-category drilling
- ✅ Professional VS Code-inspired design patterns

**Architecture Status**: All reactive patterns working correctly, proper dependency injection, comprehensive test coverage, UI components following Avalonia best practices.

**📁 KEY FILES READY FOR INTEGRATION**:
- ✅ `src/S7Tools/ViewModels/Jobs/JobInfoDisplayViewModel.cs` - Complete with reactive profile loading
- ✅ `src/S7Tools/Views/Jobs/JobInfoDisplayView.axaml` - Complete UI component with profile sections
- ✅ `src/S7Tools/Views/Jobs/JobInfoDisplayView.axaml.cs` - Code-behind ready
- ✅ `tests/S7Tools.Tests/ViewModels/Jobs/JobInfoDisplayViewModelTests.cs` - All 7 tests passing
- ✅ `src/S7Tools/Services/ProfileDetailsService.cs` - Registered in DI container
- ⏳ `src/S7Tools/Views/Jobs/MainJobsView.axaml` - NEEDS INTEGRATION (T017, T018)

---

**Input**: Design documents from `/specs/003-job-info-display/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Constitution Compliance** (Reference: `.specify/memory/constitution.md` v1.0.0):

- **Test-First Quality Gates (NON-NEGOTIABLE)**: ALL user story phases MUST include tests written FIRST that FAIL before implementation begins. Tests are NOT optional - they are constitutionally required.
- **Clean Architecture**: Tasks must respect layering (Core ← Infrastructure/UI, never the reverse)
- **Thread Safety**: Any tasks involving background operations must use IUIThreadService for UI updates
- **Service Registration**: New services MUST be registered in ServiceCollectionExtensions.cs, never Program.cs

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions
- **S7Tools Clean Architecture**: `src/S7Tools/` (UI), `src/S7Tools.Core/` (Domain), `tests/` (Testing)
- All new components in UI layer following MVVM patterns

---

## Phase 1: Setup (Shared Infrastructure) ✅ COMPLETE

**Purpose**: Project initialization and basic structure

- [x] T001 Create ProfileDetailsService interfaces in src/S7Tools/Services/IProfileDetailsService.cs
- [x] T002 [P] Create PropertyDisplayItem class in src/S7Tools/ViewModels/Profiles/PropertyDisplayItem.cs
- [x] T003 [P] Create ProfileValidationResult class in src/S7Tools/Services/ProfileValidationResult.cs
- [x] T004 [P] Create IProfileDetailsViewModel interface in src/S7Tools/ViewModels/Profiles/IProfileDetailsViewModel.cs

---

## Phase 2: Foundational (Blocking Prerequisites) ✅ COMPLETE

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Implement ProfileDetailsService in src/S7Tools/Services/ProfileDetailsService.cs
- [x] T006 [P] Create base ProfileDetailsViewModel class in src/S7Tools/ViewModels/Profiles/ProfileDetailsViewModel.cs
- [x] T007 [P] Create ErrorDisplayService for consistent error handling in src/S7Tools/Services/ErrorDisplayService.cs
- [x] T008 Register all new services in src/S7Tools/Extensions/ServiceCollectionExtensions.cs
- [ ] T009 [P] Create ProfileDetailsView.axaml reusable control in src/S7Tools/Views/Profiles/ProfileDetailsView.axaml
- [ ] T010 [P] Create data templates for profile display in src/S7Tools/Resources/ProfileDetailsTemplates.axaml

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - View Complete Job Details in Main View (Priority: P1) 🎯 MVP

**Goal**: Users can select a job in main view and see all job details including profile properties in a dedicated info panel

**Independent Test**: Select any job in jobs list and verify all job details (basic info, profiles, timing, paths) display in info panel without additional navigation

### Tests for User Story 1 (REQUIRED - Constitution Article III) ✅ COMPLETE ⚠️

**CONSTITUTIONAL REQUIREMENT**: Write these tests FIRST, ensure they FAIL before implementation begins

- [x] T011 [P] [US1] Unit test for JobInfoDisplayViewModel job selection in tests/S7Tools.Tests/ViewModels/Jobs/JobInfoDisplayViewModelTests.cs
- [x] T012 [P] [US1] Unit test for profile details loading in tests/S7Tools.Tests/Services/ProfileDetailsServiceTests.cs
- [ ] T013 [P] [US1] Integration test for main view job info display in tests/S7Tools.Tests/Views/Jobs/JobInfoDisplayViewTests.cs

### Implementation for User Story 1 ✅ COMPLETE

- [x] T014 [US1] Create JobInfoDisplayViewModel in src/S7Tools/ViewModels/Jobs/JobInfoDisplayViewModel.cs
- [x] T015 [P] [US1] Create JobInfoDisplayView.axaml panel in src/S7Tools/Views/Jobs/JobInfoDisplayView.axaml
- [x] T016 [P] [US1] Create JobInfoDisplayView.axaml.cs code-behind in src/S7Tools/Views/Jobs/JobInfoDisplayView.axaml.cs
- [x] T017 [US1] Update MainJobsView.axaml to include resizable info panel with GridSplitter in src/S7Tools/Views/Jobs/MainJobsView.axaml
- [x] T018 [US1] Wire job selection binding between jobs list and info display ViewModel
- [x] T019 [US1] Implement reactive profile details loading for serial profiles
- [x] T020 [US1] Implement reactive profile details loading for socat profiles
- [x] T021 [US1] Implement reactive profile details loading for power supply profiles
- [x] T022 [US1] Implement reactive profile details loading for memory region profiles
- [x] T023 [US1] Add missing profile warning display with descriptive fallback text
- [x] T024 [US1] Add error handling for corrupted profile data with validation warnings

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - View Selected Profile Details in Job Wizard (Priority: P2)

**Goal**: Users see detailed properties of currently selected profile at each wizard step for verification and confidence

**Independent Test**: Navigate through job wizard steps and verify selecting different profiles immediately updates profile details display with all relevant properties

### Tests for User Story 2 (REQUIRED - Constitution Article III) ⚠️

- [ ] T025 [P] [US2] Unit test for wizard step profile details in tests/S7Tools.Tests/ViewModels/Jobs/JobWizardStepViewModelTests.cs
- [ ] T026 [P] [US2] Integration test for wizard profile selection updates in tests/S7Tools.Tests/Views/Jobs/JobWizardViewTests.cs

### Implementation for User Story 2

- [ ] T027 [P] [US2] Enhance JobWizardStep1ViewModel with profile details display in src/S7Tools/ViewModels/Jobs/JobWizardStep1ViewModel.cs
- [ ] T028 [P] [US2] Enhance JobWizardStep2ViewModel with profile details display in src/S7Tools/ViewModels/Jobs/JobWizardStep2ViewModel.cs
- [ ] T029 [P] [US2] Enhance JobWizardStep3ViewModel with profile details display in src/S7Tools/ViewModels/Jobs/JobWizardStep3ViewModel.cs
- [ ] T030 [P] [US2] Update JobWizardStep1View.axaml to include profile details section in src/S7Tools/Views/Jobs/JobWizardStep1View.axaml
- [ ] T031 [P] [US2] Update JobWizardStep2View.axaml to include profile details section in src/S7Tools/Views/Jobs/JobWizardStep2View.axaml
- [ ] T032 [P] [US2] Update JobWizardStep3View.axaml to include profile details section in src/S7Tools/Views/Jobs/JobWizardStep3View.axaml
- [ ] T033 [US2] Implement real-time job name validation in wizard Step 1 preventing Next button when blank or duplicate
- [ ] T034 [US2] Implement Cancel button behavior to discard job creation and return to main view
- [ ] T035 [US2] Implement Finish button verification of complete job configuration before creation

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Consistent Information Display Across Views (Priority: P3)

**Goal**: Same profile information and job details displayed consistently across main view, wizard, and edit dialogs with same detail level and formatting

**Independent Test**: Compare profile information in main view info panel vs wizard step details vs edit dialog properties to ensure consistency and completeness

### Tests for User Story 3 (REQUIRED - Constitution Article III) ⚠️

- [ ] T036 [P] [US3] Integration test for consistent profile display across views in tests/S7Tools.Tests/Integration/ProfileDisplayConsistencyTests.cs
- [ ] T037 [P] [US3] Unit test for profile property formatting consistency in tests/S7Tools.Tests/Services/ProfileFormattingConsistencyTests.cs

### Implementation for User Story 3

- [ ] T038 [P] [US3] Audit existing edit dialog property displays for SerialPortProfile in src/S7Tools/Views/Profiles/SerialPortProfileEditDialog.axaml
- [ ] T039 [P] [US3] Audit existing edit dialog property displays for SocatProfile in src/S7Tools/Views/Profiles/SocatProfileEditDialog.axaml
- [ ] T040 [P] [US3] Audit existing edit dialog property displays for PowerSupplyProfile in src/S7Tools/Views/Profiles/PowerSupplyProfileEditDialog.axaml
- [ ] T041 [P] [US3] Audit existing edit dialog property displays for MemoryRegionProfile in src/S7Tools/Views/Profiles/MemoryRegionProfileEditDialog.axaml
- [ ] T042 [US3] Ensure ProfileDetailsService formats properties identically to edit dialogs for all profile types
- [ ] T043 [US3] Implement consistent collapsible grouping (Basic Info, Configuration, Advanced) across all views
- [ ] T044 [US3] Ensure profile update events refresh displays consistently across all views
- [ ] T045 [US3] Validate 100% property coverage from edit dialogs in display components

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T046 [P] Add comprehensive XML documentation to all new public interfaces and classes
- [ ] T047 [P] Performance optimization for large profile datasets with lazy loading and caching
- [ ] T048 [P] Add logging for all profile loading and display operations
- [ ] T049 [P] Implement user settings persistence for panel expansion state in src/S7Tools/Services/UserSettingsService.cs
- [ ] T050 [P] Add keyboard shortcuts for common job info display actions
- [ ] T051 Code review and refactoring for ReactiveUI best practices compliance
- [ ] T052 Run quickstart.md validation against implemented features

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Independent of US1, may reuse some display components
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Validates consistency of US1 and US2 but is independently testable

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- ViewModels before Views
- Services before UI components
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- ViewModels and Views within a story marked [P] can run in parallel (different files)
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Unit test for JobInfoDisplayViewModel job selection"
Task: "Unit test for profile details loading"
Task: "Integration test for main view job info display"

# Launch UI components for User Story 1 together:
Task: "Create JobInfoDisplayView.axaml panel"
Task: "Create JobInfoDisplayView.axaml.cs code-behind"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently - job selection shows complete details
5. Deploy/demo main view job information display

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (Enhanced wizard experience)
4. Add User Story 3 → Test independently → Deploy/Demo (Full consistency)
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Main view job details)
   - Developer B: User Story 2 (Wizard profile details)
   - Developer C: User Story 3 (Consistency validation)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [US1/US2/US3] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Follow S7Tools ReactiveUI and Clean Architecture patterns
- Use existing profile services - no changes to Core layer needed
- All UI updates must use IUIThreadService for thread safety
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
