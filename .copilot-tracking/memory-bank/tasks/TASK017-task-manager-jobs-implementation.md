# [TASK017] - Task Manager and Jobs Implementation

**Status:** In Progress
**Added:** 2025-01-17
**Updated:** 2025-10-17
**Priority:** High
**Complexity:** High

## Summary

Implement the core S7Tools functionality: Task Manager and Jobs system for automated S7-1200 PLC memory dumping operations. This includes VSCode-style activity bar navigation, job creation/management, task scheduling, and parallel execution with resource coordination.

## Context & Motivation

S7Tools needs its primary functionality: orchestrating automated memory dumps of S7-1200 PLCs with compatible bootloader versions. The system must:

1. **Manage Jobs** - Create, modify, save, delete, and template-based job configurations
2. **Schedule Tasks** - Queue, schedule, and execute tasks with resource coordination
3. **Coordinate Resources** - Handle parallel execution when resources don't conflict
4. **Integrate Bootloader Logic** - Use existing S7 bootloader integration for memory operations

## Architecture Overview

### Activity Bar Structure
```
Activity Bar (Primary Navigation):
├── 🎯 Task Manager (Main functionality)
├── 📋 Jobs Management
├── ⚙️ Settings (existing)
└── 📊 Logs (existing)
```

### Side Panel Groups (Collapsible)
```
Task Manager Activity:
├── 📝 Created Jobs
├── ⏰ Scheduled Tasks
├── 📤 Queued Tasks
├── ✅ Finished Tasks
└── 🔄 Active Tasks

Jobs Activity:
├── 💼 All Jobs
├── 📋 Job Templates
└── 🗂️ Job Categories
```

## Implementation Plan

### Phase 1: Core Domain Models & Interfaces (8-12 hours)

#### 1.1 Job Domain Models
- **Job** - Complete job configuration with GUID, name, profiles, timings
- **MemoryRegionProfile** - Start address, size, output path configuration
- **JobExecutionContext** - Runtime state and progress tracking
- **TaskState** - Enumeration for task lifecycle states

#### 1.2 Core Service Interfaces
- **IJobManager** - CRUD operations for job configurations
- **ITaskScheduler** - Task queuing, scheduling, and execution coordination
- **IResourceCoordinator** - Resource conflict detection and allocation
- **IBootloaderOrchestrator** - S7-1200 bootloader operation sequence

#### 1.3 Extended Models
```csharp
public class Job : IProfileBase
{
    public Guid JobId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    // Profile References
    public int SerialProfileId { get; set; }
    public int SocatProfileId { get; set; }
    public int PowerSupplyProfileId { get; set; }
    public int MemoryRegionProfileId { get; set; }

    // Timing Configuration
    public TimeSpan PowerOnTime { get; set; }
    public TimeSpan PowerOffDelay { get; set; }

    // Output Configuration
    public string OutputPath { get; set; }
    public bool UseDefaultPath { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsTemplate { get; set; }
}

public class TaskExecution
{
    public Guid TaskId { get; set; }
    public Guid JobId { get; set; }
    public TaskState State { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Progress { get; set; }
}

public enum TaskState
{
    Created,
    Queued,
    Scheduled,
    Running,
    Completed,
    Failed,
    Cancelled,
    Paused
}
```

### Phase 2: Service Implementations (10-14 hours)

#### 2.1 Job Management Service
```csharp
public class JobManager : StandardProfileManager<Job>, IJobManager
{
    Task<ValidationResult> ValidateJobAsync(Job job);
    Task<Job> CreateFromTemplateAsync(Guid templateId, string newName);
    Task<IEnumerable<Job>> GetTemplatesAsync();
    Task<bool> SetAsTemplateAsync(Guid jobId, bool isTemplate);
}
```

#### 2.2 Resource Coordinator
```csharp
public class ResourceCoordinator : IResourceCoordinator
{
    Task<ResourceLockResult> TryAcquireResourcesAsync(Job job);
    Task ReleaseResourcesAsync(Guid taskId);
    Task<bool> HasResourceConflictAsync(Job job1, Job job2);
    Task<IEnumerable<string>> GetAvailableResourcesAsync();
}
```

