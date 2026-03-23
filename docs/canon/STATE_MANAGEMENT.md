---
title: "S7Tools State Management — The Ledger Rules"
version: "1.0.0"
created: "2026-03-23"
status: "canon"
tags: ["state-management", "canon", "mvvm", "reactiveui", "prescriptive"]
related:
  - docs/canon/ARCHITECTURE.md
  - docs/canon/DATA_FLOW.md
---

# S7Tools State Management — The Ledger Rules

> **Canon Status**: This document is prescriptive. It defines exactly how application state is
> structured, owned, and mutated. All ViewModel and service state must adhere to these rules.

---

## Table of Contents

1. [State Taxonomy](#1-state-taxonomy)
2. [State Ownership Rules](#2-state-ownership-rules)
3. [ViewModel State Pattern](#3-viewmodel-state-pattern)
4. [Application Settings State](#4-application-settings-state)
5. [Profile State](#5-profile-state)
6. [Job & Task State](#6-job--task-state)
7. [Connection State](#7-connection-state)
8. [Log State](#8-log-state)
9. [Immutability Rules](#9-immutability-rules)
10. [State Change Propagation Rules](#10-state-change-propagation-rules)

---

## 1. State Taxonomy

S7Tools recognizes five categories of state, each with distinct ownership and lifecycle rules:

| Category | Scope | Owner | Persistence | Mutability |
|----------|-------|-------|-------------|------------|
| **Settings State** | Application-wide | `IApplicationSettingsService` | `UserSettings.json` | Via `UpdateSettingsAsync` only |
| **Profile State** | Domain aggregate | `IXxxProfileService` (extends `StandardProfileManager<T>`) | Profile JSON files | Via service CRUD methods only |
| **Job/Task State** | Domain entity lifecycle | `IJobScheduler` / `ITaskScheduler` | In-memory + optionally serialized | Via scheduler state machine only |
| **Connection State** | Hardware session | `ISocatService` / `ISerialPortService` / `IPowerSupplyService` | In-memory (no persistence) | Via connect/disconnect operations |
| **UI State** | ViewModel-local | Individual `ViewModel` | Layout/Theme via service | Via reactive property setters |

---

## 2. State Ownership Rules

**Rule 1**: Each piece of state has exactly **one canonical owner**. No two services or ViewModels
may independently maintain copies of the same logical state without a defined synchronization
contract.

**Rule 2**: ViewModels do not own primary state — they **project** state from services. When a
service's state changes, the ViewModel re-reads from the service and rebuilds its local projection.

**Rule 3**: Services own domain state. ViewModels own presentation state (selected item, filter
text, loading indicators). These two categories must never be conflated.

**Rule 4**: Persistent state (profiles, settings) is always the authoritative source of truth, not
an in-memory cache. After any write, the in-memory representation must be refreshed from the
persisted state to guarantee consistency.

---

## 3. ViewModel State Pattern

### 3.1 Reactive Property Declaration

All ViewModel properties that the UI binds to must use `RaiseAndSetIfChanged`:

```csharp
public class SerialPortPageViewModel : ReactiveObject, IDisposable
{
    // ── Backing fields ─────────────────────────────────────────────────────
    private ObservableCollection<SerialPortProfileViewModel> _profiles = [];
    private SerialPortProfileViewModel? _selectedProfile;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    // ── Reactive properties ────────────────────────────────────────────────
    public ObservableCollection<SerialPortProfileViewModel> Profiles
    {
        get => _profiles;
        private set => this.RaiseAndSetIfChanged(ref _profiles, value);
    }

    public SerialPortProfileViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
}
```

### 3.2 Command Declaration

Commands must declare their executability condition explicitly:

```csharp
// ── Observable conditions ──────────────────────────────────────────────────
IObservable<bool> canSave = this.WhenAnyValue(
    x => x.SelectedProfile,
    x => x.IsLoading,
    (profile, loading) => profile != null && !loading);

IObservable<bool> canDelete = this.WhenAnyValue(
    x => x.SelectedProfile,
    profile => profile != null && !profile.IsDefault);

// ── Commands ───────────────────────────────────────────────────────────────
SaveCommand    = ReactiveCommand.CreateFromTask(SaveAsync,    canSave);
DeleteCommand  = ReactiveCommand.CreateFromTask(DeleteAsync,  canDelete);
RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
```

### 3.3 Collection Refresh Pattern

When reloading a collection from a service, **always replace the collection** to trigger DataGrid
refresh:

```csharp
private async Task RefreshAsync()
{
    IsLoading = true;
    try
    {
        var profiles = await _profileService.GetAllAsync().ConfigureAwait(false);
        var vms = profiles.Select(p => new SerialPortProfileViewModel(p)).ToList();

        await _uiThreadService.InvokeAsync(() =>
        {
            Profiles.Clear();
            foreach (var vm in vms)
                Profiles.Add(vm);

            // Re-select previously selected item by identity
            SelectedProfile = _targetId.HasValue
                ? Profiles.FirstOrDefault(p => p.Id == _targetId.Value)
                : Profiles.FirstOrDefault();
        });
    }
    finally
    {
        IsLoading = false;
    }
}
```

**PROHIBITED**: Mutating `Profiles` from a background thread directly or trying to partially
update individual items without clearing and re-adding.

### 3.4 Settings Subscription Pattern

ViewModels that react to settings changes must:

```csharp
public class LogViewerViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public LogViewerViewModel(IApplicationSettingsService settings)
    {
        // Subscribe to specific settings keys
        settings.SettingsChanged += OnSettingsChanged;
        _disposables.Add(Disposable.Create(() => settings.SettingsChanged -= OnSettingsChanged));

        // Initialize from current settings
        RefreshFromSettings(settings.Current);
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        RefreshFromSettings(((IApplicationSettingsService)sender!).Current);
    }

    private void RefreshFromSettings(AppSettings s)
    {
        AutoScrollLogs    = s.Ui.AutoScrollLogs;
        ShowTimestamp     = s.Ui.ShowTimestampInLogs;
        ShowCategory      = s.Ui.ShowCategoryInLogs;
        ShowLogLevel      = s.Ui.ShowLogLevelInLogs;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
```

---

## 4. Application Settings State

### 4.1 Structure

`AppSettings` is the single, strongly-typed root of all application configuration:

```
AppSettings
├── LoggingSettings   Logging    (level, file size, directories)
├── UiSettings        Ui         (theme, scroll behavior, log display)
├── PathSettings      Paths      (log dirs, export dirs)
├── ProfileSettings   Profiles   (file paths to profile JSON files)
├── SerialSettings    Serial     (default baud rate, port discovery)
├── NetworkSettings   Network    (default ports, retries)
├── SocatSettings     Socat      (max instances, shutdown timeout)
├── PowerSupplySettings PowerSupply (timeouts, reconnect policy)
├── PlcSettings       Plc        (connection timeout, retry count)
├── JobSettings       Jobs       (max parallel, queue size)
├── TaskSettings      Tasks      (polling interval, persistence)
├── MemoryRegionSettings MemoryRegion (addressing defaults)
├── ExportSettings    Export     (output formats, directories)
└── MemoryDumpSettings MemoryDump (segment delays, default folder)
```

### 4.2 Mutation Contract

| Operation | Method | Persistence | Notification |
|-----------|--------|-------------|-------------|
| Read current value | `_settings.Current.Xxx` | N/A | N/A |
| Update one or more fields | `UpdateSettingsAsync(s => { s.Xxx = y; })` | Writes `UserSettings.json` | `SettingsChanged` event |
| Reset to defaults | `ResetAllSettingsAsync()` | Overwrites `UserSettings.json` | `SettingsChanged` event |
| Reload from disk | `LoadSettingsAsync()` | N/A (reads) | `SettingsChanged` event |

### 4.3 Precedence Order

When settings are loaded, later sources override earlier ones:

```
1. appsettings.json     (application defaults, read-only, not modified at runtime)
2. UserSettings.json    (user overrides, writable via IWritableOptions<T>)
```

---

## 5. Profile State

### 5.1 Canonical Profile Types

| Profile Type | Interface | File | Profile Manager |
|-------------|-----------|------|----------------|
| `SerialPortProfile` | `ISerialPortProfileService` | `Resources/Profiles/Serial/SerialProfiles.json` | `SerialPortProfileService` |
| `SocatProfile` | `ISocatProfileService` | `Resources/Profiles/Socat/SocatProfiles.json` | `SocatProfileService` |
| `PowerSupplyProfile` | `IPowerSupplyProfileService` | `Resources/Profiles/PowerSupply/PowerSupplyProfiles.json` | `PowerSupplyProfileService` |
| `MemoryMappingProfile` | `IMemoryRegionProfileService` | `Resources/Profiles/Memory/MemoryProfiles.json` | `MemoryRegionProfileService` |
| `JobProfile` | `IJobManager` | `Resources/JobProfiles/profiles.json` | `JobManager` |
| `PayloadSetProfile` | `IPayloadSetProfileService` | `Resources/Profiles/PayloadSets/PayloadSetProfiles.json` | `PayloadSetProfileService` |

### 5.2 Profile Invariants

1. Every profile has a unique non-zero `Id` (assigned by `StandardProfileManager` on create).
2. Profile names must be unique within a type (enforced by `IProfileValidator`).
3. Exactly **zero or one** profile per type may have `IsDefault = true`.
4. A default profile **cannot be deleted** — another must be designated first.
5. `CreatedAt` and `ModifiedAt` timestamps are always managed by the service, never by the caller.

### 5.3 Profile State Lifecycle

```
Created → Validated → Persisted → In-Memory list updated → Event raised → ViewModels refresh
```

---

## 6. Job & Task State

### 6.1 Job State Machine

```
         ┌─────────────────────────────────────────────────────────┐
         │                     State Machine                       │
         │                                                         │
         │   Created ──► Queued ──► Running ──► Completed         │
         │                  │          │                           │
         │                  └──────────┴──────► Failed             │
         │                  │                                      │
         │                  └──────────────────► Cancelled         │
         └─────────────────────────────────────────────────────────┘
```

**Allowed transitions**:

| From | To | Trigger |
|------|-----|---------|
| `Created` | `Queued` | `IJobScheduler.EnqueueAsync()` |
| `Queued` | `Running` | Resources acquired by `IResourceCoordinator` |
| `Queued` | `Cancelled` | `IJobScheduler.CancelJobAsync()` |
| `Running` | `Completed` | `IBootloaderService` returns success |
| `Running` | `Failed` | `IBootloaderService` throws or returns failure |
| `Running` | `Cancelled` | `CancellationToken` cancelled |

**PROHIBITED**: Transitioning directly from `Created` to `Running`, or from `Completed`/`Failed`
to any other state.

### 6.2 Task State Machine (TaskExecution)

```
         Pending ──► Running ──► Completed
                        │
                        ├──────► Failed
                        └──────► Cancelled
```

`TaskExecution.UpdateState(state, message)` is the **only** legal way to transition task state.

### 6.3 Job State Ownership

- `IJobScheduler` owns the complete lifecycle and state of all `Job` instances.
- `IJobManager` owns persistence of `JobProfile` definitions (not executions).
- ViewModels subscribe to `IJobScheduler.JobStateChanged` and `JobProgressChanged` events;
  they never hold authoritative job state themselves.

---

## 7. Connection State

### 7.1 Connection State Per Service

Each hardware connection service maintains its own connection state:

| Service | State Enum | Stored In | Key States |
|---------|-----------|-----------|-----------|
| `ISerialPortService` | In-memory flag | Service | Open / Closed |
| `ISocatService` | Per-instance dictionary | `SocatProcessManager` | Running / Stopped |
| `IPowerSupplyService` | `ConnectionState` enum | Service | Connected / Disconnected / Error |

### 7.2 Connection State Rules

1. Connection state is **never persisted**. On startup, all connections are assumed closed.
2. Connection state changes are communicated via events (`ConnectionStateChanged`, `ProcessStateChanged`).
3. ViewModels must reflect live connection state from the service via event subscription, not by
   polling or holding state independently.
4. The `IResourceCoordinator` tracks **resource usage** (which jobs hold which resources) — this
   is orthogonal to connection state.

---

## 8. Log State

### 8.1 Application Log Store

```
ILogDataStore (singleton)
- Bounded ConcurrentQueue<LogEntry> (max 10,000 entries)
- When full: oldest entries are evicted (ring buffer behavior)
- Thread-safe reads and writes
- Raises NewEntryAdded event (fire-and-forget, no ordering guarantees)
```

### 8.2 Per-Task Log Stores

```
ICentralizedTaskLogService (singleton)
- Dictionary<Guid, (ITaskLogDataStore main, ITaskLogDataStore process, ITaskLogDataStore protocol)>
- Created on task start via ITaskLoggerFactory
- Available to UI until explicitly removed
- Not evicted automatically (explicitly removed after task display is closed)
```

### 8.3 Log State Rules

1. The application-wide `ILogDataStore` is read-only from the ViewModel's perspective.
   Only `ILoggerProvider` implementations write to it.
2. Per-task log stores are created by `ITaskLoggerFactory` and accessed by
   `ICentralizedTaskLogService`. ViewModels subscribe to updates for their specific task ID.
3. Log entries are immutable records. They cannot be edited after creation.
4. Log entry filtering (level, category, search text) is **presentation state** owned by
   `LogViewerViewModel`, not by the data store.

---

## 9. Immutability Rules

### 9.1 Domain Models

Domain models returned from services must be treated as immutable snapshots:

```csharp
// CORRECT: treat service result as immutable
var profile = await _service.GetByIdAsync(id);
var updated = profile with { Name = "New Name" }; // C# record with-expression
await _service.UpdateAsync(updated);

// FORBIDDEN: mutating a returned object directly
var profile = await _service.GetByIdAsync(id);
profile.Name = "New Name"; // ❌ undefined behavior, not persisted
await _service.UpdateAsync(profile);
```

### 9.2 AppSettings Snapshot

The object returned by `IApplicationSettingsService.Current` is a live snapshot. **Never
store a reference** to it across async operations — always re-read from `Current`.

```csharp
// CORRECT: re-read after await
async Task DoSomethingAsync()
{
    await LongOperationAsync();
    var level = _settings.Current.Logging.Level; // ✅ fresh read
}

// FORBIDDEN: stale reference
async Task DoSomethingAsync()
{
    var settings = _settings.Current; // captured before await
    await LongOperationAsync();
    var level = settings.Logging.Level; // ❌ may be stale
}
```

---

## 10. State Change Propagation Rules

### 10.1 Event Threading Contract

All events raised by services that are consumed by ViewModels must arrive on the **UI thread**:

```csharp
// In ApplicationSettingsService — correct pattern
private void RaiseSettingsChanged(SettingsChangedEventArgs args)
{
    var handler = SettingsChanged;
    if (handler is null) return;

    if (_uiThreadService is not null)
        _uiThreadService.PostToUIThread(() => handler(this, args)); // ✅ UI-safe
    else
        handler(this, args);
}
```

### 10.2 Propagation Order

State changes propagate strictly in this order:
1. Service mutates internal state
2. Service persists to storage (if applicable)
3. Service raises event
4. Event marshalled to UI thread
5. ViewModel updates reactive properties
6. Avalonia re-renders affected controls

**No step may be skipped or reordered.**

### 10.3 Subscription Disposal

Every subscription must be disposed when the ViewModel is disposed:

```csharp
public void Dispose()
{
    // Unsubscribe events
    _settingsService.SettingsChanged -= OnSettingsChanged;
    _jobScheduler.JobStateChanged   -= OnJobStateChanged;

    // Dispose reactive subscriptions
    _disposables.Dispose();
}
```

Failure to unsubscribe causes memory leaks and potential null-reference exceptions after shutdown.
