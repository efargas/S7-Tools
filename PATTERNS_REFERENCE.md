# S7Tools Architectural Patterns Reference
**Version**: 1.1
**Last Updated**: 2025-11-07
**Purpose**: Comprehensive reference for all architectural patterns used in S7Tools

**Note**: Version 1.1 includes ViewModels/Views categorization (November 6, 2025) and post-reorganization validation (November 7, 2025).

---

## Table of Contents

1. [Core Architectural Patterns](#core-architectural-patterns)
2. [Profile Management Patterns](#profile-management-patterns)
3. [Concurrency & Threading Patterns](#concurrency--threading-patterns)
4. [MVVM & ReactiveUI Patterns](#mvvm--reactiveui-patterns)
5. [Service Patterns](#service-patterns)
6. [Task Management Patterns](#task-management-patterns)
7. [Error Handling Patterns](#error-handling-patterns)
8. [Testing Patterns](#testing-patterns)

---

## Core Architectural Patterns

### 1. Clean Architecture

**Description**: Separation of concerns with dependency inversion

**Structure**:
```
Domain (S7Tools.Core)
   ↑
Application (S7Tools)
   ├── ViewModels/     (Categorized: Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks)
   ├── Views/          (Categorized: mirrors ViewModels structure)
   ├── Services/       (Application services)
   └── Extensions/     (DI configuration)
   ↑
Infrastructure (S7Tools.Infrastructure.*)
```

**Namespace Conventions** (Updated 2025-11-06):
- ViewModels: `S7Tools.ViewModels.{Category}` (e.g., `S7Tools.ViewModels.Pages`)
- Views: `S7Tools.Views.{Category}` (e.g., `S7Tools.Views.Pages`)
- Core Models: `S7Tools.Core.Models.{Domain}` (e.g., `S7Tools.Core.Models.Jobs`)
- Services: `S7Tools.Services.{Domain}` or `S7Tools.Core.Services.Interfaces`

**Categories**:
- **Base**: Base classes (ViewModelBase, ReactiveObject extensions)
- **Controls**: Reusable control ViewModels (PropertyDisplayItem)
- **Dialogs**: Dialog ViewModels (ConfirmationDialog, InputDialog)
- **Jobs**: Job management ViewModels
- **Layout**: Layout/shell ViewModels (MainWindow, Navigation, BottomPanel)
- **Pages**: Page ViewModels (Home, Connections, LogViewer, About, PlcInput)
- **Profiles**: Profile management ViewModels (Serial, Socat, PowerSupply)
- **Settings**: Settings ViewModels
- **Tasks**: Task management ViewModels

**Rules**:
- Domain has NO external dependencies
- All dependencies point inward toward domain
- Interfaces defined in domain, implemented in outer layers
- ViewModels and Views organized by functional category, not layer

**Example**:
```csharp
// Domain (S7Tools.Core)
namespace S7Tools.Core.Services.Interfaces
{
    public interface IProfileManager<T> where T : class, IProfileBase
    {
        Task<T> CreateAsync(T profile, CancellationToken ct = default);
        // ... other methods
    }
}

// Application (S7Tools)
namespace S7Tools.Services
{
    public class StandardProfileManager<T> : IProfileManager<T>
    {
        // Implementation
    }
}

// ViewModels (Categorized)
namespace S7Tools.ViewModels.Pages
{
    public class HomeViewModel : ViewModelBase
    {
        // Page ViewModel implementation
    }
}

// Views (Categorized - mirrors ViewModels)
namespace S7Tools.Views.Pages
{
    public partial class HomeView : UserControl
    {
        // View implementation
    }
}
```

**ViewLocator Pattern**:
The ViewLocator automatically resolves Views from ViewModels:
```csharp
// Input:  S7Tools.ViewModels.Pages.HomeViewModel
// Output: S7Tools.Views.Pages.HomeView

// Works by replacing ".ViewModels." with ".Views." and "ViewModel" with "View"
```

**References**:
- `src/S7Tools.Core/` - Domain layer
  - `Models/Jobs/` - Job domain models (JobProfile, JobManagerOptions, etc.)
  - `Services/Interfaces/` - Service contracts
  - `Exceptions/` - Custom exception hierarchy
- `src/S7Tools/` - Application layer
  - `ViewModels/` - Categorized by function (Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks)
  - `Views/` - Categorized (mirrors ViewModels structure)
  - `Services/` - Application services
  - `Extensions/ServiceCollectionExtensions.cs` - DI registration
- `src/S7Tools.Infrastructure.*/` - Infrastructure layer

---

### 2. Dependency Injection Pattern

**Description**: Constructor-based dependency injection throughout

**Registration**: Centralized in `ServiceCollectionExtensions.cs`

**Example**:
```csharp
// Service registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS7ToolsFoundationServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IActivityBarService, ActivityBarService>();
        services.TryAddSingleton<ISerialPortService, SerialPortService>();
        services.TryAddSingleton<ISocatService, SocatService>();
        return services;
    }
}

// Service consumption
public class SomeViewModel
{
    private readonly ISerialPortService _serialPortService;

    public SomeViewModel(ISerialPortService serialPortService)
    {
        _serialPortService = serialPortService
            ?? throw new ArgumentNullException(nameof(serialPortService));
    }
}
```

**Key Files**:
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` - All service registration

---

## Profile Management Patterns

### 3. Unified Profile Management Pattern

**Description**: StandardProfileManager<T> provides consistent CRUD operations for all profile types

**Benefits**:
- Eliminates code duplication
- Consistent behavior across profile types
- Thread-safe by default
- Standardized validation and error handling

**Template Method Pattern Implementation**:
```csharp
public abstract class StandardProfileManager<T> : IProfileManager<T>, IDisposable
    where T : class, IProfileBase, new()
{
    // Template methods - subclasses must implement
    protected abstract T CreateDefaultProfile();
    protected abstract Task<ValidationResult> ValidateProfileAsync(T profile,
        CancellationToken ct);

    // Standard implementation for all profiles
    public async Task<T> CreateAsync(T profile, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Validate
            var validationResult = await ValidateProfileAsync(profile, ct);
            if (!validationResult.IsValid)
                throw new ValidationException("Profile", validationResult.Errors);

            // Assign ID
            profile.Id = GetNextAvailableId();

            // Set timestamps
            profile.CreatedAt = DateTime.UtcNow;
            profile.ModifiedAt = DateTime.UtcNow;

            // Save
            _profiles.Add(profile);
            await SaveToFileAsync(ct).ConfigureAwait(false);

            return profile;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

**Concrete Implementation Example**:
```csharp
public class SerialPortProfileService : StandardProfileManager<SerialPortProfile>,
    ISerialPortProfileService
{
    protected override SerialPortProfile CreateDefaultProfile()
    {
        return new SerialPortProfile
        {
            Name = "Default Serial Port",
            Device = "/dev/ttyUSB0",
            BaudRate = 115200,
            // ... other defaults
        };
    }

    protected override async Task<ValidationResult> ValidateProfileAsync(
        SerialPortProfile profile, CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(profile.Device))
            errors.Add(new ValidationError("Device", "Device path is required"));

        if (profile.BaudRate <= 0)
            errors.Add(new ValidationError("BaudRate", "Baud rate must be positive"));

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }
}
```

**Key Features**:
1. **Gap-Filling ID Assignment**: Reuses deleted IDs
2. **Name Uniqueness**: Automatic conflict resolution with (2), (3), etc.
3. **Default Profile Management**: Only one default per type
4. **Read-Only Protection**: System profiles cannot be modified/deleted
5. **Audit Trail**: CreatedAt/ModifiedAt timestamps
6. **Thread Safety**: Semaphore protection for all operations

**Key Files**:
- `src/S7Tools/Services/StandardProfileManager.cs` - Base implementation
- `src/S7Tools/Services/*ProfileService.cs` - Concrete implementations

---

## Concurrency & Threading Patterns

### 4. Internal Method Pattern (Semaphore Safety)

**Description**: Prevents semaphore deadlocks by separating public and internal methods

**Problem**: Nested semaphore acquisitions cause deadlocks

**Solution**:
```csharp
// PUBLIC API - Acquires semaphore
public async Task<bool> IsPortInUseAsync(int port, CancellationToken ct = default)
{
    await _semaphore.WaitAsync(ct).ConfigureAwait(false);
    try
    {
        return await IsPortInUseInternalAsync(port, ct).ConfigureAwait(false);
    }
    finally
    {
        _semaphore.Release();
    }
}

// INTERNAL - Assumes semaphore is held, NO acquisition
private async Task<bool> IsPortInUseInternalAsync(int port, CancellationToken ct)
{
    // Safe to call from within locked context
    // Can call other *Internal methods without risk
    return false;
}
```

**Debug Logging Pattern**:
```csharp
_logger.LogInformation("🔒 Waiting for semaphore...");
await _semaphore.WaitAsync(ct).ConfigureAwait(false);
_logger.LogInformation("🔓 Semaphore acquired");
try
{
    // Work
}
finally
{
    _logger.LogInformation("🔓 Releasing semaphore...");
    _semaphore.Release();
}
```

**Rules**:
- ✅ Public methods acquire/release semaphore
- ✅ Internal methods assume lock is held
- ✅ Never call public method from within lock
- ✅ Always release in finally block

**Key Files**:
- `src/S7Tools/Services/StandardProfileManager.cs`
- `src/S7Tools/Services/SocatService.cs`
- `src/S7Tools/Services/SerialPortService.cs`
- `src/S7Tools/Services/PowerSupplyService.cs`

---

### 5. UI Thread Marshaling Pattern

**Description**: Explicit UI thread access via IUIThreadService

**Interface**:
```csharp
public interface IUIThreadService
{
    Task InvokeAsync(Action action);
    Task<T> InvokeAsync<T>(Func<T> function);
    bool IsOnUIThread { get; }
}
```

**Usage**:
```csharp
// In service or background task
await _uiThreadService.InvokeAsync(() =>
{
    // UI updates here
    SomeObservableCollection.Add(item);
    SomeProperty = newValue;
});
```

**Why Not SynchronizationContext?**
- Explicit is better than implicit
- Testable (can mock IUIThreadService)
- No hidden context capture
- Clear intent in code

**Key Files**:
- `src/S7Tools/Services/UIThreadService.cs` - Implementation
- Various ViewModels and Services - Usage

---

### 6. Resource Coordination Pattern

**Description**: Prevent resource conflicts during parallel task execution

**Purpose**:
- Prevent multiple tasks from using same serial port
- Prevent multiple tasks from using same TCP port
- Prevent multiple tasks from accessing same power supply
- Enable safe parallel execution when resources don't conflict

**Implementation**:
```csharp
public class ResourceCoordinator : IResourceCoordinator
{
    private readonly Dictionary<ResourceKey, ResourceState> _resources = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryAcquire(IEnumerable<ResourceKey> keys)
    {
        var keyList = keys.ToList();

        _semaphore.Wait();
        try
        {
            // Check if ALL resources are available
            foreach (var key in keyList)
            {
                if (_resources.TryGetValue(key, out var state) && state.IsLocked)
                    return false; // Any locked resource fails entire acquisition
            }

            // All available - acquire ALL atomically
            foreach (var key in keyList)
            {
                _resources[key] = new ResourceState { IsLocked = true, /*...*/ };
            }

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Release(IEnumerable<ResourceKey> keys)
    {
        _semaphore.Wait();
        try
        {
            foreach (var key in keys)
            {
                if (_resources.TryGetValue(key, out var state))
                {
                    state.IsLocked = false;
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

**Key Features**:
- **All-or-nothing acquisition** prevents partial locks and deadlocks
- **Thread-safe** with semaphore protection
- **Resource tracking** with metadata (timestamps, task IDs)
- **Conflict detection** before execution
- **Comprehensive stress testing**

**Usage Example**:
```csharp
// Before starting a job
var requiredResources = new[]
{
    new ResourceKey("serial", profile.Device),
    new ResourceKey("tcp", profile.Port.ToString()),
    new ResourceKey("modbus", $"{profile.Host}:{profile.Port}")
};

if (_resourceCoordinator.TryAcquire(requiredResources))
{
    try
    {
        // Execute job with exclusive resource access
        await ExecuteJobAsync();
    }
    finally
    {
        _resourceCoordinator.Release(requiredResources);
    }
}
else
{
    // Resources busy - queue or reject
}
```

**Key Files**:
- `src/S7Tools/Services/ResourceCoordinator.cs` - Implementation
- `tests/S7Tools.Tests/Services/ResourceCoordinatorTests.cs` - Comprehensive tests

---

## MVVM & ReactiveUI Patterns

### 7. Reactive Property Pattern

**Description**: Properties that notify on change using ReactiveUI

**Basic Pattern**:
```csharp
public class SomeViewModel : ReactiveObject
{
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
}
```

**With Validation**:
```csharp
private string _profileName = string.Empty;
public string ProfileName
{
    get => _profileName;
    set
    {
        this.RaiseAndSetIfChanged(ref _profileName, value);
        ValidatePropertyAsync(nameof(ProfileName));
    }
}
```

**Observable Property Chain**:
```csharp
// Avoid large WhenAnyValue tuples (max 12 properties)
// Use individual subscriptions instead

this.WhenAnyValue(x => x.Property1)
    .Skip(1) // Skip initial value
    .Subscribe(_ => OnProperty1Changed())
    .DisposeWith(_disposables);

this.WhenAnyValue(x => x.Property2)
    .Skip(1)
    .Subscribe(_ => OnProperty2Changed())
    .DisposeWith(_disposables);
```

**Key Files**:
- All ViewModels in `src/S7Tools/ViewModels/`

---

### 8. Reactive Command Pattern

**Description**: Commands with validation and async execution

**Basic Command**:
```csharp
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

public SomeViewModel()
{
    SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
}

private async Task SaveAsync()
{
    // Async work here
}
```

**Command with Validation**:
```csharp
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

public SomeViewModel()
{
    var canExecute = this.WhenAnyValue(
        x => x.IsValid,
        x => x.HasChanges,
        (valid, changes) => valid && changes);

    SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canExecute);
}
```

**Command with Parameter**:
```csharp
public ReactiveCommand<ProfileModel, Unit> DeleteCommand { get; }

public SomeViewModel()
{
    DeleteCommand = ReactiveCommand.CreateFromTask<ProfileModel>(
        async profile => await DeleteProfileAsync(profile));
}
```

**Error Handling**:
```csharp
public SomeViewModel()
{
    SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);

    // Subscribe to command errors
    SaveCommand.ThrownExceptions
        .Subscribe(ex =>
        {
            _logger.LogError(ex, "Save failed");
            StatusMessage = $"Save failed: {ex.Message}";
        })
        .DisposeWith(_disposables);
}
```

**Key Files**:
- All ViewModels with commands in `src/S7Tools/ViewModels/`

---

### 9. Disposal Pattern in ViewModels

**Description**: Proper cleanup of subscriptions and resources

**Pattern**:
```csharp
public class SomeViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    public SomeViewModel()
    {
        // Add subscriptions to composite disposable
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(OnSearchTextChanged)
            .DisposeWith(_disposables);

        SaveCommand.ThrownExceptions
            .Subscribe(HandleError)
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _disposables.Dispose();
            // Dispose other managed resources
        }

        _disposed = true;
    }
}
```

**Key Rules**:
- ✅ Use `CompositeDisposable` for subscriptions
- ✅ Use `DisposeWith(_disposables)` on all subscriptions
- ✅ Implement proper Dispose pattern
- ✅ Dispose services if owned by ViewModel

---

## Service Patterns

### 10. Enhanced Service Decorator Pattern

**Description**: Wrap existing service with additional capabilities without breaking changes

**Example**: Enhanced Bootloader Service

**Base Interface**:
```csharp
public interface IBootloaderService
{
    Task<byte[]> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent)> progress,
        CancellationToken cancellationToken = default);
}
```

**Enhanced Interface**:
```csharp
public interface IEnhancedBootloaderService : IBootloaderService
{
    Task<byte[]> DumpWithTaskTrackingAsync(
        TaskExecution taskExecution,
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    Task<ValidationResult> ValidateResourcesAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default);

    void ConfigureRetryPolicy(RetryConfiguration configuration);
}
```

**Implementation**:
```csharp
public class EnhancedBootloaderService : IEnhancedBootloaderService
{
    private readonly IBootloaderService _baseBootloaderService;
    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly IValidationService _validationService;
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
    private RetryConfiguration _retryConfiguration = RetryConfiguration.Default;

    public async Task<byte[]> DumpWithTaskTrackingAsync(
        TaskExecution taskExecution,
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Update TaskExecution state
            taskExecution.UpdateState(TaskState.Running, "Initializing...");

            // Create progress reporter that updates TaskExecution
            var progressReporter = new Progress<(string stage, double percent)>(
                progress =>
                {
                    var (stage, percent) = progress;
                    var operation = GetUserFriendlyOperationName(stage);
                    taskExecution.UpdateProgress(percent * 100.0, operation);
                });

            // Execute with retry logic
            return await ExecuteWithRetryAsync(
                () => _baseBootloaderService.DumpAsync(
                    profiles, progressReporter, cancellationToken),
                RetryableOperations.All,
                taskExecution,
                cancellationToken);
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    // Delegate base interface to wrapped service
    public Task<byte[]> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent)> progress,
        CancellationToken cancellationToken = default)
    {
        return _baseBootloaderService.DumpAsync(profiles, progress, cancellationToken);
    }
}
```

**Benefits**:
- Non-breaking enhancement of existing service
- Decorator pattern for clean separation
- Can enhance multiple services with same pattern
- Easy to test (can mock base service)

**Key Files**:
- `src/S7Tools/Services/Bootloader/BootloaderService.cs` - Base
- `src/S7Tools/Services/Bootloader/EnhancedBootloaderService.cs` - Enhanced

---

### 11. Retry with Exponential Backoff Pattern

**Description**: Automatic retry with configurable exponential backoff

**Configuration**:
```csharp
public class RetryConfiguration
{
    public int MaxConnectionRetries { get; set; } = 3;
    public int MaxDataTransferRetries { get; set; } = 2;
    public int MaxAllRetries { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseExponentialBackoff { get; set; } = true;

    public static RetryConfiguration Conservative => new()
    {
        MaxAllRetries = 5,
        InitialRetryDelay = TimeSpan.FromSeconds(2),
        BackoffMultiplier = 2.5
    };

    public static RetryConfiguration Aggressive => new()
    {
        MaxAllRetries = 1,
        InitialRetryDelay = TimeSpan.FromMilliseconds(500),
        UseExponentialBackoff = false
    };
}
```

**Implementation**:
```csharp
private async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    RetryableOperations retryableOperation,
    TaskExecution taskExecution,
    CancellationToken cancellationToken)
{
    var maxRetries = GetMaxRetriesForOperation(retryableOperation);
    var currentDelay = _retryConfiguration.InitialRetryDelay;

    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            if (attempt > 0)
            {
                taskExecution.UpdateProgress(
                    taskExecution.ProgressPercentage,
                    $"Retrying operation (attempt {attempt + 1}/{maxRetries + 1})");

                await Task.Delay(currentDelay, cancellationToken);
            }

            return await operation();
        }
        catch (OperationCanceledException)
        {
            throw; // Never retry cancellation
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            _logger.LogWarning(ex,
                "Operation failed (attempt {Attempt}/{MaxAttempts}). Retrying...",
                attempt + 1, maxRetries + 1);

            // Calculate next delay with exponential backoff
            if (_retryConfiguration.UseExponentialBackoff)
            {
                currentDelay = TimeSpan.FromMilliseconds(
                    Math.Min(
                        currentDelay.TotalMilliseconds * _retryConfiguration.BackoffMultiplier,
                        _retryConfiguration.MaxRetryDelay.TotalMilliseconds));
            }
        }
    }

    throw new BootloaderOperationException(
        $"Operation failed after {maxRetries + 1} attempts");
}
```

**Key Features**:
- Configurable retry counts per operation type
- Exponential backoff with max delay cap
- Never retries cancellation
- Progress reporting during retries
- Comprehensive logging

---

## Task Management Patterns

### 12. Task Execution State Management Pattern

**Description**: Rich task lifecycle tracking with automatic state transitions

**Domain Model**:
```csharp
public class TaskExecution
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public TaskState State { get; private set; }
    public TaskPriority Priority { get; init; }
    public double ProgressPercentage { get; private set; }
    public string CurrentOperation { get; private set; }
    public DateTime? QueuedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Dictionary<string, object> ProgressData { get; } = new();

    public void UpdateState(TaskState newState, string? message = null)
    {
        State = newState;

        // Automatic timestamp management
        switch (newState)
        {
            case TaskState.Queued:
                QueuedAt = DateTime.UtcNow;
                break;
            case TaskState.Running:
                StartedAt = DateTime.UtcNow;
                break;
            case TaskState.Completed:
            case TaskState.Failed:
            case TaskState.Cancelled:
                CompletedAt = DateTime.UtcNow;
                break;
        }

        if (!string.IsNullOrEmpty(message))
            CurrentOperation = message;
    }

    public void UpdateProgress(double percentage, string operation,
        Dictionary<string, object>? progressData = null)
    {
        ProgressPercentage = Math.Clamp(percentage, 0.0, 100.0);
        CurrentOperation = operation;

        if (progressData != null)
        {
            foreach (var kvp in progressData)
                ProgressData[kvp.Key] = kvp.Value;
        }
    }

    public TimeSpan? GetDuration()
    {
        if (StartedAt == null) return null;
        var end = CompletedAt ?? DateTime.UtcNow;
        return end - StartedAt.Value;
    }
}
```

**State Machine**:
```
Queued → Running → Completed
           ↓
         Paused → Running
           ↓
        Failed/Cancelled
