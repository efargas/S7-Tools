---
title: "Configuration & Logging System — System Blueprint"
version: "1.0.0"
created: "2026-03-23"
last-updated: "2026-03-23"
status: "current"
tags: ["blueprint", "canon", "configuration", "logging", "appsettings", "prescriptive"]
related:
  - docs/canon/ARCHITECTURE.md
  - docs/canon/CORE_API_CONTRACT.md
  - docs/canon/DATA_FLOW.md
---

# Configuration & Logging System — System Blueprint

> **Canon Status**: This document prescribes the complete configuration and logging architecture
> for S7Tools. It defines loading strategy, mutation contract, schema, logging pipeline, and all
> rules for appsettings and log output. All code must conform to this specification.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Configuration Architecture](#2-configuration-architecture)
3. [AppSettings Schema (Full)](#3-appsettings-schema-full)
4. [Configuration Loading Pipeline](#4-configuration-loading-pipeline)
5. [Runtime Settings Mutation Pipeline](#5-runtime-settings-mutation-pipeline)
6. [Configuration Anti-Patterns Audit](#6-configuration-anti-patterns-audit)
7. [Logging Architecture](#7-logging-architecture)
8. [Log Pipeline Diagram](#8-log-pipeline-diagram)
9. [Per-Task Logging System](#9-per-task-logging-system)
10. [Log Configuration Schema](#10-log-configuration-schema)
11. [Logging Anti-Patterns Audit](#11-logging-anti-patterns-audit)

---

## 1. Overview

S7Tools uses a two-tier configuration system:

1. **Compile-time defaults** (`AppSettings` property initializers in C#)
2. **Runtime overrides** (`appsettings.json` + `UserSettings.json`, loaded at startup)

The logging system is built on **Microsoft.Extensions.Logging** with two custom providers:
- `DataStoreLoggerProvider` — in-memory ring buffer for UI real-time log viewer
- `UnifiedLoggerProvider` — delegates to `ILogSink[]` (file sinks, task sinks)

---

## 2. Configuration Architecture

### 2.1 File Hierarchy

```
appsettings.json          (APPLICATION defaults — READ-ONLY, version-controlled)
    ↕ (later sources override earlier)
UserSettings.json         (USER overrides — WRITABLE, excluded from version control)
    ↕ (merged at startup by IConfiguration)
AppSettings (C# object)   (in-memory representation — bound via IOptions<AppSettings>)
    ↕ (runtime access via)
IApplicationSettingsService.Current
```

### 2.2 File Locations

| File | Path (relative to `AppDomain.CurrentDomain.BaseDirectory`) | Writable? |
|------|------------------------------------------------------------|-----------|
| `appsettings.json` | `appsettings.json` | No |
| `UserSettings.json` | `Resources/AppSettings/UserSettings.json` | Yes |

### 2.3 JSON Structure

Both files use the same JSON structure with root key `"App"`:

```json
{
  "App": {
    "Logging": { ... },
    "Ui": { ... },
    "Paths": { ... },
    "Profiles": { ... },
    "Serial": { ... },
    "Network": { ... },
    "Socat": { ... },
    "PowerSupply": { ... },
    "Plc": { ... },
    "Jobs": { ... },
    "Tasks": { ... },
    "MemoryRegion": { ... },
    "Export": { ... },
    "MemoryDump": { ... }
  }
}
```

### 2.4 The WritableOptions Pattern

`WritableOptions<T>` is the canonical mechanism for persisting runtime setting changes:

```
WritableOptions<AppSettings>
├── Holds reference to IOptionsMonitor<AppSettings> (live reactive value)
├── Holds path to UserSettings.json
├── Protected by SemaphoreSlim (thread-safe writes)
└── Atomic write: serialize → temp file → Rename(temp, target)
```

**Invariant**: `WritableOptions<T>` must never partially write a settings file.
If serialization or file write fails, the original file is left intact.

---

## 3. AppSettings Schema (Full)

### `App.Logging`

| Property | Type | Default | Validation | Description |
|----------|------|---------|-----------|-------------|
| `Level` | `string` | `"Information"` | `[Required]` | Minimum log level. Accepted: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` |
| `EnableFileLogging` | `bool` | `true` | — | Enables rolling file logger |
| `MaxFileSize` | `long` | `10485760` (10 MB) | `[Range(1024, 1073741824)]` | Max bytes per log file before rotation |
| `MaxFiles` | `int` | `5` | `[Range(1, 100)]` | Max number of rotated files to retain |
| `LogDirectory` | `string` | `"Resources/Logs/Main"` | `[Required]` | Directory for app log files |
| `ExportDirectory` | `string` | `"Resources/Logs/Exported"` | `[Required]` | Directory for exported log files |

### `App.Ui`

| Property | Type | Default | Validation | Description |
|----------|------|---------|-----------|-------------|
| `Theme` | `string` | `"System"` | `[Required]` | UI theme: `Light`, `Dark`, `System` |
| `StartMinimized` | `bool` | `false` | — | Start window minimized |
| `AutoRefreshInterval` | `int` | `2000` | `[Range(100, 60000)]` | UI data refresh interval in ms |
| `AutoScrollLogs` | `bool` | `true` | — | Auto-scroll log viewer |
| `ShowTimestampInLogs` | `bool` | `true` | — | Show timestamp column in log viewer |
| `ShowCategoryInLogs` | `bool` | `true` | — | Show category column in log viewer |
| `ShowLogLevelInLogs` | `bool` | `true` | — | Show log level column in log viewer |

### `App.Serial`

| Property | Type | Default | Validation | Description |
|----------|------|---------|-----------|-------------|
| `DefaultBaudRate` | `int` | `9600` | `[Range(300, 4000000)]` | Default baud rate |
| `DefaultDataBits` | `int` | `8` | `[Range(5, 8)]` | Default data bits |
| `DefaultParity` | `string` | `"None"` | — | Default parity: `None`, `Even`, `Odd` |
| `DefaultStopBits` | `string` | `"One"` | — | Default stop bits: `One`, `Two` |
| `IncludeUsbPorts` | `bool` | `true` | — | Include `/dev/ttyUSB*` in discovery |
| `IncludeAcmPorts` | `bool` | `true` | — | Include `/dev/ttyACM*` in discovery |
| `IncludeStandardPorts` | `bool` | `true` | — | Include `/dev/ttyS*` in discovery |
| `MaxScanPorts` | `int` | `32` | `[Range(1, 256)]` | Max ports to scan |
| `ScanIntervalSeconds` | `int` | `5` | `[Range(1, 60)]` | Interval between discovery scans |
| `PortTestTimeoutMs` | `int` | `1000` | `[Range(100, 30000)]` | Timeout for testing a single port |

### `App.Network`

| Property | Type | Default | Validation | Description |
|----------|------|---------|-----------|-------------|
| `DefaultSocatPort` | `int` | `2023` | `[Range(1, 65535)]` | Default socat TCP bridge port |
| `PowerSupplyPort` | `int` | `502` | `[Range(1, 65535)]` | Default Modbus TCP port |
| `ConnectionRetries` | `int` | `3` | `[Range(0, 10)]` | TCP connection retry count |

### `App.Socat`

| Property | Type | Default | Validation | Description |
|----------|------|---------|-----------|-------------|
| `MaxConcurrentInstances` | `int` | `5` | `[Range(1, 100)]` | Max simultaneous socat processes |
| `AutoConfigureSerialDevice` | `bool` | `true` | — | Configure serial device on socat start |
| `ProcessShutdownTimeoutSeconds` | `int` | `5` | `[Range(1, 60)]` | Graceful shutdown timeout |
| `StatusRefreshIntervalSeconds` | `int` | `2` | `[Range(1, 60)]` | Status poll interval |
| `CaptureProcessOutput` | `bool` | `true` | — | Capture socat stdout/stderr |

### `App.PowerSupply`

| Property | Type | Default | Validation | Description |
|----------|------|---------|-----------|-------------|
| `MaxProfiles` | `int` | `100` | `[Range(1, 1000)]` | Max stored profiles |
| `AutoLoadDefaultProfile` | `bool` | `true` | — | Load default profile on startup |
| `AutoSaveProfiles` | `bool` | `true` | — | Save profiles after each change |
| `DefaultConnectionTimeoutMs` | `int` | `5000` | `[Range(100, 60000)]` | Modbus connect timeout |
| `EnableConnectionPooling` | `bool` | `true` | — | Enable Modbus connection pooling |
| `EnableAutoReconnect` | `bool` | `true` | — | Auto-reconnect on connection loss |
| `ReconnectDelayMs` | `int` | `2000` | `[Range(100, 60000)]` | Delay between reconnect attempts |
| `MaxReconnectAttempts` | `int` | `5` | `[Range(0, 20)]` | Max reconnect attempts |
| `ConfirmPowerOff` | `bool` | `true` | — | Confirm dialog before power off |
| `ConfirmPowerOn` | `bool` | `false` | — | Confirm dialog before power on |
| `PowerStateChangeDelayMs` | `int` | `1000` | `[Range(100, 10000)]` | Delay after power state change |
| `AutoReadStateAfterConnect` | `bool` | `true` | — | Read power state on connect |
| `StatusRefreshIntervalMs` | `int` | `5000` | `[Range(100, 60000)]` | Status poll interval |
| `ShowPowerStateNotifications` | `bool` | `true` | — | Show UI notification on power change |
| `ShowConnectionNotifications` | `bool` | `true` | — | Show UI notification on connect |
| `LogModbusOperations` | `bool` | `false` | — | Log individual Modbus operations |
| `LogConnectionStateChanges` | `bool` | `true` | — | Log connection transitions |
| `LogPowerStateChanges` | `bool` | `true` | — | Log power state transitions |

### `App.MemoryDump`

| Property | Type | Default | Validation | Description |
|----------|------|---------|-----------|-------------|
| `DefaultFolder` | `string` | `""` | — | Default output folder for dump files |
| `SegmentDumpDelayMilliseconds` | `int` | `5000` | `[Range(0, 60000)]` | Delay between segment dumps |
| `IterationDumpDelayMilliseconds` | `int` | `5000` | `[Range(0, 60000)]` | Delay between dump iterations |

---

## 4. Configuration Loading Pipeline

```
Program.ConfigureServices()
         │
         ▼
new ConfigurationBuilder()
  .AddJsonFile("appsettings.json",       optional: true, reloadOnChange: true)
  .AddJsonFile("UserSettings.json",      optional: true, reloadOnChange: true)
  .Build()
         │ IConfiguration
         ▼
services.AddSingleton<IConfiguration>(configuration)
         │
         ▼
var appSection = configuration.GetSection("App")
         │
         ▼
services.AddOptions<AppSettings>()
  .Bind(appSection)
  .ValidateDataAnnotations()  ← runs [Range], [Required] validators
  .ValidateOnStart()          ← fails application startup if invalid
         │
         ▼
services.AddTransient<IWritableOptions<AppSettings>>(provider =>
    new WritableOptions<AppSettings>(
        basePath: AppDomain.CurrentDomain.BaseDirectory,
        optionsMonitor: provider.GetRequired<IOptionsMonitor<AppSettings>>(),
        section: "App",
        file: "Resources/AppSettings/UserSettings.json"))
         │
         ▼
ApplicationSettingsService registered as IApplicationSettingsService
  (wraps IWritableOptions<AppSettings>)
         │
         ▼
All services inject IApplicationSettingsService
  Access settings via: _settings.Current.Xxx
```

### Startup Validation

If `appsettings.json` or `UserSettings.json` contain invalid values (e.g., `SegmentDumpDelayMilliseconds: -1`),
`ValidateOnStart()` causes the DI container to throw `OptionsValidationException` at startup,
**before any services are created**. This is intentional — fail fast, not silently.

---

## 5. Runtime Settings Mutation Pipeline

```
User changes setting (e.g., log level) in SettingsPage
         │
         ▼
SettingsViewModel.SaveCommand.Execute()
         │ calls
         ▼
IApplicationSettingsService.UpdateSettingsAsync(s =>
{
    s.Logging.Level = "Debug";
    s.Ui.AutoScrollLogs = false;
})
         │
         ▼
IWritableOptions<AppSettings>.UpdateAsync(action)
  1. await _semaphore.WaitAsync()           ← thread-safe write lock
  2. Read UserSettings.json into JsonNode   ← preserve existing values
  3. Get or create "App" section node
  4. action(currentSettings)               ← apply mutation
  5. Serialize updated "App" to JsonNode
  6. File.WriteAllText(tempPath, json)      ← atomic: write to temp
  7. File.Move(tempPath, targetPath)        ← atomic: rename to final
  8. _semaphore.Release()
         │
         ▼
IOptionsMonitor<AppSettings> detects file change (reloadOnChange: true)
→ Reloads and re-binds AppSettings from updated UserSettings.json
         │
         ▼
IApplicationSettingsService raises SettingsChanged event on UI thread
         │
         ▼
Subscribed ViewModels call RefreshFromSettings(_settings.Current)
         │
         ▼
UI re-renders affected controls
```

---

## 6. Configuration Anti-Patterns Audit

The following patterns are **prohibited** and must be eliminated wherever found:

### AP-C1: Raw `IConfiguration` access in services

```csharp
// ❌ FOUND PATTERN — MUST NOT EXIST IN SERVICES
var level = _configuration["App:Logging:Level"];

// ✅ CORRECT
var level = _settings.Current.Logging.Level;
```

### AP-C2: Hard-coded path strings

```csharp
// ❌ FORBIDDEN
var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "app.log");

// ✅ CORRECT
var logPath = _pathService.ResolvePath(_settings.Current.Logging.LogDirectory);
```

### AP-C3: Settings section key strings scattered in code

```csharp
// ❌ FORBIDDEN — magic strings, no compile-time safety
_config.GetSection("App:Logging:Level").Value

// ✅ CORRECT — strongly typed
_settings.Current.Logging.Level
```

### AP-C4: Storing `Current` snapshot across awaits

```csharp
// ❌ FORBIDDEN — settings may change during await
var s = _settings.Current;
await LongRunningOperationAsync();
var level = s.Logging.Level;  // stale!

// ✅ CORRECT
await LongRunningOperationAsync();
var level = _settings.Current.Logging.Level;  // fresh
```

### AP-C5: Mutating the live `AppSettings` object

```csharp
// ❌ FORBIDDEN — mutation is not persisted, not notified
_settings.Current.Logging.Level = "Debug";

// ✅ CORRECT
await _settings.UpdateSettingsAsync(s => { s.Logging.Level = "Debug"; });
```

---

## 7. Logging Architecture

### 7.1 Provider Stack

The logging system registers two custom `ILoggerProvider` implementations:

```
Microsoft.Extensions.Logging (ILoggerFactory)
         │
         ├── DataStoreLoggerProvider   (always registered)
         │       └── LogDataStore (ILogDataStore)
         │           └── ConcurrentQueue<LogEntry> (max 10,000, ring buffer)
         │
         └── UnifiedLoggerProvider    (registered when file logging enabled)
                 └── ILogSink[]
                     └── FileLogSink  (singleton, bounded Channel<LogEntry>)
                         └── StreamWriter (per file path, kept open)
```

### 7.2 LogEntry Model

```csharp
public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }   // UTC
    public LogLevel       LogLevel  { get; init; }
    public string         Category  { get; init; }   // from ILogger<T> generic param
    public string         Message   { get; init; }   // formatted message
    public Exception?     Exception { get; init; }
    public string?        Scope     { get; init; }   // serialized scope state
    public IReadOnlyDictionary<string, object?>? Properties { get; init; } // structured props
}
```

### 7.3 Category Routing in FileLogSink

`CombinedFileLoggerConfiguration.GetFilePathForCategory(categoryName)` routes log entries:

```
Category starts with "Task.*":
  → Last segment == "Process"   → task-process.log
  → Last segment == "Protocol"  → task-protocol.log
  → Any other Task.*            → task-main.log

Any other category:
  → DefaultLogPath (app.log)
```

### 7.4 Log Level Integration with AppSettings

The log level defined in `AppSettings.Logging.Level` is used to configure the minimum log level
at runtime. When `SettingsChanged` fires with a new `Level` value, the logger provider's minimum
level is updated without restarting the application.

---

## 8. Log Pipeline Diagram

```
Service code:
  _logger.LogInformation("Socat started on port {Port}", port)
         │
         ▼
ILogger<SocatService> (MEL framework)
  applies category: "S7Tools.Services.Socat.SocatService"
  applies minimum level filter
         │
         ├──────────────────────────────────────────────────┐
         ▼                                                  ▼
DataStoreLogger.Log(...)                     UnifiedLogger.Log(...)
  - Creates LogEntry { Timestamp, Level,       - Iterates ILogSink[]
    Category, Message, Properties }            - Calls sink.Write(entry)
  - Enqueues in LogDataStore                         │
  - Fires NewEntryAdded event                        ▼
         │                                    FileLogSink.Write(entry)
         ▼                                      - Writes to Channel<LogEntry>
LogViewerViewModel                              - Background task reads channel
  - Subscribes to NewEntryAdded                 - Routes by category → file path
  - Projects LogEntry → display model           - StreamWriter.WriteLineAsync(formatted)
  - Applies filter (level, category, text)      - Flushes every 1 second
  - Updates ObservableCollection on UI thread
         │
         ▼
LogViewer DataGrid renders new rows
```

---

## 9. Per-Task Logging System

### 9.1 Isolation Principle

Each job execution gets its own isolated log stores and file outputs.
Task logs are completely separate from application logs.

### 9.2 Creation Pipeline

```
IJobScheduler dispatches job
         │
         ▼
ITaskLoggerFactory.CreateTaskLoggerAsync(taskId, taskName)
  1. Resolves task log directory:
     {LogsDirectory}/Tasks/{SanitizedName}_{yyyyMMdd_HHmmss}_{taskId[..8]}/
  2. Creates two DataStoreLoggerProvider instances:
     - mainProvider    → new LogDataStore(maxEntries=10_000)
     - processProvider → new LogDataStore(maxEntries=10_000)
  3. Registers stores in ICentralizedTaskLogService.GetOrCreateStoresForTask(taskId)
  4. Creates file loggers:
     - mainFileLogger    → task-main.log
     - processFileLogger → task-process.log
  5. Returns TaskLogger:
     - .Main    = CompositeLogger(mainProvider + mainFileLogger)
     - .Process = CompositeLogger(processProvider + processFileLogger)
```

### 9.3 Usage in Pipeline

```csharp
// In BootloaderService:
effectiveTaskLogger.LogInformation("--- Stage 9: Bootloader Handshake ---");
//  ↑ goes to: in-memory TaskMainDataStore + Tasks/{name}/task-main.log

processLogger?.LogDebug("socat[PID 1234]: Connected on port 2023");
//  ↑ goes to: in-memory TaskProcessDataStore + Tasks/{name}/task-process.log
```

### 9.4 Viewing Task Logs in UI

```
TaskLogViewerViewModel
  - Injects ICentralizedTaskLogService
  - Gets (mainStore, processStore) by TaskId
  - Subscribes to mainStore.NewEntryAdded and processStore.NewEntryAdded
  - Displays entries in tabbed log view (Main tab, Process tab)
```

### 9.5 Log File Name Sanitization

Task names used as directory components must be sanitized:

```csharp
private static string SanitizeFileName(string name)
{
    // Remove OS-illegal characters
    var invalid = Path.GetInvalidFileNameChars();
    var sb = new StringBuilder(name.Length);
    foreach (char c in name)
        sb.Append(invalid.Contains(c) ? '_' : c);
    // Limit length to 50 characters
    return sb.ToString()[..Math.Min(sb.Length, 50)];
}
```

---

## 10. Log Configuration Schema

### `DataStoreLoggerConfiguration` (in-memory provider)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LogLevel` | `LogLevel` | `Information` | Minimum log level to capture |
| `EventId` | `int?` | `null` | Filter by event ID (null = all) |
| `IncludeScopes` | `bool` | `true` | Include scope chain in entries |
| `CaptureProperties` | `bool` | `true` | Capture structured log properties |
| `CategoryFilter` | `string?` | `null` | Wildcard pattern filter (e.g., `"S7Tools.*"`) |
| `FormatMessages` | `bool` | `true` | Format message template on capture |
| `MaxMessageLength` | `int?` | `10000` | Truncate messages over this length |
| `CaptureStackTrace` | `bool` | `true` | Capture full stack traces for exceptions |

### `CombinedFileLoggerConfiguration` (file sink)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LogLevel` | `LogLevel` | `Information` | Minimum level for file output |
| `DefaultLogPath` | `string` | `"Logs/app.log"` | Path for all non-task categories |
| `TaskMainLogPath` | `string` | `"Logs/Main/main.log"` | Task main log path |
| `TaskProcessLogPath` | `string` | `"Logs/Process/process.log"` | Task process log path |
| `TaskProtocolLogPath` | `string` | `"Logs/Protocol/protocol.log"` | Task protocol log path |

### `LogDataStoreOptions` (in-memory ring buffer)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxEntries` | `int` | `10000` | Maximum log entries before eviction (ring) |

---

## 11. Logging Anti-Patterns Audit

### AP-L1: String interpolation in log messages

```csharp
// ❌ FORBIDDEN — destroys structured log properties
_logger.LogInformation($"Connected to {port} at {baudRate} baud");

// ✅ CORRECT — structured template, queryable properties
_logger.LogInformation("Connected to {Port} at {BaudRate} baud", port, baudRate);
```

### AP-L2: `Console.WriteLine` outside of `--diag` path

```csharp
// ❌ FORBIDDEN in service code
Console.WriteLine($"Profile loaded: {profile.Name}");

// ✅ CORRECT
_logger.LogInformation("Profile loaded: {ProfileName}", profile.Name);

// ⚠️ EXCEPTION: Permitted in Program.cs within --diag block only
Console.WriteLine($"[S7Tools] Loaded {count} profiles"); // Keep console for --diag flag
```

### AP-L3: Catching and swallowing exceptions without logging

```csharp
// ❌ FORBIDDEN
try { await LoadAsync(); }
catch { } // silent failure

// ✅ CORRECT
try { await LoadAsync(); }
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to load profiles from {Path}", filePath);
    throw; // or handle and log
}
```

### AP-L4: Logging inside a semaphore with blocking calls

```csharp
// ❌ RISKY — logging should not block the semaphore acquisition path
await _semaphore.WaitAsync();
try
{
    _logger.LogInformation("Acquired lock");  // ✅ this is fine
    // ... work ...
}
finally
{
    _semaphore.Release();
    _logger.LogInformation("Released lock"); // ✅ fine (after release)
}
```

### AP-L5: Creating `ILogger` instances manually (outside DI)

```csharp
// ❌ FORBIDDEN
var logger = new Logger<MyService>(new LoggerFactory());

// ✅ CORRECT — injected by DI
public MyService(ILogger<MyService> logger) { _logger = logger; }
```

### AP-L6: Logging at wrong level

```csharp
// ❌ WRONG LEVEL — routine information logged at Debug
_logger.LogDebug("Job {JobId} completed successfully", jobId);

// ✅ CORRECT — business operation at Information
_logger.LogInformation("Job {JobId} completed successfully", jobId);

// ❌ WRONG LEVEL — expected path logged as Warning
_logger.LogWarning("User cancelled the operation"); // ← this is normal/expected

// ✅ CORRECT
_logger.LogInformation("Operation cancelled by user");
```

---

## Appendix: Recommended `appsettings.json` Template

The following `appsettings.json` should be present at the application base directory and
version-controlled. It defines safe production defaults. User overrides go in `UserSettings.json`.

```json
{
  "App": {
    "Logging": {
      "Level": "Information",
      "EnableFileLogging": true,
      "MaxFileSize": 10485760,
      "MaxFiles": 5,
      "LogDirectory": "Resources/Logs/Main",
      "ExportDirectory": "Resources/Logs/Exported"
    },
    "Ui": {
      "Theme": "System",
      "StartMinimized": false,
      "AutoRefreshInterval": 2000,
      "AutoScrollLogs": true,
      "ShowTimestampInLogs": true,
      "ShowCategoryInLogs": true,
      "ShowLogLevelInLogs": true
    },
    "Serial": {
      "DefaultBaudRate": 9600,
      "DefaultDataBits": 8,
      "DefaultParity": "None",
      "DefaultStopBits": "One",
      "IncludeUsbPorts": true,
      "IncludeAcmPorts": true,
      "IncludeStandardPorts": true,
      "MaxScanPorts": 32,
      "ScanIntervalSeconds": 5,
      "PortTestTimeoutMs": 1000
    },
    "Network": {
      "DefaultSocatPort": 2023,
      "PowerSupplyPort": 502,
      "ConnectionRetries": 3
    },
    "Socat": {
      "MaxConcurrentInstances": 5,
      "AutoConfigureSerialDevice": true,
      "ProcessShutdownTimeoutSeconds": 5,
      "StatusRefreshIntervalSeconds": 2,
      "CaptureProcessOutput": true
    },
    "PowerSupply": {
      "MaxProfiles": 100,
      "AutoLoadDefaultProfile": true,
      "AutoSaveProfiles": true,
      "DefaultConnectionTimeoutMs": 5000,
      "EnableConnectionPooling": true,
      "EnableAutoReconnect": true,
      "ReconnectDelayMs": 2000,
      "MaxReconnectAttempts": 5,
      "ConfirmPowerOff": true,
      "ConfirmPowerOn": false,
      "PowerStateChangeDelayMs": 1000,
      "AutoReadStateAfterConnect": true,
      "StatusRefreshIntervalMs": 5000,
      "ShowPowerStateNotifications": true,
      "ShowConnectionNotifications": true,
      "LogModbusOperations": false,
      "LogConnectionStateChanges": true,
      "LogPowerStateChanges": true
    },
    "MemoryDump": {
      "DefaultFolder": "",
      "SegmentDumpDelayMilliseconds": 5000,
      "IterationDumpDelayMilliseconds": 5000
    }
  }
}
```
