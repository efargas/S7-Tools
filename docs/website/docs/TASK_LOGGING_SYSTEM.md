---
title: "Task-Specific Logging System"
version: "1.0.0"
created: "2025-11-13"
last-updated: "2025-11-13"
status: "current"
tags: ["logging", "architecture", "task-management", "monitoring"]
related:
  - docs/architecture/overview.md
  - docs/patterns/system-patterns.md
  - docs/guides/development-workflow.md
---

# Task-Specific Logging System

## Overview

S7Tools now includes a comprehensive task-specific logging system that captures all activity related to individual task executions. Each task gets its own dedicated logging infrastructure with multiple log channels and both in-memory and file-based storage.

## Architecture

### Components

1. **TaskLogger** (`S7Tools.Core/Models/Jobs/TaskLogger.cs`)
   - Metadata container for task-specific logging
   - Tracks logger instances, file paths, and statistics
   - Attached to each `TaskExecution` instance

2. **TaskLoggerFactory** (`S7Tools/Services/Logging/TaskLoggerFactory.cs`)
   - Creates and manages task-specific loggers
   - Provides three logging channels per task:
     - **Main Logger**: General task operations
     - **Protocol Logger**: TCP/socat communication
     - **Process Logger**: Socat stdout/stderr output

3. **Integration with EnhancedTaskScheduler**
   - Automatically creates logger when task starts
   - Logs all task lifecycle events
   - Finalizes and saves logs when task completes

## Features

### Multiple Log Channels

Each task execution has **three separate log channels**:

| Channel | Purpose | Log Level | Max Entries |
|---------|---------|-----------|-------------|
| **Main** | General task operations, progress, errors | Debug | 10,000 |
| **Protocol** | TCP/socat communication details | Trace | 50,000 |
| **Process** | Socat process stdout/stderr | Debug | 20,000 |

### Dual Storage

Each log channel has **two storage targets**:

1. **In-Memory DataStore**
   - Circular buffer for real-time UI display
   - Fast access for monitoring
   - Thread-safe concurrent access
   - Configured max entries per channel

2. **File Storage**
   - Persistent log files on disk
   - Automatic directory creation
   - Organized by task name and timestamp

### Log Organization

Logs are organized in directories per task:

```
Logs/
└── Tasks/
    └── {TaskName}_{Timestamp}_{TaskId}/
        ├── main.log        # General operations
        ├── protocol.log    # TCP/socat communication
        └── process.log     # Socat stdout/stderr
```

Example:
```
Logs/Tasks/MemoryDump_20251113_143022_a3b5c7d9/
├── main.log
├── protocol.log
└── process.log
```

## Usage

### Automatic Integration

The system is **automatically integrated** into task execution:

1. **Task Start**:
   - `EnhancedTaskScheduler` creates logger via `TaskLoggerFactory`
   - Logger attached to `TaskExecution.Logger`
   - All three channels initialized

2. **During Execution**:
   - Main logger captures task lifecycle events
   - Protocol logger ready for TCP/socat communication
   - Process logger ready for socat output

3. **Task Completion**:
   - Logger finalized automatically
   - All logs flushed to files
   - Statistics calculated (total entries, file size)

### Accessing Task Logs

```csharp
// Get task execution
TaskExecution task = await taskScheduler.GetTaskAsync(taskId);

// Access logger metadata
TaskLogger? logger = task.Logger;
if (logger != null)
{
    Console.WriteLine($"Main log: {logger.MainLogFilePath}");
    Console.WriteLine($"Protocol log: {logger.ProtocolLogFilePath}");
    Console.WriteLine($"Process log: {logger.ProcessLogFilePath}");
    Console.WriteLine($"Total entries: {logger.TotalLogEntries}");
    Console.WriteLine($"Total size: {logger.TotalLogFilesSize} bytes");
}
```

### Accessing In-Memory Logs

