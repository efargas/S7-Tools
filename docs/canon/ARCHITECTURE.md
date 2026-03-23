---
title: "S7Tools Architecture — The Constitution"
version: "1.0.0"
created: "2026-03-23"
status: "canon"
tags: ["architecture", "canon", "hexagonal", "solid", "prescriptive"]
---

# S7Tools Architecture — The Constitution

> **Canon Status**: This document is prescriptive, not descriptive. It defines the architectural laws
> to which all code **must** conform — not merely how code currently behaves.

---

## Table of Contents

1. [Mission Statement](#1-mission-statement)
2. [Central Philosophy](#2-central-philosophy)
3. [Architectural Commandments](#3-architectural-commandments)
4. [Layer Model](#4-layer-model)
5. [Dependency Rules](#5-dependency-rules)
6. [Project Mapping to Layers](#6-project-mapping-to-layers)
7. [Mandatory Patterns](#7-mandatory-patterns)
8. [Prohibited Anti-Patterns](#8-prohibited-anti-patterns)
9. [Configuration & Settings Law](#9-configuration--settings-law)
10. [Logging Law](#10-logging-law)
11. [Evolving the Architecture](#11-evolving-the-architecture)

---

## 1. Mission Statement

S7Tools is a cross-platform desktop application for **Siemens S7-1200 PLC security research and
firmware analysis**. Its architecture must support:

- Automated, multi-step PLC memory extraction workflows with hardware resource management.
- Safe concurrent execution of long-running jobs without hardware resource conflicts.
- A rich, reactive UI with real-time log observation and progress tracking.
- Long-term maintainability and testability in a domain of embedded hardware interaction.

---

## 2. Central Philosophy

The architecture of S7Tools is governed by two philosophies operating in tandem:

### 2.1 Hexagonal Architecture (Ports & Adapters)

The application's **business logic** (the "Core") must be **completely independent** of external
concerns such as UI frameworks, file systems, network protocols, and hardware interfaces.

```
┌──────────────────────────────────────────────────────┐
│                   External World                     │
│  (UI, OS, Hardware, File System, Serial Port, PLC)  │
│                                                      │
│   Adapters translate between external and Core        │
└──────────────────┬───────────────────────────────────┘
                   │ (Adapters)
                   ↓
┌──────────────────────────────────────────────────────┐
│                 Ports (Interfaces)                   │
│   IBootloaderService  ISerialPortService             │
│   ISocatService       IPowerSupplyService            │
│   IJobScheduler       IResourceCoordinator          │
└──────────────────┬───────────────────────────────────┘
                   │ (implements)
                   ↓
┌──────────────────────────────────────────────────────┐
│              Application Core (Domain)               │
│   Business Rules   Domain Models   Domain Events     │
│   Profile Aggregates  Job Entities  Task Scheduling  │
│          Zero external framework dependencies         │
└──────────────────────────────────────────────────────┘
```

**The Rule**: The Core must **never** import or reference infrastructure or UI namespaces.
Violations of this rule are forbidden.

### 2.2 SOLID Principles

Every module in the system must adhere to the five SOLID principles:

| Principle | Application |
|-----------|------------|
| **Single Responsibility** | Each class has one and only one reason to change |
| **Open/Closed** | Extend via new implementations, not by modifying existing code |
| **Liskov Substitution** | All `IProfileManager<T>` implementations are freely interchangeable |
| **Interface Segregation** | No service interface has more than one logical responsibility |
| **Dependency Inversion** | All layers depend on abstractions defined in `S7Tools.Core` |

---

## 3. Architectural Commandments

These are the **immutable laws** of the S7Tools architecture:

1. **The Core is Sacred**: `S7Tools.Core` must have zero dependencies on any other project in the
   solution, and zero `PackageReference` to infrastructure or UI libraries.

2. **Interfaces Live in Core**: Every service interface (`ISerialPortService`, `IJobScheduler`,
   `IBootloaderService`, etc.) must be declared in `S7Tools.Core.Interfaces`.

3. **One Source of Truth for Settings**: Application settings are loaded once at startup from
   `appsettings.json` overlaid with `UserSettings.json`. They are bound to `AppSettings` via
   `IOptions<AppSettings>`. **No service may read raw JSON or `IConfiguration` at runtime**;
   all settings access must go through `IApplicationSettingsService`.

4. **Structured Logging Only**: No service may call `Console.WriteLine`, `Debug.Print`, or
   `Trace.Write` for operational output. All output goes through `ILogger<T>` with structured
   message templates (no string interpolation in log messages).

5. **Thread Safety is Non-Negotiable**: Every service that holds mutable state must protect that
   state with a `SemaphoreSlim`. The **Internal Method Pattern** (see §7) must be used to prevent
   deadlocks.

6. **DI Owns All Lifetimes**: No service may be instantiated with `new` outside of factory lambdas
   registered in the DI container. Services are registered exclusively in
   `ServiceCollectionExtensions.cs`.

7. **Async All the Way Down**: Any I/O operation (file, serial, network, process) must be `async`.
   Blocking calls (`Task.Wait()`, `.Result`) in non-test code are forbidden.

8. **Atomic Writes for Persistence**: All file writes for persistent data (profiles, settings) must
   use a write-to-temp-then-rename pattern to prevent data corruption.

9. **Domain Exceptions for Domain Errors**: All error conditions that the application can anticipate
   must be represented by typed exceptions from `S7Tools.Core.Exceptions`, not by returning
   `null`, `false`, or raw `Exception`.

10. **UI Thread Discipline**: Any code that updates an `ObservableCollection` or raises an event
    consumed by the UI must do so on the UI thread via `IUIThreadService.PostToUIThread()` or
    `InvokeAsync()`.

---

## 4. Layer Model

```
┌────────────────────────────────────────────────────────────────┐
│  Layer 4: Presentation                                         │
│  Project: S7Tools (Views, ViewModels, App.axaml.cs)           │
│  Technology: Avalonia UI + ReactiveUI                          │
│  Allowed Dependencies: → Layer 3, → Layer 1                   │
└──────────────────────────────┬─────────────────────────────────┘
                               │
┌──────────────────────────────▼─────────────────────────────────┐
│  Layer 3: Application Services                                  │
│  Project: S7Tools (Services/, Extensions/)                     │
│  Technology: .NET 10, Microsoft.Extensions.*                   │
│  Allowed Dependencies: → Layer 2, → Layer 1                   │
└──────────────────────────────┬─────────────────────────────────┘
                               │
┌──────────────────────────────▼─────────────────────────────────┐
│  Layer 2: Infrastructure                                        │
│  Project: S7Tools.Infrastructure.Logging                       │
│  Technology: Custom ILoggerProvider, Channel<T>, File I/O      │
│  Allowed Dependencies: → Layer 1                              │
└──────────────────────────────┬─────────────────────────────────┘
                               │
┌──────────────────────────────▼─────────────────────────────────┐
│  Layer 1: Domain (Core)                                        │
│  Project: S7Tools.Core                                         │
│  Technology: Pure C# — no framework dependencies               │
│  Allowed Dependencies: NONE                                    │
└────────────────────────────────────────────────────────────────┘
```

---

## 5. Dependency Rules

```
ALLOWED:
  S7Tools            → S7Tools.Core
  S7Tools            → S7Tools.Infrastructure.Logging
  S7Tools.Infrastructure.Logging → S7Tools.Core
  S7Tools.Diagnostics → S7Tools.Core

FORBIDDEN:
  S7Tools.Core       → S7Tools          (inner cannot depend on outer)
  S7Tools.Core       → S7Tools.Infrastructure.Logging
  S7Tools.Infrastructure.Logging → S7Tools
```

Violations of these rules **must** be caught by CI and treated as build failures.

---

## 6. Project Mapping to Layers

| Project | Layer | Responsibility |
|---------|-------|---------------|
| `S7Tools.Core` | Domain | Models, interfaces, exceptions, domain services, validation, constants |
| `S7Tools.Infrastructure.Logging` | Infrastructure | `ILoggerProvider`, `ILogDataStore`, `FileLogSink`, log sinks |
| `S7Tools` | Application + Presentation | Services, ViewModels, Views, DI composition root |
| `S7Tools.Diagnostics` | Dev Tooling | Standalone diagnostic runner (not part of production system) |

---

## 7. Mandatory Patterns

### 7.1 Unified Profile Management Pattern

All profile types (`SerialPortProfile`, `SocatProfile`, `PowerSupplyProfile`, `JobProfile`)
**must** be managed through `StandardProfileManager<T>`. No profile service may re-implement
CRUD logic independently.

```csharp
// CORRECT
public class MyProfileService : StandardProfileManager<MyProfile>, IMyProfileService
{
    protected override MyProfile CreateDefaultProfile() => new() { Name = "Default" };
}

// FORBIDDEN
public class MyProfileService : IMyProfileService
{
    // Re-implementing Load/Save/Create/Delete manually
}
```

### 7.2 Internal Method Pattern (Deadlock Prevention)

Any method that acquires a `SemaphoreSlim` must **never** call another method that also acquires
the same semaphore. Use the Internal Method Pattern:

```csharp
// Public API — acquires semaphore
public async Task<bool> IsPortInUseAsync(int port)
{
    await _semaphore.WaitAsync().ConfigureAwait(false);
    try { return await IsPortInUseInternalAsync(port); }
    finally { _semaphore.Release(); }
}

// Internal — assumes semaphore already held, no acquisition
private async Task<bool> IsPortInUseInternalAsync(int port) { ... }
```

### 7.3 WritableOptions Pattern

All persistent application settings **must** be modified through `IWritableOptions<AppSettings>`,
which atomically writes changes to `UserSettings.json`.

```csharp
// CORRECT: atomic, persistent, notifies listeners
await _options.UpdateAsync(s => { s.Logging.Level = "Debug"; return Task.CompletedTask; });

// FORBIDDEN: bypasses notifications, not persistent
var settings = _options.CurrentValue;
settings.Logging.Level = "Debug"; // mutating the live object directly
```

### 7.4 ReactiveUI ViewModel Pattern

All ViewModels must:
- Inherit from `ReactiveObject`
- Use `this.RaiseAndSetIfChanged(ref _field, value)` for reactive properties
- Declare `ReactiveCommand<TInput, TOutput>` for user actions
- Use `this.WhenAnyValue(...)` for derived state (max 12 operands)
- Implement `IDisposable` and dispose subscriptions in `Dispose()`

### 7.5 Service Disposal Order

Services must be disposed in reverse-dependency order during application shutdown:
1. Task Scheduler (drains queue)
2. Connection Providers (serialPort, socat, power supply)
3. Profile Services
4. UI/Layout/Theme services
5. Logging services (last, so all shutdown messages are captured)

---

## 8. Prohibited Anti-Patterns

| Anti-Pattern | Reason | Correct Alternative |
|-------------|--------|-------------------|
| `Console.WriteLine` in services | Bypasses structured logging | Use `_logger.LogInformation(...)` |
| `string.Format(...)` in log messages | Prevents structured query | Use message templates: `"User {Id}"` |
| Registering services in `Program.cs` | Breaks DI composition root | Register in `ServiceCollectionExtensions.cs` |
| Nested `SemaphoreSlim.WaitAsync()` | Causes deadlocks | Use Internal Method Pattern |
| `Task.Wait()` or `.Result` in services | Causes deadlock on UI thread | Use `await` everywhere |
| `new ConcreteService()` in code | Bypasses DI, untestable | Inject via constructor |
| Raw `IConfiguration["key"]` access | Bypasses type safety | Use `IApplicationSettingsService.Current` |
| `WhenAnyValue` with >12 properties | Performance regression | Use individual subscriptions |
| Modifying `ObservableCollection` from background thread | UI crash | Use `IUIThreadService.PostToUIThread()` |

---

## 9. Configuration & Settings Law

> See [`CONFIGURATION_AND_LOGGING_SYSTEM.md`](./CONFIGURATION_AND_LOGGING_SYSTEM.md) for the
> full system blueprint.

**Key laws**:

1. Configuration is loaded at startup only. The composition root (`Program.cs`) binds
   `appsettings.json` + `UserSettings.json` into `AppSettings` via `IOptions<T>`.
2. All settings access in services goes through `IApplicationSettingsService.Current`.
3. Runtime mutations go through `IApplicationSettingsService.UpdateSettingsAsync(action)`.
4. `UserSettings.json` is the **only** writable settings file. `appsettings.json` is read-only.
5. The configuration section root key is `"App"`. All properties are mapped to `AppSettings`.
6. Data annotation validation (`[Range]`, `[Required]`) runs on startup via `ValidateOnStart()`.

---

## 10. Logging Law

> See [`CONFIGURATION_AND_LOGGING_SYSTEM.md`](./CONFIGURATION_AND_LOGGING_SYSTEM.md) for the
> full system blueprint.

**Key laws**:

1. All operational logging uses `ILogger<T>` from Microsoft.Extensions.Logging.
2. Log messages use structured templates, never string interpolation.
3. The `DataStoreLoggerProvider` (in-memory ring buffer) is always registered.
4. The `UnifiedLoggerProvider` (file sink) is registered for persistent output.
5. Per-task logging uses isolated `DataStoreLoggerProvider` instances created by
   `ITaskLoggerFactory` — these are not shared with the application-wide log store.
6. `Console.WriteLine` is **only** permitted in `Program.cs` within the `--diag` diagnostic flag
   path, and every such call must be followed by a comment `// Keep console for --diag flag`.

---

## 11. Evolving the Architecture

When adding new features:

1. **Define the Port first**: Add the interface to `S7Tools.Core.Interfaces.Services/`.
2. **Build the Domain model**: Add models/exceptions to `S7Tools.Core.Models/`.
3. **Implement the Adapter**: Add the implementation class in `S7Tools/Services/`.
4. **Register in DI**: Register in the appropriate `AddS7Tools*` extension method in
   `ServiceCollectionExtensions.cs`.
5. **Write tests first**: Unit tests should depend only on the `S7Tools.Core` interface.
6. **Document the pattern**: Update `docs/patterns/system-patterns.md`.

**Phase markers in DI** (`// Phase N refactoring`) are acceptable temporary annotations during
active refactoring. They must be resolved before merging to main.
