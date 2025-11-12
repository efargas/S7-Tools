# Data Model: Bootloader Integration

**Feature**: 011-bootloader-integration
**Phase**: Phase 1 (Design)
**Created**: 2025-11-12
**Status**: Complete

## Overview

This document defines the domain entities and value objects required for bootloader integration. All entities follow S7Tools Clean Architecture patterns with IProfileBase implementation for profile types and proper separation between domain models (S7Tools.Core) and application concerns.

## Entity Definitions

### 1. Job

**Purpose**: Represents a complete bootloader execution job with state tracking and profile references.

**Location**: `src/S7Tools.Core/Models/Jobs/Job.cs`

**Type**: Entity (mutable with identity)

**Fields**:

| Field | Type | Required | Default | Validation | Description |
|-------|------|----------|---------|------------|-------------|
| Id | int | Yes | Auto-assigned | > 0 | Unique job identifier (gap-filling from 1) |
| Name | string | Yes | - | 1-100 chars, unique | User-friendly job name |
| Description | string | No | "" | Max 500 chars | Job purpose/notes |
| ProfileSet | JobProfileSet | Yes | - | Non-null, all refs valid | Configuration references |
| State | JobState | Yes | Created | Valid enum value | Current execution state |
| CreatedAt | DateTime | Yes | DateTime.UtcNow | UTC timestamp | Job creation time |
| ModifiedAt | DateTime | Yes | DateTime.UtcNow | UTC timestamp | Last state change |
| QueuedAt | DateTime? | No | null | UTC timestamp if queued | Queue entry time |
| StartedAt | DateTime? | No | null | UTC timestamp if started | Execution start time |
| CompletedAt | DateTime? | No | null | UTC timestamp if completed | Execution end time |
| Progress | double | Yes | 0.0 | 0.0-100.0 | Completion percentage |
| CurrentOperation | string | No | "" | Max 200 chars | Current stage description |
| OutputPath | string | Yes | - | Valid directory path | Memory dump save location |
| ErrorMessage | string? | No | null | Max 1000 chars | Failure reason if State = Failed |

**Relationships**:

- `Job` → `JobProfileSet` (composition, 1:1)
- `JobProfileSet` → `SerialProfileRef`, `SocatProfileRef`, `PowerProfileRef`, `MemoryRegionProfile`, `PayloadSetProfile` (aggregation, 1:1 each)

**State Transitions**:

```
Created → Queued → Running → {Completed, Failed, Canceled}
         ↓         ↓         ↑
         └─────────┴─────────┘ (Canceled state reachable from Queued/Running)
```

**Business Rules**:

1. Name uniqueness enforced by StandardProfileManager<Job>
2. State transitions must be valid (cannot go from Completed back to Queued)
3. Timestamps must follow chronological order (QueuedAt ≤ StartedAt ≤ CompletedAt)
4. Progress must be 0.0 when Created/Queued, 100.0 when Completed, >0.0 when Running
5. ErrorMessage only populated when State = Failed
6. OutputPath must exist as directory before job execution starts

**Example**:

```csharp
var job = new Job
{
    Id = 1,
    Name = "S7-1200 CPU Firmware Dump",
    Description = "Extract full 64KB firmware from CPU 1214C DC/DC/DC",
    ProfileSet = new JobProfileSet
    {
        Serial = new SerialProfileRef { ProfileId = 5, ProfileName = "COM3 115200 8N1" },
        Socat = new SocatProfileRef { ProfileId = 2, ProfileName = "Bridge TCP 10102" },
        Power = new PowerProfileRef { ProfileId = 1, ProfileName = "Main Power Supply 24V" },
        MemoryRegion = new MemoryRegionProfile { /* ... */ },
        PayloadSet = new PayloadSetProfile { /* ... */ }
    },
    State = JobState.Created,
    CreatedAt = DateTime.UtcNow,
    ModifiedAt = DateTime.UtcNow,
    Progress = 0.0,
    OutputPath = "/home/user/plc-dumps/cpu1214c-20251112"
};
```

---

### 2. JobProfileSet

**Purpose**: Aggregates all required profile references for a complete bootloader job execution.

**Location**: `src/S7Tools.Core/Models/Jobs/JobProfileSet.cs`

**Type**: Value Object (immutable aggregate)

