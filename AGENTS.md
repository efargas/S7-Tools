---
# AGENTS.md — Coding Agent Onboarding & Best Practices Guide

## Purpose
This document provides essential onboarding, architecture, and coding standards for all coding agents working on the S7Tools repository. It is designed to ensure consistency, maintainability, and compliance with project rules. **Do not include task-specific or session-specific notes here.**

---

## Project Overview

**S7Tools** is a cross-platform desktop application for Siemens S7-1200 PLC communication, built with .NET 8, Avalonia UI, and ReactiveUI. The project implements Clean Architecture, MVVM, and comprehensive logging.

---

## Key Agent Guidelines

1. **Follow Clean Architecture**: All dependencies flow inward. Core/domain has no external dependencies. Infrastructure and UI depend on Core only.
2. **MVVM Pattern**: Use ReactiveUI for ViewModels. All ViewModels must use `RaiseAndSetIfChanged` for properties and `ReactiveCommand` for commands.
3. **Service-Oriented Design**: All business logic is encapsulated in services, registered via DI in `ServiceCollectionExtensions.cs`.
4. **Dependency Injection**: Register all services/interfaces in the DI container. Never register services directly in `Program.cs`.
5. **Logging**: Use `ILogger<T>` for all logging. Integrate with the custom DataStore provider for real-time UI logs.
6. **Testing**: Follow AAA (Arrange–Act–Assert) for all unit tests. Place tests in the appropriate test project.
7. **EditorConfig**: Enforce code style and formatting with `.editorconfig`. Run `dotnet format` before commit.
8. **Documentation**: All public APIs must have XML documentation. Update the Memory Bank (`systemPatterns.md`) after significant changes.
9. **Error Handling**: Never swallow exceptions. Always log and rethrow or return a failure result.
10. **Thread Safety**: Use `IUIThreadService` for all UI thread operations. Never block the UI thread with I/O.

---

## Folder & File Structure

- `src/S7Tools/` — Main UI project
  - `ViewModels/` — Categorized ViewModels (Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks)
  - `Views/` — Categorized Views (mirrors ViewModels structure)
  - `Services/` — Application services
  - `Extensions/` — DI registration (ServiceCollectionExtensions.cs)
- `src/S7Tools.Core/` — Domain models, interfaces (no external deps)
  - `Models/Jobs/` — Job-related domain models (JobProfile, JobManagerOptions, etc.)
  - `Services/Interfaces/` — Service contracts
  - `Exceptions/` — Custom exception hierarchy
- `src/S7Tools.Infrastructure.Logging/` — Logging infrastructure
- `tests/` — Unit and integration tests
- `.copilot-tracking/memory-bank/` — Project documentation, patterns, and Memory Bank

---

## Namespace Conventions

- **ViewModels**: `S7Tools.ViewModels.{Category}` where Category is one of:
  - Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks
- **Views**: `S7Tools.Views.{Category}` (mirrors ViewModels categories)
- **Core Domain**: `S7Tools.Core.Models.{Domain}` (e.g., Jobs, Configuration)
- **Services**: `S7Tools.Services.{Domain}` (e.g., Jobs, Bootloader)

### ViewLocator Pattern
The ViewLocator automatically resolves Views from ViewModels:
```csharp
// ViewModel namespace: S7Tools.ViewModels.Pages.HomeViewModel
// View namespace:      S7Tools.Views.Pages.HomeView
```
This works seamlessly with the categorized folder structure.

---

## Coding Standards (Summary)

- **C#**: 4-space indentation, PascalCase for types, camelCase for private fields with `_` prefix
- **XAML**: 2-space indentation, PascalCase for elements
- **Interfaces**: Prefix with `I` (e.g., `IActivityBarService`)
- **Async Methods**: Use `ConfigureAwait(false)` in library code
- **ViewModels**: Inherit from `ReactiveObject`, use `RaiseAndSetIfChanged`
- **Service Registration**: Use `ServiceCollectionExtensions.cs` only
- **Logging**: Use structured logging, never string interpolation in log messages
- **Validation**: Use centralized validation patterns (see `systemPatterns.md`)
- **Reusable Controls**: Extract duplicate UI sections into UserControls (e.g., `SerialPortDiscoveryControl`)

---

## Development Commands (MANDATORY)

**CRITICAL REQUIREMENT**: All coding agents MUST use terminal commands for .NET operations. VS Code tasks are FORBIDDEN.

