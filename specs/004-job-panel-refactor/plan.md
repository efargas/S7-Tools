# Implementation Plan: Job Panel Layout Refactor

**Branch**: `004-job-panel-refactor` | **Date**: 2025-10-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-job-panel-refactor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Refactor the job panel layout in the main jobs view to address three critical layout issues: (1) add horizontal scrolling capability to the main job profile view when content exceeds available width, (2) implement dynamic responsive sizing where the main content area claims all available space as panels resize, and (3) anchor the right panel perfectly to the window's right edge with zero gap spacing. This is a pure UI layout enhancement using existing Avalonia Grid and ScrollViewer controls without requiring architectural changes or new business logic.

## Technical Context

**Language/Version**: C# with .NET 8
**Primary Dependencies**: Avalonia UI 11.x, ReactiveUI 19.x, Projektanker.Icons.Avalonia
**Storage**: N/A (UI layout changes only)
**Testing**: xUnit for unit tests, existing UI testing patterns
**Target Platform**: Cross-platform desktop (Windows, Linux, macOS) via Avalonia
**Project Type**: Desktop application - existing UI layout modification
**Performance Goals**: 60fps (16ms response time) for layout updates and resize operations
**Constraints**: Must maintain 300px-600px panel width constraints, support minimum 800px window width
**Scale/Scope**: Single view modification (JobsMainContentView.axaml), approximately 3-5 XAML/C# files affected

## Constitution Check

**GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.**

✅ **POST-PHASE 1 RE-EVALUATION**: All principles remain compliant after design phase

Constitution Check (required):

- **Reference**: `.specify/memory/constitution.md` (Version: 1.0.0)
- **Assessment**: All changes comply with constitutional principles

**Compliance Analysis**:

✅ **I. Clean Architecture & Layered Boundaries**: PASS

- Changes are limited to UI layer (Views and code-behind)
- No modifications to Core/domain assemblies required
- No new dependencies on infrastructure layers

✅ **II. MVVM (ReactiveUI) & UI Contracts**: PASS

- Existing ReactiveUI patterns maintained
- No new ViewModels or commands required
- Changes affect only layout and visual behavior

✅ **III. Test-First Quality Gates**: PASS

- UI layout changes testable via visual validation
- Existing test suite remains valid
- No breaking changes to tested contracts

✅ **IV. Thread Safety & Concurrency Contracts**: PASS

- No multi-threading implications
- All changes are UI thread operations
- No new semaphore or locking requirements

✅ **V. Observability, Versioning & Simplicity**: PASS

- No new logging requirements (layout operations)
- No public API or schema changes
- Simple, focused layout improvements

**Mitigations**: None required - all principles satisfied


## Project Structure

### Documentation (this feature)

```
specs/004-job-panel-refactor/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
src/S7Tools/
├── Views/
│   ├── JobsMainContentView.axaml      # Main layout modification target
│   ├── JobsMainContentView.axaml.cs   # Grid management code-behind updates
│   └── Jobs/
│       └── JobInfoDisplayView.axaml   # Minor scrolling adjustments
├── ViewModels/
│   └── JobsMainContentViewModel.cs    # No changes required
└── Styles/
    └── MainStyles.axaml              # Potential styling updates

tests/S7Tools.Tests/
├── Views/
│   └── JobsMainContentViewTests.cs   # Layout behavior validation
└── Integration/
    └── LayoutResizingTests.cs        # Window/panel resize testing
```

**Structure Decision**: Single desktop application project structure. Changes are focused on the existing JobsMainContentView and its layout behavior. No new projects, services, or major architectural components required - purely UI layout modifications within the existing Avalonia application structure.

## Complexity Tracking

**Note**: No constitutional violations present - this section not needed for this feature.

All constitutional principles are satisfied with this pure UI layout enhancement. No complexity justification required.