**Fields**:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Serial | SerialProfileRef | Yes | Non-null, ProfileId > 0 | Serial port configuration reference |
| Socat | SocatProfileRef | Yes | Non-null, ProfileId > 0 | TCP/serial bridge configuration |
| Power | PowerProfileRef | Yes | Non-null, ProfileId > 0 | Power supply control configuration |
| MemoryRegion | MemoryRegionProfile | Yes | Non-null, valid address ranges | Memory dump region definition |
| PayloadSet | PayloadSetProfile | Yes | Non-null, base path exists | Bootloader payload locations |

**Validation Rules**:

1. All profile references must point to existing profiles (validated during job creation)
2. Serial profile device must be accessible at execution time
3. Socat profile port must not conflict with active jobs
4. Power profile modbus connection must be reachable
5. Memory region start + length must not overflow 32-bit address space
6. Payload set base path must contain stager.bin and dump_mem.bin files

**Example**:

```csharp
var profileSet = new JobProfileSet
{
    Serial = new SerialProfileRef { ProfileId = 5, ProfileName = "COM3 115200 8N1" },
    Socat = new SocatProfileRef { ProfileId = 2, ProfileName = "Bridge TCP 10102" },
    Power = new PowerProfileRef { ProfileId = 1, ProfileName = "Main Power Supply 24V" },
    MemoryRegion = new MemoryRegionProfile
    {
        Id = 3,
        Name = "CPU Firmware 64KB",
        StartAddress = 0x20000000,
        Length = 65536,
        Description = "Full CPU firmware dump"
    },
    PayloadSet = new PayloadSetProfile
    {
        Id = 1,
        Name = "Development Payloads",
        BasePath = "/home/user/S7-Tools/bootloader-payloads/payloads"
    }
};
```

---

### 3. JobState

**Purpose**: Enumeration of valid job execution states.

**Location**: `src/S7Tools.Core/Models/Jobs/JobState.cs`

**Type**: Enum

**Values**:

| Value | Numeric | Description | Terminal State |
|-------|---------|-------------|----------------|
| Created | 0 | Job created but not queued | No |
| Queued | 1 | Job waiting in execution queue | No |
| Running | 2 | Job currently executing | No |
| Completed | 3 | Job finished successfully | Yes |
| Failed | 4 | Job failed with error | Yes |
| Canceled | 5 | Job canceled by user | Yes |

**State Transition Matrix**:

| From \ To | Created | Queued | Running | Completed | Failed | Canceled |
|-----------|---------|--------|---------|-----------|--------|----------|
| Created   | ✅      | ✅     | ❌      | ❌        | ❌     | ✅       |
| Queued    | ❌      | ✅     | ✅      | ❌        | ❌     | ✅       |
| Running   | ❌      | ❌     | ✅      | ✅        | ✅     | ✅       |
| Completed | ❌      | ❌     | ❌      | ✅        | ❌     | ❌       |
| Failed    | ❌      | ❌     | ❌      | ❌        | ✅     | ❌       |
| Canceled  | ❌      | ❌     | ❌      | ❌        | ❌     | ✅       |

**Example**:

```csharp
public enum JobState
{
    Created = 0,
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Canceled = 5
}
```

---

### 4. ResourceKey

**Purpose**: Value object identifying a system resource (serial port, TCP port, modbus connection) for conflict detection.

**Location**: `src/S7Tools.Core/Models/Jobs/ResourceKey.cs`

**Type**: Value Object (immutable with value equality)

**Fields**:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Type | ResourceType | Yes | Valid enum value | Resource category (Serial/TCP/Modbus) |
| Identifier | string | Yes | Non-empty, max 200 chars | Resource-specific identifier |

**ResourceType Enum**:

```csharp
public enum ResourceType
{
    Serial = 0,   // Identifier = port path (e.g., "/dev/ttyUSB0", "COM3")
    Tcp = 1,      // Identifier = port number (e.g., "10102", "502")
    Modbus = 2    // Identifier = "host:port" (e.g., "192.168.1.10:502")
}
```

**Equality Semantics**:

Two ResourceKey instances are equal if both Type and Identifier match (case-insensitive for identifiers).

**Example**:

```csharp
var serialKey = new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0");
var tcpKey = new ResourceKey(ResourceType.Tcp, "10102");
var modbusKey = new ResourceKey(ResourceType.Modbus, "192.168.1.10:502");

// Equality
var key1 = new ResourceKey(ResourceType.Serial, "COM3");
var key2 = new ResourceKey(ResourceType.Serial, "com3");
Assert.True(key1 == key2); // Case-insensitive
```

**Usage in Resource Coordination**:

```csharp
// Job A resources
var resourcesA = new[]
{
    new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0"),
    new ResourceKey(ResourceType.Tcp, "10102"),
    new ResourceKey(ResourceType.Modbus, "192.168.1.10:502")
};

// Job B resources (different serial, same TCP → conflict)
var resourcesB = new[]
{
    new ResourceKey(ResourceType.Serial, "/dev/ttyUSB1"),
    new ResourceKey(ResourceType.Tcp, "10102"), // CONFLICT!
    new ResourceKey(ResourceType.Modbus, "192.168.1.11:502")
};

if (!_resourceCoordinator.TryAcquire(resourcesB))
{
    // Queue Job B for later execution
}
```

---

### 5. SerialProfileRef

**Purpose**: Reference to an existing SerialPortProfile with snapshot of key settings.

**Location**: `src/S7Tools.Core/Models/Jobs/SerialProfileRef.cs`

**Type**: Value Object (immutable reference + snapshot)

**Fields**:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| ProfileId | int | Yes | > 0 | ID of SerialPortProfile |
| ProfileName | string | Yes | Non-empty | Snapshot of profile name at job creation |
| Device | string | Yes | Non-empty | Serial port path (e.g., "/dev/ttyUSB0") |

**Rationale for Snapshot**:

Job execution may occur hours/days after creation. If user modifies the referenced SerialPortProfile (e.g., changes baud rate), the job should fail validation during execution rather than silently use wrong settings. Device field provides quick access for resource conflict detection without profile lookup.

**Example**:

```csharp
var serialRef = new SerialProfileRef
{
    ProfileId = 5,
    ProfileName = "COM3 115200 8N1",
    Device = "/dev/ttyUSB0"
};

// At execution time, validate profile still exists and device matches
var actualProfile = await _serialProfileManager.GetByIdAsync(serialRef.ProfileId);
if (actualProfile == null || actualProfile.Device != serialRef.Device)
{
    throw new ProfileValidationException("Serial profile changed or deleted");
}
```

---

### 6. SocatProfileRef

**Purpose**: Reference to an existing SocatProfile with snapshot for conflict detection.

**Location**: `src/S7Tools.Core/Models/Jobs/SocatProfileRef.cs`

**Type**: Value Object (immutable reference + snapshot)

**Fields**:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| ProfileId | int | Yes | > 0 | ID of SocatProfile |
| ProfileName | string | Yes | Non-empty | Snapshot of profile name |
| Port | int | Yes | 1-65535 | TCP port number for bridge |

**Example**:

```csharp
var socatRef = new SocatProfileRef
{
    ProfileId = 2,
    ProfileName = "Bridge TCP 10102",
    Port = 10102
};

// Resource conflict detection
var tcpKey = new ResourceKey(ResourceType.Tcp, socatRef.Port.ToString());
```

---

### 7. PowerProfileRef

**Purpose**: Reference to an existing PowerSupplyProfile with snapshot.

**Location**: `src/S7Tools.Core/Models/Jobs/PowerProfileRef.cs`

**Type**: Value Object (immutable reference + snapshot)

**Fields**:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| ProfileId | int | Yes | > 0 | ID of PowerSupplyProfile |
| ProfileName | string | Yes | Non-empty | Snapshot of profile name |
| ModbusAddress | string | Yes | Valid host:port | Modbus TCP endpoint (e.g., "192.168.1.10:502") |

**Example**:

```csharp
var powerRef = new PowerProfileRef
{
    ProfileId = 1,
    ProfileName = "Main Power Supply 24V",
    ModbusAddress = "192.168.1.10:502"
};

// Resource conflict detection
var modbusKey = new ResourceKey(ResourceType.Modbus, powerRef.ModbusAddress);
```

---

### 8. MemoryRegionProfile

**Purpose**: Defines a memory region to dump from the PLC (extends/reuses existing MemoryRegionProfile if already exists, or creates new).

**Location**: `src/S7Tools.Core/Models/Jobs/MemoryRegionProfile.cs` (NEW) or `src/S7Tools.Core/Models/Profiles/MemoryRegionProfile.cs` (EXISTING - check during implementation)

**Type**: Entity (if new) or Reference (if extending existing profile system)

