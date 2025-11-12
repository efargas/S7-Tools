# Developer Quickstart: Bootloader Integration

**Feature**: 011-bootloader-integration
**Phase**: Phase 1 (Design)
**Created**: 2025-11-12
**Audience**: Developers implementing or extending bootloader functionality

## 🚀 60-Second Onboarding

### What This Feature Does

Integrates S7-1200 PLC bootloader functionality into S7Tools with:
- **Job-based execution** - Create reusable bootloader job profiles
- **Parallel execution** - Run 4+ concurrent jobs on independent hardware
- **Resource coordination** - Automatic conflict detection (serial/TCP/modbus)
- **Real-time monitoring** - Progress bars and logging for 5-300 second operations

### Architecture at a Glance

```
┌─────────────────────────────────────────────────────────┐
│                  UI Layer (Avalonia)                    │
│  TaskManagerViewModel → JobsManagementViewModel         │
└──────────────────┬──────────────────────────────────────┘
                   │ Depends On
┌──────────────────▼──────────────────────────────────────┐
│            Application Services Layer                    │
│  JobScheduler → ResourceCoordinator → BootloaderService │
└──────────────────┬──────────────────────────────────────┘
                   │ Implements
┌──────────────────▼──────────────────────────────────────┐
│          Core Interfaces (S7Tools.Core)                  │
│  IJobScheduler, IResourceCoordinator, IBootloaderService│
└──────────────────┬──────────────────────────────────────┘
                   │ Adapts
┌──────────────────▼──────────────────────────────────────┐
│       Reference Implementation (SiemensS7-Bootloader)   │
│  PlcClient, PlcProtocol, PlcTransport (UNCHANGED)       │
└─────────────────────────────────────────────────────────┘
```

### Key Files to Review Before Coding

1. **Contracts**: `specs/011-bootloader-integration/contracts/` - All service interfaces
2. **Data Model**: `specs/011-bootloader-integration/data-model.md` - Domain entities
3. **Research**: `specs/011-bootloader-integration/research.md` - Architectural decisions
4. **Constitution**: `.specify/memory/constitution.md` - Governance rules

---

## 📦 Project Structure

### New Files to Create (Phase 2 Implementation)

```
src/S7Tools.Core/Services/Interfaces/
├── IBootloaderService.cs       # Orchestration service interface
├── IJobScheduler.cs            # Job queue management interface
├── IResourceCoordinator.cs     # Resource locking interface
├── IPlcClient.cs               # Bootloader client interface (adapter target)
├── IPlcProtocol.cs             # Protocol layer interface (adapter target)
├── IPlcTransport.cs            # Transport layer interface (adapter target)
└── IPayloadProvider.cs         # Payload loading interface

src/S7Tools.Core/Models/Jobs/
├── Job.cs                      # Job entity
├── JobState.cs                 # State enumeration
├── JobProfileSet.cs            # Profile aggregation
├── ResourceKey.cs              # Resource identifier value object
├── SerialProfileRef.cs         # Serial profile reference
├── SocatProfileRef.cs          # Socat profile reference
├── PowerProfileRef.cs          # Power profile reference
├── MemoryRegionProfile.cs      # Memory region definition (may exist from spec 008)
└── PayloadSetProfile.cs        # Payload configuration

src/S7Tools/Services/Bootloader/
└── BootloaderService.cs        # IBootloaderService implementation

src/S7Tools/Services/Tasking/
├── JobScheduler.cs             # IJobScheduler implementation
├── ResourceCoordinator.cs      # IResourceCoordinator implementation
└── JobEvents.cs                # Event definitions

src/S7Tools/Services/Adapters/
├── PlcClientAdapter.cs         # Wraps reference PlcClient
├── PlcProtocolAdapter.cs       # Wraps reference PlcProtocol
├── PlcTransportAdapter.cs      # Wraps reference ICommunicationChannel
├── PayloadProviderAdapter.cs   # File system payload loading
├── PowerSupplyAdapter.cs       # Wraps existing IPowerSupplyService
└── SocatAdapter.cs             # Wraps existing ISocatService

src/S7Tools/ViewModels/Tasks/
├── TaskManagerViewModel.cs     # Job monitoring UI
└── JobsManagementViewModel.cs  # Job profile CRUD

src/S7Tools/Views/Tasks/
├── TaskManagerView.axaml       # Multi-tab interface (Active/Scheduled/Finished)
└── JobsManagementView.axaml    # Job profile management

src/S7Tools/Resources/JobProfiles/
└── profiles.json               # Job persistence (create with [] if missing)

tests/S7Tools.Core.Tests/Tasking/
├── ResourceCoordinatorTests.cs # Resource locking tests (10+ tests)
└── SchedulerTests.cs           # Queue management tests (15+ tests)

tests/S7Tools.Tests/Services/Bootloader/
└── BootloaderServiceTests.cs   # End-to-end workflow tests (10+ tests)
```

