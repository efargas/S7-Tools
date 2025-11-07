# S7Tools Constitution
<!--
Sync Impact Report v1.2.0
- Version change: 1.1.0 -> 1.2.0 (MINOR: New mandatory requirements)
- Updated sections: Development Workflow (terminal commands mandatory), Additional Constraints (terminal commands forbidden)
- Added content: Mandatory terminal commands requirement, VS Code tasks prohibition, rationale for consistency
- New governance: All .NET operations must use terminal commands only
- Quality baselines: 308 tests (99.7% passing), 0 errors/0 warnings, A+ grade (98/100)
- Memory Bank integration: systemPatterns.md alignment
- Last major update: November 7, 2025 (Terminal commands mandate)
-->

## Core Principles

### I. Clean Architecture & Layered Boundaries

All code MUST follow Clean Architecture with categorized organization: domain/core assemblies contain business
types, interfaces and validation and MUST have no direct dependencies on UI or infrastructure. ViewModels and
Views MUST be organized into functional categories (Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles,
Settings, Tasks) with namespace pattern `S7Tools.ViewModels.{Category}` and `S7Tools.Views.{Category}`.
Implementation details (UI, logging, storage, platform-specific code) MUST live in infrastructure or UI
projects and depend inward on core. Public APIs in Core MUST be minimal, well-documented, and stable.
Rationale: strong layering with categorized organization preserves testability, enables parallel team ownership,
improves discoverability, and prevents accidental coupling of UI/infrastructure into domain logic.

### II. MVVM (ReactiveUI) & UI Contracts

All UI screens and interactions MUST be implemented using MVVM with ReactiveUI patterns. ViewModels MUST
inherit from ReactiveObject and use RaiseAndSetIfChanged for property updates. Commands MUST be ReactiveCommand
and use explicit CanExecute observables. UI composition and DI contracts MUST be defined in Core or a shared
contracts assembly; View-only behavior belongs in Views. Rationale: MVVM + ReactiveUI provides predictable
data flow and testability of UIs.

### III. Test-First Quality Gates (NON-NEGOTIABLE)

Project work MUST be guided by test-first discipline: unit tests (xUnit), integration tests, and focused
contract tests for public behaviors. Tests MUST follow AAA (Arrange-Act-Assert) and async tests MUST be
written as async Task (no .Result/.Wait). Current quality baseline: 308 tests across 3 test projects
(S7Tools.Tests, S7Tools.Core.Tests, S7Tools.Infrastructure.Logging.Tests) with 99.7% pass rate (1
intentionally skipped). All CI pipelines and local work MUST pass the test suite before merging; maintainers
MUST not merge failing tests. Build quality MUST maintain 0 errors, 0 warnings standard. Rationale: tests
are the primary safety net for refactors and ensure regressions are caught early; quality gates prevent
technical debt accumulation.

### IV. Thread Safety & Concurrency Contracts

All multi-threaded behavior MUST follow explicit contracts. Long-running or blocking I/O MUST not run on the UI
thread. Use IUIThreadService for UI updates from background threads. Semaphore or lock-based APIs MUST use the
internal-method pattern (public methods acquire locks, internal methods assume the lock is held) to avoid
deadlocks - this pattern is critical for preventing semaphore deadlocks. Resource coordination MUST use
IResourceCoordinator for conflict detection and proper resource locking during parallel operations. Rationale:
the app interacts with hardware and background services—strict concurrency rules with proven patterns prevent
subtle, production-impacting deadlocks and resource conflicts.

### V. Observability, Versioning & Simplicity

Structured logging (ILogger<T>) and an in-memory bounded DataStore for UI log viewing MUST be present. Public
facing formats (profiles, exports) MUST use semantically versioned schemas when evolving. Adopt semantic
versioning for releases (MAJOR.MINOR.PATCH) and require a documented migration path for breaking changes.
Favor simple, explicit designs (YAGNI) to reduce maintenance burden. Rationale: observability is vital for
debugging hardware interactions; clear versioning protects integrations.

## Additional Constraints

- Platform & runtime: .NET 8 is the supported SDK/runtime. The UI is built with Avalonia and uses ReactiveUI.
- Terminal Commands: ALL .NET operations MUST use terminal commands only (`dotnet clean`, `dotnet restore`,
  `dotnet build`, `dotnet test`, `dotnet format`, `dotnet run`). VS Code tasks are strictly FORBIDDEN.