```

**Key Features**:
- Immutable state transitions
- Automatic timestamp management
- Progress tracking with metadata
- Duration calculations
- Rich domain model

**Key Files**:
- `src/S7Tools.Core/Models/TaskExecution.cs`
- `src/S7Tools/Services/Tasking/EnhancedTaskScheduler.cs`

---

### 13. Priority-Based Task Scheduler Pattern

**Description**: Queue and execute tasks by priority with resource coordination

**Implementation**:
```csharp
public class EnhancedTaskScheduler : ITaskScheduler
{
    private readonly Dictionary<TaskPriority, Queue<TaskExecution>> _queues = new()
    {
        { TaskPriority.Critical, new Queue<TaskExecution>() },
        { TaskPriority.High, new Queue<TaskExecution>() },
        { TaskPriority.Normal, new Queue<TaskExecution>() },
        { TaskPriority.Low, new Queue<TaskExecution>() }
    };

    private readonly List<TaskExecution> _runningTasks = new();
    private readonly SemaphoreSlim _schedulerSemaphore = new(1, 1);
    private int _maxConcurrentTasks = 4;

    public async Task<TaskExecution> ScheduleTaskAsync(
        TaskExecution task,
        CancellationToken cancellationToken = default)
    {
        await _schedulerSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Add to appropriate priority queue
            _queues[task.Priority].Enqueue(task);
            task.UpdateState(TaskState.Queued, "Waiting for execution");

            // Try to start task if capacity available
            await TryStartNextTaskInternalAsync(cancellationToken);

            return task;
        }
        finally
        {
            _schedulerSemaphore.Release();
        }
    }

    private async Task TryStartNextTaskInternalAsync(
        CancellationToken cancellationToken)
    {
        // Check if we have capacity
        if (_runningTasks.Count >= _maxConcurrentTasks)
            return;

        // Find highest priority task with available resources
        foreach (var priority in new[] {
            TaskPriority.Critical, TaskPriority.High,
            TaskPriority.Normal, TaskPriority.Low })
        {
            var queue = _queues[priority];

            foreach (var task in queue)
            {
                // Check resource availability
                if (CanAcquireResources(task))
                {
                    queue.Dequeue();
                    _runningTasks.Add(task);

                    // Start execution asynchronously
                    _ = Task.Run(async () => await ExecuteTaskAsync(task, cancellationToken));

                    return;
                }
            }
        }
    }
}
```

**Key Features**:
- Priority-based queuing (Critical → High → Normal → Low)
- Concurrent execution with configurable limits
- Resource coordination integration
- Automatic promotion of scheduled tasks
- Statistics and monitoring

---

## Error Handling Patterns

### 14. Custom Exception Hierarchy Pattern

**Description**: Domain-specific exceptions for semantic error handling

**Hierarchy**:
```
S7ToolsException (base)
├── ProfileException
│   ├── ProfileNotFoundException
│   ├── DuplicateProfileNameException
│   ├── DefaultProfileDeletionException
│   └── ReadOnlyProfileModificationException
├── ConnectionException
├── ValidationException
└── ConfigurationException
```

**Base Exception**:
```csharp
public class S7ToolsException : Exception
{
    public S7ToolsException(string message) : base(message) { }

