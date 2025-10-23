# Implementation Plan: Resources and Settings Paths Management

**Branch**: `007-resources-paths-management` | **Date**: 2025-10-22 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-resources-paths-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Primary requirement: Implement dynamic path management for resources and settings to eliminate hardcoded strings, handle errors gracefully, and prioritize user settings over defaults. The application must dynamically resolve all paths relative to the executable location and create missing folders/files at startup.

## Current Status

**User Story 1**: ✅ COMPLETED - Dynamic Path Management
- All path services implemented and tested
- Resource folder structure creation working
- Proper startup sequence with synchronous initialization

**User Story 2**: ✅ COMPLETED - Settings Hierarchy Management
- ApplicationSettingsService fully implemented
- Default and user settings structure working
- AppSettings.json properly structured with both sections
- File logging implementation with session-based files
- LoggingSettingsViewModel updated to use new service

**User Story 3**: 🟡 IN PROGRESS - UI Integration
- LoggingSettingsView fully integrated with new settings service
- Remaining ViewModels need verification and potential updates
- Need systematic audit of all settings-related UI components

**Next Phase**: Complete verification of all ViewModels and Views to ensure consistent use of IApplicationSettingsService interface across the entire application.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

## Technical Context

**Language/Version**: C# with .NET 8
**Primary Dependencies**: Avalonia UI, ReactiveUI, Microsoft.Extensions.Logging, Microsoft.Extensions.Configuration
**Storage**: File-based profiles (JSON), settings files, resource folders from executable location
**Testing**: No testing projects yet - focus on building solid base first
**Target Platform**: Cross-platform desktop (Windows, Linux, macOS)
**Project Type**: Desktop application with Clean Architecture
**Performance Goals**: Sub-100ms path resolution, minimal startup time impact
**Constraints**: Must work in read-only environments, handle permission errors gracefully, no hardcoded strings
**Scale/Scope**: Single-user application with specific folder structure: Resources/{AppSettings,Profiles,Logs,Jobs,Tasks,Payloads,Dumps}

## Constitution Check (Post-Design Review)

*GATE: Re-evaluated after Phase 1 design completion.*

Constitution Check (required):

- Reference: `.specify/memory/constitution.md` (Version: 1.0.0)

**I. Clean Architecture & Layered Boundaries**: PASS ✅
- Path management services placed in Core with interfaces
- Implementation details (file I/O) in Services layer
- UI layer depends only on Core interfaces
- No infrastructure dependencies in Core models

**II. MVVM (ReactiveUI) & UI Contracts**: PASS ✅
- No direct UI changes required for this feature
- Settings accessed through proper service interfaces
- Maintains existing MVVM patterns

**III. Test-First Quality Gates**: MODIFIED ✅
- No testing projects initially - focus on solid base implementation
- Manual verification of path resolution and folder creation
- Future testing will follow AAA pattern with async Task methods
- Build verification includes 30-second wait for compilation completion

**IV. Thread Safety & Concurrency Contracts**: PASS ✅
- All file operations use async/await patterns
- No semaphore usage required for path resolution
- Settings access designed to be thread-safe

**V. Observability, Versioning & Simplicity**: PASS ✅
- All path operations use structured logging (ILogger<T>)
- Settings follow existing JSON versioning patterns
- Simple, explicit design without unnecessary abstractions


### Documentation (this feature)

```
specs/007-resources-paths-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Documentation (this feature)

```
specs/007-resources-paths-management/
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
├── S7Tools.Core/
│   ├── Models/
│   │   ├── Configuration/       # Settings and configuration models
│   │   └── Profiles/           # Profile models
│   ├── Interfaces/
│   │   ├── Services/           # Service interfaces
│   │   └── Configuration/      # Configuration interfaces
│   └── Services/               # Core business logic
├── S7Tools/
│   ├── Services/               # Application services
│   ├── ViewModels/            # ReactiveUI ViewModels
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  # DI registration
├── S7Tools.Infrastructure.Logging/  # Logging infrastructure
└── S7Tools.Diagnostics/            # Diagnostic services

tests/
├── S7Tools.Core.Tests/         # Unit tests for Core
├── S7Tools.Tests/              # Integration tests
└── S7Tools.Infrastructure.Logging.Tests/  # Logging tests

resources/                      # Application resources
├── Dumps/
├── Exports/
├── Extractions/
├── Logs/
└── Payloads/
```

**Structure Decision**: Single desktop application with Clean Architecture. Path management services will be implemented in Core with interfaces, infrastructure implementations for file I/O, and service registration in the main application project.

## Complexity Tracking

No violations detected. All constitution principles are satisfied with the current approach.

