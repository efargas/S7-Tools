# Research: Bootloader Integration

**Feature**: 011-bootloader-integration
**Phase**: Phase 0 (Research)
**Created**: 2025-01-XX
**Status**: Complete

## Overview

This document consolidates architectural research findings for integrating S7-1200 PLC bootloader functionality into S7Tools. All technical unknowns from the specification and planning phases have been resolved through analysis of existing codebase patterns, reference implementation (SiemensS7-Bootloader), and constitutional requirements.

## Research Questions & Findings

### RQ1: Integration Pattern Selection

**Question**: How should the S7-1200 bootloader reference implementation (SiemensS7-Bootloader) be integrated into S7Tools without violating the constitution's zero-modification constraint?

**Decision**: **Adapter Pattern** wrapping the reference implementation without forking or modifying original code.

**Rationale**:
- **Constitutional Constraint**: Constitution v1.2.0 Article I (Clean Architecture) and Additional Constraints require "zero reference code modification" to maintain alignment with upstream security research
- **Maintainability**: Reference code continues to evolve with new exploits and techniques - adapter pattern allows seamless updates by replacing binary assemblies
- **Separation of Concerns**: Adapters isolate reference implementation's communication patterns from S7Tools' Clean Architecture boundaries
- **Testing**: Adapters provide clean seam for mocking in unit tests without coupling to reference implementation details

**Alternatives Considered**:
1. **Fork and Modify** - Rejected: Violates zero-modification constraint, creates maintenance burden, loses upstream improvements
2. **Direct Dependency** - Rejected: Leaks reference code abstractions (ICommunicationChannel, PlcProtocol) across S7Tools layers, violates Clean Architecture
3. **Event-Driven Bridge** - Rejected: Overly complex for synchronous bootloader operations, introduces unnecessary latency

**Implementation Evidence**:
- Adapters: `PlcTransportAdapter`, `PlcProtocolAdapter`, `PlcClientAdapter`, `PayloadProviderAdapter` (4 core adapters)
- Reference assemblies: `SiemensS7-Bootloader.dll` added as NuGet local package or project reference with InternalsVisibleTo
- Interface segregation: S7Tools.Core interfaces (IPlcTransport, IPlcProtocol, IPlcClient) define contracts, adapters implement by delegating to reference types

---

### RQ2: Concurrency Architecture for Multi-Job Execution

**Question**: What concurrency model should orchestrate parallel bootloader job execution while preventing resource conflicts (serial ports, TCP ports, modbus connections)?

**Decision**: **Event-Driven Job Scheduler with Resource Coordinator** using TryAcquire/Release pattern (no thread pools, no nested semaphores).

**Rationale**:
- **Constitutional Requirement**: Article IV (Thread Safety) prohibits nested semaphore acquisitions and mandates "Use IUIThreadService for all UI thread operations"
- **Resource Conflict Prevention**: Serial ports (COM1), TCP ports (socat bridge 10102), modbus connections (power supply 192.168.1.10:502) must be locked per SC-006 "100% conflict prevention"
- **Performance Goal**: SC-002 requires "4+ concurrent jobs on independent hardware" - event-driven scheduler enables parallel execution when resources don't conflict
- **Existing Pattern**: S7Tools already uses Internal Method Pattern for semaphore safety - extend this to IResourceCoordinator service

**Alternatives Considered**:
1. **Thread Pool with Locks** - Rejected: Risk of nested semaphore deadlocks, violates Article IV thread safety patterns
2. **Actor Model (TPL Dataflow)** - Rejected: Overly complex for job count (typically <10 concurrent), introduces new dependency
3. **Simple Queue (Sequential Only)** - Rejected: Fails SC-002 requirement for parallel execution on independent hardware

**Implementation Evidence**:
- `IResourceCoordinator`: Centralized TryAcquire(ResourceKey[]) / Release(ResourceKey[]) with no nested locks
- `JobScheduler`: Event-driven state machine (Created → Queued → Running → Completed/Failed/Canceled) with progress events
- `ResourceKey`: Value object with type (serial/tcp/modbus) and identifier (port path, port number, host:port)
- Integration: `BootloaderService.DumpAsync()` acquires resources before workflow, releases in finally block

---

### RQ3: Job Profile Storage and Persistence

