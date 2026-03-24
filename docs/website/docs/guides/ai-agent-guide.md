---
title: AI Coding Agent Onboarding Guide
version: 1.0.7
created: '2025-01-15'
last-updated: '2026-03-17'
status: current
tags:
- guide
- ai-agent
- onboarding
- quick-start
related:
- docs/architecture/overview.md
- docs/patterns/_index.md
- docs/guides/development-workflow.md
---
# AI Coding Agent Onboarding Guide

## 30-Second Onboarding Workflow

**Start Here** → Follow this exact sequence for rapid onboarding:

1. **Read This Section** (10 seconds) - Get orientation
2. **Read Architecture Overview** (10 seconds) - Understand system design
   - → [`docs/architecture/overview.md`](../architecture/overview.md)
3. **Scan Pattern Catalog** (10 seconds) - Know available patterns
   - → [`docs/patterns/_index.md`](../patterns/_index.md)
4. **Reference Specific Patterns** (as needed) - Deep dive when implementing

**Total Time**: < 30 seconds to locate any pattern or architectural decision.

## Context Gathering Strategy

### Efficient AI Agent Workflow

```python
# Recommended context gathering for any task
def gather_context_for_task(task_description):
    # Phase 1: Core Architecture (ALWAYS read - 20 seconds)
    architecture = read("docs/architecture/overview.md")
    clean_arch = read("docs/architecture/clean-architecture.md")

    # Phase 2: Identify Relevant Patterns (10 seconds)
    pattern_catalog = read("docs/patterns/_index.md")
    relevant_patterns = filter_by_tags(pattern_catalog, task_description)

    # Phase 3: Deep Dive (as needed - 30-60 seconds)
    for pattern in relevant_patterns[:3]:  # Top 3 most relevant
        pattern_details = read(pattern)
        examples = read(pattern + "/examples/")

    # Phase 4: Existing Implementation Reference (optional)
    if has_similar_feature(task_description):
        similar_code = find_similar_implementation()
        cross_reference_with_patterns(similar_code)

    # Phase 5: Template Selection (if creating new code)
    template = select_template_from("docs/templates/", task_description)

    return context
```

### Tag-Based Pattern Discovery

Use tags to quickly find relevant patterns:

```bash
# Find all MVVM-related patterns
grep -r "tags:.*mvvm" docs/patterns/

# Find thread safety patterns
grep -r "tags:.*thread" docs/patterns/

# Find service-related patterns
grep -r "tags:.*service" docs/patterns/
```

## Quick Reference: Key Files

| Need | File | Purpose |
|------|------|---------|
| **Master Index** | [`docs/INDEX.md`](../INDEX.md) | All documentation entry point |
| **System Architecture** | [`docs/architecture/overview.md`](../architecture/overview.md) | Clean Architecture + MVVM |
| **Pattern Catalog** | [`docs/patterns/_index.md`](../patterns/_index.md) | All implementation patterns |
| **Profile Management** | [`docs/patterns/profile-management.md`](../patterns/profile-management.md) | Unified profile pattern |
| **Semaphore Safety** | [`docs/patterns/internal-method.md`](../patterns/internal-method.md) | Deadlock prevention |
| **Development Workflow** | [`docs/guides/development-workflow.md`](./development-workflow.md) | Daily commands & workflow |
| **Latest Review** | [`docs/reviews/LATEST.md`](../reviews/LATEST.md) | Current quality baseline |

---

## Purpose
This document provides essential onboarding, architecture, and coding standards for all coding agents working on the S7Tools repository. It is designed to ensure consistency, maintainability, and compliance with project rules.

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
- `specs/` — Feature specifications and planning documents for AI agent working memory

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

1. **Start with the Master Index** — Read [`docs/INDEX.md`](../INDEX.md) to understand documentation structure
2. **Read Core Architecture** — [`docs/architecture/overview.md`](../architecture/overview.md) is the single source of truth for system design
3. **Use ONLY terminal commands** — Never use VS Code tasks for build, test, format, or run operations
4. **Reference patterns as needed** — Check [`docs/patterns/_index.md`](../patterns/_index.md) for implementation patterns
5. **Follow established patterns** — All patterns documented in `docs/patterns/` directory
6. **Update documentation** — When adding new patterns, update relevant files in `docs/` structure

