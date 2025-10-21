````markdown
# Tasks: Remove Options and Flags Columns from Profile List Viewers

**Input**: Design documents from `/specs/002-remove-profile-list-columns/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Constitution Compliance** (Reference: `.specify/memory/constitution.md` v1.0.0):

- **Test-First Quality Gates**: Manual UI verification required for pure cosmetic changes (tests are optional for UI-only modifications)
- **Clean Architecture**: This is a pure UI layer change with no cross-layer impacts
- **Thread Safety**: No concurrency concerns for static XAML column removal
- **Service Registration**: No new services required

**Organization**: This feature implements a single user story (UI simplification) with no complex dependencies.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1)
- Include exact file paths in descriptions

## Path Conventions
Based on plan.md: Single .NET project structure with `src/` at repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and verification of current state

- [X] T001 Verify current project builds successfully: `dotnet build src/S7Tools.sln --configuration Debug`
- [X] T002 [P] Backup current XAML files before modification to allow easy rollback
- [X] T003 [P] Verify all target views exist and contain Options/Flags columns

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Analyze current column structure and prepare for removal

**⚠️ CRITICAL**: Analysis must be complete before any XAML modification

- [X] T004 Analyze current DataGrid column structure in all four target views
- [X] T005 Document exact line numbers and column definitions to be removed
- [X] T006 Verify ViewModel properties for Options and Flags are preserved (no changes needed)

**Checkpoint**: Foundation ready - column removal can now begin

---

## Phase 3: User Story 1 - Remove Options and Flags Columns (Priority: P1) 🎯 MVP

**Goal**: Remove Options and Flags columns from all profile list viewers to simplify UI and improve usability

**Independent Test**: Visual verification that Options and Flags columns no longer appear in any profile list view, while Edit dialogs still provide full access to these fields

### Manual Verification for User Story 1 (UI Testing Required)

**UI Verification Approach**: Manual testing appropriate for pure cosmetic changes with no business logic impact

- [X] T007 [P] [US1] Pre-change verification: Document current column layout in all four views
- [X] T008 [P] [US1] Pre-change verification: Confirm Options and Flags data is accessible in Edit dialogs

### Implementation for User Story 1

- [X] T009 [P] [US1] Remove Options and Flags columns from src/S7Tools/Views/SerialPortsSettingsView.axaml
- [X] T010 [P] [US1] Remove Options and Flags columns from src/S7Tools/Views/SocatSettingsView.axaml
- [X] T011 [P] [US1] Remove Options and Flags columns from src/S7Tools/Views/PowerSupplySettingsView.axaml
- [X] T012 [P] [US1] Remove Options and Flags columns from src/S7Tools/Views/JobsMainContentView.axaml
- [X] T013 [US1] Build project to verify no compilation errors: `dotnet build src/S7Tools.sln --configuration Debug`
- [X] T014 [US1] Run application to verify no runtime errors: `dotnet run --project src/S7Tools`

**Checkpoint**: At this point, all profile list views should no longer display Options and Flags columns

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and validation

- [X] T015 [P] [US1] Visual verification: Confirm Options and Flags columns removed from SerialPortsSettingsView
- [X] T016 [P] [US1] Visual verification: Confirm Options and Flags columns removed from SocatSettingsView
- [X] T017 [P] [US1] Visual verification: Confirm Options and Flags columns removed from PowerSupplySettingsView
- [X] T018 [P] [US1] Visual verification: Confirm Options and Flags columns removed from JobsMainContentView
- [X] T019 [P] [US1] Functional verification: Confirm Options and Flags remain editable in all Edit dialogs
- [X] T020 [P] [US1] Functional verification: Test profile CRUD operations work normally
- [X] T021 [US1] Run quickstart.md validation steps to ensure all success criteria met

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - critical analysis phase
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **Polish (Phase 4)**: Depends on User Story 1 completion

### User Story Dependencies

- **User Story 1 (P1)**: Single story implementation - no dependencies on other stories
- This feature implements only one user story (UI simplification)

### Within User Story 1

- Pre-change verification MUST happen before any XAML modifications
- All four XAML files can be modified in parallel (marked [P])
- Build verification MUST happen after all XAML changes
- Visual verification MUST happen after successful build and run

### Parallel Opportunities

- **Setup Phase**: Tasks T002 and T003 can run in parallel
- **Foundational Phase**: No parallel opportunities (sequential analysis required)
- **User Story 1 Implementation**: Tasks T009, T010, T011, T012 can all run in parallel (different files)
- **Polish Phase**: All verification tasks T015-T020 can run in parallel

---

## Parallel Example: User Story 1 Implementation

```bash
# Launch all XAML modifications together:
Task: "Remove Options and Flags columns from src/S7Tools/Views/SerialPortsSettingsView.axaml"
Task: "Remove Options and Flags columns from src/S7Tools/Views/SocatSettingsView.axaml"
Task: "Remove Options and Flags columns from src/S7Tools/Views/PowerSupplySettingsView.axaml"
Task: "Remove Options and Flags columns from src/S7Tools/Views/JobsMainContentView.axaml"