**Fields** (if creating new entity):

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | int | Yes | > 0 | Profile identifier |
| Name | string | Yes | 1-100 chars | Region name (e.g., "CPU Firmware 64KB") |
| Description | string | No | Max 500 chars | Region purpose |
| StartAddress | uint | Yes | Valid 32-bit address | Memory start (e.g., 0x20000000) |
| Length | uint | Yes | > 0, no overflow | Bytes to dump |
| CreatedAt | DateTime | Yes | UTC timestamp | Profile creation time |
| ModifiedAt | DateTime | Yes | UTC timestamp | Last modification |
| IsDefault | bool | Yes | - | Default region flag |
| IsReadOnly | bool | Yes | - | System profile protection |

**Validation Rules**:

1. StartAddress + Length must not overflow uint.MaxValue (4GB limit)
2. Length must be > 0 and ≤ 16MB (practical limit for PLC memory dumps)
3. StartAddress should align to standard PLC memory regions (0x20000000 for S7-1200)

**Example**:

```csharp
var memoryRegion = new MemoryRegionProfile
{
    Id = 3,
    Name = "CPU Firmware 64KB",
    Description = "Full CPU firmware dump from bootloader",
    StartAddress = 0x20000000,
    Length = 65536,
    CreatedAt = DateTime.UtcNow,
    ModifiedAt = DateTime.UtcNow,
    IsDefault = false,
    IsReadOnly = false
};

// Validation
if ((ulong)memoryRegion.StartAddress + memoryRegion.Length > uint.MaxValue)
{
    throw new ValidationException("MemoryRegion", "Address range overflow");
}
```

**Note**: If MemoryRegionProfile already exists in the codebase (from spec 008-memory-regions-profiling), this entity may only need minor extensions. Verify during implementation Phase 2.

---

### 9. PayloadSetProfile

**Purpose**: Configures the file system location of bootloader binary payloads (stager.bin, dump_mem.bin).

**Location**: `src/S7Tools.Core/Models/Jobs/PayloadSetProfile.cs`

**Type**: Entity implementing IProfileBase

**Fields**:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | int | Yes | > 0 | Profile identifier |
| Name | string | Yes | 1-100 chars, unique | Profile name |
| Description | string | No | Max 500 chars | Profile purpose |
| BasePath | string | Yes | Valid directory path, exists | Base directory containing payloads |
| CreatedAt | DateTime | Yes | UTC timestamp | Profile creation time |
| ModifiedAt | DateTime | Yes | UTC timestamp | Last modification |
| IsDefault | bool | Yes | - | Default payload set flag |
| IsReadOnly | bool | Yes | - | System profile protection |

**Payload File Conventions**:

- Stager: `{BasePath}/stager/stager.bin`
- Memory Dumper: `{BasePath}/dump_mem/dump_mem.bin`

**Validation Rules**:

1. BasePath must exist as directory
2. BasePath must contain stager/stager.bin file
3. BasePath must contain dump_mem/dump_mem.bin file
4. Payload files must be non-empty (> 0 bytes)
5. Payload files must be readable

**Example**:

```csharp
var payloadSet = new PayloadSetProfile
{
    Id = 1,
    Name = "Development Payloads",
    Description = "Locally built bootloader payloads from Docker",
    BasePath = "/home/user/S7-Tools/bootloader-payloads/payloads",
    CreatedAt = DateTime.UtcNow,
    ModifiedAt = DateTime.UtcNow,
    IsDefault = true,
    IsReadOnly = false
};

// Validation
var stagerPath = Path.Combine(payloadSet.BasePath, "stager", "stager.bin");
var dumperPath = Path.Combine(payloadSet.BasePath, "dump_mem", "dump_mem.bin");

if (!File.Exists(stagerPath) || !File.Exists(dumperPath))
{
    throw new ValidationException("PayloadSet", "Required payload files missing");
}
```

---

## Entity Relationships Diagram