---

## References

- **Master Index**: [`docs/INDEX.md`](../INDEX.md) - Entry point for all documentation
- **Architecture**: [`docs/architecture/`](../architecture/) - System design and decisions
- **Patterns**: [`docs/patterns/`](../patterns/) - All implementation patterns and examples
- **Guides**: [`docs/guides/`](./_index.md) - Development workflows and guides
- **Latest Review**: [`docs/reviews/LATEST.md`](../reviews/LATEST.md) - Current quality baseline
- **Templates**: [`docs/templates/`](../templates/) - Code and documentation templates

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
- Job profiles path (Options): `src/S7Tools/bin/Debug/net10.0/Resources/Jobs/Jobs.json`
- Memory region profiles path: User-configurable via Settings → Memory Regions
- `PlcClientStub` registered as temporary implementation of `IPlcClient`; factory resolves it until real client is provided
- `JobWizardPlaceholderViewModel` is an ACTIVE fallback mechanism (NOT dead code)

## Code Quality Standards (Updated 2025-11-10)

**Build Status**: ✅ 0 errors, 0 warnings (P0+P1: eliminated 59 duplicate resource warnings)
**Test Status**: ✅ **361 tests (360 passing, 1 intentionally skipped) = 99.7% pass rate** (up from 308 tests)
**Code Quality**: ✅ A+ grade (98/100)

**Recent P0+P1 Improvements (2025-11-10)**:

**P0 - Critical (COMPLETE)**:
- **Localization Complete**: 56 new UIStrings resources added, 7 hardcoded strings migrated
- **Custom Exceptions**: Added `DialogParentNotFoundException` with 6 comprehensive unit tests
- **Resource Organization**: 8 categories (Clipboard, Status, Profile, Import/Export, PowerSupply, Path, Errors, Values)
- **Namespace Standardization**: All ViewModels use `S7Tools.Resources.Strings`
- **Build Quality**: Achieved zero warnings (previously 59 duplicate resource warnings)

**P1 - High (COMPLETE)**:
- **Magic Numbers Eliminated**: 38 magic numbers/strings extracted to constants
  - `DateTimeFormats` class: 5 format constants (11 usages)
  - `NetworkConstants` class: 4 port-related constants (6 usages)
  - `ColorPalette` class: 9 color groups, 27 RGB values
  - `MemoryConstants` class: 4 memory-related constants (8 usages)
- **Documentation Updated**: `systemPatterns.md` v2.2 with 4 new architectural patterns
- **Pattern Validation**: Verified JobWizardPlaceholder as active fallback (KEEPING)
- **PATTERNS_REFERENCE.md**: Updated to v1.2 with new patterns:
  - Memory Region Profile Management pattern
  - Job Wizard multi-step pattern
  - ProfileEditDialogService pattern
  - Application Settings Service pattern

**Key Quality Achievements**:

- Clean Architecture properly implemented
- ViewModels/Views organized into 9 functional categories
- Unified Profile Management with StandardProfileManager&lt;T&gt;
- Memory Region Profiling with segment selection and validation
- Job Wizard with multi-step navigation and fallback mechanism
- Internal Method Pattern for semaphore safety (no deadlocks)
- Resource Coordination Pattern for parallel execution
- Custom Exception Hierarchy for semantic error handling (including DialogParentNotFoundException)
- Type-safe constants for all magic numbers and strings
- Comprehensive testing with 99.7% pass rate (+6 tests from P0)
- Centralized localization with UIStrings.resx (1800+ entries)
- Comprehensive pattern documentation in PATTERNS_REFERENCE.md v1.2

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

## Related Documentation

- [Index](../INDEX.md)
- [Readme](../README.md)
- [Overview](../architecture/overview.md)
- [_Index](_index.md)
- [Development Standards](development-standards.md)
- [Development Workflow](development-workflow.md)
- [Onboarding](onboarding.md)
- [_Index](../patterns/_index.md)
- [Internal Method](../patterns/internal-method.md)
- [Profile Management](../patterns/profile-management.md)
- [2025 11 10 Quality Improvements](../reviews/2025-11-10-quality-improvements.md)
- [Latest](../reviews/LATEST.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
