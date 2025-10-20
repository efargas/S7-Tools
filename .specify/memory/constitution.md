# S7Tools Constitution
<!--
Sync Impact Report
- Version change: template -> 1.0.0
- Modified principles:
	- [PRINCIPLE_1_NAME] -> I. Clean Architecture & Layered Boundaries
	- [PRINCIPLE_2_NAME] -> II. MVVM (ReactiveUI) & UI Contracts
	- [PRINCIPLE_3_NAME] -> III. Test-First Quality Gates (NON-NEGOTIABLE)
	- [PRINCIPLE_4_NAME] -> IV. Thread Safety & Concurrency Contracts
	- [PRINCIPLE_5_NAME] -> V. Observability, Versioning & Simplicity
- Added sections: Additional Constraints; Development Workflow & Quality Gates (concrete content)
- Removed sections: none
- Templates reviewed:
	- .specify/templates/plan-template.md ✅ aligned (Constitution Check placeholder retained)
	- .specify/templates/spec-template.md ✅ aligned
	- .specify/templates/tasks-template.md ✅ aligned
- Follow-up TODOs:
	- None; all template placeholders in this file replaced. If a project-level ratification date formal record is required,
		add it to project governance metadata outside this file.
-->

## Core Principles

### I. Clean Architecture & Layered Boundaries

All code MUST follow Clean Architecture: domain/core assemblies contain business types, interfaces and validation
and MUST have no direct dependencies on UI or infrastructure. Implementation details (UI, logging, storage,
platform-specific code) MUST live in infrastructure or UI projects and depend inward on core. Public APIs
in Core MUST be minimal, well-documented, and stable. Rationale: strong layering preserves testability,
enables parallel team ownership, and prevents accidental coupling of UI/infrastructure into domain logic.

### II. MVVM (ReactiveUI) & UI Contracts

All UI screens and interactions MUST be implemented using MVVM with ReactiveUI patterns. ViewModels MUST
inherit from ReactiveObject and use RaiseAndSetIfChanged for property updates. Commands MUST be ReactiveCommand
and use explicit CanExecute observables. UI composition and DI contracts MUST be defined in Core or a shared
contracts assembly; View-only behavior belongs in Views. Rationale: MVVM + ReactiveUI provides predictable
data flow and testability of UIs.

### III. Test-First Quality Gates (NON-NEGOTIABLE)

Project work MUST be guided by test-first discipline: unit tests (xUnit), integration tests, and focused
contract tests for public behaviors. Tests MUST follow AAA (Arrange-Act-Assert) and async tests MUST be
written as async Task (no .Result/.Wait). All CI pipelines and local work MUST pass the test suite before
merging; maintainers MUST not merge failing tests. Rationale: tests are the primary safety net for refactors
and ensure regressions are caught early.

### IV. Thread Safety & Concurrency Contracts

All multi-threaded behavior MUST follow explicit contracts. Long-running or blocking I/O MUST not run on the UI
thread. Use IUIThreadService for UI updates from background threads. Semaphore or lock-based APIs MUST use the
internal-method pattern (public methods acquire locks, internal methods assume the lock is held) to avoid
deadlocks. Rationale: the app interacts with hardware and background services—strict concurrency rules
prevent subtle, production-impacting deadlocks.

### V. Observability, Versioning & Simplicity

Structured logging (ILogger<T>) and an in-memory bounded DataStore for UI log viewing MUST be present. Public
facing formats (profiles, exports) MUST use semantically versioned schemas when evolving. Adopt semantic
versioning for releases (MAJOR.MINOR.PATCH) and require a documented migration path for breaking changes.
Favor simple, explicit designs (YAGNI) to reduce maintenance burden. Rationale: observability is vital for
debugging hardware interactions; clear versioning protects integrations.

## Additional Constraints

- Platform & runtime: .NET 8 is the supported SDK/runtime. The UI is built with Avalonia and uses ReactiveUI.
- Service registration: All services MUST be registered via `ServiceCollectionExtensions.cs`; do NOT register
	services directly in `Program.cs`.
- Profiles: Persistent profile storage follows the `StandardProfileManager<T>` pattern; job profiles are persisted
	under `src/resources/JobProfiles/profiles.json` (Options pattern). Ensure the file exists or create with `[]`.
- Concurrency: Avoid nested semaphore acquisitions. Use the documented internal-method pattern for APIs that
	acquire synchronization primitives.
- Packaging: Follow Avalonia publish recommendations; CI must validate build for Debug and Release.

## Development Workflow & Quality Gates

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

## Governance

1. Amendments: Changes to this constitution MUST be proposed as a pull request against the `SpecKit` branch
   (or the repository default if SpecKit is unavailable). Each amendment PR MUST include:
   - A clear rationale and summary of the change.
   - The semantic versioning impact (major/minor/patch) and migration notes for any breaking changes.
   - Updated Memory Bank entries (e.g., `.copilot-tracking/memory-bank/systemPatterns.md`) when patterns change.

2. Approval: An amendment MUST receive at least one approving review from a project maintainer or an owner of
   the affected area before merging. For high‑impact governance changes (MAJOR version bumps), two approvers
   are recommended.

3. Versioning Policy:
   - MAJOR: Backwards-incompatible governance or principle removals/semantic redefinitions.
   - MINOR: New principle or materially expanded guidance that affects project workflows.
   - PATCH: Editorial clarifications, wording fixes, or non-semantic updates.

4. Compliance: PRs that affect public contracts, profiles, or DI registration MUST include a "Constitution Check"
   in the plan/spec describing how the change complies. CI and reviewers MUST verify the plan/spec check.

**Version**: 1.0.0 | **Ratified**: 2025-10-20 | **Last Amended**: 2025-10-20
