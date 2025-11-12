# Feature Specification: Bootloader Integration with Job-Based Task Execution

**Feature Branch**: `011-bootloader-integration`
**Created**: 2025-11-12
**Status**: Draft
**Input**: User description: "bootloader functionality real integration integration, using jobs for passing the payloads, arguments, to the tasks"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Execute Single Memory Dump Job (Priority: P1)

A security researcher needs to extract firmware from a single S7-1200 PLC for analysis. They configure a job with serial port settings, network bridge configuration, power supply control, memory region selection, and payload paths, then execute it through the Task Manager to obtain a complete memory dump file.

**Why this priority**: This is the core value proposition - enabling automated, systematic PLC memory extraction. Without this, the entire bootloader integration feature has no practical use. This story delivers immediate, measurable value to security researchers.

**Independent Test**: Can be fully tested by creating a job profile with all required configurations (serial, socat, power, memory region, payloads), executing it via Task Manager, and verifying a binary dump file is created at the specified output path with correct size matching the memory region configuration.

**Acceptance Scenarios**:

1. **Given** a user has configured a job profile with serial port `/dev/ttyUSB0` at 115200 baud, socat bridge on port 20000, power supply at `192.168.1.100:502`, memory region `0x20000000` to `0x20010000` (64KB), and payload base path `bootloader-payloads/payloads/`, **When** the user executes the job from Task Manager, **Then** the system configures the serial port, launches the socat bridge, power cycles the PLC, performs bootloader handshake, installs stager and dumper payloads, extracts 64KB of memory, saves it to the configured output path as `dump-{jobId}.bin`, and updates job status to "Completed" with the output file path

2. **Given** a job is executing and reaches the "Installing Stager" phase, **When** the user views the Task Manager, **Then** they see real-time progress updates showing current stage (e.g., "handshake: 20%", "stager: 30%", "dump: 50%") and can monitor the operation's progress through completion

3. **Given** a job execution fails during the handshake phase due to PLC communication timeout, **When** the failure occurs, **Then** the system marks the job as "Failed", logs the specific error with context (stage, timeout duration), releases all acquired resources (serial port, TCP port, modbus connection), and displays a user-friendly error message indicating the failure point

---

### User Story 2 - Queue Multiple Jobs with Resource Coordination (Priority: P2)

A research team needs to dump memory from multiple S7-1200 PLCs connected to different serial ports. They create multiple jobs targeting different hardware resources, and the Task Manager automatically coordinates parallel execution for jobs using different resources while queuing jobs that would conflict.

**Why this priority**: Enables efficient batch operations and maximizes hardware utilization. This dramatically improves productivity for teams working with multiple devices, but depends on P1 single job execution working correctly first.

**Independent Test**: Can be tested by creating 3 jobs: Job A uses `/dev/ttyUSB0` and port 20000, Job B uses `/dev/ttyUSB1` and port 20001, Job C uses `/dev/ttyUSB0` and port 20002. When all three are queued, Jobs A and B should execute in parallel (different serial ports), while Job C waits until Job A completes (same serial port).

**Acceptance Scenarios**:

1. **Given** two jobs are queued where Job 1 uses serial port `/dev/ttyUSB0` on TCP port 20000 and Job 2 uses serial port `/dev/ttyUSB1` on TCP port 20001, **When** the Task Manager processes the queue, **Then** both jobs execute in parallel since they use disjoint resources (different serial ports and TCP ports)

2. **Given** two jobs are queued where both target the same serial port `/dev/ttyUSB0` but different TCP ports, **When** the first job is running, **Then** the Task Manager holds the second job in "Queued" state until the first job completes and releases the serial port resource

3. **Given** a job is running and holds resources (serial: `/dev/ttyUSB0`, tcp: `127.0.0.1:20000`, modbus: `192.168.1.100:502:1`), **When** the job completes or fails, **Then** all three resources are released atomically, allowing queued jobs requiring any of these resources to proceed

---

### User Story 3 - Create and Manage Job Profiles (Priority: P2)

A user needs to create reusable job configurations for different PLC setups (production vs. development environments, different memory regions, different payload versions). They create job profiles with descriptive names, configure all parameters, save them for later execution, and can duplicate/edit existing profiles.

**Why this priority**: Enables workflow efficiency and reduces configuration errors through reusability. Important for production use but not critical for initial bootloader integration validation.