```
┌─────────────────────────┐
│       Job               │
│ (Entity)                │
├─────────────────────────┤
│ + Id: int               │
│ + Name: string          │
│ + ProfileSet: JobProf...│───┐
│ + State: JobState       │   │
│ + Progress: double      │   │
│ + OutputPath: string    │   │
│ + CreatedAt: DateTime   │   │
│ + QueuedAt: DateTime?   │   │
│ + StartedAt: DateTime?  │   │
│ + CompletedAt: DateTime?│   │
└─────────────────────────┘   │
                              │ Composition 1:1
                              ↓
              ┌───────────────────────────┐
              │   JobProfileSet           │
              │   (Value Object)          │
              ├───────────────────────────┤
              │ + Serial: SerialProfileRef│───────┐
              │ + Socat: SocatProfileRef  │───┐   │
              │ + Power: PowerProfileRef  │─┐ │   │
              │ + MemoryRegion: MemoryR...│ │ │   │
              │ + PayloadSet: PayloadSet..│ │ │   │
              └───────────────────────────┘ │ │   │
                                            │ │   │
          ┌─────────────────────────────────┘ │   │
          │ ┌───────────────────────────────┘ │   │
          │ │ ┌────────────────────────────────┘   │
          │ │ │                                    │
          ↓ ↓ ↓                                    ↓
    Profile References (Value Objects)    MemoryRegionProfile (Entity)
    - SerialProfileRef                    - Id, Name, Description
      • ProfileId, ProfileName            - StartAddress, Length
      • Device (snapshot)                 - IsDefault, IsReadOnly
    - SocatProfileRef
      • ProfileId, ProfileName
      • Port (snapshot)
    - PowerProfileRef
      • ProfileId, ProfileName
      • ModbusAddress (snapshot)
                                          PayloadSetProfile (Entity)
                                          - Id, Name, Description
                                          - BasePath
                                          - IsDefault, IsReadOnly

┌─────────────────┐
│   ResourceKey   │
│ (Value Object)  │
├─────────────────┤
│ + Type: enum    │ ← Used by ResourceCoordinator
│ + Identifier: str│   for conflict detection
└─────────────────┘
```

---

## Validation Summary

| Entity | Validation Rules | Enforced By |
|--------|------------------|-------------|
| Job | Name unique, state transitions valid, timestamps chronological | StandardProfileManager<Job>, JobScheduler |
| JobProfileSet | All profile refs exist, resources available | JobScheduler pre-execution validation |
| ResourceKey | Type valid, identifier non-empty | Constructor validation |
| SerialProfileRef | ProfileId exists, device accessible | Pre-execution validation |
| SocatProfileRef | ProfileId exists, port not in use | ResourceCoordinator |
| PowerProfileRef | ProfileId exists, modbus reachable | Pre-execution validation |
| MemoryRegionProfile | Address + length no overflow, length > 0 | Constructor validation |
| PayloadSetProfile | BasePath exists, contains required .bin files | Constructor + IPayloadProvider |

---

## Storage and Persistence

### Job Persistence

- **Format**: JSON serialization via System.Text.Json
- **Location**: `src/resources/JobProfiles/profiles.json`
- **Manager**: `StandardProfileManager<Job>` with thread-safe CRUD operations
- **File Creation**: If profiles.json missing, create with empty array `[]` on first save

### Memory Region Profile Persistence

- **Format**: JSON (if new entity) or existing profile system
- **Location**: Determined during implementation - check if spec 008 already created memory region profile persistence
- **Manager**: Reuse existing manager or create `StandardProfileManager<MemoryRegionProfile>`

### Payload Set Profile Persistence

- **Format**: JSON serialization
- **Location**: `src/resources/PayloadProfiles/profiles.json` (new directory)
- **Manager**: `StandardProfileManager<PayloadSetProfile>`

---

## Migration Path (If Applicable)

If existing Job or MemoryRegionProfile models exist from previous work:

1. **Backup existing data**: Copy current profiles.json files before schema changes
2. **Add new fields**: Extend existing models with new fields (use `[JsonIgnore]` for backward compatibility if needed)
3. **Data migration script**: Update existing profiles to include new required fields with sensible defaults
4. **Version schema**: Add SchemaVersion field to detect old vs new format

**No migration expected for this feature** - bootloader integration is net-new functionality.

---

## Next Steps

After data model approval:

1. **Generate Contracts** (`/contracts/` directory) - 10 service interface definitions
2. **Create Quickstart Guide** (`quickstart.md`) - Developer onboarding for bootloader integration
3. **Update Agent Context** (`.specify/scripts/bash/update-agent-context.sh copilot`) - Add new models to AI context

---

## References

- **Specification**: `specs/011-bootloader-integration/spec.md` (user stories FR-001 to FR-020)
- **Implementation Plan**: `specs/011-bootloader-integration/plan.md` (technical context, project structure)
- **Research**: `specs/011-bootloader-integration/research.md` (architectural decisions)
- **Existing Patterns**: `docs/patterns/profile-management.md` (IProfileBase, StandardProfileManager<T>)
- **Constitution**: `.specify/memory/constitution.md` (Clean Architecture, domain model guidelines)

---

**Data Model Complete**: 9 entities defined with fields, relationships, validation rules, and examples. Ready for contract generation (Phase 1 next step).