**Question**: How should job profiles (serial + socat + power + memory region + payload configurations) be stored and persisted?

**Decision**: **JSON file-based storage** using existing StandardProfileManager<T> pattern with JobProfile model persisted at `src/resources/JobProfiles/profiles.json`.

**Rationale**:
- **Existing Pattern**: S7Tools already uses StandardProfileManager<T> for SerialPortProfile, SocatProfile, PowerSupplyProfile with JSON persistence
- **Simplicity**: Small dataset (typically <100 job profiles), no complex queries needed, JSON human-readable for debugging
- **Constitutional Alignment**: Article VI (Simplicity) - "Prefer simple, maintainable solutions over complex abstractions" - no database overhead needed
- **File Location Convention**: Follows existing pattern: src/resources/{Category}Profiles/profiles.json

**Alternatives Considered**:
1. **SQLite Database** - Rejected: Overkill for <100 records, adds complexity, violates simplicity principle
2. **XML Configuration** - Rejected: More verbose than JSON, less readable, no advantage for this use case
3. **In-Memory Only** - Rejected: Fails persistence requirement, user loses job profiles on app restart

**Implementation Evidence**:
- `JobProfile : IProfileBase` - Implements existing profile interface with Id, Name, Description, IsDefault, IsReadOnly
- `JobProfileSet` - Aggregates 5 profile references (serial, socat, power, memory, payload) as value object
- `StandardProfileManager<JobProfile>` - Reuses existing thread-safe CRUD with semaphore Internal Method Pattern
- Storage path: Configured via JobManagerOptions.ProfilesPath with default "src/resources/JobProfiles/profiles.json"

---

### RQ4: Progress Reporting and UI Responsiveness

**Question**: How should long-running bootloader operations (5-300 seconds per SC-001) report progress to the UI without blocking and maintain <500ms update latency per SC-008?

**Decision**: **IProgress<T> with IUIThreadService marshaling** using reactive progress events and circular buffer logging.

**Rationale**:
- **Constitutional Requirement**: Article IV mandates "Never block UI thread with I/O operations" and "Use IUIThreadService for all UI thread operations"
- **Existing Infrastructure**: S7Tools.Infrastructure.Logging already has DataStore circular buffer with INotifyCollectionChanged for real-time log display
- **.NET Standard Pattern**: IProgress<T> is idiomatic for async progress reporting with automatic context capture
- **Performance**: Meets SC-008 <500ms latency - progress events throttled in ViewModel with ReactiveUI throttle operators

**Alternatives Considered**:
1. **Direct ObservableCollection Updates** - Rejected: Violates UI thread safety, risks cross-thread collection modification exceptions
2. **SignalR / WebSockets** - Rejected: Overkill for desktop app, unnecessary network layer
3. **Polling Status Endpoint** - Rejected: Introduces latency, wastes CPU cycles, violates reactive architecture

**Implementation Evidence**:
- `BootloaderService.DumpAsync(IProgress<BootloaderProgress> progress)` - Accepts progress reporter
- `BootloaderProgress` - Value type with Stage (string), Percentage (double), CurrentOperation (string)
- `TaskManagerViewModel` - Subscribes to progress events, marshals to UI thread via IUIThreadService, updates Progress property with RaiseAndSetIfChanged
- Logging: All bootloader operations log to ILogger<T> → DataStore → real-time UI display in LogViewer

---

### RQ5: Payload Management and File System Access

**Question**: How should bootloader payloads (stager.bin, dump_mem.bin) be discovered, validated, and loaded from the file system?

**Decision**: **IPayloadProvider abstraction with file system adapter** using configurable base path and payload name conventions.

**Rationale**:
- **Flexibility**: Payload location varies (development: bootloader-payloads/payloads/, production: custom user path)
- **Validation**: Payloads must exist before job execution (FR-007 edge case: missing payload file) - fail fast during job creation
- **Testing**: IPayloadProvider abstraction allows mock payloads in tests without file I/O
- **Constitutional Pattern**: Follows Article I dependency inversion - interface in Core, file system implementation in Infrastructure

**Alternatives Considered**:
1. **Embedded Resources** - Rejected: Payloads change frequently during research, embedding requires recompilation
2. **Hardcoded Paths** - Rejected: Not portable, fails on user machines with different directory structures
3. **Database BLOB Storage** - Rejected: Violates simplicity, payloads are already filesystem artifacts from build process