**Independent Test**: Can be tested by creating a job profile named "S7-1200 Boot Sector Dump" with specific configurations, saving it, verifying it appears in the job list, duplicating it as "S7-1200 Full Memory Dump" with different memory region, and executing both profiles to verify they use correct configurations.

**Acceptance Scenarios**:

1. **Given** a user opens the Job Wizard, **When** they configure serial port profile, socat profile, power supply profile, select memory region (0x20000000-0x20001000 for 4KB boot sector), set payload base path to `bootloader-payloads/payloads/`, and output path to `~/dumps/`, **Then** the system creates a job profile, assigns a unique ID, validates all required fields are populated, and saves it to `src/resources/JobProfiles/profiles.json`

2. **Given** an existing job profile named "Development PLC Dump", **When** the user selects "Duplicate" and provides the new name "Production PLC Dump", **Then** the system creates a new profile with a new unique ID, copies all configuration from the source profile, allows the user to modify any settings (e.g., change serial port from `/dev/ttyUSB0` to `/dev/ttyUSB1`), and saves both profiles independently

3. **Given** a job profile with memory region configuration of 64KB starting at 0x20000000, **When** the user executes the job, **Then** the system passes the exact start address (0x20000000) and length (0x10000) to the memory dumper payload and verifies the returned dump file size matches the configured region size

---

### User Story 4 - Monitor Job Execution with Real-Time Logs (Priority: P3)

A user executes a complex bootloader operation and needs to monitor detailed progress and troubleshoot issues. They view the Task Manager UI which shows real-time stage updates (socat setup, power cycle, handshake, stager installation, memory dump, teardown) with percentage progress, and can access detailed logs for each operation.

**Why this priority**: Improves user experience and debugging capability but not essential for core functionality. Users can verify success/failure without real-time monitoring in initial implementation.

**Independent Test**: Can be tested by starting a job execution and observing the Task Manager UI updates in real-time showing: "Socat Setup (5%)" → "Power Cycle (10%)" → "Handshake (20%)" → "Stager Install (30%)" → "Memory Dump (50%)" → "Teardown (95%)" → "Completed (100%)", with detailed logs accessible via the logging panel.

**Acceptance Scenarios**:

1. **Given** a job is executing the bootloader handshake phase, **When** the handshake completes successfully and retrieves the bootloader version "1.0.0", **Then** the Task Manager displays stage update "Handshake: 20%" and the log panel shows structured log entry: "Bootloader handshake successful. Version: 1.0.0" with timestamp and debug-level details

2. **Given** a job execution encounters a timeout during stager installation (e.g., PLC takes longer than expected to respond), **When** the timeout occurs, **Then** the system logs a warning with context (operation: "InstallStager", timeout: "30s", attempt: "1/3"), automatically retries with exponential backoff, and updates the UI to show "Stager Install: Retrying (attempt 2/3)"

3. **Given** multiple jobs are executing simultaneously, **When** the user views the Task Manager, **Then** they see a list showing each job's current state, progress percentage, current operation stage, and can expand any job to view its detailed execution log separate from other jobs

---

### Edge Cases

- What happens when the serial port is already in use by another process (e.g., another instance of S7Tools or external terminal program)?
  - System attempts to acquire the serial port resource lock; if already held, queues the job and displays status "Waiting for resource: serial:/dev/ttyUSB0"

- How does the system handle PLC power cycle failures (e.g., modbus connection timeout, power supply not responding)?
  - System logs error with full context (modbus host/port/coil, timeout duration), marks job as "Failed", releases all resources, and suggests troubleshooting steps in the status message (e.g., "Power supply not reachable at 192.168.1.100:502. Check network connectivity.")

- What happens when payload files are missing or corrupted at the configured base path?
  - System validates payload existence during job creation/validation phase before execution begins; if missing during execution, fails immediately with clear error "Payload not found: {basePath}/stager.bin" and marks job as "Failed" without attempting hardware operations

- How does the system handle partial memory dumps when the PLC disconnects mid-operation?
  - System detects disconnection during DumpMemoryAsync operation, logs the failure with bytes received vs. expected, saves partial dump with suffix `.partial`, marks job as "Failed", includes diagnostic info about which memory range was successfully dumped

