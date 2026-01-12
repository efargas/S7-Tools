# Task-Specific Logging System

## Overview

S7Tools includes a comprehensive task-specific logging system that captures all activity related to individual task executions. Each task gets its own dedicated logging infrastructure with multiple log channels and both in-memory and file-based storage.

## Architecture

### Components

1.  **TaskLogger** (`S7Tools.Core/Models/Jobs/TaskLogger.cs`)
    *   Metadata container for task-specific logging.
    *   Tracks logger instances, file paths, and statistics.
    *   Attached to each `TaskExecution` instance.

2.  **TaskLoggerFactory** (`S7Tools/Services/Logging/TaskLoggerFactory.cs`)
    *   Creates and manages task-specific loggers.
    *   Provides three logging channels per task:
        *   **Main Logger**: General task operations.
        *   **Protocol Logger**: TCP/socat communication.
        *   **Process Logger**: Socat stdout/stderr output.
    *   Manages lifecycle, including creation and finalization (flushing/closing).

3.  **CentralizedTaskLogService** (`S7Tools/Services/CentralizedTaskLogService.cs`)
    *   Manages the lifecycle of `ITaskLogDataStore` instances.
    *   Ensures data stores are reused or created consistently for a given Task ID.

4.  **Integration with EnhancedTaskScheduler**
    *   Automatically creates logger when task starts.
    *   Logs all task lifecycle events.
    *   Finalizes and saves logs when task completes.

## Features

### Multiple Log Channels

Each task execution has **three separate log channels**:

| Channel | Purpose | Log Level | Max Entries |
| :--- | :--- | :--- | :--- |
| **Main** | General task operations, progress, errors | Debug | 10,000 |
| **Protocol** | TCP/socat communication details | Trace | 50,000 |
| **Process** | Socat process stdout/stderr | Debug | 20,000 |

### Dual Storage

Each log channel has **two storage targets**:

1.  **In-Memory DataStore** (`LogDataStore`)
    *   Circular buffer for real-time UI display.
    *   Fast access for monitoring.
    *   Thread-safe concurrent access.
    *   Configured max entries per channel.

2.  **File Storage** (`FileLogger`)
    *   Persistent log files on disk.
    *   Automatic directory creation.
    *   Organized by task name and timestamp.
    *   Uses a simple, non-blocking `StreamWriter` wrapper.

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

1.  **Task Start**:
    *   `EnhancedTaskScheduler` creates logger via `TaskLoggerFactory`.
    *   Logger attached to `TaskExecution.Logger`.
    *   All three channels initialized.

2.  **During Execution**:
    *   Main logger captures task lifecycle events.
    *   Protocol logger ready for TCP/socat communication.
    *   Process logger ready for socat output.

3.  **Task Completion**:
    *   Logger finalized automatically (`FinalizeTaskLoggerAsync`).
    *   All logs flushed to files.
    *   Statistics calculated (total entries, file size).

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
// Get in-memory DataStore for real-time UI via TaskLoggerFactory
object? mainStoreObj = taskLoggerFactory.GetTaskDataStore(taskId, TaskLogType.Main);

if (mainStoreObj is ILogDataStore mainStore)
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

*   **Main Logger**: Debug level (configurable via `logging.level`), 10,000 entries.
*   **Protocol Logger**: Configured level, 50,000 entries.
*   **Process Logger**: Configured level, 20,000 entries.

### Enable/Disable Log Channels

```csharp
// Create logger with specific channels
var logger = await taskLoggerFactory.CreateTaskLoggerAsync(
    taskId,
    taskName,
    captureProtocol: true,        // Enable protocol logging
    captureProcessOutput: true,   // Enable process logging
    cancellationToken);
```

## Log Content Examples

### Main Log

```
[2025-11-13 14:30:22.123] [Information] Task execution started: MemoryDump (ID: a3b5c7d9) at 2025-11-13 14:30:22
[2025-11-13 14:30:22.456] [Information] Job profile loaded: S7-1200 Full Dump
[2025-11-13 14:30:22.789] [Information] Starting bootloader execution
[2025-11-13 14:30:32.890] [Information] Task completed successfully. Execution time: 00:00:10.5
```

## Implementation Details

### Composite Logger

The system uses a `CompositeLogger` to broadcast log entries to multiple destinations (DataStore + File) simultaneously.

```csharp
internal class CompositeLogger(ILogger[] loggers) : ILogger
{
    public void Log<TState>(...)
    {
        foreach (ILogger logger in _loggers)
        {
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
```

### Thread Safety

*   **Creation**: Protected by a `SemaphoreSlim`.
*   **Writing**: `FileLogger` uses a `SemaphoreSlim` to synchronize writes to the file stream. `LogDataStore` uses a `ConcurrentQueue`.
*   **Cleanup**: `FinalizeTaskLoggerAsync` handles disposal of providers, sinks, and file streams safely.

## Benefits

*   **Complete Task Audit Trail**: Every task execution fully logged.
*   **Troubleshooting**: Detailed logs help diagnose failures.
*   **Real-Time Monitoring**: In-memory logs for live UI updates.
*   **Historical Analysis**: Persistent files for post-mortem.
*   **Performance**: Asynchronous/buffered writes minimize impact on task execution time.

## Related Documentation

*   [Task Execution Model](../src/S7Tools.Core/Models/Jobs/TaskExecution.cs)
*   [Task Scheduler](../src/S7Tools/Services/Tasking/EnhancedTaskScheduler.cs)
*   [LogDataStore](../src/S7Tools.Infrastructure.Logging/Core/Storage/LogDataStore.cs)
*   [System Patterns](./patterns/system-patterns.md)

---

**Last Updated**: 2025-11-20
**Status**: Core Implementation Complete
