# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **P0 Code Quality Implementation - Phase 1 & 2 Complete** (2025-11-10): Completed all P0 localization and exception handling tasks
    - **UIStrings Localization**: Added 56 new resource entries organized in 8 categories (Clipboard, Status, Profile, Import/Export, PowerSupply, Path, Errors, Values)
    - **ViewModels Updated**: Migrated 7 hardcoded strings to UIStrings.resx across 4 ViewModels
        - `PowerSupplySettingsViewModel`: 3 "Unknown" status strings → `UIStrings.Status_PowerUnknown`
        - `LoggingTestViewModel`: 1 clipboard message → `UIStrings.Status_LogsExportedToClipboard`
        - `DuplicateMemoryRegionProfileDialogViewModel`: 2 validation messages
        - `JobWizardMemoryRegionStepViewModel`: 3 validation messages
    - **Custom Exception**: Created `DialogParentNotFoundException` following S7ToolsException pattern
    - **Code Quality**: Eliminated all 59 duplicate resource warnings (build now: 0 errors, 0 warnings)
    - **Test Coverage**: Added 6 unit tests for DialogParentNotFoundException (AAA pattern)
    - **Test Results**: 361 total tests (360 passed, 1 skipped) = 99.7% pass rate (up from 355 tests)
    - Namespace standardization: All resource references use `S7Tools.Resources.Strings`

### Changed

- **S7Tools Constitution Update** (2025-11-07): Enhanced constitutional governance framework to v1.2.0
    - **MANDATORY Terminal Commands**: All .NET operations (build, test, format, run) MUST use terminal commands only
    - **VS Code Tasks FORBIDDEN**: Prohibited use of VS Code tasks for .NET operations to ensure consistency
    - Updated Development Workflow to mandate terminal commands for all coding agents and interactions
    - Added constitutional requirement with rationale: consistency, reproducibility, dependency avoidance
    - Enhanced Additional Constraints to include terminal commands prohibition
    - Constitutional compliance now includes terminal command verification
    - All agents and GitHub Copilot must follow: `dotnet clean`, `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet format`, `dotnet run`

- **S7Tools Constitution Update** (2025-11-07): Enhanced constitutional governance framework to v1.1.0
    - Updated Core Principles to include categorized ViewModels/Views organization (9 functional categories)
    - Enhanced Testing Standards to reflect current 308 tests with 99.7% pass rate baseline
    - Expanded Threading Contracts to include Internal Method Pattern and Resource Coordination requirements
    - Added comprehensive Additional Constraints covering domain exceptions, resource coordination, reusable controls
    - Enhanced Development Workflow with Memory Bank maintenance requirements
    - Updated Governance section to reference current quality baseline (A+ grade 98/100)
    - Added mandatory patterns: Unified Profile Management, Custom Domain Exceptions, Reusable Controls
    - Constitutional compliance now includes established pattern adherence verification

- **Post-Reorganization Validation and Code Review** (2025-11-07): Comprehensive validation after ViewModels/Views reorganization
    - Fixed test namespace imports in 6 test files to match new categorized structure
    - Resolved xUnit1030 warning in ResourceCoordinatorTests by removing ConfigureAwait(false)
    - Created comprehensive code review report (COMPREHENSIVE_CODE_REVIEW_2025-11-07.md)
    - Quality grade maintained: A+ (98/100)
    - Build quality: 0 errors, 0 warnings
    - Test results: 308 tests (99.7% passing, 1 intentionally skipped)
    - Archived previous review (COMPREHENSIVE_CODE_REVIEW_2025-10-23.md)
    - Updated all documentation references to use stable LATEST_REVIEW.md link

- **ViewModels/Views Folder Reorganization** (2025-11-06): Implemented categorized folder structure for better code organization
    - **ViewModels** now organized into 9 categories: Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks
    - **Views** now organized into 9 categories mirroring ViewModels structure
    - **Namespaces updated** to `S7Tools.ViewModels.{Category}` and `S7Tools.Views.{Category}`
    - **ViewLocator pattern** seamlessly supports categorized namespaces
    - **XAML xmlns** declarations updated to category-specific namespaces
    - All ~110 files moved using `git mv` to preserve history
    - Benefits: Improved code discoverability, clearer feature boundaries, scalable architecture
    - See `.copilot-tracking/memory-bank/REORGANIZATION_2025-11-06.md` for complete details

## [Constitution-1.0.0] - 2025-10-20

### Added

- **Project Constitution (v1.0.0)**: Established formal governance framework for S7Tools development
    - **Core Principles**: Clean Architecture & Layered Boundaries, MVVM (ReactiveUI) & UI Contracts, Test-First Quality Gates (NON-NEGOTIABLE), Thread Safety & Concurrency Contracts, Observability/Versioning & Simplicity
    - **Additional Constraints**: Platform requirements (.NET 8, Avalonia UI), service registration patterns, profile management standards, concurrency guidelines
    - **Development Workflow & Quality Gates**: Branching strategy, PR requirements, code registration, formatting/lint rules, CI requirements
    - **Governance**: Amendment procedures, approval processes, versioning policy, compliance requirements
- Constitution check requirements integrated into spec and plan templates
- Formal ratification date: October 20, 2025

### Changed

- Updated `.specify/templates/plan-template.md` to include Constitution Check requirements
- Updated `.specify/templates/spec-template.md` to include Constitution Compliance section

### Technical Notes

- Constitution file location: `.specify/memory/constitution.md`
- All placeholders resolved with concrete S7Tools-specific governance rules
- Templates aligned with constitutional requirements for compliance verification

---

## Historical Context

This is the first formal governance framework for S7Tools. Prior development followed informal patterns documented in `AGENTS.md` and README files. The constitution codifies these practices into enforceable project standards.