---

## 💻 Common Development Tasks

### Task 1: Create a Bootloader Job

```csharp
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

// Step 1: Create job with profile references
var job = new Job
{
    Id = 1, // Auto-assigned by StandardProfileManager<Job>
    Name = "CPU Firmware Dump",
    Description = "Extract 64KB firmware from S7-1200 CPU 1214C",
    ProfileSet = new JobProfileSet
    {
        Serial = new SerialProfileRef { ProfileId = 5, ProfileName = "COM3 115200 8N1", Device = "/dev/ttyUSB0" },
        Socat = new SocatProfileRef { ProfileId = 2, ProfileName = "TCP Bridge 10102", Port = 10102 },
        Power = new PowerProfileRef { ProfileId = 1, ProfileName = "24V Power Supply", ModbusAddress = "192.168.1.10:502" },
        MemoryRegion = new MemoryRegionProfile
        {
            Id = 3,
            Name = "Full Firmware",
            StartAddress = 0x20000000,
            Length = 65536
        },
        PayloadSet = new PayloadSetProfile
        {
            Id = 1,
            Name = "Development Payloads",
            BasePath = "/home/user/S7-Tools/bootloader-payloads/payloads"
        }
    },
    State = JobState.Created,
    CreatedAt = DateTime.UtcNow,
    ModifiedAt = DateTime.UtcNow,
    Progress = 0.0,
    OutputPath = "/home/user/plc-dumps/cpu1214c-20251112.bin"
};

// Step 2: Enqueue job via scheduler
var jobScheduler = serviceProvider.GetRequiredService<IJobScheduler>();
await jobScheduler.EnqueueAsync(job);

// Job transitions: Created → Queued → Running → Completed
```

### Task 2: Monitor Job Progress in UI

```csharp
// In TaskManagerViewModel
public class TaskManagerViewModel : ReactiveObject
{
    private readonly IJobScheduler _jobScheduler;
    private readonly IUIThreadService _uiThreadService;

    public ObservableCollection<Job> ActiveJobs { get; } = new();

    public TaskManagerViewModel(IJobScheduler jobScheduler, IUIThreadService uiThreadService)
    {
        _jobScheduler = jobScheduler;
        _uiThreadService = uiThreadService;

        // Subscribe to job state changes
        _jobScheduler.JobStateChanged += OnJobStateChanged;
        _jobScheduler.JobProgressChanged += OnJobProgressChanged;
    }

    private void OnJobStateChanged(object? sender, JobStateChangedEventArgs e)
    {
        _ = _uiThreadService.InvokeAsync(() =>
        {
            // Update UI collections based on state transitions
            var job = ActiveJobs.FirstOrDefault(j => j.Id == e.JobId);
            if (job != null && e.NewState == JobState.Completed)
            {
                ActiveJobs.Remove(job);
                CompletedJobs.Add(job);
                StatusMessage = $"Job '{job.Name}' completed successfully";
            }
        });
    }

    private void OnJobProgressChanged(object? sender, JobProgressChangedEventArgs e)
    {
        _ = _uiThreadService.InvokeAsync(() =>
        {
            var job = ActiveJobs.FirstOrDefault(j => j.Id == e.JobId);
            if (job != null)
            {
                job.Progress = e.ProgressPercentage;
                job.CurrentOperation = e.CurrentOperation;
            }
        });
    }
}
```

### Task 3: Execute Bootloader Workflow Directly

