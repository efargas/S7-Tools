````markdown
---
description: "Task list for Job Panel Layout Refactor feature implementation"
---

# Tasks: Job Panel Layout Refactor

**Input**: Design documents from `/specs/004-job-panel-refactor/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Constitution Compliance** (Reference: `.specify/memory/constitution.md` v1.0.0):

- **Clean Architecture**: Tasks respect layering (UI changes only, no impact on Core/domain)
- **MVVM with ReactiveUI**: Layout changes maintain existing reactive patterns
- **Thread Safety**: All layout operations are UI thread operations, no background threading concerns
- **Service Registration**: No new services required for this UI layout enhancement

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions
- **Single project**: `src/S7Tools/` for UI components, `tests/S7Tools.Tests/` for tests
- All paths relative to repository root `/home/kali/WS/S7-Tools/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and environment verification

- [x] T001 Verify current build status and ensure clean baseline in src/S7Tools.sln
- [x] T002 [P] Review existing JobsMainContentView layout structure in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T003 [P] Backup current layout implementation to backup/xaml/JobsMainContentView-backup.axaml

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core layout infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Analyze current Grid column configuration in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T005 Review existing panel state management methods in src/S7Tools/Views/JobsMainContentView.axaml.cs
- [x] T006 Document current layout constraints and identify modification points
- [x] T007 [P] Validate existing responsive behavior baseline for comparison testing

**Checkpoint**: Foundation analysis complete - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Horizontal Scrolling for Main Job Profile View (Priority: P1) 🎯 MVP

**Goal**: Enable horizontal scrolling in the main job profile view when content exceeds available width

**Independent Test**: Expand right panel and verify horizontal scrollbar automatically appears in main content area when job profiles DataGrid content exceeds visible width

### Implementation for User Story 1

- [x] T008 [P] [US1] Wrap main content area (Column 0) in ScrollViewer with HorizontalScrollBarVisibility="Auto" in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T009 [US1] Configure ScrollViewer properties for optimal performance (CanContentScroll="True") in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T010 [US1] Ensure proper content measurement for DataGrid within ScrollViewer in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T011 [US1] Test horizontal scrolling with collapsed panel state - verify no unnecessary scrollbars
- [x] T012 [US1] Test horizontal scrolling with expanded panel (400px width) - verify scrollbar appears when needed
- [x] T013 [US1] Test scroll position preservation during panel state transitions
- [x] T014 [US1] Validate 60fps scrolling performance during horizontal scroll operations

**Checkpoint**: At this point, horizontal scrolling should be fully functional and testable independently

---

## Phase 4: User Story 2 - Dynamic Responsive Layout Sizing (Priority: P1)

**Goal**: Make main job profile view dynamically claim all available space as panels resize, with responsive behavior

**Independent Test**: Resize window and right panel in various combinations and verify main content area always claims maximum available space with proper proportional adjustments

### Implementation for User Story 2

- [x] T015 [P] [US2] Update Grid column definitions for dynamic space claiming (ensure Column 0 uses star sizing) in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T016 [US2] Enhance OnJobInfoToggleClick method for improved collapsed→expanded transition in src/S7Tools/Views/JobsMainContentView.axaml.cs
- [x] T017 [US2] Enhance OnCloseJobInfoPanelClick method for improved expanded→collapsed transition in src/S7Tools/Views/JobsMainContentView.axaml.cs
- [x] T018 [US2] Implement real-time layout feedback during GridSplitter resize operations in src/S7Tools/Views/JobsMainContentView.axaml.cs
- [x] T019 [US2] Add MinWidth/MaxWidth constraint enforcement (300px-600px) for panel column in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T020 [US2] Test window resize from 1200px to 800px with collapsed panel - verify main content expands
- [x] T021 [US2] Test panel resize from 300px to 500px - verify main content adjusts by exact width difference
- [x] T022 [US2] Test GridSplitter real-time feedback during drag operations
- [x] T023 [US2] Validate layout updates complete within 16ms (60fps performance requirement)
- [ ] T024 [US2] Test edge case: minimum window width (800px) with maximum panel width (600px)

**Checkpoint**: At this point, dynamic responsive layout should be fully functional and testable independently

