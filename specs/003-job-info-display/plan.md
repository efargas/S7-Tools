# Implementation Plan: Enhanced Job Information Display

**Branch**: `003-job-info-display` | **Date**: 2025-10-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-job-info-display/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enhanced job information display for the main jobs view and job wizard. Users will see detailed profile information without opening edit dialogs. Main jobs view gets a dedicated info panel showing complete job details including selected profiles and their properties. Job wizard shows detailed properties of currently selected profiles at each step. This improves job verification workflow and reduces navigation overhead.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

## Technical Context

**Language/Version**: C# / .NET 8
**Primary Dependencies**: Avalonia UI 11.0, ReactiveUI 20.1, Microsoft.Extensions.DependencyInjection
**Storage**: JSON file storage via Options pattern (profiles.json), in-memory collections
**Testing**: xUnit with FluentAssertions, async Task patterns
**Target Platform**: Cross-platform desktop (Windows, macOS, Linux)
**Project Type**: Desktop application - MVVM with Clean Architecture
**Performance Goals**: UI responsiveness <100ms for profile selection/display updates
**Constraints**: Read-only information display, must handle missing profile references gracefully
**Scale/Scope**: Small to medium datasets (hundreds of jobs, dozens of profiles per type)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution Check (required):

- Reference: `.specify/memory/constitution.md` (Version: 1.0.0)

**Impacted Principles**:

- **I. Clean Architecture & Layered Boundaries**: ✓ PASS - Changes are limited to UI layer (Views/ViewModels), no changes to Core domain models or service interfaces
- **II. MVVM (ReactiveUI) & UI Contracts**: ✓ PASS - New ViewModels will inherit from ReactiveObject, use RaiseAndSetIfChanged, and ReactiveCommand patterns
- **III. Test-First Quality Gates**: ✓ PASS - New ViewModels and services will have comprehensive unit tests following AAA pattern
- **IV. Thread Safety & Concurrency Contracts**: ✓ PASS - UI updates will use IUIThreadService, no new synchronization primitives needed
- **V. Observability, Versioning & Simplicity**: ✓ PASS - No new public formats, leverages existing logging infrastructure

**Post-Design Re-Check**:

After completing Phase 1 design (data-model.md, contracts/, quickstart.md):

- **Service Registration**: ✓ COMPLIANT - New services registered in `ServiceCollectionExtensions.cs` as required
- **Clean Architecture**: ✓ COMPLIANT - All new components in UI layer, existing Core models unchanged
- **MVVM Patterns**: ✓ COMPLIANT - ViewModels use ReactiveObject, reactive properties, and ReactiveCommand
- **Thread Safety**: ✓ COMPLIANT - Background operations use proper async patterns with UI thread marshaling
- **Testing Strategy**: ✓ COMPLIANT - Comprehensive unit testing strategy defined with AAA pattern

**Compliance Status**: ✓ FULLY COMPLIANT - No constitution violations. This is a pure UI enhancement that follows all established patterns and requires no architectural compromises.


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

## Project Structure

### Documentation (this feature)

```
specs/003-job-info-display/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (S7Tools Clean Architecture)

```
src/
├── S7Tools/                     # UI Layer
│   ├── ViewModels/
│   │   ├── Jobs/
│   │   │   ├── JobInfoDisplayViewModel.cs      # New: Job details display
│   │   │   └── JobWizardStepViewModelBase.cs   # Enhanced: Profile details
│   │   └── Profiles/
│   │       └── ProfileDetailsViewModel.cs      # New: Reusable profile display
│   ├── Views/
│   │   ├── Jobs/
│   │   │   ├── JobInfoDisplayView.axaml        # New: Job details panel
│   │   │   └── JobWizardStepViews.axaml        # Enhanced: Profile details sections
│   │   └── Profiles/
│   │       └── ProfileDetailsView.axaml        # New: Reusable profile details
│   └── Services/
│       └── IProfileDetailsService.cs           # New: Profile formatting service
├── S7Tools.Core/                # Domain Layer
│   ├── Models/
│   │   └── [existing profile models]           # No changes needed
│   └── Services/
│       └── [existing profile services]         # No changes needed
└── S7Tools.Infrastructure.*/    # Infrastructure Layer
    └── [no changes needed]
```

**Structure Decision**: This follows S7Tools' established Clean Architecture. Changes are isolated to the UI layer (Views/ViewModels) with a new service for formatting profile details. No changes to Core domain models or Infrastructure layers are required.

## Complexity Tracking

**No violations to justify** - This feature is fully compliant with the constitution and follows all established patterns.

