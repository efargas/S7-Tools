---
title: "S7Tools Data Flow — The Blueprint"
version: "1.0.0"
created: "2026-03-23"
last-updated: "2026-03-23"
status: "current"
tags: ["data-flow", "canon", "prescriptive", "unidirectional"]
related:
  - docs/canon/ARCHITECTURE.md
  - docs/canon/STATE_MANAGEMENT.md
---

# S7Tools Data Flow — The Blueprint

> **Canon Status**: This document prescribes the **mandatory, unidirectional data flow** for all
> interactions in S7Tools. Deviations from this flow are architectural violations.

---

## Table of Contents

1. [Core Principle: Unidirectional Data Flow](#1-core-principle-unidirectional-data-flow)
2. [Primary Application Data Flow](#2-primary-application-data-flow)
3. [Configuration Data Flow](#3-configuration-data-flow)
4. [Logging Data Flow](#4-logging-data-flow)
5. [Profile CRUD Data Flow](#5-profile-crud-data-flow)
6. [Job Execution Data Flow](#6-job-execution-data-flow)
7. [Prohibited Data Flow Anti-Patterns](#7-prohibited-data-flow-anti-patterns)

---

## 1. Core Principle: Unidirectional Data Flow

All data in S7Tools flows in **one direction only**:

```
User Action → ViewModel Command → Application Service → Domain → Infrastructure → State Update → ViewModel re-render
```

**No data may flow backwards** (e.g., Infrastructure → ViewModel directly, or Service → View).
All state changes must be observed through the ViewModel layer.

---

## 2. Primary Application Data Flow

### 2.1 Complete Flow Diagram

```
┌────────────────────────────────────────────────────────────────────────────────┐
│  USER INPUT                                                                    │
│  (Button click / field change / menu selection)                               │
└─────────────────────┬──────────────────────────────────────────────────────────┘
                      │ (Avalonia UI event)
                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  VIEW (Avalonia .axaml)                                                         │
│  - Binds to ViewModel via DataContext                                           │
│  - Invokes ReactiveCommand via {Binding MyCommand}                             │
│  - Never contains business logic                                               │
│  - Never accesses services directly                                            │
└─────────────────────┬───────────────────────────────────────────────────────────┘
                      │ (ReactiveCommand.Execute)
                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  VIEWMODEL (ReactiveObject)                                                     │
│  - Validates user input (UI-level only)                                        │
│  - Invokes Application Service method                                          │
│  - Observes IObservable<T> / Events for state changes                         │
│  - Projects domain state → UI state                                            │
│  - Never accesses Infrastructure or Core directly for side effects            │
└─────────────────────┬───────────────────────────────────────────────────────────┘
                      │ (async method call via injected service interface)
                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  APPLICATION SERVICE (Layer 3)                                                  │
│  - Enforces business rules and invariants                                      │
│  - Orchestrates domain operations                                              │
│  - Coordinates cross-cutting concerns (logging, settings)                     │
│  - Raises domain events or returns results                                     │
│  - Never references ViewModels or Views                                        │
└──────────┬──────────────────────────────────────────┬───────────────────────────┘
           │ (domain operation)                       │ (side effects)
           ▼                                          ▼
┌──────────────────────┐                  ┌───────────────────────────────────────┐
│  DOMAIN (Core)       │                  │  INFRASTRUCTURE                        │
│  - Pure business     │                  │  - File system                         │
│    logic             │                  │  - Serial port / network               │
│  - Domain models     │                  │  - Logging (FileLogSink, DataStore)    │
│  - Invariants        │                  │  - OS shell execution                  │
│  - No I/O            │                  │                                       │
└──────────┬───────────┘                  └───────────────────────────────────────┘
           │ (returns domain model/result)
           ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  APPLICATION SERVICE receives result, raises event / updates state             │
└─────────────────────┬───────────────────────────────────────────────────────────┘
                      │ (EventHandler<T> or IObservable<T>)
                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  VIEWMODEL observes change (via PostToUIThread if needed)                      │
│  Updates reactive properties → triggers UI re-render                          │
└─────────────────────┬───────────────────────────────────────────────────────────┘
                      │ (Avalonia binding propagation)
                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  VIEW re-renders updated properties                                            │
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Configuration Data Flow

### 3.1 Startup Configuration Loading

```
┌─────────────────────────────────────────────────────────┐
│  Program.ConfigureServices()                            │
│  1. ConfigurationBuilder                                │
│     .AddJsonFile("appsettings.json",    optional:true)  │
│     .AddJsonFile("UserSettings.json",   optional:true)  │
│     .Build()                            ← IConfiguration│
│                                                         │
│  2. services.AddOptions<AppSettings>()                  │
│       .Bind(config.GetSection("App"))                   │
│       .ValidateDataAnnotations()                        │
│       .ValidateOnStart()   ← fails fast on bad config   │
│                                                         │
│  3. services.AddTransient<IWritableOptions<AppSettings>>│
│       (factory creates WritableOptions<T>)              │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│  IApplicationSettingsService (singleton)               │
│  - Wraps IWritableOptions<AppSettings>                 │
│  - Exposes Current → AppSettings (read-only view)      │
│  - Raises SettingsChanged on UI thread                 │
└─────────────────────┬───────────────────────────────────┘
                      │ (injected into services)
                      ▼
┌─────────────────────────────────────────────────────────┐
│  All services read settings via:                        │
│  _settingsService.Current.Logging.Level                │
│  _settingsService.Current.Serial.DefaultBaudRate       │
│  etc.                                                   │
└─────────────────────────────────────────────────────────┘
```

### 3.2 Runtime Settings Update Flow

```
User changes setting in UI
        │
        ▼
SettingsViewModel.SaveCommand
        │
        ▼
IApplicationSettingsService.UpdateSettingsAsync(action)
        │
        ▼
IWritableOptions<AppSettings>.UpdateAsync(action)
        │  ← serializes current AppSettings
        │  ← applies action delta
        │  ← writes to temp file
        │  ← atomically renames temp → UserSettings.json
        ▼
IOptionsMonitor<AppSettings> reloads (reloadOnChange:true)
        │
        ▼
IApplicationSettingsService raises SettingsChanged on UI thread
        │
        ▼
Subscribed ViewModels refresh from _settingsService.Current
        │
        ▼
UI re-renders updated values
```

**PROHIBITED**: Directly mutating the `AppSettings` object returned by `Current`. It is a snapshot
and mutations will not be persisted or propagated.

---

## 4. Logging Data Flow

### 4.1 Application-Wide Logging Pipeline

```
Service calls ILogger<T>.LogInformation("Message {Key}", value)
        │
        ▼
Microsoft.Extensions.Logging infrastructure
        │
        ├──────────────────────────────────────────────┐
        ▼                                              ▼
DataStoreLoggerProvider                   UnifiedLoggerProvider
(in-memory ring buffer)                   (delegates to ILogSink[])
        │                                              │
        ▼                                              ▼
LogDataStore (ILogDataStore)              FileLogSink
- ConcurrentQueue<LogEntry>               - Channel<LogEntry> (bounded, 10k)
- Max 10,000 entries                      - Background Task processes entries
- Thread-safe                             - Routes by category → file path
        │                                 - Batches + flushes every 1s
        ▼
LogViewerViewModel
- Subscribes to ILogDataStore.NewEntryAdded
- Projects LogEntry → LogEntryViewModel
- Filters by level / category / search text
        │
        ▼
UI LogViewer DataGrid re-renders
```

### 4.2 Per-Task Logging Pipeline

```
ITaskLoggerFactory.CreateTaskLoggerAsync(taskId, taskName)
        │
        ▼
Creates isolated DataStoreLoggerProvider instances:
  - mainProvider    → ITaskLogDataStore (main execution log)
  - processProvider → ITaskLogDataStore (process stdout/stderr)
        │
        ▼
Registers task stores in ICentralizedTaskLogService
        │
        ▼
Returns TaskLogger (wraps both providers)
        │
        ▼
BootloaderService / JobScheduler use TaskLogger for all task output
        │
        ├──── mainProvider  → persisted to {LogsDirectory}/Tasks/{task}_{ts}/task-main.log
        └──── processProvider → persisted to {LogsDirectory}/Tasks/{task}_{ts}/task-process.log

TaskLogViewerViewModel reads from ICentralizedTaskLogService.GetStoresForTask(taskId)
```

---

## 5. Profile CRUD Data Flow

### 5.1 Create Profile Flow

```
User fills form in dialog
        │
        ▼
ProfileCreateDialogViewModel validates input
(UI-level validation: required fields, format)
        │
        ▼ (on SaveCommand.Execute)
ISerialPortProfileService.CreateAsync(profile)
        │
        ▼
StandardProfileManager<T>.CreateAsync(profile)
  - Acquires _semaphore
  - Calls ValidateAsync(profile) [domain validation]
  - Assigns unique Id
  - Appends to in-memory list
  - Calls SaveInternalAsync()   ← writes temp file → atomic rename
  - Releases _semaphore
        │
        ▼
ProfilePageViewModel receives ProfileChanged event
  - Reloads collection from service
  - Re-selects new profile by Id
        │
        ▼
UI DataGrid renders updated list, new row selected
```

### 5.2 Profile Update Flow

```
User edits profile in dialog → dialog closes on SaveAsync success
        │
        ▼
IXxxProfileService.UpdateAsync(profile)
        │
        ▼
StandardProfileManager<T>.UpdateAsync(profile)
  - Acquires _semaphore
  - Validates with IProfileValidator
  - Replaces existing entry in list
  - Atomic file write
  - Releases _semaphore
        │
        ▼
Profiles collection reloaded → target profile re-selected by Id
```

---

## 6. Job Execution Data Flow

> See also: [`JOB_EXECUTION_PIPELINE.md`](./JOB_EXECUTION_PIPELINE.md)

```
User clicks "Run Job" in JobsPage
        │
        ▼
JobsPageViewModel.RunJobCommand.Execute(selectedJobProfile)
        │
        ▼
IJobManager.EnqueueJobAsync(jobProfile)  → creates Job entity (state: Created)
        │
        ▼
IJobScheduler.EnqueueAsync(job)  → job.State = Queued
        │
        ▼
JobScheduler background loop checks:
  - Are required resources (serialPort, socatPort) free?
  - IResourceCoordinator.TryAcquireAsync([resources])
        │
  Yes ──┼──── Executes job:
        │       IBootloaderService.DumpWithTaskTrackingAsync(taskExec, profiles)
        │         - IResourceCoordinator holds all resources during execution
        │         - IProgress<T> events → TaskExecution.Progress updated
        │         - ITaskLoggerFactory creates isolated task logger
        │
  No ───┼──── Job stays Queued
        │       IResourceCoordinator.WaitForResourcesAsync(...)
        │       Job promoted to Running when resources free
        │
        ▼
Job completes → job.State = Completed | Failed
IResourceCoordinator releases resources
JobStateChanged event raised
        │
        ▼
JobsPageViewModel observes JobStateChanged → refreshes UI
```

---

## 7. Prohibited Data Flow Anti-Patterns

The following data flow patterns are **explicitly forbidden**:

### Anti-Pattern 1: Service-to-ViewModel Dependency

```csharp
// FORBIDDEN: Service depends on ViewModel
public class BootloaderService
{
    private readonly JobsPageViewModel _vm; // ❌ NEVER
    public BootloaderService(JobsPageViewModel vm) { _vm = vm; }
}

// CORRECT: Service raises events; ViewModel subscribes
public class BootloaderService
{
    public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;
}
```

### Anti-Pattern 2: ViewModel Reading Infrastructure Directly

```csharp
// FORBIDDEN: ViewModel reads from file system directly
public class LogViewerViewModel
{
    public void Load() {
        var lines = File.ReadAllLines("app.log"); // ❌ NEVER
    }
}

// CORRECT: ViewModel reads from service/data store
public class LogViewerViewModel
{
    public void Load() {
        var entries = _logDataStore.GetAll(); // ✅
    }
}
```

### Anti-Pattern 3: Circular Event Propagation

```
// FORBIDDEN: SettingsChanged → UpdateSettings → SettingsChanged → infinite loop
_settingsService.SettingsChanged += (s, e) => {
    _settingsService.UpdateSettingsAsync(x => ...); // ❌ triggers SettingsChanged again
};
```

### Anti-Pattern 4: Background Thread UI Update

```csharp
// FORBIDDEN: Updating UI-bound collection from background thread
await Task.Run(() => {
    Profiles.Add(newProfile); // ❌ crashes Avalonia
});

// CORRECT: Marshal to UI thread
await Task.Run(async () => {
    var profiles = await _service.GetAllAsync();
    await _uiThreadService.InvokeAsync(() => {
        Profiles.Clear();
        Profiles.AddRange(profiles); // ✅
    });
});
```

### Anti-Pattern 5: Raw Configuration Access in Services

```csharp
// FORBIDDEN: Service reads IConfiguration directly at runtime
public class SerialPortService
{
    public int GetBaudRate() => int.Parse(_config["App:Serial:DefaultBaudRate"]); // ❌
}

// CORRECT: Use strongly-typed settings
public class SerialPortService
{
    public int GetBaudRate() => _settings.Current.Serial.DefaultBaudRate; // ✅
}
```