```csharp
// For direct execution without job scheduler (e.g., testing)
var bootloaderService = serviceProvider.GetRequiredService<IBootloaderService>();

// Create progress reporter
var progress = new Progress<BootloaderProgress>(p =>
{
    Console.WriteLine($"[{p.Percentage:F1}%] {p.CurrentOperation} ({p.Stage})");
});

try
{
    // Execute workflow
    var dumpData = await bootloaderService.DumpMemoryAsync(profileSet, progress, cts.Token);

    // Save dump to file
    await File.WriteAllBytesAsync("/path/to/output.bin", dumpData);

    Console.WriteLine($"Memory dump complete: {dumpData.Length} bytes");
}
catch (BootloaderException ex)
{
    Console.WriteLine($"Bootloader failed: {ex.Message}");
}
catch (ResourceUnavailableException ex)
{
    Console.WriteLine($"Resources locked: {ex.Message}");
}
```

### Task 4: Coordinate Resources for Parallel Jobs

```csharp
// Resource coordinator automatically handles conflicts
var coordinator = serviceProvider.GetRequiredService<IResourceCoordinator>();

// Job A resources
var resourcesA = new[]
{
    new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0"),
    new ResourceKey(ResourceType.Tcp, "10102"),
    new ResourceKey(ResourceType.Modbus, "192.168.1.10:502")
};

// Job B resources (different serial port → NO conflict)
var resourcesB = new[]
{
    new ResourceKey(ResourceType.Serial, "/dev/ttyUSB1"),
    new ResourceKey(ResourceType.Tcp, "10103"),
    new ResourceKey(ResourceType.Modbus, "192.168.1.11:502")
};

// Both jobs acquire successfully (independent hardware)
if (coordinator.TryAcquire(resourcesA) && coordinator.TryAcquire(resourcesB))
{
    // Execute Job A and Job B in parallel
    await Task.WhenAll(
        ExecuteJobAsync(jobA, resourcesA),
        ExecuteJobAsync(jobB, resourcesB)
    );
}
```

### Task 5: Register Services in DI Container

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddS7ToolsBootloaderServices(this IServiceCollection services)
{
    // Core services
    services.TryAddSingleton<IJobScheduler, JobScheduler>();
    services.TryAddSingleton<IResourceCoordinator, ResourceCoordinator>();
    services.TryAddSingleton<IBootloaderService, BootloaderService>();

    // Adapters
    services.TryAddSingleton<IPlcClient, PlcClientAdapter>();
    services.TryAddSingleton<IPlcProtocol, PlcProtocolAdapter>();
    services.TryAddSingleton<IPlcTransport, PlcTransportAdapter>();
    services.TryAddSingleton<IPayloadProvider, FilePayloadProvider>();

    // Profile managers
    services.TryAddSingleton<IProfileManager<Job>, StandardProfileManager<Job>>();
    services.TryAddSingleton<IProfileManager<PayloadSetProfile>, StandardProfileManager<PayloadSetProfile>>();

    // ViewModels
    services.TryAddSingleton<TaskManagerViewModel>();
    services.TryAddSingleton<JobsManagementViewModel>();

    return services;
}
```

---

## 🧪 Testing Workflow

### Unit Test Example (AAA Pattern)

```csharp
using Xunit;
using FluentAssertions;
using S7Tools.Core.Models.Jobs;
using S7Tools.Services.Tasking;

public class ResourceCoordinatorTests
{
    [Fact]
    public void TryAcquire_AvailableResources_ReturnsTrue()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resources = new[]
        {
            new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0"),
            new ResourceKey(ResourceType.Tcp, "10102")
        };

        // Act
        var acquired = coordinator.TryAcquire(resources);