```csharp
// Get in-memory DataStore for real-time UI
ILogDataStore? mainStore = (ILogDataStore?)taskLoggerFactory.GetTaskDataStore(
    taskId,
    TaskLogType.Main);

if (mainStore != null)
{
    var recentLogs = mainStore.Entries.TakeLast(100);
    foreach (var log in recentLogs)
    {
        Console.WriteLine($"[{log.Timestamp}] {log.Level}: {log.Message}");
    }
}
```

## Configuration

### TaskLoggerFactory Settings

```csharp
// Default settings (can be customized)
- Main Logger: Debug level, 10,000 entries
- Protocol Logger: Trace level, 50,000 entries
- Process Logger: Debug level, 20,000 entries
```

### Enable/Disable Log Channels

```csharp
// Create logger with optional process output capture
var logger = await taskLoggerFactory.CreateTaskLoggerAsync(
    taskId,
    taskName,
    captureProcessOutput: true,   // Enable process logging (default: true)
    cancellationToken);
```

## Log Content Examples

### Main Log

```
[2025-11-13 14:30:22.123] [Information] Task execution started: MemoryDump (ID: a3b5c7d9) at 2025-11-13 14:30:22
[2025-11-13 14:30:22.456] [Information] Job profile loaded: S7-1200 Full Dump
[2025-11-13 14:30:22.789] [Information] Starting bootloader execution
[2025-11-13 14:30:23.012] [Information] Progress: Socat Setup - 10.0%
[2025-11-13 14:30:24.345] [Information] Progress: Power Cycle - 20.0%
[2025-11-13 14:30:26.678] [Information] Progress: Handshake - 30.0%
[2025-11-13 14:30:30.901] [Information] Progress: Memory Dump - 80.0%
[2025-11-13 14:30:32.234] [Information] Bootloader dump completed. Size: 65536 bytes
[2025-11-13 14:30:32.567] [Information] Output saved to: /path/to/dump-a3b5c7d9.bin
[2025-11-13 14:30:32.890] [Information] Task completed successfully. Execution time: 00:00:10.5
```

### Protocol Log (Future Enhancement)

When socat/TCP communication is instrumented:

```
[2025-11-13 14:30:23.456] [Trace] TCP connection established: 192.168.1.100:102
[2025-11-13 14:30:23.789] [Trace] TX: 03 00 00 16 11 E0 00 00 00 01 00 C1 02 10 00 C2 02 03 00 C0 01 0A
[2025-11-13 14:30:23.912] [Trace] RX: 03 00 00 1B 02 F0 80 32 03 00 00 00 01 00 08 00 00 F0 00 00 01 00 01 03 C0
[2025-11-13 14:30:24.045] [Trace] Handshake acknowledged
```

### Process Log (Future Enhancement)

When socat stdout/stderr is captured:

```
[2025-11-13 14:30:23.234] [Debug] socat[12345] I socat Version 1.7.4.0
[2025-11-13 14:30:23.567] [Debug] socat[12345] I socket(1, 2, 0) -> 3
[2025-11-13 14:30:23.890] [Debug] socat[12345] I starting data transfer loop with FDs [3,3] and [4,4]
[2025-11-13 14:30:32.123] [Debug] socat[12345] I transferred 65536 bytes from 3 to 4
```

## Next Steps: Socat Output Capture

To complete the process logging, integrate socat stdout/stderr capture:

### 1. Modify SocatService.StartSocatAsync

```csharp
// In SocatService.cs
public async Task<SocatProcessInfo> StartSocatAsync(
    SocatConfiguration configuration,
    string serialDevice,
    ILogger? processLogger = null,  // NEW: Optional process logger
    CancellationToken cancellationToken = default)
{
    // ... existing code ...

    // Attach process logger to stdout/stderr handlers
    process.OutputDataReceived += (_, e) =>
    {
        if (e.Data != null)
        {
            outputBuilder!.AppendLine(e.Data);
            _logger.LogTrace("Socat output: {Output}", e.Data);
            processLogger?.LogDebug("socat[{ProcessId}] {Output}", process.Id, e.Data);  // NEW
        }
    };

    process.ErrorDataReceived += (_, e) =>
    {
        if (e.Data != null)
        {
            errorBuilder!.AppendLine(e.Data);
            _logger.LogWarning("Socat error: {Error}", e.Data);
            processLogger?.LogWarning("socat[{ProcessId}] ERROR: {Error}", process.Id, e.Data);  // NEW
        }
    };

    // ... rest of code ...
}
```