- What happens when two jobs are configured with the same TCP port for socat bridge?
  - Resource coordinator detects port conflict during resource acquisition attempt, prevents simultaneous execution, queues the second job with status "Waiting for resource: tcp:127.0.0.1:20000"

- How does the system handle job cancellation requests during active hardware operations?
  - User can request cancellation via UI, system propagates CancellationToken through all async operations, cleans up resources (stops socat bridge, releases serial port, logs cancellation), marks job as "Canceled" with stage info (e.g., "Canceled during: Memory Dump at 45%")

- What happens when output directory path is not writable or disk is full?
  - System validates output path writability before job execution begins; if validation fails, prevents job start with error "Output path not writable: ~/dumps/. Check permissions." If disk fills during execution, fails gracefully with error "Failed to write dump file: Disk full" and includes partial dump size in logs

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST define core bootloader service contracts (IPlcClient, IPlcProtocol, IPlcTransport, IPayloadProvider, IPowerSupplyService, ISocatService, IBootloaderService) in the Core domain layer with no external dependencies
- **FR-002**: System MUST implement adapter pattern wrappers (PlcTransportAdapter, PlcProtocolAdapter, PlcClientAdapter, PayloadProviderAdapter, PowerSupplyAdapter, SocatAdapter) that delegate to the reference SiemensS7-Bootloader implementation without modifying reference behavior
- **FR-003**: System MUST implement IBootloaderService orchestration that coordinates the complete bootloader workflow: socat bridge setup → power cycle → handshake → stager installation → memory dump → resource teardown
- **FR-004**: System MUST implement IJobScheduler that maintains a queue of pending jobs, executes jobs when resources become available, and emits JobStateChanged events for UI updates
- **FR-005**: System MUST implement IResourceCoordinator that prevents resource conflicts by tracking acquired resources (serial ports, TCP ports, modbus connections) and blocking jobs that would create conflicts
- **FR-006**: System MUST define Job domain model with properties: unique ID (Guid), name, list of required resources (ResourceKey[]), profile set (JobProfileSet), current state (JobState enum: Created/Queued/Running/Completed/Failed/Canceled), and creation timestamp
- **FR-007**: System MUST define JobProfileSet containing references to SerialProfileRef (device, baud, parity, data bits, stop bits), SocatProfileRef (TCP port, ephemeral flag), PowerProfileRef (modbus host/port/coil, delay), MemoryRegionProfile (start address, length), PayloadSetProfile (base path), and output path for dump files
- **FR-008**: System MUST persist job profiles to `src/resources/JobProfiles/profiles.json` using the Options pattern and ensure file exists or create it with empty array `[]` on first run
- **FR-009**: System MUST register all services in ServiceCollectionExtensions.cs with appropriate lifetimes: IResourceCoordinator (Singleton), IJobScheduler (Singleton), IBootloaderService (Transient), adapters (Transient), and factory for IPlcClient creation from JobProfileSet
- **FR-010**: System MUST implement TaskManagerViewModel with observable collection of jobs, commands for creating/queuing jobs, subscription to JobScheduler events for real-time UI updates, and proper disposal of event subscriptions
- **FR-011**: System MUST save memory dump output to configured path with filename format `dump-{jobId}.bin` and create output directory if it doesn't exist
- **FR-012**: System MUST report progress through IProgress<(string stage, double percent)> callback during bootloader operations with standard stages: "socat" (5%), "power" (10%), "handshake" (20%), "stager" (30%), "dump" (50%), "teardown" (95%)
- **FR-013**: System MUST handle job execution failures gracefully by logging errors with context, updating job state to Failed, releasing all acquired resources, and providing user-friendly error messages
- **FR-014**: System MUST support parallel execution of jobs with disjoint resource sets (e.g., Job A uses /dev/ttyUSB0 and Job B uses /dev/ttyUSB1 can run simultaneously)
- **FR-015**: System MUST queue jobs that would conflict with currently running jobs and automatically start them when resources become available
- **FR-016**: System MUST use ConfigureAwait(false) in all service layer async operations to prevent context capture and follow established thread safety patterns
- **FR-017**: System MUST log all bootloader operations using structured logging (ILogger<T>) with appropriate log levels (Information for progress, Warning for retries, Error for failures) and include contextual properties (jobId, stage, resource identifiers)
- **FR-018**: System MUST validate job profiles before execution to ensure: serial port exists and is accessible, TCP port is available, payload files exist at base path, output directory is writable, memory region parameters are valid (start address + length doesn't overflow)
- **FR-019**: System MUST support job cancellation via CancellationToken propagation through all async operations, ensuring proper cleanup of hardware resources and socat bridge processes
- **FR-020**: System MUST create Task Manager UI in the categorized architecture under ViewModels/Tasks/ and Views/Tasks/ following established MVVM patterns with ReactiveUI

### Key Entities *(include if feature involves data)*

- **Job**: Represents a bootloader memory dump operation with unique identifier (Guid), user-friendly name, list of required resources for conflict detection, complete profile set containing all configuration, current execution state (Created/Queued/Running/Completed/Failed/Canceled), creation timestamp, and optional completion timestamp
- **JobProfileSet**: Aggregates all configuration needed for bootloader execution including serial port settings (device path, baud rate, parity, data bits, stop bits), socat bridge settings (TCP port number, ephemeral flag), power supply control (modbus host/port/coil address, power cycle delay), memory region definition (start address, byte length), payload file locations (base directory path), and output file path for dump artifact
- **ResourceKey**: Value object identifying a lockable resource with kind discriminator ("serial", "tcp", "modbus") and unique identifier (e.g., device path for serial, "host:port" for TCP/modbus), used by ResourceCoordinator for conflict detection
- **JobState**: Enumeration defining job lifecycle states - Created (initial state before queuing), Queued (waiting for resources), Running (actively executing), Completed (successful finish with output file), Failed (error occurred with logged cause), Canceled (user-requested termination)
- **SerialProfileRef**: Configuration for serial port communication containing device path (e.g., /dev/ttyUSB0), baud rate (e.g., 115200), parity setting (None/Even/Odd), data bits count (7/8), and stop bits (One/Two)
- **SocatProfileRef**: Configuration for TCP/serial bridge containing target TCP port number for socat listener and ephemeral flag indicating if port should be auto-selected
- **PowerProfileRef**: Configuration for PLC power control via modbus containing power supply host address, modbus TCP port, relay coil number, and power cycle delay in seconds between OFF and ON
- **MemoryRegionProfile**: Defines memory extraction range with start address (uint) and length in bytes (uint), used to configure the memory dumper payload
- **PayloadSetProfile**: Contains base directory path where bootloader binary payloads (stager.bin, dump_mem.bin) are located, used by PayloadProvider to load files

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can successfully execute a complete PLC memory dump operation from job creation to output file generation in under 5 minutes (excluding hardware power cycle time) for a 64KB memory region
- **SC-002**: System supports parallel execution of at least 4 concurrent jobs targeting different hardware resources (4 different serial ports) without resource conflicts or performance degradation
- **SC-003**: Job queue automatically processes pending jobs when resources become available, with zero manual intervention required - 100% of queued jobs start execution within 5 seconds of resource release
- **SC-004**: All bootloader operations report progress updates at minimum 6 distinct stages (socat, power, handshake, stager, dump, teardown) visible in real-time through Task Manager UI
- **SC-005**: 95% of job execution failures provide diagnostic error messages that clearly identify the failure stage and cause (e.g., "Handshake failed: Timeout waiting for bootloader response after 30s")
- **SC-006**: System prevents 100% of resource conflicts - no two jobs can simultaneously access the same serial port, TCP port, or modbus connection
- **SC-007**: Memory dump output files match configured memory region sizes with zero byte discrepancy - a 64KB (65536 bytes) region produces exactly a 65536-byte dump file
- **SC-008**: Task Manager UI reflects job state changes (Created → Queued → Running → Completed/Failed) within 500ms of actual state transition in the scheduler
- **SC-009**: System completes resource cleanup within 3 seconds after job completion or failure, releasing all locks and ensuring serial ports and TCP ports are available for new jobs
- **SC-010**: Job profile validation catches 100% of configuration errors (missing payloads, invalid paths, inaccessible ports) before hardware operations begin, preventing wasted power cycles

### Constitution Compliance

This feature affects multiple core principles and cross-cutting concerns. Constitution compliance check against v1.2.0:

**Article I: Clean Architecture & Layered Boundaries** ✅ COMPLIANT
- All bootloader service contracts (IPlcClient, IBootloaderService, IJobScheduler, IResourceCoordinator) defined in S7Tools.Core/Services/Interfaces with zero external dependencies
- Job domain models (Job, JobProfileSet, ResourceKey, JobState) located in S7Tools.Core/Models/Jobs following established categorization
- Adapter implementations reside in S7Tools/Services/Adapters (infrastructure layer) and depend only on Core interfaces
- ViewModels organized under S7Tools.ViewModels.Tasks/ and Views under S7Tools.Views.Tasks/ following the 9-category pattern
- Public APIs in Core are minimal (10 interfaces, 8 models) and well-documented with XML comments

**Article II: MVVM (ReactiveUI) & UI Contracts** ✅ COMPLIANT
- TaskManagerViewModel inherits from ReactiveObject and uses RaiseAndSetIfChanged for all observable properties (Jobs collection, selected job, status messages)
- All commands (CreateJobCommand, CancelJobCommand, RefreshCommand) are ReactiveCommand<TInput, TOutput> with explicit CanExecute observables
- UI composition contracts (IJobScheduler events) defined in Core; View-specific behavior (DataGrid styling, progress bars) confined to Views
- No code-behind logic - all presentation logic lives in ViewModels with proper DI and testability

**Article III: Test-First Quality Gates** ✅ COMPLIANT WITH REQUIREMENTS
- Comprehensive unit test suite planned across 3 test projects:
  - S7Tools.Core.Tests/Tasking/ResourceCoordinatorTests.cs - resource locking and conflict detection
  - S7Tools.Core.Tests/Tasking/SchedulerTests.cs - queue management, parallel execution, state transitions
  - S7Tools.Tests/Services/Bootloader/BootloaderServiceTests.cs - orchestration flow with fakes
- All tests follow AAA pattern and async tests use async Task (no .Result/.Wait)
- Integration smoke test for end-to-end memory dump flow with reference hardware
- Tests MUST pass before merging (builds on current 99.7% pass rate baseline)
- Target: Add 40+ new tests (ResourceCoordinator: 10, Scheduler: 15, BootloaderService: 10, Adapters: 5+)

**Article IV: Thread Safety & Concurrency Contracts** ✅ COMPLIANT
- IUIThreadService used for all Task Manager UI updates from background job execution threads
- ResourceCoordinator implements explicit locking with TryAcquire/Release pattern preventing deadlocks
- No nested semaphore acquisitions - internal method pattern not applicable as scheduler uses event-driven model
- All service async methods use ConfigureAwait(false) to prevent context capture
- CancellationToken propagated through entire operation chain for graceful shutdown
- Proper resource cleanup in finally blocks ensures locks always released even on exceptions

**Article V: Observability, Versioning & Simplicity** ✅ COMPLIANT
- Structured logging throughout with ILogger<T> and contextual properties (jobId, stage, resourceKeys)
- In-memory DataStore integration for real-time log viewing in UI
- Job profiles use JSON serialization with schema evolution support (can add fields without breaking existing profiles)
- Simple, explicit design - no over-engineered abstractions, YAGNI principle followed
- Progress reporting through standard IProgress<T> .NET pattern

**Additional Constraints** ✅ COMPLIANT
- .NET 8 runtime with Avalonia UI and ReactiveUI framework
- Service registration in ServiceCollectionExtensions.cs (9 new registrations: adapters, scheduler, coordinator, bootloader service)
- Job profiles persist to src/resources/JobProfiles/profiles.json following Options pattern (FR-008)
- No nested semaphore acquisitions - uses event-driven coordination instead
- Builds on existing StandardProfileManager<T> pattern for job profile CRUD operations
- Resource coordination uses IResourceCoordinator for conflict detection (extends existing pattern)

**Quality Gates Impact**:
- No VS Code tasks - all operations use terminal commands (dotnet build, dotnet test, dotnet run)
- EditorConfig compliance required before commit
- Build must maintain 0 errors, 0 warnings standard
- Memory Bank updates required: systemPatterns.md sections on Job Scheduling, Resource Coordination, Bootloader Orchestration

**Migration Path**:
- Existing job creation UI (Job Wizard) extends to support payload and memory region configuration
- Existing profile management services (ISerialPortService, ISocatService, IPowerSupplyService) wrapped by adapters
- No breaking changes to existing APIs - purely additive feature

**Mitigations**: None required - feature is fully compliant with constitutional principles through design.

