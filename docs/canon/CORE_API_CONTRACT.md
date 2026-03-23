---
title: "S7Tools Core API Contract — The Engine Manual"
version: "1.0.0"
created: "2026-03-23"
last-updated: "2026-03-23"
status: "current"
tags: ["api-contract", "canon", "services", "interfaces", "prescriptive"]
related:
  - docs/canon/ARCHITECTURE.md
  - docs/canon/DATA_FLOW.md
---

# S7Tools Core API Contract — The Engine Manual

> **Canon Status**: This document defines the **formal, internal API** of the S7Tools service
> layer. All services must adhere to these contracts. ViewModels and external consumers interact
> with the system exclusively through these interfaces.

---

## Table of Contents

1. [API Contract Principles](#1-api-contract-principles)
2. [Settings & Configuration API](#2-settings--configuration-api)
3. [Profile Management API](#3-profile-management-api)
4. [Job & Task Orchestration API](#4-job--task-orchestration-api)
5. [Hardware Interface API](#5-hardware-interface-api)
6. [Logging API](#6-logging-api)
7. [UI Services API](#7-ui-services-api)
8. [Cross-Cutting Service Rules](#8-cross-cutting-service-rules)

---

## 1. API Contract Principles

### 1.1 Interface Purity

All service interfaces defined in `S7Tools.Core.Interfaces` must be:
- **Framework-free**: No reference to Avalonia, ReactiveUI, or any UI framework type
- **Pure abstractions**: No default implementations unless trivially correct
- **Testable**: Every interface must be fully mockable without special infrastructure

### 1.2 Method Semantics

| Method Pattern | Semantics | Return Type |
|---------------|-----------|-------------|
| `GetXxxAsync()` | Read, never mutates | `Task<T>` or `Task<IReadOnlyList<T>>` |
| `CreateAsync(x)` | Creates new entity, persists, raises event | `Task<T>` where T is created entity |
| `UpdateAsync(x)` | Updates existing entity, persists, raises event | `Task` |
| `DeleteAsync(id)` | Removes entity, persists, raises event | `Task` |
| `ConnectAsync()` | Establishes hardware/network connection | `Task<bool>` or `Task` |
| `DisconnectAsync()` | Tears down connection cleanly | `Task` |
| `EnqueueAsync(x)` | Adds work to queue, returns immediately | `Task<T>` |
| `IsXxxAsync()` | State check, read-only | `Task<bool>` |

### 1.3 Error Communication

Services must not return `null` or `false` to indicate domain errors. Use:
- **Typed exceptions** from `S7Tools.Core.Exceptions` for anticipated failures
- **Logging** for recoverable/non-fatal conditions
- **Return values** (`bool`, result types) only for expected true/false decisions

---

## 2. Settings & Configuration API

### 2.1 `IApplicationSettingsService`

**Location**: `S7Tools.Core.Interfaces.Services.IApplicationSettingsService`

**Responsibility**: Single point of access for all application configuration at runtime.

```csharp
public interface IApplicationSettingsService
{
    /// <summary>
    /// Gets the current, strongly-typed application settings snapshot.
    /// This value may change when settings are updated. Always read fresh.
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Fired on the UI thread whenever any setting is changed.
    /// Subscribe to react to setting changes in ViewModels.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Applies a mutation to settings and persists to UserSettings.json atomically.
    /// Raises SettingsChanged after successful write.
    /// </summary>
    Task UpdateSettingsAsync(Action<AppSettings> updateAction);

    /// <summary>
    /// Reloads settings from disk sources. Use when files are modified externally.
    /// Raises SettingsChanged after reload.
    /// </summary>
    Task LoadSettingsAsync();

    /// <summary>
    /// Resets all settings to compiled defaults and persists.
    /// Raises SettingsChanged after reset.
    /// </summary>
    Task ResetAllSettingsAsync();

    /// <summary>
    /// Restores defaults (alias for ResetAllSettingsAsync).
    /// </summary>
    Task RestoreDefaultsAsync();

    /// <summary>
    /// Exports the current settings subset (logging + UI) as a JSON string.
    /// </summary>
    string ExportSettingsToJson();

    /// <summary>
    /// Imports settings from a JSON string (partial update, only known keys).
    /// Returns true on success.
    /// </summary>
    Task<bool> ImportSettingsFromJsonAsync(string json);
}
```

**Consumer Rules**:
- Read settings via `_settings.Current.XxxSection.Property`
- Never store `_settings.Current` in a field — always re-read per operation
- Subscribe to `SettingsChanged` and call `RefreshFromSettings(settings.Current)` in handler
- Unsubscribe in `Dispose()`

### 2.2 `IWritableOptions<T>`

**Location**: `S7Tools.Core.Interfaces.Services.IWritableOptions<T>`

**Responsibility**: Thread-safe atomic persistence of strongly-typed options.

```csharp
public interface IWritableOptions<T> : IOptionsSnapshot<T> where T : class, new()
{
    T CurrentValue { get; }
    void Update(Action<T> applyChanges);
    Task UpdateAsync(Func<T, Task> applyChanges);
}
```

**Implementation Contract** (enforced by `WritableOptions<T>`):
- Must acquire `SemaphoreSlim` before reading or writing
- Must write to a temp file then atomically rename to target
- Must not throw if the target file does not yet exist (create on first write)
- Must preserve unrelated JSON sections when writing

---

## 3. Profile Management API

### 3.1 `IProfileManager<T>` — Base CRUD Interface

**Location**: `S7Tools.Core.Interfaces.Services.IProfileManager<T>`

```csharp
public interface IProfileManager<T> where T : class, IProfileBase
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<T?> GetDefaultAsync(CancellationToken ct = default);
    Task<T> CreateAsync(T profile, CancellationToken ct = default);
    Task UpdateAsync(T profile, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SetDefaultAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
}
```

### 3.2 Specialized Profile Service Interfaces

Each profile type exposes its own interface extending `IProfileManager<T>` with domain-specific
operations:

**`ISerialPortProfileService`** extends `IProfileManager<SerialPortProfile>`:
```csharp
Task<bool> IsPortInUseAsync(int port, CancellationToken ct = default);
Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken ct = default);
```

**`ISocatProfileService`** extends `IProfileManager<SocatProfile>`:
```csharp
Task<bool> IsPortConflictAsync(int port, int excludeProfileId = 0, CancellationToken ct = default);
```

**`IPowerSupplyProfileService`** extends `IProfileManager<PowerSupplyProfile>`:
```csharp
Task<IReadOnlyList<PowerSupplyProfile>> GetByIpAsync(string ipAddress, CancellationToken ct = default);
```

**`IJobManager`** extends `IProfileManager<JobProfile>`:
```csharp
Task<bool> CanExecuteAsync(int jobProfileId, CancellationToken ct = default);
```

### 3.3 Profile Validation Contract

Profile validation is performed by `IProfileValidator<T>` before any create/update:

```csharp
public interface IProfileValidator<T> where T : IProfileBase
{
    Task<ValidationResult> ValidateAsync(T profile, IReadOnlyList<T> existingProfiles);
}
```

`ValidationResult` must carry structured error messages, not raw strings. The validator must check:
1. Name is non-empty and ≤ 100 characters
2. Name is unique within the collection (excluding the profile being updated by Id)
3. Domain-specific invariants (port ranges, IP format, etc.)

---

## 4. Job & Task Orchestration API

### 4.1 `IJobScheduler`

**Location**: `S7Tools.Core.Interfaces.Services.IJobScheduler`

```csharp
public interface IJobScheduler
{
    event EventHandler<JobStateChangedEventArgs>?    JobStateChanged;
    event EventHandler<JobProgressChangedEventArgs>? JobProgressChanged;

    Task<Job>  EnqueueAsync(Job job, CancellationToken ct = default);
    Task<bool> CancelJobAsync(int jobId, CancellationToken ct = default);
    Task<Job?> GetJobAsync(int jobId, CancellationToken ct = default);
    Task<IReadOnlyList<Job>> GetAllJobsAsync(CancellationToken ct = default);
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
```

**Scheduler Invariants**:
- `EnqueueAsync` must return synchronously with the `Queued` job — it does not start execution.
- Execution begins only when the scheduler's background loop is running (`StartAsync` called).
- Resource acquisition happens inside the scheduler loop via `IResourceCoordinator`, not in `EnqueueAsync`.
- The `JobStateChanged` event fires on every legal state transition.

### 4.2 `IResourceCoordinator`

**Location**: `S7Tools.Core.Interfaces.Services.IResourceCoordinator`

```csharp
public interface IResourceCoordinator
{
    Task<ResourceHandle> TryAcquireAsync(
        IReadOnlyList<string> resourceIds,
        int jobId,
        CancellationToken ct = default);

    Task ReleaseAsync(ResourceHandle handle, CancellationToken ct = default);
    Task<bool> IsResourceInUseAsync(string resourceId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetInUseResourcesAsync(CancellationToken ct = default);
}
```

**Resource IDs**: Resources are identified by opaque strings. Canonical resource IDs are:
- Serial port: `/dev/ttyUSB0`, `/dev/ttyACM0`, `COM3`, etc. (the device path)
- Socat TCP port: `tcp:2023`
- Power supply Modbus connection: `modbus:192.168.1.100:502`

**Acquisition semantics**: `TryAcquireAsync` must be all-or-nothing. If any resource in the list
is unavailable, it must either wait (blocking with CancellationToken support) or throw
`ResourceUnavailableException`. It must never partially acquire resources.

### 4.3 `IBootloaderService`

**Location**: `S7Tools.Core.Interfaces.Services.IBootloaderService`

```csharp
public interface IBootloaderService
{
    RetryConfiguration RetryConfiguration { get; }
    void UpdateRetryConfiguration(RetryConfiguration configuration);

    Task<BootloaderResult> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        ILogger? taskLogger = null,
        ILogger? processLogger = null,
        CancellationToken ct = default);

    Task<BootloaderResult> DumpWithTaskTrackingAsync(
        TaskExecution taskExecution,
        JobProfileSet profiles,
        CancellationToken ct = default);
}
```

**BootloaderResult** must carry:
- `Success: bool`
- `Stage: string` (final stage reached)
- `BytesDumped: long`
- `ElapsedTime: TimeSpan`
- `Error: Exception?` (null on success)
- `DumpFilePath: string?` (path to output file on success)

---

## 5. Hardware Interface API

### 5.1 `ISerialPortService`

**Responsibility**: Manages physical serial port discovery, configuration, and connection lifecycle.

```csharp
public interface ISerialPortService
{
    Task<IReadOnlyList<DiscoveredPort>> DiscoverPortsAsync(CancellationToken ct = default);
    Task<bool> TestPortAsync(string portName, SerialPortProfile profile, CancellationToken ct = default);
    Task OpenAsync(string portName, SerialPortProfile profile, CancellationToken ct = default);
    Task CloseAsync(string portName, CancellationToken ct = default);
    Task<bool> IsOpenAsync(string portName, CancellationToken ct = default);
    Task ConfigurePortAsync(string portName, SerialPortProfile profile, CancellationToken ct = default);

    event EventHandler<SerialPortDiscoveredEventArgs>? PortDiscovered;
    event EventHandler<SerialPortRemovedEventArgs>?   PortRemoved;
}
```

### 5.2 `ISocatService`

**Responsibility**: Manages `socat` process lifecycle for serial-to-TCP bridging.

```csharp
public interface ISocatService
{
    Task<SocatInstance> StartAsync(SocatProfile profile, CancellationToken ct = default);
    Task StopAsync(int profileId, CancellationToken ct = default);
    Task StopAllAsync(CancellationToken ct = default);
    Task<bool> IsRunningAsync(int profileId, CancellationToken ct = default);
    Task<IReadOnlyList<SocatInstance>> GetRunningInstancesAsync(CancellationToken ct = default);
    Task<bool> IsPortInUseAsync(int port, CancellationToken ct = default);

    event EventHandler<SocatStateChangedEventArgs>? ProcessStateChanged;
}
```

### 5.3 `IPowerSupplyService`

**Responsibility**: Controls Modbus TCP power supply (connect, read state, power on/off).

```csharp
public interface IPowerSupplyService
{
    Task ConnectAsync(PowerSupplyProfile profile, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<PowerState> GetPowerStateAsync(CancellationToken ct = default);
    Task SetPowerAsync(bool on, CancellationToken ct = default);
    Task<bool> IsConnectedAsync(CancellationToken ct = default);

    event EventHandler<PowerStateChangedEventArgs>?      PowerStateChanged;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}
```

### 5.4 `IPlcClient`

**Responsibility**: Low-level PLC serial protocol communication (stager, dumper, memory read).

```csharp
public interface IPlcClient : IDisposable
{
    Task ConnectAsync(CancellationToken ct = default);
    Task<bool> EnterSubprotocolAsync(CancellationToken ct = default);
    Task InstallStagerAsync(byte[] stagerPayload, CancellationToken ct = default);
    Task InstallDumperAsync(byte[] dumperPayload, CancellationToken ct = default);
    Task<byte[]> DumpMemorySegmentAsync(uint address, int length, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
```

**IPlcClient is created per-job** via a factory delegate:
```csharp
Func<JobProfileSet, IPlcClient> clientFactory
```
It must not be registered as a singleton — each job execution gets its own instance.

---

## 6. Logging API

### 6.1 `ILogDataStore`

**Location**: `S7Tools.Infrastructure.Logging.Core.Storage.ILogDataStore`

```csharp
public interface ILogDataStore
{
    void AddEntry(LogEntry entry);
    IReadOnlyList<LogEntry> GetAll();
    IReadOnlyList<LogEntry> GetFiltered(LogLevel minLevel, string? categoryFilter = null);
    void Clear();
    int Count { get; }

    event EventHandler<LogEntryAddedEventArgs>? NewEntryAdded;
}
```

**Rules**:
- Bounded ring buffer (max configurable, default 10,000 entries)
- Thread-safe for concurrent writes from multiple `ILogger<T>` instances
- `NewEntryAdded` fires for every entry added; consumers must be resilient to bursts

### 6.2 `ITaskLogDataStore`

**Location**: `S7Tools.Infrastructure.Logging.Core.Storage.ITaskLogDataStore`

Same contract as `ILogDataStore` but scoped to a single `TaskExecution`. Created by
`ITaskLoggerFactory` and lifetime-managed by `ICentralizedTaskLogService`.

### 6.3 Structured Logging Rules

All log messages must follow these structural rules:

```csharp
// ✅ CORRECT: structured template
_logger.LogInformation("Loaded {ProfileCount} serial port profiles", profiles.Count);
_logger.LogWarning("Connection failed for port {PortName}: {ErrorCode}", port, error);
_logger.LogError(ex, "Failed to dump segment at address {Address:X8}", address);

// ❌ FORBIDDEN: string interpolation destroys structure
_logger.LogInformation($"Loaded {profiles.Count} serial port profiles");

// ❌ FORBIDDEN: concatenation
_logger.LogWarning("Connection failed for port " + port);
```

**Log Level Usage**:

| Level | When to Use |
|-------|-------------|
| `Trace` | Detailed protocol bytes, step-by-step internal loops |
| `Debug` | Diagnostic values, internal state, service initialization details |
| `Information` | Normal business operations: "started", "connected", "profile saved" |
| `Warning` | Recoverable issues: retries, fallbacks, deprecated usage |
| `Error` | Caught exception that prevents an operation from completing |
| `Critical` | Application cannot continue; restart required |

---

## 7. UI Services API

UI services are registered in `S7Tools` and provide the bridge between services and the UI layer:

### 7.1 `IUIThreadService`

```csharp
public interface IUIThreadService
{
    void PostToUIThread(Action action);
    Task InvokeAsync(Action action);
    bool CheckAccess();
}
```

**Rule**: Every event handler in a ViewModel that updates reactive properties must run on the UI
thread. When an event is raised from a background service, use `_uiThreadService.PostToUIThread()`.

### 7.2 `IDialogService`

```csharp
public interface IDialogService
{
    Task<TResult?> ShowDialogAsync<TDialog, TResult>(object? parameter = null)
        where TDialog : class;
    Task ShowMessageAsync(string title, string message, MessageType type = MessageType.Info);
    Task<bool> ShowConfirmAsync(string title, string message);
}
```

### 7.3 `IPathService`

```csharp
public interface IPathService
{
    string BaseDirectory    { get; }
    string ResourcesDirectory { get; }
    string LogsDirectory    { get; }
    string ProfilesDirectory { get; }
    string ExportDirectory  { get; }

    string ResolvePath(string relativePath);
    string EnsureDirectory(string path);
}
```

**Rule**: All file system paths throughout the application must be resolved through `IPathService`.
Hard-coded absolute paths are forbidden.

---

## 8. Cross-Cutting Service Rules

### 8.1 Service Constructor Contract

Every service constructor must:
1. Accept all dependencies via constructor parameters (DI injection)
2. Guard all required parameters with `ArgumentNullException.ThrowIfNull()`
3. Initialize state synchronously (no `await` in constructors)
4. Not start background tasks in the constructor — start in `InitializeAsync()` if needed

```csharp
public sealed class SomeCriticalService : ISomeCriticalService, IDisposable
{
    private readonly ILogger<SomeCriticalService> _logger;
    private readonly IApplicationSettingsService _settings;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public SomeCriticalService(
        ILogger<SomeCriticalService> logger,
        IApplicationSettingsService settings)
    {
        _logger  = logger   ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger.LogInformation("{Service} initialized", nameof(SomeCriticalService));
    }
}
```

### 8.2 Async Method Contract

Every public async method must:
1. Accept `CancellationToken ct = default` as the last parameter
2. Pass the token to all awaited operations
3. Use `ConfigureAwait(false)` on all awaits inside service code (non-UI context)
4. Not catch `OperationCanceledException` unless specific cleanup is needed

```csharp
public async Task<Result> DoWorkAsync(Payload input, CancellationToken ct = default)
{
    await _lock.WaitAsync(ct).ConfigureAwait(false);
    try
    {
        ct.ThrowIfCancellationRequested();
        var data = await _repository.LoadAsync(ct).ConfigureAwait(false);
        return ProcessData(data, input);
    }
    finally
    {
        _lock.Release();
    }
}
```

### 8.3 Disposal Contract

Every service holding unmanaged resources (`SemaphoreSlim`, `CancellationTokenSource`, `Channel<T>`,
file streams) must implement `IDisposable` and/or `IAsyncDisposable`:

```csharp
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
    _cts.Cancel();
    _cts.Dispose();
    _lock.Dispose();
    GC.SuppressFinalize(this);
}
```

Services that were created outside of DI (e.g., `TaskLogger` instances) must be explicitly disposed
by their owner factory after the task completes.