    public S7ToolsException(string message, Exception innerException)
        : base(message, innerException) { }

    protected S7ToolsException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
```

**Domain Exception with Context**:
```csharp
public class ProfileException : S7ToolsException
{
    public int? ProfileId { get; init; }
    public string? ProfileName { get; init; }

    public ProfileException(string message, int profileId, string profileName)
        : base(message)
    {
        ProfileId = profileId;
        ProfileName = profileName;
    }

    public ProfileException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Specific Exception**:
```csharp
public class ProfileNotFoundException : ProfileException
{
    public ProfileNotFoundException(int profileId)
        : base($"Profile with ID {profileId} was not found.", profileId, null)
    {
    }

    public ProfileNotFoundException(string message, int profileId)
        : base(message, profileId, null)
    {
    }
}
```

**Usage in Services**:
```csharp
public async Task<T> UpdateAsync(T profile, CancellationToken ct = default)
{
    T? existingProfile = _profiles.FirstOrDefault(p => p.Id == profile.Id);

    if (existingProfile == null)
        throw new ProfileNotFoundException(profile.Id);

    if (!existingProfile.CanModify())
        throw new ReadOnlyProfileModificationException(
            existingProfile.Id, existingProfile.Name);

    // ... rest of implementation
}
```

**Catching Specific Exceptions**:
```csharp
try
{
    await _profileManager.UpdateAsync(profile);
}
catch (ProfileNotFoundException ex)
{
    StatusMessage = $"Profile not found: {ex.ProfileId}";
}
catch (DuplicateProfileNameException ex)
{
    StatusMessage = $"Name '{ex.DuplicateName}' already exists.";
}
catch (ReadOnlyProfileModificationException ex)
{
    StatusMessage = $"Cannot modify read-only profile '{ex.ProfileName}'.";
}
catch (S7ToolsException ex)
{
    // Catch-all for other domain exceptions
    _logger.LogError(ex, "Domain operation failed");
    StatusMessage = $"Operation failed: {ex.Message}";
}
```

**Key Files**:
- `src/S7Tools.Core/Exceptions/` - All exception classes
- Various services and ViewModels - Usage

---

## Testing Patterns

### 15. AAA Pattern (Arrange-Act-Assert)

**Description**: Standard test structure for clarity

**Example**:
```csharp
[Fact]
public async Task CreateAsync_ValidProfile_ShouldSucceed()
{
    // Arrange
    var service = CreateService();
    var profile = new SerialPortProfile
    {
        Name = "Test Profile",
        Device = "/dev/ttyUSB0",
        BaudRate = 115200
    };

    // Act
    var result = await service.CreateAsync(profile);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().BeGreaterThan(0);
    result.Name.Should().Be("Test Profile");
    result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}
```

**Test Naming Convention**:
```
MethodName_Scenario_ExpectedOutcome

Examples:
- CreateAsync_ValidProfile_ShouldSucceed
- UpdateAsync_NonExistentProfile_ShouldThrowNotFoundException
- DeleteAsync_DefaultProfile_ShouldThrowDefaultProfileDeletionException
```

---

### 16. Async Test Pattern

**Description**: Proper async test methods

**✅ CORRECT**:
```csharp
[Fact]
public async Task SomeOperationAsync_ShouldComplete()
{
    // Arrange
    var service = CreateService();

    // Act
    var result = await service.SomeOperationAsync();

    // Assert
    result.Should().NotBeNull();
}
```

**❌ INCORRECT**:
```csharp
[Fact]
public void SomeOperationAsync_ShouldComplete()
{
    var service = CreateService();
    var result = service.SomeOperationAsync().Result; // Blocks!
    result.Should().NotBeNull();
}

[Fact]
public void SomeOperationAsync_ShouldComplete()
{
    var service = CreateService();
    var task = service.SomeOperationAsync();
    Task.WaitAll(task); // Blocks! (xUnit1031 warning)
    task.Result.Should().NotBeNull();
}
```

**Multiple Async Operations**:
```csharp
[Fact]
public async Task MultipleOperations_ShouldCompleteInParallel()
{
    var service = CreateService();

    var tasks = new[]
    {
        service.Operation1Async(),
        service.Operation2Async(),
        service.Operation3Async()
    };

    // ✅ CORRECT: await Task.WhenAll
    var results = await Task.WhenAll(tasks);

    // ❌ INCORRECT: Task.WaitAll(tasks)

    results.Should().HaveCount(3);
}
```

---

### 17. Exception Testing Pattern

**Description**: Testing specific exception types and messages

**Basic Exception Test**:
```csharp
[Fact]
public async Task UpdateAsync_NonExistentProfile_ShouldThrowNotFoundException()
{
    // Arrange
    var service = CreateService();
    var profile = new SerialPortProfile { Id = 999, Name = "Test" };

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ProfileNotFoundException>(
        () => service.UpdateAsync(profile));

    exception.ProfileId.Should().Be(999);
}
```

**Testing Exception Message**:
```csharp
[Fact]
public async Task CreateAsync_DuplicateName_ShouldThrowWithMessage()
{
    // Arrange
    var service = CreateService();
    await service.CreateAsync(new SerialPortProfile { Name = "Existing" });

    // Act & Assert
    var exception = await Assert.ThrowsAsync<DuplicateProfileNameException>(
        () => service.CreateAsync(new SerialPortProfile { Name = "Existing" }));

    exception.Message.Should().Contain("already exists");
    exception.ExistingName.Should().Be("Existing");
}
```

---

## Summary

This reference provides patterns for:

1. **Architecture**: Clean Architecture, DI
2. **Profiles**: Unified management with StandardProfileManager<T>
3. **Concurrency**: Internal Method Pattern, UI marshaling, resource coordination
4. **MVVM**: Reactive properties, commands, disposal
5. **Services**: Decorator pattern, retry logic
6. **Tasks**: State management, priority scheduling
7. **Errors**: Custom exception hierarchy
8. **Testing**: AAA pattern, async tests, exception tests

**For New Features**:
1. Identify applicable patterns from this reference
2. Follow existing implementations as examples
3. Maintain consistency with established patterns
4. Add tests using testing patterns
5. Update this document if new patterns emerge

**References**:
- `reviews/LATEST_REVIEW.md` - Latest detailed code review
- `.copilot-tracking/memory-bank/systemPatterns.md` - Architecture guide
- `.github/copilot-instructions.md` - Development instructions
