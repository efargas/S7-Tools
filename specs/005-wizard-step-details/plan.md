# Implementation Plan: Enhanced Wizard Step Profile Details

**Branch**: `005-wizard-step-details` | **Date**: 2025-10-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-wizard-step-details/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enhance the job creation wizard steps to display comprehensive profile configuration details inline within existing profile details sections. Users will see complete configuration information (all properties, flags, options, metadata) for Serial, Socat, and Power Supply profiles, matching the detail level shown in the main job management view. The enhancement integrates seamlessly within existing wizard layouts without adding separate panels or changing navigation flow.

## Technical Context

**Language/Version**: C# latest targeting .NET 8.0
**Primary Dependencies**: Avalonia UI 11.3.6, ReactiveUI 20.1.1, Microsoft.Extensions.DependencyInjection 8.0.0
**Storage**: Profile persistence via StandardProfileManager<T> pattern with JSON serialization
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Cross-platform desktop (Windows, Linux, macOS) via Avalonia
**Project Type**: Desktop application with MVVM architecture
**Performance Goals**: <100ms profile detail updates, responsive UI with 50+ properties displayed
**Constraints**: Must integrate within existing wizard layout, maintain current navigation flow, follow ReactiveUI patterns
**Scale/Scope**: Enhancement affects 3 wizard steps, 3 profile types, integrates with existing JobInfoDisplayView patterns

## Constitution Check

**Gate: Must pass before Phase 0 research. Re-check after Phase 1 design.**

Constitution Check (reference: `.specify/memory/constitution.md` Version: 1.0.0):

### I. Clean Architecture & Layered Boundaries - PASS ✅

- **Compliance**: Enhancement adds profile detail display logic to existing ViewModels, maintaining clean separation
- **Details**: No new dependencies introduced to Core layer; UI enhancements stay within MVVM presentation layer
- **Rationale**: Existing JobInfoDisplayView patterns provide proven template for profile detail display

### II. MVVM (ReactiveUI) & UI Contracts - PASS ✅

- **Compliance**: Uses existing ReactiveUI patterns with RaiseAndSetIfChanged for profile selection updates
- **Details**: Extends existing wizard ViewModels without changing MVVM contracts; integrates with existing reactive property chains
- **Rationale**: Leverages current WhenAnyValue reactive patterns for immediate profile detail updates

### III. Test-First Quality Gates - PASS ✅

- **Compliance**: Will include unit tests for ViewModel enhancements and integration tests for UI updates
- **Details**: Following AAA pattern for all new test scenarios; maintaining 100% test pass rate requirement
- **Rationale**: Enhanced profile display logic is testable through ViewModel property changes and mock profile data

### IV. Thread Safety & Concurrency Contracts - PASS ✅

- **Compliance**: Profile detail updates occur on UI thread via existing reactive property mechanisms
- **Details**: No new background operations introduced; uses existing profile service patterns that handle thread safety
- **Rationale**: Enhancement leverages existing thread-safe profile loading and UI update patterns

### V. Observability, Versioning & Simplicity - PASS ✅

- **Compliance**: Uses existing ILogger<T> patterns; no schema changes to profile formats
- **Details**: Enhanced display logic maintains existing profile versioning; follows YAGNI by reusing JobInfoDisplayView patterns
- **Rationale**: No breaking changes to profile persistence; purely additive UI enhancement

**Overall Assessment**: NO VIOLATIONS - All constitutional principles maintained

### Post-Phase 1 Design Re-evaluation ✅

After completing research, data model, and contracts design, the constitutional compliance remains intact:

- **Clean Architecture**: No changes to Core layer; enhancement stays in presentation layer
- **MVVM Patterns**: Computed properties follow existing ReactiveUI patterns
- **Test-First**: Clear testing strategy defined with unit and integration tests
- **Thread Safety**: Uses existing thread-safe profile loading and UI update patterns
- **Observability**: Maintains existing logging patterns without schema changes


## Project Structure

### Documentation (this feature)

```
specs/005-wizard-step-details/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
src/
├── S7Tools/                     # Main Avalonia application
│   ├── ViewModels/
│   │   └── Jobs/
│   │       ├── JobWizardViewModel.cs          # Enhanced with comprehensive profile details
│   │       ├── JobWizardStep1ViewModel.cs     # Serial profile details (if needed)
│   │       ├── JobWizardStep2ViewModel.cs     # Socat profile details (if needed)
│   │       └── JobWizardStep3ViewModel.cs     # Power profile details (if needed)
│   ├── Views/
│   │   └── Jobs/
│   │       ├── JobWizardView.axaml            # Enhanced profile details sections
│   │       └── JobInfoDisplayView.axaml       # Reference implementation
│   ├── Services/
│   │   └── ProfileDetailDisplayService.cs     # Shared profile detail formatting logic
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs     # Service registration
├── S7Tools.Core/
│   └── Models/                  # Profile models (existing, no changes)
│       ├── SerialPortProfile.cs
│       ├── SocatProfile.cs
│       └── PowerSupplyProfile.cs
└── S7Tools.Infrastructure.Logging/             # No changes needed

tests/
├── S7Tools.Tests/
│   ├── ViewModels/
│   │   └── Jobs/
│   │       ├── JobWizardViewModelTests.cs     # Enhanced profile detail tests
│   │       └── JobWizardStepViewModelTests.cs # Step-specific detail tests
│   └── Services/
│       └── ProfileDetailDisplayServiceTests.cs # Service unit tests
└── S7Tools.Core.Tests/                        # No changes needed
```

**Structure Decision**: Single project enhancement following existing MVVM patterns. The feature extends existing wizard ViewModels and Views without requiring new projects or major architectural changes. Leverages existing JobInfoDisplayView patterns for consistent profile detail display.

## Complexity Tracking

**Note**: Fill ONLY if Constitution Check has violations that must be justified

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |

**Assessment**: No complexity violations - feature follows existing patterns and maintains architectural simplicity.