# Launch all verification tasks together:
Task: "Visual verification: Confirm Options and Flags columns removed from SerialPortsSettingsView"
Task: "Visual verification: Confirm Options and Flags columns removed from SocatSettingsView"
Task: "Visual verification: Confirm Options and Flags columns removed from PowerSupplySettingsView"
Task: "Visual verification: Confirm Options and Flags columns removed from JobsMainContentView"
```

---

## Implementation Strategy

### MVP First (Complete Feature in One Pass)

This feature represents a single, complete user story with no incremental delivery options:

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (analyze current state)
3. Complete Phase 3: User Story 1 (remove columns)
4. Complete Phase 4: Polish (verify success)
5. **STOP and VALIDATE**: Test feature independently using quickstart.md criteria

### Single Story Delivery

This feature cannot be delivered incrementally as it represents one atomic UI improvement:

1. Setup + Foundational → Ready for implementation
2. User Story 1 → Complete feature delivered
3. Polish → Verified and ready for deployment

The feature is "all or nothing" - partial column removal would create inconsistent UX across profile types.

### Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (coordination required)
2. For User Story 1 implementation:
   - Developer A: SerialPortsSettingsView.axaml + SocatSettingsView.axaml
   - Developer B: PowerSupplySettingsView.axaml + JobsMainContentView.axaml
3. Team collaborates on build verification and testing

---

## Success Criteria Summary

1. **Visual Verification**: Options and Flags columns removed from all four profile list views
2. **Functionality Preservation**: Options and Flags remain fully editable in Edit dialogs
3. **No Regressions**: All profile management operations continue to work normally
4. **Build Success**: Project builds and runs without errors
5. **UI Improvement**: Profile lists display cleaner, more focused layout

---

## Technical Implementation Details

### Column Removal Pattern

Each XAML file requires removal of two DataGridTextColumn definitions:

```xml
<!-- Remove this entire block for Options -->
<DataGridTextColumn Binding="{Binding Options}" Width="150" MinWidth="100">
  <DataGridTextColumn.Header>
    <TextBlock Text="Options" Margin="0,0,8,0" TextTrimming="CharacterEllipsis" />
  </DataGridTextColumn.Header>
</DataGridTextColumn>

<!-- Remove this entire block for Flags -->
<DataGridTextColumn Binding="{Binding Flags}" Width="120" MinWidth="80">
  <DataGridTextColumn.Header>
    <TextBlock Text="Flags" Margin="0,0,8,0" TextTrimming="CharacterEllipsis" />
  </DataGridTextColumn.Header>
</DataGridTextColumn>
```

### Rollback Strategy

If rollback is needed:
```bash
git checkout HEAD~1 -- src/S7Tools/Views/SerialPortsSettingsView.axaml
git checkout HEAD~1 -- src/S7Tools/Views/SocatSettingsView.axaml
git checkout HEAD~1 -- src/S7Tools/Views/PowerSupplySettingsView.axaml
git checkout HEAD~1 -- src/S7Tools/Views/JobsMainContentView.axaml
```

---

## Notes

- [P] tasks = different files, can be worked on simultaneously
- [US1] label maps all tasks to the single user story for traceability
- Manual UI testing is appropriate for pure cosmetic changes
- No data model or business logic changes required
- Zero risk of data loss or functional regression
- Feature can be completed in a single development session