---

## Phase 5: User Story 3 - Right Panel Window Edge Anchoring (Priority: P2)

**Goal**: Anchor right panel perfectly to window's right edge with zero gap spacing

**Independent Test**: Expand right panel and visually verify it extends completely to window's right edge with no gaps, maintaining anchoring during window resize

### Implementation for User Story 3

- [x] T025 [P] [US3] Remove any margins causing gaps between panel and window edge in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T026 [P] [US3] Ensure panel Border element has proper BorderThickness for edge alignment in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T027 [US3] Configure Grid.Column alignment to ensure panel extends to window edge in src/S7Tools/Views/JobsMainContentView.axaml
- [x] T028 [US3] Test edge anchoring with minimum panel width (300px) - verify no gaps
- [x] T029 [US3] Test edge anchoring with maximum panel width (600px) - verify no overflow or gaps
- [x] T030 [US3] Test edge anchoring behavior during horizontal window resize operations
- [x] T031 [US3] Validate visual consistency with application theme (background: #252526, border: #464647)
- [x] T032 [US3] Test professional appearance across different window sizes (800px to 1920px width)

**Checkpoint**: All user stories should now be independently functional with professional appearance

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements and validation across all user stories

- [x] T033 [P] Comprehensive testing of all three user stories working together seamlessly
- [x] T034 [P] Performance validation: ensure all layout operations maintain 60fps across all stories
- [x] T035 [P] Visual consistency check: verify professional VS Code-style appearance
- [x] T036 [P] Edge case testing: rapid window resize, extreme panel widths, small window sizes
- [x] T037 [P] Code cleanup and optimization in src/S7Tools/Views/JobsMainContentView.axaml.cs
- [x] T038 [P] Documentation update: comment complex layout logic for future maintenance
- [x] T039 Run quickstart.md validation scenarios to ensure all acceptance criteria met

## BONUS IMPROVEMENTS COMPLETED

**Additional enhancements beyond original scope:**

- [x] **T040** [BONUS] Increased settings column width from 120px to 180px in all property tables for better text visibility
- [x] **T041** [BONUS] Removed MaxHeight constraints from property tables to enable dynamic content sizing
- [x] **T042** [BONUS] Optimized 3-column Grid layout for proper GridSplitter functionality

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
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Independent of US1, can run in parallel
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Independent of US1/US2, but lower priority

### Within Each User Story

- Implementation tasks before testing tasks within same story
- XAML layout changes before code-behind method updates
- Basic functionality before performance validation
- Core implementation before edge case testing

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- XAML and styling tasks within different stories marked [P] can run in parallel
- Testing tasks across different stories can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch XAML modifications for User Story 1:
Task: "Wrap main content area in ScrollViewer in src/S7Tools/Views/JobsMainContentView.axaml"
Task: "Configure ScrollViewer properties in src/S7Tools/Views/JobsMainContentView.axaml"
```

## Parallel Example: Cross-Story

```bash
# Once foundational complete, these can run in parallel:
Task: "[US1] Wrap main content area in ScrollViewer"
Task: "[US2] Update Grid column definitions for dynamic space claiming"
Task: "[US3] Remove margins causing gaps between panel and window edge"
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2 - Both P1)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Horizontal Scrolling)
4. Complete Phase 4: User Story 2 (Dynamic Responsive Layout)
5. **STOP and VALIDATE**: Test core layout functionality independently
6. Deploy/demo if ready (horizontal scrolling + responsive layout = core value)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Core scrolling works
3. Add User Story 2 → Test independently → Core responsive behavior works
4. Add User Story 3 → Test independently → Polish edge anchoring
5. Each story adds visual/functional value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (small, focused effort)
2. Once Foundational is done:
   - Developer A: User Story 1 (Horizontal Scrolling)
   - Developer B: User Story 2 (Dynamic Responsive Layout)
   - Developer C: User Story 3 (Window Edge Anchoring)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files or independent XAML sections, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Focus: This is pure UI layout enhancement - no business logic, services, or architectural changes
- Performance target: 60fps (16ms) for all layout operations
- Visual target: Professional VS Code-style panel behavior