**Implementation Evidence**:
- `IPayloadProvider` interface: GetStagerAsync() / GetMemoryDumperAsync() → returns byte[]
- `FilePayloadProvider` implementation: Reads from PayloadSetProfile.BasePath + "/stager/stager.bin" convention
- `PayloadSetProfile : IProfileBase` - User-configurable base path with validation (directory exists, contains required files)
- Error handling: Missing payload throws PayloadNotFoundException (custom exception) during job queue validation

---

## Technology Stack Clarifications

### Confirmed Dependencies

All dependencies resolved - **zero "NEEDS CLARIFICATION" markers** in Technical Context:

| Technology | Version | Purpose | Source |
|------------|---------|---------|--------|
| C# | 12.0 | Language features (file-scoped namespaces, record types) | .csproj TargetFramework net8.0 |
| .NET | 8.0 | Runtime and BCL | Existing baseline |
| Avalonia UI | 11.x | Cross-platform UI framework | Existing dependency |
| ReactiveUI | 19.x | MVVM reactive bindings | Existing dependency |
| NModbus4 | Latest | Modbus TCP for power supply control | Existing dependency |
| System.IO.Ports | 8.0 | Serial port communication | Existing dependency |
| xUnit | 2.6 | Test framework | Existing test baseline |
| FluentAssertions | 6.x | Test assertions | Existing test baseline |

### Platform Constraints

- **Development**: Linux (Kali) with .NET 8 SDK - serial ports via /dev/ttyUSB*, socat native
- **Deployment**: Cross-platform (Windows/Linux/macOS) - conditional socat availability checked at runtime
- **Build**: Zero errors, zero warnings (constitutional requirement) - TreatWarningsAsErrors enforced

---

## Performance Validation

### Benchmarking Strategy

| Metric | Target | Measurement Approach | Evidence Location |
|--------|--------|---------------------|-------------------|
| Memory dump time | <5 min for 64KB | Stopwatch in integration tests with real PLC | tests/S7Tools.Tests/Services/Bootloader/PerformanceTests.cs |
| Concurrent jobs | 4+ simultaneous | Parallel test with 4 independent PLCs | tests/S7Tools.Core.Tests/Tasking/ConcurrencyTests.cs |
| UI update latency | <500ms | ReactiveUI throttle with 300ms + manual measurement | benchmarks/S7Tools.Benchmarks/UIResponsivenessTests.cs |
| Resource cleanup | <3s | Stopwatch in ResourceCoordinator tests | tests/S7Tools.Core.Tests/Tasking/ResourceCoordinatorTests.cs |

Performance tests will be added in Phase 2 (implementation) alongside unit tests per Article III (Test-First).

---

## Open Questions (None Remaining)

All technical unknowns from spec.md and plan.md have been researched and resolved:

- ✅ **Integration Pattern**: Adapter pattern selected
- ✅ **Concurrency Model**: Event-driven scheduler with resource coordinator
- ✅ **Persistence**: JSON file-based with StandardProfileManager<T>
- ✅ **Progress Reporting**: IProgress<T> with IUIThreadService
- ✅ **Payload Management**: IPayloadProvider with file system adapter
- ✅ **Technology Stack**: All dependencies confirmed from existing baseline

**Next Phase**: Phase 1 (Design) - Generate data-model.md, contracts/, quickstart.md

---

## References

- **Constitution**: `.specify/memory/constitution.md` v1.2.0 (Articles I-V + constraints)
- **Existing Plans**: `.github/agents/workspace/future-plans/20251009-s7-bootloader-integration-details.md` (interface definitions)
- **Reference Code**: `SiemensS7-Bootloader` repository (adapter wrapper target)
- **Existing Patterns**: `docs/patterns/system-patterns.md` (Internal Method Pattern, StandardProfileManager<T>, IUIThreadService)
- **Specification**: `specs/011-bootloader-integration/spec.md` (user stories, functional requirements, success criteria)
- **Implementation Plan**: `specs/011-bootloader-integration/plan.md` (technical context, constitution check, project structure)

---

**Research Complete**: All architectural decisions documented with rationale and alternatives. Ready for Phase 1 design artifact generation.
