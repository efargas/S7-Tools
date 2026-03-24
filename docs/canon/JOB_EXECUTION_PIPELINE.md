---
title: "Job Execution Pipeline — System Blueprint"
version: "1.0.0"
created: "2026-03-23"
last-updated: "2026-03-23"
status: "current"
tags: ["blueprint", "canon", "job-scheduler", "task-execution", "prescriptive"]
related:
  - docs/canon/ARCHITECTURE.md
  - docs/canon/CORE_API_CONTRACT.md
  - docs/canon/PLC_MEMORY_DUMP_PIPELINE.md
---

# Job Execution Pipeline — System Blueprint

> **Canon Status**: This document prescribes the complete lifecycle of a Job from user creation to
> final persistence. All implementations of `IJobScheduler`, `IJobManager`, and
> `IResourceCoordinator` must conform to this specification.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Entity Definitions](#2-entity-definitions)
3. [Pipeline Trigger](#3-pipeline-trigger)
4. [Complete Pipeline Diagram](#4-complete-pipeline-diagram)
5. [Phase Specifications](#5-phase-specifications)
6. [Resource Coordination Contract](#6-resource-coordination-contract)
7. [Scheduling Algorithm](#7-scheduling-algorithm)
8. [Job State Machine](#8-job-state-machine)
9. [Task Execution Model](#9-task-execution-model)
10. [Error & Cancellation Contract](#10-error--cancellation-contract)
11. [Persistence Contract](#11-persistence-contract)

---

## 1. Overview

The Job Execution Pipeline manages the complete lifecycle of hardware operations in S7Tools. It
provides:

- **Queue management**: Multiple jobs queued, prioritized, and dequeued in order
- **Resource coordination**: Prevents simultaneous use of the same serial port or Modbus connection
- **Parallel execution**: Jobs sharing no resources run concurrently
- **Retry and cancellation**: Operators can cancel queued or running jobs
- **Full observability**: Real-time state changes and progress events

The pipeline involves three cooperating services:
- `IJobManager`: Persistent profile definitions (not execution state)
- `IJobScheduler`: Execution lifecycle, queue, and resource-aware scheduling
- `IResourceCoordinator`: Hardware resource locking during execution

---

## 2. Entity Definitions

### `JobProfile` (Persistent Definition)

```
JobProfile
├── int Id
├── string Name
├── bool IsDefault
├── JobType Type           (MemoryDump | Custom)
├── int SerialProfileId    (FK → SerialPortProfile.Id)
├── int SocatProfileId     (FK → SocatProfile.Id)
├── int PowerProfileId     (FK → PowerSupplyProfile.Id)
├── int PayloadProfileId   (FK → PayloadSetProfile.Id)
├── int MemoryProfileId    (FK → MemoryMappingProfile.Id)
├── int DumpCount          (1..N iterations)
├── string? DumpOutputFolder
├── bool IsScheduled
├── DateTime? NextRunAt
├── TimeSpan? RecurringInterval
└── RetryConfiguration RetryConfig
```

### `Job` (Execution Instance — immutable record)

```
Job
├── int Id
├── string Name
├── JobState State         (Created | Queued | Running | Completed | Failed | Cancelled)
├── int ProfileId          (FK → JobProfile.Id)
├── JobProfileSet Profiles (resolved snapshot at enqueue time)
├── DateTimeOffset? QueuedAt
├── DateTimeOffset? StartedAt
├── DateTimeOffset? CompletedAt
├── DateTimeOffset ModifiedAt
├── string? ErrorMessage
└── double ProgressPercent
```

### `TaskExecution` (Live Execution Context)

```
TaskExecution
├── Guid TaskId
├── string TaskName
├── TaskState State
├── string? StatusMessage
├── double ProgressPercent
├── ILogger TaskLogger      (isolated per-task logger)
├── ILogger ProcessLogger   (socat/shell output)
└── CancellationToken CancellationToken
```

---

## 3. Pipeline Trigger

A job enters the pipeline through one of three triggers:

### Trigger A: Manual Execution

```
User clicks "Run" on a JobProfile in JobsPage
    → JobsPageViewModel.RunJobCommand.Execute(selectedProfileId)
    → IJobManager.CreateJobAsync(profileId)   // creates Job entity from profile
    → IJobScheduler.EnqueueAsync(job)         // adds to queue
```

### Trigger B: Scheduled Execution

```
ITaskScheduler background loop fires
    → Checks all JobProfiles where IsScheduled=true AND NextRunAt ≤ now
    → IJobManager.CreateJobAsync(profileId) for each due profile
    → IJobScheduler.EnqueueAsync(job)
    → Updates profile.NextRunAt = now + RecurringInterval
```

### Trigger C: Diagnostic Mode

```
Program.Main(args) with --diag flag
    → Direct service initialization and profile count validation
    → Does NOT enqueue jobs
    → Exits after diagnostics complete
```

---

## 4. Complete Pipeline Diagram

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  TRIGGER  (Manual / Scheduled / API)                                            │
└──────────────────────────────┬───────────────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│  PHASE 1: Profile Resolution                                                    │
│  IJobManager.CreateJobAsync(profileId)                                          │
│  - Loads JobProfile from persistence                                            │
│  - Resolves all sub-profiles (Serial, Socat, Power, Payload, Memory)           │
│  - Validates profile set completeness                                           │
│  - Constructs Job entity with State=Created and resolved JobProfileSet snapshot │
└──────────────────────────────┬───────────────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│  PHASE 2: Enqueue                                                               │
│  IJobScheduler.EnqueueAsync(job)                                                │
│  - Validates Job.State == Created                                               │
│  - Sets Job.State = Queued, QueuedAt = now                                     │
│  - Stores in _jobs dictionary                                                   │
│  - Fires JobStateChanged(Created → Queued)                                      │
└──────────────────────────────┬───────────────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│  PHASE 3: Scheduler Loop (background Task)                                      │
│  Runs continuously while IJobScheduler.IsRunning                               │
│  - Iterates Queued jobs in enqueue order                                        │
│  - For each Queued job:                                                         │
│      IResourceCoordinator.TryAcquireAsync([serialPort, socatPort, modbus])     │
│      ├── Resources FREE → advance to PHASE 4                                   │
│      └── Resources BUSY → skip (check again next loop iteration, 1s poll)      │
└──────────────────────────────┬───────────────────────────────────────────────────┘
                               │ (resources acquired)
                               ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│  PHASE 4: Task Execution Setup                                                  │
│  - Sets Job.State = Running, StartedAt = now                                   │
│  - Fires JobStateChanged(Queued → Running)                                      │
│  - ITaskLoggerFactory.CreateTaskLoggerAsync(taskId, jobName)                   │
│    → creates isolated log stores + file outputs                                │
│  - Constructs TaskExecution { TaskId, TaskLogger, ProcessLogger, CT }          │
└──────────────────────────────┬───────────────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│  PHASE 5: Bootloader Pipeline Execution                                         │
│  IBootloaderService.DumpWithTaskTrackingAsync(taskExecution, profiles)         │
│  - Full 14-stage pipeline (see PLC_MEMORY_DUMP_PIPELINE.md)                   │
│  - Progress reported via TaskExecution.UpdateProgress(percent, stage)          │
│  - Fires JobProgressChanged events throughout                                   │
└──────────────────────┬────────────────────────────────────┬──────────────────────┘
                       │ SUCCESS                             │ FAILURE / CANCEL
                       ▼                                     ▼
┌──────────────────────────────┐               ┌──────────────────────────────────┐
│  PHASE 6A: Complete          │               │  PHASE 6B: Fail / Cancel         │
│  Job.State = Completed       │               │  Job.State = Failed / Cancelled  │
│  Job.CompletedAt = now       │               │  Job.ErrorMessage = ex.Message   │
│  Job.ProgressPercent = 100%  │               │  Job.CompletedAt = now           │
│  Fires JobStateChanged       │               │  Fires JobStateChanged           │
│  Resource handles released   │               │  Resource handles released       │
└──────────────────────────────┘               └──────────────────────────────────┘
                       │                                     │
                       └──────────────────┬──────────────────┘
                                          ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│  PHASE 7: Persistence                                                           │
│  IJobManager.PersistJobResultAsync(job)                                        │
│  - Appends job execution record to history file                                 │
│  - Updates JobProfile.LastRunAt, LastRunResult                                 │
│  - Calculates NextRunAt if recurring                                            │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Phase Specifications

### Phase 1: Profile Resolution

- `IJobManager` must snapshot all sub-profiles at creation time.
- If any sub-profile ID cannot be resolved (deleted since job was defined), throw
  `ProfileNotFoundException` and abort.
- The `JobProfileSet` stored in the `Job` entity is immutable — profile changes after enqueue do
  not affect running or queued jobs.

### Phase 2: Enqueue

- `EnqueueAsync` must be non-blocking (returns immediately after state transition).
- The call must be idempotent: enqueuing an already-Queued job throws `InvalidOperationException`.
- The scheduler must handle concurrent `EnqueueAsync` calls safely (thread-safe dictionary).

### Phase 3: Scheduler Loop

- The scheduler loop runs on a dedicated background `Task` (started via `StartAsync()`).
- Poll interval: 1 second (configurable via `AppSettings.Tasks.PollingIntervalMs`).
- Jobs are dispatched in FIFO order within each priority tier.
- A job can only be dispatched if **all** its required resources are free simultaneously.
- Multiple jobs may run in parallel if they require non-overlapping resource sets.
- Maximum parallel jobs: configurable via `AppSettings.Jobs.MaxParallelJobs` (default: unlimited).

### Phase 4: Task Execution Setup

- A new `Guid` is assigned as `TaskId` per execution (not the `JobProfile.Id`).
- Task log directories follow the pattern:
  `{LogsDirectory}/Tasks/{SanitizedJobName}_{yyyyMMdd_HHmmss}_{taskId[..8]}/`
- Three log streams are created per task:
  - `task-main.log` — orchestration/business logic logs
  - `task-process.log` — socat/shell process stdout/stderr
  - `task-protocol.log` — low-level PLC protocol bytes (optional, toggled by settings)

### Phases 6A/6B: Completion

- Resource handles are always released (in a `finally` block), even on cancellation.
- On cancellation, if the dump file was partially written, it is **retained with a `.partial`
  extension** for diagnostic purposes.
- `OperationCanceledException` is caught and results in `JobState.Cancelled`, not `Failed`.

---

## 6. Resource Coordination Contract

### Resource ID Naming Convention

Resource IDs are OS-level device identifiers with a type prefix:

```
Serial Port: "/dev/ttyUSB0"    (Linux) or "COM3"  (Windows)
Socat Port:  "tcp:2023"
Modbus:      "modbus:192.168.1.100:502"
```

### Acquisition Semantics

```csharp
// CORRECT: all-or-nothing acquisition
ResourceHandle handle = await _coordinator.TryAcquireAsync(
    [serialResourceId, socatResourceId, modbusResourceId],
    job.Id,
    cancellationToken);

// On success: all three resources are locked to this job
// On failure (any one busy): waits and retries

// FORBIDDEN: sequential acquisition (risk of partial lock / deadlock)
await _coordinator.TryAcquireAsync([serialResourceId], job.Id);  // ❌ partial
await _coordinator.TryAcquireAsync([socatResourceId], job.Id);   // ❌ partial
```

### Acquisition Timeout

If resources are not acquired within `AppSettings.Jobs.ResourceAcquisitionTimeoutMs` (default:
never), the job transitions to `Failed` with message `"Resource acquisition timed out"`.

### Release Semantics

Resources must be released in the `finally` block of Phase 5, regardless of outcome:

```csharp
ResourceHandle? handle = null;
try
{
    handle = await _coordinator.TryAcquireAsync(resources, job.Id, ct);
    await _bootloader.DumpWithTaskTrackingAsync(taskExec, profiles, ct);
}
finally
{
    if (handle != null)
        await _coordinator.ReleaseAsync(handle, ct);
}
```

---

## 7. Scheduling Algorithm

The scheduler implements **Priority-aware FIFO with resource conflict avoidance**:

```
Priority Queue (sorted by Priority DESC, then QueuedAt ASC):
  ┌── CRITICAL jobs first (PriorityLevel.Critical)
  ├── HIGH jobs (PriorityLevel.High)
  ├── NORMAL jobs (PriorityLevel.Normal)  ← default
  └── LOW jobs (PriorityLevel.Low)

For each scheduler loop iteration:
  For each job in priority order:
    If job.State == Queued:
      requiredResources = DeriveResourceIds(job.Profiles)
      If IResourceCoordinator.AreAllFreeAsync(requiredResources):
        → Dispatch job (PHASE 4+)
      Else:
        → Skip (will retry next loop)
```

**No starvation policy**: A `LOW` priority job that has waited more than
`AppSettings.Jobs.MaxWaitTimeMs` (default: 30 minutes) is automatically promoted to `HIGH`.

---

## 8. Job State Machine

```
                    ┌──────────┐
                    │ Created  │
                    └─────┬────┘
                          │ EnqueueAsync()
                          ▼
                    ┌──────────┐
               ┌──► │  Queued  │ ◄──── (re-queued after resource wait)
               │    └─────┬────┘
               │          │ Resources acquired
               │          │ + State = Running
               │          ▼
               │    ┌──────────┐
               │    │ Running  │
               │    └──┬───┬───┘
               │       │   │
               │   ┌───┘   └────────────────┐
               │   │ success               │ failure/cancel
               │   ▼                       ▼
               │ ┌───────────┐       ┌──────────┐    ┌───────────┐
               │ │ Completed │       │  Failed  │    │ Cancelled │
               │ └───────────┘       └──────────┘    └───────────┘
               │
               └──── (scheduler tries next job)
```

**Terminal states**: `Completed`, `Failed`, `Cancelled` — no transitions possible from these.

---

## 9. Task Execution Model

### TaskExecution is NOT a Job

`TaskExecution` is a transient execution context, created fresh for each job run. It:
- Is not persisted (exists only in memory during execution)
- Holds references to the isolated `ILogger` instances for this run
- Carries the `CancellationToken` for the run
- Is passed through the entire call stack so all stages can log to the correct task log

### Progress Propagation

```
IBootloaderService.DumpWithTaskTrackingAsync(taskExec, ...)
    ├── Reports to taskExec.UpdateProgress(percent, stage)
    ├── IProgress<T> callback updates Job.ProgressPercent
    └── IJobScheduler fires JobProgressChanged event
            │
            ▼
    JobsPageViewModel.OnJobProgressChanged(e)
    → updates observable job entry progress bar
```

---

## 10. Error & Cancellation Contract

### On Exception

```csharp
catch (BootloaderException ex)
{
    _logger.LogError(ex, "Job {JobId} failed at stage {Stage}", job.Id, ex.Stage);
    job = job with { State = JobState.Failed, ErrorMessage = ex.Message };
    JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(job.Id, JobState.Running, JobState.Failed, ex));
}
```

### On Cancellation

```csharp
catch (OperationCanceledException)
{
    _logger.LogInformation("Job {JobId} was cancelled", job.Id);
    job = job with { State = JobState.Cancelled };
    JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(job.Id, JobState.Running, JobState.Cancelled, null));
}
```

### On Cancellation of Queued Job

- `IJobScheduler.CancelJobAsync(jobId)` sets `Job.State = Cancelled` immediately if `Queued`.
- If `Running`, it signals the `CancellationToken` held by the task execution.
- The running task must check `ct.ThrowIfCancellationRequested()` at each stage boundary.

---

## 11. Persistence Contract

### Job History

Each completed/failed/cancelled job execution is appended to:
`{ResourcesDirectory}/JobHistory/history.json`

Format:
```json
[
  {
    "jobId": 42,
    "profileId": 7,
    "profileName": "Full IRAM Dump",
    "state": "Completed",
    "queuedAt": "2026-03-23T10:00:00Z",
    "startedAt": "2026-03-23T10:00:01Z",
    "completedAt": "2026-03-23T10:03:45Z",
    "bytesRead": 262144,
    "dumpFilePath": "dumps/iram_20260323_100001.bin",
    "errorMessage": null
  }
]
```

### Profile Last-Run Update

After each execution, `JobProfile.LastRunAt` and `LastRunState` are updated in
`Resources/JobProfiles/profiles.json`. This is done **after** the execution is complete to
avoid corrupting the profile during execution.

### Scheduled Job Re-Scheduling

If `JobProfile.IsScheduled = true` and `RecurringInterval` is set:
```
NextRunAt = completedAt + RecurringInterval
```
If the pipeline fails, `NextRunAt` is still advanced (no catch-up scheduling).
If the pipeline is cancelled, `NextRunAt` is **not** advanced (re-run at the original scheduled time).