- Categorized Architecture: ViewModels/Views MUST be organized into 9 functional categories (Base, Controls,
  Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks) with proper namespace conventions.
- Service registration: All services MUST be registered via `ServiceCollectionExtensions.cs`; do NOT register
  services directly in `Program.cs`.
- Profile Management: Persistent profile storage follows the `StandardProfileManager<T>` pattern; job profiles
  are persisted under `src/resources/JobProfiles/profiles.json` (Options pattern). Ensure the file exists or
  create with `[]`.
- Concurrency: Avoid nested semaphore acquisitions. Use the documented internal-method pattern for APIs that
  acquire synchronization primitives.
- Exception Handling: Use domain-specific exceptions from `S7Tools.Core/Exceptions/` hierarchy for semantic
  error handling. Custom exceptions provide better debugging context and enable targeted error handling.
- Resource Coordination: Use IResourceCoordinator for managing shared resources and preventing conflicts during
  parallel operations.
- Reusable Controls: Extract duplicate UI sections into UserControls (e.g., SerialPortDiscoveryControl) with
  proper ViewModel injection patterns for maintainability and consistency.
- Packaging: Follow Avalonia publish recommendations; CI must validate build for Debug and Release.## Development Workflow & Quality Gates

- Terminal Commands Only: All .NET operations (build, test, format, run) MUST use terminal commands (`dotnet clean`,
  `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet format`, `dotnet run`). VS Code tasks are FORBIDDEN
  for consistency, reproducibility, and dependency avoidance.
- Branching: Feature branches SHOULD follow `[XXX-brief-description]` naming and reference the spec or task
  that motivated the change.
- Pull Requests: Every PR that changes public behavior MUST include an updated spec or migration notes,
  relevant tests, and pass CI. At least one approving review by a project maintainer or domain owner is required.
- Code Registration: New services MUST be registered in `ServiceCollectionExtensions.cs`; keep registrations
  grouped by feature area for clarity.
- Formatting & Lint: Run `dotnet format` before committing. The repository enforces EditorConfig rules.
- Continuous Integration: CI MUST run `dotnet restore`, `dotnet build` and `dotnet test`. Tests must pass.
- Quality Gates: PRs that fail tests, introduce new TODOs without justification, or break layering WILL be
  blocked until corrected.
- Memory Bank Maintenance: Update `.copilot-tracking/memory-bank/systemPatterns.md` and related files after
  significant architectural changes or when implementing new patterns.

## Governance

1. Amendments: Changes to this constitution MUST be proposed as a pull request against the `SpecKitNew` branch
   (or the repository default if SpecKitNew is unavailable). Each amendment PR MUST include:
   - A clear rationale and summary of the change.
   - The semantic versioning impact (major/minor/patch) and migration notes for any breaking changes.
   - Updated Memory Bank entries (e.g., `.copilot-tracking/memory-bank/systemPatterns.md`) when patterns change.
   - Reference to current quality baseline (308 tests, 99.7% pass rate, A+ grade 98/100) if affected.

2. Approval: An amendment MUST receive at least one approving review from a project maintainer or an owner of
   the affected area before merging. For high‑impact governance changes (MAJOR version bumps), two approvers
   are recommended.

3. Versioning Policy:
   - MAJOR: Backwards-incompatible governance or principle removals/semantic redefinitions.
   - MINOR: New principle or materially expanded guidance that affects project workflows.
   - PATCH: Editorial clarifications, wording fixes, or non-semantic updates.

4. Compliance: PRs that affect public contracts, profiles, or DI registration MUST include a "Constitution Check"
   in the plan/spec describing how the change complies. CI and reviewers MUST verify the plan/spec check. All
   new patterns MUST follow established architecture: Unified Profile Management with StandardProfileManager<T>,
   Internal Method Pattern for semaphore operations, Custom Domain Exceptions for semantic error handling,
   Reusable Controls Pattern for UI consistency, and Resource Coordination Pattern for conflict resolution.

**Version**: 1.2.0 | **Ratified**: 2025-10-20 | **Last Amended**: 2025-11-07