#### 2.3 Task Scheduler
```csharp
public class TaskScheduler : ITaskScheduler
{
    Task<Guid> ScheduleJobAsync(Guid jobId, DateTime? scheduledTime = null);
    Task<bool> CancelTaskAsync(Guid taskId);
    Task<bool> PauseTaskAsync(Guid taskId);
    Task<bool> ResumeTaskAsync(Guid taskId);
    Task<IEnumerable<TaskExecution>> GetTasksByStateAsync(TaskState state);
}
```

### Phase 3: Bootloader Integration Enhancement (8-10 hours)

#### 3.1 Bootloader Orchestration Service
```csharp
public class BootloaderOrchestrator : IBootloaderOrchestrator
{
    Task<OperationResult> ExecuteMemoryDumpAsync(Job job, IProgress<DumpProgress> progress);
    Task<bool> ValidateBootloaderCompatibilityAsync(string serialDevice);
    Task<VersionInfo> GetBootloaderVersionAsync(string serialDevice);
}
```

#### 3.2 Memory Dump Process Steps
1. **Serial Configuration** - Apply stty settings using serial profile
2. **Socat Server Start** - Launch socat with conflict checking
3. **Modbus Connection** - Establish and maintain power supply connection
4. **Power Cycle** - OFF → Wait → ON → Wait for PLC boot
5. **Bootloader Entry** - OFF → Wait → ON (special mode entry)
6. **Handshaking** - Establish bootloader communication
7. **Version Check** - Verify bootloader compatibility
8. **Stager Installation** - Install payload stager (with optional user confirmation)
9. **Dumper Installation** - Install memory dumper payload
10. **Memory Dump** - Execute memory region dump with progress tracking

### Phase 4: UI Implementation (12-16 hours)

#### 4.1 Activity Bar Enhancement
- Add Task Manager and Jobs activities to existing activity bar
- Implement activity switching with state preservation
- Add visual indicators for active tasks and job counts

#### 4.2 Task Manager ViewModels
```csharp
public class TaskManagerViewModel : ViewModelBase
{
    public ObservableCollection<TaskExecution> CreatedJobs { get; }
    public ObservableCollection<TaskExecution> ScheduledTasks { get; }
    public ObservableCollection<TaskExecution> QueuedTasks { get; }
    public ObservableCollection<TaskExecution> ActiveTasks { get; }
    public ObservableCollection<TaskExecution> FinishedTasks { get; }

    public ReactiveCommand<Unit, Unit> StartTaskCommand { get; }
    public ReactiveCommand<Unit, Unit> StopTaskCommand { get; }
    public ReactiveCommand<Unit, Unit> ScheduleTaskCommand { get; }
    public ReactiveCommand<Unit, Unit> EnqueueTaskCommand { get; }
}
```

#### 4.3 Jobs Management ViewModels
```csharp
public class JobsManagementViewModel : ProfileManagementViewModelBase<Job>
{
    public ObservableCollection<Job> AllJobs { get; }
    public ObservableCollection<Job> JobTemplates { get; }

    public ReactiveCommand<Unit, Unit> CreateFromTemplateCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAsTemplateCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportJobCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportJobCommand { get; }
}
```

#### 4.4 Job Configuration Dialog
- **Job Details** - Name, description, template options
- **Profile Selection** - Dropdowns for Serial, Socat, PowerSupply, Memory Region profiles
- **Timing Configuration** - Power on/off timing controls
- **Output Settings** - Path selection, naming patterns
- **Validation** - Real-time validation with error display

### Phase 5: Advanced Features (8-12 hours)

#### 5.1 Parallel Execution Engine
- Resource conflict detection and queuing
- Parallel task execution when resources are disjoint
- Load balancing and resource optimization
- Progress aggregation and status reporting

#### 5.2 Task Monitoring & Logging
- Real-time progress indicators with percentage completion
- Detailed operation logging integrated with existing log system
- Error recovery and retry mechanisms
- Task history and execution statistics

#### 5.3 Template System
- Job templates for common memory dump configurations
- Template inheritance and customization
- Built-in templates for standard S7-1200 configurations
- Template sharing and import/export

### Phase 6: Testing & Integration (10-12 hours)

#### 6.1 Unit Testing
- Job management operations (CRUD, validation, templates)
- Resource coordination and conflict detection
- Task scheduling and state management
- Bootloader orchestration sequences

#### 6.2 Integration Testing
- End-to-end memory dump simulation
- Parallel execution scenarios
- Resource conflict resolution
- Error handling and recovery