        // Assert
        acquired.Should().BeTrue();
        coordinator.GetLockedResources().Should().HaveCount(2);
    }

    [Fact]
    public void TryAcquire_ConflictingResource_ReturnsFalse()
    {
        // Arrange
        var coordinator = new ResourceCoordinator();
        var resources1 = new[] { new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0") };
        var resources2 = new[] { new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0") };

        coordinator.TryAcquire(resources1); // Lock resource

        // Act
        var acquired = coordinator.TryAcquire(resources2);

        // Assert
        acquired.Should().BeFalse(); // Conflict detected
    }
}
```

### Running Tests

```bash
# Run all new tests
cd /home/kali/WS/S7-Tools
dotnet test src/S7Tools.sln --filter "FullyQualifiedName~Bootloader|Tasking"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ResourceCoordinatorTests"

# Run with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🔧 Debugging Tips

### Enable Bootloader Diagnostic Logging

```csharp
// In appsettings.json or programmatically
{
  "Logging": {
    "LogLevel": {
      "S7Tools.Services.Bootloader": "Debug",
      "S7Tools.Services.Tasking": "Debug"
    }
  }
}
```

### View Resource Locks in Real-Time

```csharp
// Subscribe to resource lock events
_resourceCoordinator.ResourceLockChanged += (s, e) =>
{
    Console.WriteLine($"[{e.Action}] Resource: {e.Resource.Type}:{e.Resource.Identifier}, Job: {e.JobId}");
};
```

### Trace Job State Transitions

```csharp
// Subscribe to job state changes
_jobScheduler.JobStateChanged += (s, e) =>
{
    Console.WriteLine($"Job {e.JobId}: {e.PreviousState} → {e.NewState} ({e.Timestamp})");
};
```

---

## 📚 Key Patterns to Follow

### 1. Internal Method Pattern (Semaphore Safety)

```csharp
// Public API acquires semaphore
public async Task<T> GetByIdAsync(int id)
{
    await _semaphore.WaitAsync();
    try { return await GetByIdInternalAsync(id); }
    finally { _semaphore.Release(); }
}

// Internal method assumes semaphore held
private Task<T> GetByIdInternalAsync(int id) { /* no WaitAsync */ }
```

### 2. UI Thread Marshaling (Article IV)

```csharp
// ALWAYS use IUIThreadService for UI updates
await _uiThreadService.InvokeAsync(() =>
{
    ProgressPercentage = newValue;
    StatusMessage = "Updated";
});
```

### 3. ConfigureAwait(false) in Service Layer

```csharp
// Service layer methods MUST use ConfigureAwait(false)
await _plcClient.DumpMemoryAsync(start, length).ConfigureAwait(false);
```

### 4. Structured Logging (Article V)

```csharp
// Use semantic logging with properties
_logger.LogInformation(
    "Job {JobId} started: Region={RegionName}, Size={Size} bytes",
    job.Id,
    job.ProfileSet.MemoryRegion.Name,
    job.ProfileSet.MemoryRegion.Length);
```

### 5. ReactiveUI Property Binding

```csharp
// Properties use RaiseAndSetIfChanged
private double _progressPercentage;
public double ProgressPercentage
{
    get => _progressPercentage;
    set => this.RaiseAndSetIfChanged(ref _progressPercentage, value);
}
```

---

## 🚨 Common Pitfalls to Avoid

❌ **DON'T**: Modify SiemensS7-Bootloader reference code
✅ **DO**: Wrap reference code with adapters

❌ **DON'T**: Acquire nested semaphores (deadlock risk)
✅ **DO**: Use Internal Method Pattern with single semaphore

❌ **DON'T**: Update UI from background thread directly
✅ **DO**: Marshal all UI updates via IUIThreadService

❌ **DON'T**: Use blocking `.Wait()` or `.Result` in async code
✅ **DO**: Use `await` with proper ConfigureAwait

❌ **DON'T**: Register services in Program.cs
✅ **DO**: Use ServiceCollectionExtensions.cs extension methods

---

## 📖 Additional Resources

- **Specification**: `specs/011-bootloader-integration/spec.md` - User stories and requirements
- **Implementation Plan**: `specs/011-bootloader-integration/plan.md` - Technical context
- **Research**: `specs/011-bootloader-integration/research.md` - Architectural decisions
- **Contracts**: `specs/011-bootloader-integration/contracts/` - Service interface definitions
- **Data Model**: `specs/011-bootloader-integration/data-model.md` - Domain entities
- **Constitution**: `.specify/memory/constitution.md` - Governance rules
- **Existing Patterns**: `docs/patterns/system-patterns.md` - Established S7Tools patterns

---

## 🎯 Next Steps

After reviewing this quickstart:

1. **Phase 2**: Run `/speckit.tasks` command to generate implementation tasks
2. **Implementation**: Create files following project structure above
3. **Testing**: Write tests BEFORE implementation per Article III (Test-First)
4. **Code Review**: Ensure constitutional compliance before PR

**Questions?** Review contracts in `specs/011-bootloader-integration/contracts/` directory.

---

**Quickstart Guide Complete** - You're ready to implement bootloader integration following S7Tools architecture patterns!
