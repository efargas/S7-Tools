````markdown
# Implementation Plan: Remove Options and Flags Columns from Profile List Viewers

**Branch**: `002-remove-profile-list-columns` | **Date**: 2025-10-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-remove-profile-list-columns/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Remove "Options" and "Flags" DataGrid columns from all profile management list viewers (Serial, Socat, PowerSupply, and Jobs) to simplify the UI and improve usability. This is a pure XAML change that removes visual clutter while preserving all functionality through Edit dialogs.

## Technical Context

**Language/Version**: C# .NET 8 with Avalonia UI 11.x
**Primary Dependencies**: Avalonia UI, ReactiveUI for MVVM binding
**Storage**: N/A (UI-only change)
**Testing**: Manual UI verification required
**Target Platform**: Cross-platform desktop (Windows, Linux, macOS)
**Project Type**: Desktop UI application - XAML DataGrid modifications
**Performance Goals**: Improved UI rendering performance through fewer columns
**Constraints**: Must preserve all existing functionality in Edit dialogs
**Scale/Scope**: 4 XAML files, ~8 column definitions to remove

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution Check (required):

- Reference: `.specify/memory/constitution.md` (Version: 1.0.0)
- **PASS** - Clean Architecture: This is a pure UI layer change, no cross-layer impacts
- **PASS** - MVVM Contracts: Preserves existing reactive property bindings, only removes UI columns
- **PASS** - Test-First Quality: Manual UI verification sufficient for pure cosmetic changes
- **PASS** - Thread Safety: No concurrency concerns for static XAML column removal
- **PASS** - Observability: No logging or monitoring impacts for column removal

**Assessment**: All constitutional principles satisfied. This is a low-risk UI improvement with no architectural concerns.

## Post-Phase 1 Constitution Re-Check

*GATE: Re-evaluated after design phase completion.*

Constitution Check (post-design):

- Reference: `.specify/memory/constitution.md` (Version: 1.0.0)
- **PASS** - Clean Architecture: Design confirms pure UI layer change, zero cross-layer impacts
- **PASS** - MVVM Contracts: All reactive property bindings preserved, UI contracts unchanged except column removal
- **PASS** - Test-First Quality: Manual UI verification appropriate for visual-only changes
- **PASS** - Thread Safety: Static XAML modifications require no concurrency considerations
- **PASS** - Observability: Zero impact on structured logging or DataStore functionality

**Post-Design Assessment**: Constitutional compliance fully maintained through design phase. Implementation ready to proceed with zero architectural risk.

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
src/S7Tools/Views/
├── SerialPortsSettingsView.axaml        # Remove Options & Flags columns
├── SocatSettingsView.axaml              # Remove Options & Flags columns
├── PowerSupplySettingsView.axaml        # Remove Options & Flags columns
└── JobsMainContentView.axaml             # Remove Options & Flags columns

# No changes needed to:
src/S7Tools.Core/                         # Models and interfaces unchanged
src/S7Tools/Views/*EditContent.axaml     # Edit dialogs preserve functionality
src/S7Tools/ViewModels/                   # ViewModels unchanged
```

**Structure Decision**: Standard Avalonia UI project structure. Only DataGrid column definitions in XAML views will be modified. No architectural changes or new files required.

## Complexity Tracking

*No violations detected - this implementation is fully compliant with the S7Tools constitution.*

All changes are purely cosmetic UI improvements with no architectural complexity.