#### 6.3 UI Testing
- ViewModel behavior validation
- Command execution and state updates
- Dialog validation and error handling
- Activity switching and state preservation

## Technical Specifications

### Resource Management
```csharp
public class ResourceLock
{
    public string SerialDevice { get; set; }
    public int TcpPort { get; set; }
    public string PowerSupplyDevice { get; set; }
    public int PowerSupplyOutput { get; set; }
    public Guid TaskId { get; set; }
    public DateTime AcquiredAt { get; set; }
}
```

### Progress Tracking
```csharp
public class DumpProgress
{
    public int TotalBytes { get; set; }
    public int ProcessedBytes { get; set; }
    public double PercentComplete => (double)ProcessedBytes / TotalBytes * 100;
    public string CurrentOperation { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
}
```

## Success Criteria

### Functional Requirements
1. **Job Creation** - Create jobs with complete profile selection and validation
2. **Task Execution** - Successfully execute memory dump operations end-to-end
3. **Parallel Processing** - Run multiple jobs simultaneously when resources don't conflict
4. **Resource Management** - Prevent resource conflicts and queue conflicting tasks
5. **Progress Monitoring** - Real-time progress updates and detailed logging

### Technical Requirements
1. **Clean Architecture** - Maintain separation of concerns and dependency flow
2. **Error Handling** - Comprehensive error handling with domain-specific exceptions
3. **Performance** - Responsive UI during long-running operations
4. **Testing** - Comprehensive test coverage for all components
5. **Documentation** - Complete XML documentation for public APIs

## Risks & Mitigation

### High Risks
1. **Hardware Integration Complexity** - Bootloader timing and communication sensitivity
   - *Mitigation*: Extensive testing with configurable timeouts and retry logic

2. **Resource Contention** - Complex resource sharing scenarios
   - *Mitigation*: Robust resource coordinator with clear conflict resolution

3. **UI Responsiveness** - Long-running operations blocking UI
   - *Mitigation*: Proper async/await patterns with cancellation support

### Medium Risks
1. **State Management Complexity** - Multiple task states and transitions
   - *Mitigation*: Clear state machine implementation with validation

2. **Configuration Validation** - Complex interdependencies between profiles
   - *Mitigation*: Comprehensive validation service with clear error messages

## Dependencies

### Technical Dependencies
- Existing bootloader integration in S7Tools.Core
- Profile management system (Serial, Socat, PowerSupply)
- Logging infrastructure and UI framework
- Reference bootloader code for operation sequences

### Business Dependencies
- Hardware availability for testing (S7-1200 PLC, serial devices, power supplies)
- Bootloader firmware compatibility verification
- User acceptance testing for workflow validation

## Acceptance Criteria

### Phase 1 Completion
- [ ] All domain models implemented with proper validation
- [ ] Core service interfaces defined with comprehensive contracts
- [ ] Models integrated with existing profile management system

### Phase 2 Completion
- [ ] Job manager with full CRUD operations and template support
- [ ] Resource coordinator with conflict detection and resolution
- [ ] Task scheduler with queuing and state management

### Phase 3 Completion
- [ ] Bootloader orchestrator with complete operation sequence
- [ ] Integration with existing S7 bootloader components
- [ ] Error handling and progress reporting

### Phase 4 Completion
- [ ] Complete UI implementation with VSCode-style navigation
- [ ] Responsive ViewModels with proper command handling
- [ ] Job configuration dialogs with validation

### Phase 5 Completion
- [ ] Parallel execution engine with resource coordination
- [ ] Real-time monitoring and progress tracking
- [ ] Template system with import/export capabilities

### Phase 6 Completion
- [ ] Comprehensive test suite with >90% coverage
- [ ] End-to-end integration testing
- [ ] Performance validation and optimization

## Verification Plan

### Manual Testing
1. **Basic Job Flow** - Create job, configure profiles, execute task
2. **Parallel Execution** - Run multiple jobs with different resources
3. **Resource Conflicts** - Verify proper queuing when resources conflict
4. **Error Scenarios** - Test error handling and recovery
5. **Template Operations** - Create, use, and manage job templates

### Automated Testing
1. **Unit Tests** - All service implementations and domain models
2. **Integration Tests** - End-to-end workflow simulation
3. **UI Tests** - ViewModel behavior and command execution
4. **Performance Tests** - Resource usage and response times

## Timeline Estimate

