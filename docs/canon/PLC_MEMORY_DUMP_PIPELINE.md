---
title: "PLC Memory Dump Pipeline — System Blueprint"
version: "1.0.0"
created: "2026-03-23"
status: "canon"
tags: ["blueprint", "canon", "plc", "memory-dump", "bootloader", "prescriptive"]
related:
  - docs/canon/ARCHITECTURE.md
  - docs/canon/CORE_API_CONTRACT.md
  - docs/canon/JOB_EXECUTION_PIPELINE.md
---

# PLC Memory Dump Pipeline — System Blueprint

> **Canon Status**: This document prescribes the exact pipeline for the PLC memory dump workflow.
> It defines the trigger, 14-stage execution sequence, resource management, error handling, and
> success/failure outcomes. All implementations of `IBootloaderService` must conform to this spec.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Trigger & Prerequisites](#2-trigger--prerequisites)
3. [Resource Requirements](#3-resource-requirements)
4. [14-Stage Pipeline Diagram](#4-14-stage-pipeline-diagram)
5. [Stage Specifications](#5-stage-specifications)
6. [Error Handling Contract](#6-error-handling-contract)
7. [Retry Policy](#7-retry-policy)
8. [Progress Reporting Contract](#8-progress-reporting-contract)
9. [Output Contract](#9-output-contract)
10. [Teardown Contract](#10-teardown-contract)

---

## 1. Overview

The PLC Memory Dump Pipeline is the **most critical workflow** in S7Tools. It orchestrates an
automated 14-stage sequence to:

1. Establish a serial-to-TCP bridge via `socat`
2. Cycle the PLC power to enter bootloader mode
3. Install a stager payload into PLC IRAM
4. Install a memory dumper payload via the stager
5. Stream raw memory contents from the PLC to a local binary file

The pipeline is implemented in `BootloaderService.PerformBootloaderOrchestrationAsync()` and
triggered through `IBootloaderService.DumpWithTaskTrackingAsync()`.

**Key Characteristics**:
- Typical duration: 3–15 minutes depending on memory size
- Exclusive hardware resource usage (serial port + socat port + power supply)
- Supports retry on transient failures (configurable via `RetryConfiguration`)
- Real-time progress reporting (0–100%)
- Full per-task logging to isolated file outputs

---

## 2. Trigger & Prerequisites

### Trigger

The pipeline is triggered when:
- A `Job` entity with state `Queued` is dequeued by `IJobScheduler`
- All required resources are acquired from `IResourceCoordinator`
- `IBootloaderService.DumpWithTaskTrackingAsync(taskExecution, profiles)` is called

### Required Input: `JobProfileSet`

```
JobProfileSet
├── SerialPortProfile   (port name, baud rate, data bits, parity, stop bits)
├── SocatProfile        (TCP port, serial device path, socat options)
├── PowerSupplyProfile  (IP address, Modbus port, unit ID)
├── PayloadSetProfile   (base path to stager + dumper binaries)
├── MemoryMappingProfile (list of MemorySegment { Address, Length, Name })
├── DumpCount           (number of dump iterations, default 1)
└── DumpFilePath        (output path for binary dump file)
```

### Preconditions (verified in Stage 0 — Validation)

- All required profiles are non-null and structurally valid
- Payload files exist at `PayloadSetProfile.BasePath`
- Serial port device path exists on the OS
- Socat TCP port is not in use
- Power supply IP is reachable (TCP ping)

---

## 3. Resource Requirements

The pipeline requires **exclusive acquisition** of three hardware resources before execution begins:

| Resource | Resource ID Format | Owner Service |
|----------|--------------------|---------------|
| Serial Port | `/dev/ttyUSBx` or `COMx` | `ISerialPortService` |
| Socat TCP Port | `tcp:{port}` | `ISocatService` |
| Power Supply Modbus | `modbus:{ip}:{port}` | `IPowerSupplyService` |

Resources are acquired before Stage 0 via `IResourceCoordinator.TryAcquireAsync(resourceIds, jobId)`.
If any resource is unavailable, the job remains `Queued` until they become free.

---

## 4. 14-Stage Pipeline Diagram

```
                    ┌─────────────────────────────────────────────────────────────────┐
   Job.State=Queued │                BOOTLOADER ORCHESTRATION PIPELINE                │
   Resources Held   │                                                                 │
   ─────────────────┼─────────────────────────────────────────────────────────────────┤
                    │                                                                 │
  Progress: 0%      │  S0  Validate profiles & payload files                         │
  Progress: 1%      │  S1  Configure serial port (stty settings)                     │
  Progress: 3%      │  S2  Start socat bridge (serial ↔ TCP)                         │
  Progress: 5%      │  S3  Connect to power supply (Modbus TCP)                      │
  Progress: 6%      │  S4  Initial Power OFF  (ensure known state)                   │
  Progress: 10%     │  S5  Power ON PLC                                               │
  Progress: 11%     │  S6  Wait for stabilization (500–2000ms)                       │
  Progress: 12%     │  S7  Create IPlcClient & Connect (TCP via socat)               │
  Progress: 13–15%  │  S8  Power Cycle PLC  (OFF → wait → ON → wait)                │
  Progress: 15%     │  S9  Bootloader Handshake  (enter sub-protocol 0x80)           │
  Progress: 16%     │  S10 Install Stager Payload  (upload to IRAM 0x10030100)       │
  Progress: 18%     │  S11 Install Dumper Payload  (upload via stager add-hook)      │
  Progress: 20–95%  │  S12 Memory Dump  (stream all segments / full region)          │
  Progress: 95%     │  S13 Teardown  (stop dumper, disconnect PLC, stop socat)       │
  Progress: 100%    │  S14 Complete  ─── BootloaderResult { Success = true }         │
                    │                                                                 │
  On any failure:   │  ──► ErrorTeardown → Release resources → BootloaderResult fail │
                    └─────────────────────────────────────────────────────────────────┘
```

---

## 5. Stage Specifications

### Stage 0: Profile & Payload Validation (0% → 0%)

- Calls `ValidateProfileSetAsync(profiles)` — validates all profile fields
- Calls `ValidateResourcesAsync(profiles)` — checks file existence, port availability
- On failure: raises `ValidationException` → pipeline aborts → `BootloaderResult.Failure`

### Stage 1: Configure Serial Port (0% → 1%)

- Calls `ISerialPortService.ConfigurePortAsync(portName, serialProfile)`
- Sets baud rate, data bits, parity, stop bits via `stty`
- On failure: logs error, raises `SerialPortException`

### Stage 2: Start Socat Bridge (1% → 3%)

- Calls `ISocatService.StartAsync(socatProfile)`
- Creates `socat` process: `serial_device ↔ TCP:localhost:{port}`
- Waits for process to stabilize (configurable delay)
- On failure: logs error, raises `SocatException`

### Stage 3: Connect Power Supply (3% → 5%)

- Calls `IPowerSupplyService.ConnectAsync(powerProfile)`
- Opens Modbus TCP session to power supply
- On failure: logs error, raises `PowerSupplyException`

### Stage 4: Initial Power OFF (5% → 6%)

- Calls `IPowerSupplyService.SetPowerAsync(false)`
- Ensures PLC starts from a known-off state before bootloader procedure
- Waits `PowerSupplySettings.PowerStateChangeDelayMs` after state change
- On failure: proceeds (non-fatal, logs warning)

### Stage 5: Power ON PLC (6% → 10%)

- Calls `IPowerSupplyService.SetPowerAsync(true)`
- On failure: raises `PowerSupplyException`

### Stage 6: Wait for Stabilization (10% → 11%)

- Waits a fixed delay (500ms – 2000ms, configurable) for PLC boot
- Progress ticks during wait using animated progress reporting

### Stage 7: Create PLC Client & Connect (11% → 12%)

- Calls `clientFactory(profiles)` to create `IPlcClient`
- Calls `IPlcClient.ConnectAsync()` — TCP connect via socat bridge
- On failure: raises `PlcConnectionException`

### Stage 8: Power Cycle PLC (12% → 15%)

- Calls `IPowerSupplyService.SetPowerAsync(false)` → wait → `SetPowerAsync(true)` → wait
- Forces PLC into bootloader window
- Progress animates during delays

### Stage 9: Bootloader Handshake (15%)

- Calls `IPlcClient.EnterSubprotocolAsync()`
- Sends magic bytes to enter sub-protocol 0x80 mode
- Validates response: `{ 0x80, 0x00 }`
- On failure: raises `BootloaderHandshakeException`
- Supports retry (see §7)

### Stage 10: Install Stager Payload (15% → 16%)

- Calls `IPayloadProvider.GetStagerAsync(basePath)` to load binary
- Calls `IPlcClient.InstallStagerAsync(stagerBytes)`
- Writes stager to IRAM at `PlcConstants.IRAM_STAGER_START = 0x10030100`
- Hooks at index `PlcConstants.DEFAULT_STAGER_ADDHOOK_IND = 0x20`
- On failure: raises `PayloadInstallException`

### Stage 11: Install Dumper Payload (16% → 18%)

- Calls `IPayloadProvider.GetMemoryDumperAsync(basePath)` to load binary
- Calls `IPlcClient.InstallDumperAsync(dumperBytes)`
- Uploads dumper via stager's add-hook mechanism at `0x10010100`
- On failure: raises `PayloadInstallException`

### Stage 12: Memory Dump (18% → 95%)

This is the primary data transfer stage. **Two execution modes**:

#### Mode A: Segmented Dump (when `MemoryMappingProfile.Segments` are defined)
```
For each dump iteration (DumpCount):
  For each memory segment in MemoryMappingProfile.Segments:
    IPlcClient.InvokeDumperStreamAsync(segment.Address, segment.Length, ...)
    → streams raw bytes → writes to FileStream
    → reports progress per-chunk
    Wait SegmentDumpDelayMilliseconds between segments
  Wait IterationDumpDelayMilliseconds between iterations
```

#### Mode B: Full Region Dump (when no segments defined)
```
For each dump iteration (DumpCount):
  IPlcClient.InvokeDumperStreamAsync(region.Address, region.Length, ...)
  → streams entire region → writes to FileStream
  → reports progress per-chunk
  Wait IterationDumpDelayMilliseconds between iterations
```

**Streaming Contract**:
- Data is never fully buffered in memory — it is written to the file stream as received
- `FileStream` opened with `FileMode.Create, FileAccess.Write, FileShare.None`
- `FileStream.FlushAsync()` called after each iteration
- On I/O failure: raises `DumpFileWriteException`

### Stage 13: Teardown (95% → 95%)

Executed whether pipeline succeeded or failed (finally block):
1. `IPlcClient.StopDumperSessionAsync()` — gracefully stop dumper
2. `IPlcClient.DisconnectAsync()` — close TCP connection
3. `IPlcClient.Dispose()` — release socket resources
4. `ISocatService.StopAsync(profileId)` — terminate socat process
5. `IPowerSupplyService.DisconnectAsync()` — close Modbus session

### Stage 14: Complete (95% → 100%)

- Reports final progress: `(stage: "Complete", 100%)`
- Returns `BootloaderResult { Success = true, DumpFilePath = ..., BytesDumped = ..., ElapsedTime = ... }`

---

## 6. Error Handling Contract

### Exception Hierarchy

All pipeline exceptions must derive from `BootloaderException` (in `S7Tools.Core.Exceptions`):

```
BootloaderException
├── ValidationException       (Stage 0: invalid profiles/payloads)
├── SerialPortException       (Stage 1)
├── SocatException            (Stage 2)
├── PowerSupplyException      (Stages 3–5, 8)
├── PlcConnectionException    (Stage 7)
├── BootloaderHandshakeException (Stage 9)
├── PayloadInstallException   (Stages 10–11)
├── MemoryDumpException       (Stage 12)
└── DumpFileWriteException    (Stage 12: file I/O failure)
```

### Failure Flow

```
Exception thrown in any stage
        │
        ▼
BootloaderService catches, logs with structured context:
  _logger.LogError(ex, "Pipeline failed at stage {Stage}: {Message}", stage, ex.Message)
        │
        ▼
Teardown always runs (finally block)
        │
        ▼
BootloaderResult { Success = false, Error = ex, Stage = lastCompletedStage }
        │
        ▼
JobScheduler sets Job.State = Failed
JobStateChanged event fires
```

### Non-Fatal Conditions

These conditions are logged but do not abort the pipeline:
- Initial Power OFF failure (Stage 4): proceed, log warning
- Socat output capture failure: proceed, log debug
- File flush failure: log warning, continue

---

## 7. Retry Policy

Retries apply to Stages 9–11 (bootloader protocol stages), controlled by `RetryConfiguration`:

```csharp
public record RetryConfiguration
{
    public static RetryConfiguration Default => new()
    {
        MaxAttempts = 3,
        InitialDelayMs = 500,
        MaxDelayMs = 5000,
        BackoffMultiplier = 2.0,
        RetryableExceptions = [typeof(BootloaderHandshakeException), typeof(PlcConnectionException)]
    };
}
```

**Retry Logic**:
```
Attempt 1 → fail → wait 500ms
Attempt 2 → fail → wait 1000ms
Attempt 3 → fail → raise final exception (pipeline fails)
```

Stages 0–8 and 12–14 do **not** retry automatically. Callers may re-invoke the entire pipeline.

---

## 8. Progress Reporting Contract

The pipeline reports progress via `IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)>`:

| Stage | stage value | percent range | bytesRead | totalBytes |
|-------|-------------|---------------|-----------|-----------|
| 0: Validation | `"validating"` | 0.0 | null | null |
| 1: Serial | `"serial_config"` | 1.0 | null | null |
| 2: Socat | `"socat_start"` | 3.0 | null | null |
| 3: Power Connect | `"power_connect"` | 5.0 | null | null |
| 4: Power OFF | `"power_off"` | 6.0 | null | null |
| 5: Power ON | `"power_on"` | 10.0 | null | null |
| 6: Stabilize | `"stabilizing"` | 10.0–11.0 | null | null |
| 7: PLC Connect | `"plc_connect"` | 12.0 | null | null |
| 8: Power Cycle | `"power_cycle"` | 13.0–15.0 | null | null |
| 9: Handshake | `"handshake"` | 15.0 | null | null |
| 10: Stager | `"stager_install"` | 16.0 | null | null |
| 11: Dumper | `"dumper_install"` | 18.0 | null | null |
| 12: Memory Dump | `"{segment_name}"` | 20.0–95.0 | cumulative | total expected |
| 13: Teardown | `"teardown"` | 95.0 | null | null |
| 14: Complete | `"complete"` | 100.0 | final | total |

**Progress reporting must be throttled**: do not report more frequently than every 0.1% increment,
except on stage transitions (always report stage transitions).

---

## 9. Output Contract

On success, the dump file is written to `JobProfileSet.DumpFilePath`.

**File format**:
- Raw binary (no headers, no padding)
- Byte order: as received from PLC
- File name: `{JobName}_{Timestamp}_{Segment|Full}.bin`

**Metadata**: After dump, a companion `.json` metadata file is created alongside the binary:
```json
{
  "timestamp": "2026-03-23T10:00:00Z",
  "plcModel": "S7-1200",
  "segments": [
    { "name": "IRAM", "address": "0x10000000", "length": 262144, "offset": 0 }
  ],
  "bytesTotal": 262144,
  "md5": "abc123..."
}
```

---

## 10. Teardown Contract

The teardown sequence must be idempotent — calling it multiple times must not throw:

1. `client?.StopDumperSessionAsync()` — wrapped in try/catch, log any error
2. `client?.DisconnectAsync()` — wrapped in try/catch
3. `client?.Dispose()` — wrapped in try/catch
4. `socat.StopAsync(profileId)` — wrapped in try/catch
5. `power.DisconnectAsync()` — wrapped in try/catch
6. Resource handle released via `IResourceCoordinator.ReleaseAsync(handle)`

**The teardown must always run** (implemented as a `finally` block), even if the pipeline is
cancelled via `CancellationToken.Cancel()`. Orphaned hardware resources are unacceptable.