### 2. Pass Process Logger from Bootloader

```csharp
// In BootloaderService.DumpAsync
var socatInfo = await _socat.StartSocatAsync(
    profiles.Socat,
    profiles.Serial.Device,
    taskLogger?.ProcessLogger,  // Pass process logger
    cancellationToken);
```

### 3. Protocol Logging (TCP Communication)

For protocol-level logging, instrument the PLC client:

```csharp
// In PlcClient or transport layer
public async Task<byte[]> SendCommandAsync(byte[] command, ILogger? protocolLogger = null)
{
    protocolLogger?.LogTrace("TX: {Hex}", BitConverter.ToString(command));

    var response = await _transport.SendAsync(command);

    protocolLogger?.LogTrace("RX: {Hex}", BitConverter.ToString(response));

    return response;
}
```

## Benefits

### For Users

- **Complete Task Audit Trail**: Every task execution fully logged
- **Troubleshooting**: Detailed logs help diagnose failures
- **Real-Time Monitoring**: In-memory logs for live UI updates
- **Historical Analysis**: Persistent files for post-mortem

### For Developers

- **Debugging**: Detailed protocol and process logs
- **Testing**: Easy verification of task behavior
- **Metrics**: Track task performance and statistics
- **Compliance**: Comprehensive audit trails

### For Operations

- **Monitoring**: Real-time view of task execution
- **Alerting**: Detect failures from log patterns
- **Reporting**: Generate reports from log data
- **Forensics**: Investigate past task executions

## Implementation Status

### ✅ Completed

- [x] TaskLogger model
- [x] TaskLoggerFactory service
- [x] ITaskLoggerFactory interface
- [x] Integration with EnhancedTaskScheduler
- [x] In-memory DataStore per channel
- [x] File logging per channel
- [x] Automatic logger lifecycle management
- [x] DI registration

### 🔄 Next Steps

- [ ] Integrate process logger with SocatService
- [ ] Integrate protocol logger with PLC client
- [ ] Add UI for viewing task logs
- [ ] Add log export functionality
- [ ] Add log search and filtering
- [ ] Add log retention policies

## Testing

To test the system:

1. **Create and run a task**:
   ```bash
   # Run application
   dotnet run --project src/S7Tools --configuration Debug

   # Create job, queue task
   ```

2. **Check log directory**:
   ```bash
   # After task completes, check logs
   ls -la Logs/Tasks/
   ls -la Logs/Tasks/{TaskName}_{Timestamp}_{TaskId}/

   # View main log
   cat Logs/Tasks/{TaskName}_{Timestamp}_{TaskId}/main.log
   ```

3. **Verify in-memory logs**:
   - Access via TaskLoggerFactory.GetTaskDataStore()
   - Display in UI (future enhancement)

## Architecture Compliance

This implementation follows S7Tools architectural principles:

- ✅ **Clean Architecture**: Core models, App services, Infrastructure separation
- ✅ **Dependency Injection**: All services registered in DI
- ✅ **MVVM Ready**: In-memory DataStores support UI binding
- ✅ **Thread Safety**: Semaphore-protected operations
- ✅ **Dispose Pattern**: Proper resource cleanup
- ✅ **Logging Integration**: Uses existing LogDataStore infrastructure

## Related Documentation

- [Overview](architecture/overview.md)
- [Development Workflow](guides/development-workflow.md)
- [System Patterns](patterns/system-patterns.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