**Total Effort:** 56-76 hours
**Recommended Sprints:** 4-5 sprints of 2 weeks each
**Dependencies:** Sequential phases with some overlap possible

### Sprint Breakdown
- **Sprint 1:** Phase 1 + Phase 2 start (Domain models + Service foundations)
- **Sprint 2:** Phase 2 + Phase 3 (Service implementations + Bootloader integration)
- **Sprint 3:** Phase 4 (UI implementation)
- **Sprint 4:** Phase 5 (Advanced features)
- **Sprint 5:** Phase 6 (Testing and integration)

This implementation will establish S7Tools as a complete PLC memory dumping solution with professional-grade job management and task execution capabilities.

## Progress Log

### 2025-10-17
- ✅ **Phase 1 Complete**: Core domain models implemented
  - Created `JobProfile` class implementing `IProfileBase` for unified profile management
    - Template support with `CreateFromTemplateAsync()` and `SetAsTemplateAsync()` operations
    - Profile reference validation (serial, socat, power supply profiles)
    - Memory region configuration with address/size validation
    - Output path management with file naming patterns
  - Created `TaskExecution` class for tracking task runtime state and progress
    - 8-state lifecycle management (NotStarted → Completed/Failed/Canceled)
    - Real-time progress tracking with percentage and status messages
    - Performance statistics (execution time, throughput, resource usage)
    - Error tracking and resource cleanup capabilities
  - Enhanced enumerations: `TaskState` (8 states) and `TaskPriority` (4 levels)
  - Created service interfaces: `IJobManager` (extends `IProfileManager<JobProfile>`) and enhanced `ITaskScheduler`

- ✅ **Phase 2 Complete**: Service implementations created
  - Implemented `JobManager` extending `StandardProfileManager<JobProfile>`
    - Template management (create from template, set as template operations)
    - Comprehensive validation pipeline (profile references, memory regions, output paths)
    - Resource availability checking with coordination
    - Integration with existing profile management patterns
  - Implemented `EnhancedTaskScheduler` with advanced capabilities
    - Priority-based task queuing and parallel execution engine
    - Resource coordination with conflict detection and resolution
    - Real-time statistics tracking (tasks by state, execution times, throughput)
    - Task lifecycle management (pause, resume, restart, cancel operations)
    - Timer-based processing with maintenance operations
  - Achieved clean build status: 0 errors, 0 warnings
  - Seamless integration with existing S7Tools architecture and patterns

- 🔄 **Phase 3 In Progress**: Bootloader integration enhancement
  - **Analysis**: Current bootloader service already provides complete dump operations with proper progress reporting
  - **Enhancement Needed**: Integration with new TaskExecution model for detailed progress tracking
  - **Enhancement Needed**: Better error recovery and retry mechanisms for bootloader failures
  - **Enhancement Needed**: Comprehensive operation logging for diagnostics and monitoring

### Technical Achievements

**Architecture Excellence**:
- ✅ Maintained Clean Architecture with proper dependency flow
- ✅ Followed established MVVM and ReactiveUI patterns
- ✅ Integrated seamlessly with existing profile management system
- ✅ Implemented comprehensive logging and error handling

**Service Layer**:
- ✅ Job management with CRUD operations, templates, and validation
- ✅ Enhanced task scheduling with priority queuing and resource coordination
- ✅ Resource conflict detection and parallel execution engine
- ✅ Statistics tracking and performance monitoring

**Domain Models**:
- ✅ Rich domain models with proper business logic and validation
- ✅ Clean separation between configuration (JobProfile) and execution (TaskExecution)
- ✅ Resource management with proper locking and coordination
- ✅ Template system for job reuse and standardization

### Next Steps

**Phase 3 - Bootloader Integration** (Current):
- Enhance progress reporting in bootloader operations
- Add error recovery and retry mechanisms
- Integrate TaskExecution progress tracking
- Add comprehensive operation logging

**Phase 4 - UI Implementation** (Next):
- Create Task Manager and Jobs Management ViewModels
- Implement VSCode-style activity bar integration
- Add job configuration dialogs with validation
- Create real-time task monitoring views

**Phase 5 - Advanced Features**:
- Implement job templates import/export
- Add scheduled task execution
- Create task monitoring dashboard
- Add performance analytics

The foundation is now solid with excellent service implementations and domain models. The system is ready for UI integration and advanced features.

````
