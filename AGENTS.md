# AI Agent Onboarding & Context Guide

> **Primary Directive**: This file is the **single source of truth** for all AI agents. When starting a session, always read this file first. It maps the documentation structure to your potential tasks.

## 🚀 Rapid Context Loading (30 Seconds)

1.  **Understand the System**: Read `docs/architecture/overview.md`.
2.  **Know the Rules**: Read `docs/architecture/clean-architecture.md` and `docs/architecture/mvvm-patterns.md`.
3.  **Find the Tools**: Scan `docs/patterns/_index.md`.
4.  **Check Decisions**: Review `docs/architecture/decisions/_index.md` for ADRs.

## 📂 Documentation Map

| If you need to... | Go to... |
| :--- | :--- |
| **Understand the Architecture** | `docs/architecture/overview.md` |
| **Implement a New Feature** | `docs/patterns/_index.md` (Find similar patterns) |
| **Create a ViewModel** | `docs/templates/viewmodel-template.md` |
| **Create a Service** | `docs/templates/service-template.md` |
| **Fix a Bug** | `docs/reviews/LATEST.md` (Check known issues/baselines) |
| **Understand Settings** | `docs/architecture/settings-schema.md` |
| **Understand Job Wizard** | `docs/patterns/job-wizard-persistence.md` |
| **Understand Logging** | `docs/architecture/task-logging.md` |
| **Run Tests** | `docs/guides/testing-guide.md` |
| **Migrate Old Code** | `docs/guides/migration/` |

## 🛠️ Core Patterns & Rules

### 1. Clean Architecture
*   **Dependencies Flow Inward**: UI -> Core <- Infrastructure.
*   **Core has NO external dependencies**.
*   **Interfaces**: Defined in `Core`, implemented in `UI` or `Infrastructure`.

### 2. MVVM (Avalonia + ReactiveUI)
*   **ViewModels**: Inherit from `ViewModelBase` (or `ReactiveObject`).
*   **Properties**: Use `[Reactive]` or `this.RaiseAndSetIfChanged`.
*   **Commands**: Use `ReactiveCommand`.
*   **ViewLocator**: Maps `MyFeatureViewModel` -> `MyFeatureView`.

### 3. Dependency Injection
*   **Registration**: `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`.
*   **Usage**: Constructor injection ONLY.
*   **Factories**: Use `Func<T>` or specialized factories for runtime creation.

### 4. Profile Management
*   **Pattern**: Use `StandardProfileManager<T>`.
*   **Thread Safety**: Use `Internal Method Pattern` for semaphores (see `docs/patterns/internal-method.md`).

## 💻 Development Workflow

1.  **Explore**: Use `ls -R` and `read_file` to understand the current state.
2.  **Plan**: Create a step-by-step plan using `set_plan`.
3.  **Implement**:
    *   Create interface in `Core`.
    *   Create implementation in `Services` or `Infrastructure`.
    *   Register in `ServiceCollectionExtensions.cs`.
    *   Create ViewModel/View.
4.  **Verify**:
    *   Run tests: `dotnet test src/S7Tools.sln`.
    *   **NO VS Code Tasks**: Use terminal commands only.

## 🔍 Key Locations

*   **Entry Point**: `src/S7Tools/Program.cs`
*   **DI Setup**: `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`
*   **Domain Models**: `src/S7Tools.Core/Models/`
*   **ViewModels**: `src/S7Tools/ViewModels/`
*   **Services**: `src/S7Tools/Services/`
*   **Logging**: `src/S7Tools.Infrastructure.Logging/`

## ⚠️ Critical Constraints

*   **Do NOT** use `Task.Run` for UI updates; use `Dispatcher.UIThread`.
*   **Do NOT** block async code (no `.Result` or `.Wait()`).
*   **Do NOT** put business logic in code-behind (`.axaml.cs`).
*   **ALWAYS** use `ILogger<T>` for logging.

---
**Version**: 1.1.0
**Last Updated**: 2025-11-22