### Essential Terminal Commands
```bash
# Clean and build (MANDATORY pattern)
dotnet clean src/S7Tools.sln
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug

# Testing (maintain 99.7%+ pass rate: 308 tests, 1 intentionally skipped)
dotnet test src/S7Tools.sln --configuration Debug

# Code formatting (REQUIRED before any commit)
dotnet format src/S7Tools.sln

# Run application with diagnostics
dotnet run --project src/S7Tools --configuration Debug -- --diag
```

**NEVER use VS Code tasks** - Always use terminal commands for consistency and to avoid task configuration dependencies.

---

## Agent Workflow

1. **Start by reading `.copilot-tracking/memory-bank/systemPatterns.md`** — This is the single source of truth for all architecture, patterns, and rules.
2. **Use ONLY terminal commands** — Never use VS Code tasks for build, test, format, or run operations.
3. **Never duplicate documentation** — Update `systemPatterns.md` and related Memory Bank files after significant changes.
4. **Do not include session logs or task notes here** — Use the Memory Bank for all project intelligence and progress tracking.
5. **Use the agent workspace (`.github/agents/workspace/`) for temporary files only** — Never store permanent documentation or code here.

---

## References

- `.copilot-tracking/memory-bank/systemPatterns.md` — All patterns, rules, and templates
- `PATTERNS_REFERENCE.md` — Comprehensive architectural patterns documentation
- `reviews/LATEST_REVIEW.md` — Latest quality baseline and code review
- `.copilot-tracking/memory-bank/` — Project documentation and Memory Bank
- `ServiceCollectionExtensions.cs` — Service registration
- `README.md` — Project summary and setup

---

## Current Baseline Notes (for continuity)

**Last Updated**: 2025-11-10 (branch: 008-memory-regions-profiling)

- **Memory Region Profiling**: ✅ COMPLETE — Fully integrated with settings UI, import/export, and job wizard
- **Job Wizard**: ✅ COMPLETE — Multi-step wizard with memory region selection, validation, and fallback mechanism
- **Application Settings Service**: ✅ NEW — Centralized settings management with path resolution
- **Resource Coordinator**: ✅ NEW — Parallel service initialization for improved startup time
- **UI Refresh Service**: ✅ NEW — Consistent collection refresh with selection preservation

**Technical Details**:

- Scheduler uses Local timezone; due scheduled tasks are auto-promoted to the queue
- Job profiles path (Options): `src/resources/JobProfiles/profiles.json`
- Memory region profiles path: User-configurable via Settings → Memory Regions
- `PlcClientStub` registered as temporary implementation of `IPlcClient`; factory resolves it until real client is provided
- `JobWizardPlaceholderViewModel` is an ACTIVE fallback mechanism (NOT dead code)

## Code Quality Standards (Updated 2025-11-10)

**Build Status**: ✅ 0 errors, 0 warnings (P0 Phase 1 & 2: eliminated 59 duplicate resource warnings)
**Test Status**: ✅ **361 tests (360 passing, 1 intentionally skipped) = 99.7% pass rate** (up from 308 tests)
**Code Quality**: ✅ A+ grade (98/100)

**Recent P0 Improvements (2025-11-10)**:

- **Localization Complete**: 56 new UIStrings resources added, 7 hardcoded strings migrated
- **Custom Exceptions**: Added `DialogParentNotFoundException` with 6 comprehensive unit tests
- **Resource Organization**: 8 categories (Clipboard, Status, Profile, Import/Export, PowerSupply, Path, Errors, Values)
- **Namespace Standardization**: All ViewModels use `S7Tools.Resources.Strings`
- **Build Quality**: Achieved zero warnings (previously 59 duplicate resource warnings)

**Key Quality Achievements**:

- Clean Architecture properly implemented
- ViewModels/Views organized into 9 functional categories
- Unified Profile Management with StandardProfileManager<T>
- Internal Method Pattern for semaphore safety (no deadlocks)
- Resource Coordination Pattern for parallel execution
- Custom Exception Hierarchy for semantic error handling (including DialogParentNotFoundException)
- Comprehensive testing with 99.7% pass rate
- Centralized localization with UIStrings.resx (1800+ entries)

**Pattern Compliance**:

When implementing new features, always:

1. Check `PATTERNS_REFERENCE.md` for applicable patterns
2. Follow existing implementations as examples
3. Maintain consistency with established patterns
4. Add tests following AAA pattern
5. Run `dotnet format` before commit

**Code Review Process**:

- Comprehensive code reviews documented in `reviews/` directory
- Latest: `reviews/LATEST_REVIEW.md` (stable link)
- Archived reviews in `reviews/archive/`
- Use as quality baseline for all new code
- Reference for best practices and anti-patterns

---

**For any coding agent: Always align with the latest `systemPatterns.md` and never introduce new patterns or rules without updating the Memory Bank.**
