---
title: "S7Tools Architectural Patterns Reference"
version: "1.2.0"
created: "2025-10-16"
last-updated: "2025-11-10"
status: "current"
tags: ["patterns", "reference", "catalog", "profile-management", "threading", "mvvm", "services", "dialogs", "wizard", "settings"]
related:
  - "docs/architecture/overview.md"
  - "docs/architecture/clean-architecture.md"
  - "docs/architecture/mvvm-patterns.md"
  - "docs/patterns/system-patterns.md"
supersedes: []
---

# S7Tools Architectural Patterns Reference

**Purpose**: Comprehensive reference for all architectural patterns used in S7Tools

**Note**: Version 1.2 includes Memory Region Profiling, Job Wizard pattern, ProfileEditDialogService, and Application Settings Service (November 10, 2025).

---

## Quick Navigation: Essential Patterns

**New for 2025-11-10**: Five core patterns extracted into dedicated documents for rapid AI agent discovery:

| Pattern Document | Focus | Use When |
|-----------------|-------|----------|
| **[Profile Management](profile-management.md)** | IProfileBase, StandardProfileManager<T>, Template Method | Creating/managing profile types (Serial, Socat, PowerSupply, Job, MemoryRegion) |
| **[Internal Method](internal-method.md)** | Semaphore deadlock prevention | Implementing thread-safe services with concurrent operations |
| **[Resource Coordination](resource-coordination.md)** | Parallel service initialization | Optimizing startup time, managing resource conflicts |
| **[Custom Exceptions](custom-exceptions.md)** | Domain-specific exception hierarchy | Error handling with contextual information |
| **[Reusable Controls](reusable-controls.md)** | UserControl extraction pattern | Eliminating duplicate UI sections (SerialPortDiscoveryControl) |

## Table of Contents

1. [Core Architectural Patterns](#core-architectural-patterns)
2. [Profile Management Patterns](#profile-management-patterns)
3. [Concurrency & Threading Patterns](#concurrency--threading-patterns)
4. [MVVM & ReactiveUI Patterns](#mvvm--reactiveui-patterns)
5. [Service Patterns](#service-patterns)
6. [Task Management Patterns](#task-management-patterns)
7. [Dialog Patterns](#dialog-patterns)
8. [Wizard Patterns](#wizard-patterns)
9. [Settings Management Patterns](#settings-management-patterns)
10. [Error Handling Patterns](#error-handling-patterns)
11. [Testing Patterns](#testing-patterns)

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

### 3a. Memory Region Profile Management Pattern

**Description**: Specialized profile management for memory dump configurations with segment selection

**Purpose**:
- Define custom memory regions for PLC dumps
- Select specific segments to dump (Work, DB, etc.)
- Support multi-segment dumps with contiguity validation
- Import/export memory region profiles

**Domain Model**:
```csharp
public class MemoryRegionProfile : IProfileBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MemorySegment> Segments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsDefault { get; set; }
    public bool CanModify() => !IsSystemProfile;
    public bool CanDelete() => !IsSystemProfile && !IsDefault;

    // Helper methods
    public IEnumerable<MemorySegment> GetSelectedSegments()
        => Segments.Where(s => s.IsSelected);

    public bool AreSelectedSegmentsContiguous()
    {
        var selected = GetSelectedSegments().OrderBy(s => s.StartAddress).ToList();
        if (selected.Count <= 1) return true;

        for (int i = 0; i < selected.Count - 1; i++)
        {
            var current = selected[i];
            var next = selected[i + 1];
            if (current.StartAddress + current.Size != next.StartAddress)
                return false;
        }
        return true;
    }

    public uint GetTotalSize()
        => (uint)GetSelectedSegments().Sum(s => (long)s.Size);
}

public class MemorySegment
{
    public string Name { get; set; } = string.Empty;
    public uint StartAddress { get; set; }
    public uint Size { get; set; }
    public bool IsSelected { get; set; }
    public string Description { get; set; } = string.Empty;
}
```

**Service Implementation**:
```csharp
public class MemoryRegionProfileService
    : StandardProfileManager<MemoryRegionProfile>,
      IMemoryRegionProfileService
{
    protected override MemoryRegionProfile CreateDefaultProfile()
    {
        return new MemoryRegionProfile
        {
            Name = "Default Memory Region",
            Segments = new List<MemorySegment>
            {
                new() { Name = "Work Memory", StartAddress = 0x20000000,
                        Size = 0x1000, IsSelected = true },
                new() { Name = "DB Memory", StartAddress = 0x20001000,
                        Size = 0x2000, IsSelected = false }
            }
        };
    }

    protected override async Task<ValidationResult> ValidateProfileAsync(
        MemoryRegionProfile profile, CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(profile.Name))
            errors.Add(new ValidationError("Name", "Name is required"));

        if (profile.Segments == null || profile.Segments.Count == 0)
            errors.Add(new ValidationError("Segments", "At least one segment required"));

        var selected = profile.GetSelectedSegments().ToList();
        if (selected.Count == 0)
            errors.Add(new ValidationError("Segments",
                "At least one segment must be selected"));

        // Check for overlapping segments
        for (int i = 0; i < profile.Segments.Count - 1; i++)
        {
            var current = profile.Segments[i];
            for (int j = i + 1; j < profile.Segments.Count; j++)
            {
                var other = profile.Segments[j];
                if (SegmentsOverlap(current, other))
                    errors.Add(new ValidationError("Segments",
                        $"Segments '{current.Name}' and '{other.Name}' overlap"));
            }
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    private bool SegmentsOverlap(MemorySegment a, MemorySegment b)
    {
        var aEnd = a.StartAddress + a.Size;
        var bEnd = b.StartAddress + b.Size;
        return (a.StartAddress < bEnd && aEnd > b.StartAddress);
    }

    // Additional methods for import/export
    public async Task<MemoryRegionProfile> ImportFromFileAsync(
        string filePath, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(filePath, ct);
        var profile = JsonSerializer.Deserialize<MemoryRegionProfile>(json)
            ?? throw new InvalidOperationException("Failed to deserialize profile");

        // Validate and create
        return await CreateAsync(profile, ct);
    }

    public async Task ExportToFileAsync(
        int profileId, string filePath, CancellationToken ct = default)
    {
        var profile = await GetByIdAsync(profileId, ct);
        var json = JsonSerializer.Serialize(profile,
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, ct);
    }
}
```

**ViewModel Integration Example**:
```csharp
public class MemoryRegionSettingsViewModel : ViewModelBase
{
    private readonly IMemoryRegionProfileService _profileService;
    private ObservableCollection<MemoryRegionProfile> _profiles = new();
    private MemoryRegionProfile? _selectedProfile;

    public async Task LoadProfilesAsync()
    {
        var profiles = await _profileService.GetAllAsync();
        Profiles.Clear();
        Profiles.AddRange(profiles);

        // Select default or first
        SelectedProfile = profiles.FirstOrDefault(p => p.IsDefault)
                       ?? profiles.FirstOrDefault();
    }

    public async Task ImportProfileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "Memory Region Profile", Extensions = { "json" } }
            }
        };

        var result = await dialog.ShowAsync(GetParentWindow());
        if (result?.Length > 0)
        {
            try
            {
                var imported = await _profileService.ImportFromFileAsync(result[0]);
                await LoadProfilesAsync();
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == imported.Id);
                StatusMessage = UIStrings.Status_ProfileImported;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import profile");
                StatusMessage = string.Format(UIStrings.Error_ImportFailed, ex.Message);
            }
        }
    }
}
```

**Job Wizard Integration**:
```csharp
// In JobWizardMemoryRegionStepViewModel
public void ValidateStep()
{
    var errors = new List<string>();

    if (SelectedProfile == null)
    {
        errors.Add(UIStrings.Validation_MemoryRegionRequired);
        IsValid = false;
        return;
    }

    var selectedSegments = SelectedProfile.GetSelectedSegments().ToList();

    if (selectedSegments.Count == 0)
    {
        errors.Add(UIStrings.Validation_AtLeastOneSegmentRequired);
    }
    else if (selectedSegments.Count == 1)
    {
        // Single segment is always valid
        IsValid = true;
    }
    else
    {
        // Multi-segment requires contiguity
        if (!SelectedProfile.AreSelectedSegmentsContiguous())
        {
            ValidationWarning = UIStrings.Warning_NonContiguousSegments;
            // Still valid, just warn the user
        }
    }

    IsValid = errors.Count == 0;
    ValidationErrors = string.Join(Environment.NewLine, errors);
}
```

**Key Features**:
- Segment-based memory mapping
- Selection validation (single or contiguous)
- Overlap detection
- Import/export functionality
- Integration with Job Wizard
- Configurable via Application Settings

**Key Files**:
- `src/S7Tools.Core/Models/MemoryRegionProfile.cs` - Domain model
- `src/S7Tools/Services/MemoryRegionProfileService.cs` - Service implementation
- `src/S7Tools/ViewModels/Settings/MemoryRegionSettingsViewModel.cs` - Settings UI
- `src/S7Tools/ViewModels/Jobs/JobWizardMemoryRegionStepViewModel.cs` - Wizard integration

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

## Dialog Patterns

### 16. ProfileEditDialogService Pattern

**Description**: Centralized service for managing Create/Edit/Duplicate profile dialogs with consistent success patterns

**Purpose**:
- Eliminate code duplication across profile management ViewModels
- Standardize dialog workflows (Create, Edit, Duplicate)
- Enforce consistent collection refresh and selection patterns
- Handle dialog parent resolution automatically

**Service Interface**:
```csharp
public interface IProfileEditDialogService
{
    Task<T?> ShowCreateDialogAsync<T>(
        Visual parentWindow,
        Func<Task<T>> createCallback,
        Action<T>? onSuccess = null)
        where T : class, IProfileBase;

    Task<T?> ShowEditDialogAsync<T>(
        Visual parentWindow,
        T profile,
        Func<T, Task<T>> editCallback,
        Action<T>? onSuccess = null)
        where T : class, IProfileBase;

    Task<T?> ShowDuplicateDialogAsync<T>(
        Visual parentWindow,
        T sourceProfile,
        Func<T, Task<T>> duplicateCallback,
        Action<T>? onSuccess = null)
        where T : class, IProfileBase;
}
```

**Implementation Pattern**:
```csharp
public class ProfileEditDialogService : IProfileEditDialogService
{
    private readonly ILogger<ProfileEditDialogService> _logger;

    public async Task<T?> ShowCreateDialogAsync<T>(
        Visual parentWindow,
        Func<Task<T>> createCallback,
        Action<T>? onSuccess = null)
        where T : class, IProfileBase
    {
        try
        {
            // Create and show dialog
            var dialog = CreateDialogFor<T>(DialogMode.Create);
            var result = await dialog.ShowDialog<T?>(parentWindow);

            if (result == null)
                return null; // User cancelled

            // Execute callback (service CreateAsync)
            var created = await createCallback();

            // Invoke success callback (refresh collection, reselect)
            onSuccess?.Invoke(created);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile");
            throw;
        }
    }

    public async Task<T?> ShowEditDialogAsync<T>(
        Visual parentWindow,
        T profile,
        Func<T, Task<T>> editCallback,
        Action<T>? onSuccess = null)
        where T : class, IProfileBase
    {
        try
        {
            var dialog = CreateDialogFor<T>(DialogMode.Edit, profile);
            var result = await dialog.ShowDialog<T?>(parentWindow);

            if (result == null)
                return null;

            var updated = await editCallback(result);
            onSuccess?.Invoke(updated);

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit profile");
            throw;
        }
    }

    private Window CreateDialogFor<T>(DialogMode mode, T? existingProfile = null)
        where T : class, IProfileBase
    {
        // Resolve correct dialog type based on T
        // Return new instance with ViewModel configured
        throw new NotImplementedException();
    }
}
```

**ViewModel Usage Pattern**:
```csharp
public class SerialProfilesViewModel : ViewModelBase
{
    private readonly ISerialPortProfileService _profileService;
    private readonly IProfileEditDialogService _dialogService;
    private ObservableCollection<SerialPortProfile> _profiles = new();

    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<SerialPortProfile, Unit> EditCommand { get; }
    public ReactiveCommand<SerialPortProfile, Unit> DuplicateCommand { get; }

    public SerialProfilesViewModel(
        ISerialPortProfileService profileService,
        IProfileEditDialogService dialogService)
    {
        _profileService = profileService;
        _dialogService = dialogService;

        CreateCommand = ReactiveCommand.CreateFromTask(CreateProfileAsync);
        EditCommand = ReactiveCommand.CreateFromTask<SerialPortProfile>(
            EditProfileAsync);
        DuplicateCommand = ReactiveCommand.CreateFromTask<SerialPortProfile>(
            DuplicateProfileAsync);
    }

    private async Task CreateProfileAsync()
    {
        var parent = GetParentWindow();

        await _dialogService.ShowCreateDialogAsync<SerialPortProfile>(
            parent,
            createCallback: async () =>
            {
                // Dialog already validated and created profile
                // Service will save it
                var newProfile = /* get from dialog */;
                return await _profileService.CreateAsync(newProfile);
            },
            onSuccess: created =>
            {
                // Refresh and reselect
                RefreshProfilesAndSelect(created.Name);
            });
    }

    private async Task EditProfileAsync(SerialPortProfile profile)
    {
        var parent = GetParentWindow();

        await _dialogService.ShowEditDialogAsync(
            parent,
            profile,
            editCallback: async (modified) =>
            {
                return await _profileService.UpdateAsync(modified);
            },
            onSuccess: updated =>
            {
                RefreshProfilesAndSelect(updated.Id);
            });
    }

    private async Task DuplicateProfileAsync(SerialPortProfile source)
    {
        var parent = GetParentWindow();

        await _dialogService.ShowDuplicateDialogAsync(
            parent,
            source,
            duplicateCallback: async (duplicated) =>
            {
                return await _profileService.CreateAsync(duplicated);
            },
            onSuccess: created =>
            {
                RefreshProfilesAndSelect(created.Name);
            });
    }

    private void RefreshProfilesAndSelect(string profileName)
    {
        // Load all profiles
        var profiles = await _profileService.GetAllAsync();

        // Replace entire collection (triggers DataGrid refresh)
        Profiles.Clear();
        Profiles.AddRange(profiles);

        // Reselect by name (for create/duplicate)
        SelectedProfile = profiles.FirstOrDefault(p => p.Name == profileName);
    }

    private void RefreshProfilesAndSelect(int profileId)
    {
        var profiles = await _profileService.GetAllAsync();
        Profiles.Clear();
        Profiles.AddRange(profiles);

        // Reselect by ID (for edit)
        SelectedProfile = profiles.FirstOrDefault(p => p.Id == profileId);
    }
}
```

**Dialog Success Pattern** (CRITICAL):

When a dialog completes successfully:

1. **Close dialog ONLY after SaveAsync succeeds**
   ```csharp
   // In dialog ViewModel
   private async Task SaveAsync()
   {
       IsValid = await ValidateAsync();
       if (!IsValid) return;

       try
       {
           // Update profile with form values
           UpdateProfileFromForm();

           // Close with result (triggers callback)
           CloseDialog(updatedProfile);
       }
       catch (Exception ex)
       {
           // Do NOT close dialog on error
           _logger.LogError(ex, "Failed to save");
           StatusMessage = $"Error: {ex.Message}";
       }
   }
   ```

2. **Replace entire ObservableCollection to trigger refresh**
   ```csharp
   // ❌ WRONG - DataGrid won't refresh selection properly
   var updated = await _service.UpdateAsync(profile);
   var existing = Profiles.First(p => p.Id == updated.Id);
   Profiles[Profiles.IndexOf(existing)] = updated;

   // ✅ CORRECT - Full refresh guarantees UI sync
   var profiles = await _service.GetAllAsync();
   Profiles.Clear();
   Profiles.AddRange(profiles);
   ```

3. **Reselect by ID for Edit, by Name for Create/Duplicate**
   ```csharp
   // After Edit - use ID (stable identifier)
   SelectedProfile = profiles.FirstOrDefault(p => p.Id == editedId);

   // After Create/Duplicate - use Name (ID not known beforehand)
   SelectedProfile = profiles.FirstOrDefault(p => p.Name == createdName);
   ```

**Key Benefits**:
- Eliminates 50+ lines of boilerplate per ViewModel
- Consistent success patterns across all profile types
- Centralized error handling and logging
- Easy to test (mock dialog service)
- Type-safe generic implementation

**Key Files**:
- `src/S7Tools/Services/ProfileEditDialogService.cs` - Service implementation
- `src/S7Tools/ViewModels/Dialogs/*ProfileDialogViewModel.cs` - Dialog ViewModels
- `src/S7Tools/ViewModels/Profiles/*ProfilesViewModel.cs` - Usage examples

---

## Wizard Patterns

### 17. Multi-Step Wizard Pattern

**Description**: Multi-step wizard with navigation, validation, and state management

**Purpose**:
- Guide users through complex multi-step workflows
- Validate each step before proceeding
- Maintain state across steps
- Support navigation (Next, Previous, Finish)

**Architecture**:
```
JobWizardViewModel (Coordinator)
├── JobWizardSerialStepViewModel (Step 1)
├── JobWizardSocatStepViewModel (Step 2)
├── JobWizardPowerSupplyStepViewModel (Step 3)
├── JobWizardMemoryRegionStepViewModel (Step 4)
└── JobWizardSummaryStepViewModel (Step 5)
```

**Base Step ViewModel**:
```csharp
public abstract class WizardStepViewModel : ViewModelBase
{
    private bool _isValid;
    private string _validationErrors = string.Empty;

    public bool IsValid
    {
        get => _isValid;
        protected set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    public string ValidationErrors
    {
        get => _validationErrors;
        protected set => this.RaiseAndSetIfChanged(ref _validationErrors, value);
    }

    public abstract string StepTitle { get; }
    public abstract int StepNumber { get; }

    public abstract void ValidateStep();
    public abstract Task LoadDataAsync();
    public abstract Task<bool> SaveStepDataAsync();
}
```

**Coordinator ViewModel**:
```csharp
public class JobWizardViewModel : ViewModelBase
{
    private readonly List<WizardStepViewModel> _steps;
    private int _currentStepIndex;
    private WizardStepViewModel _currentStep;

    public ReactiveCommand<Unit, Unit> NextCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousCommand { get; }
    public ReactiveCommand<Unit, Unit> FinishCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public JobWizardViewModel(/* inject step ViewModels */)
    {
        _steps = new List<WizardStepViewModel>
        {
            _serialStep,
            _socatStep,
            _powerSupplyStep,
            _memoryRegionStep,
            _summaryStep
        };

        // Command validation
        var canGoNext = this.WhenAnyValue(
            x => x.CurrentStep.IsValid,
            x => x.CurrentStepIndex,
            (valid, index) => valid && index < _steps.Count - 1);

        var canGoPrevious = this.WhenAnyValue(
            x => x.CurrentStepIndex,
            index => index > 0);

        var canFinish = this.WhenAnyValue(
            x => x.CurrentStepIndex,
            x => x.CurrentStep.IsValid,
            (index, valid) => index == _steps.Count - 1 && valid);

        NextCommand = ReactiveCommand.CreateFromTask(NextAsync, canGoNext);
        PreviousCommand = ReactiveCommand.Create(Previous, canGoPrevious);
        FinishCommand = ReactiveCommand.CreateFromTask(FinishAsync, canFinish);
        CancelCommand = ReactiveCommand.Create(Cancel);

        // Initialize
        CurrentStep = _steps[0];
    }

    private async Task NextAsync()
    {
        // Validate current step
        CurrentStep.ValidateStep();
        if (!CurrentStep.IsValid)
            return;

        // Save step data
        if (!await CurrentStep.SaveStepDataAsync())
            return;

        // Move to next
        CurrentStepIndex++;
        CurrentStep = _steps[CurrentStepIndex];

        // Load next step data
        await CurrentStep.LoadDataAsync();
    }

    private void Previous()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
            CurrentStep = _steps[CurrentStepIndex];
        }
    }

    private async Task FinishAsync()
    {
        try
        {
            StatusMessage = UIStrings.Status_CreatingJob;

            // Collect all step data
            var jobProfile = new JobProfile
            {
                SerialPortProfileId = _serialStep.SelectedProfileId,
                SocatProfileId = _socatStep.SelectedProfileId,
                PowerSupplyProfileId = _powerSupplyStep.SelectedProfileId,
                MemoryRegionProfileId = _memoryRegionStep.SelectedProfileId,
                SelectedSegments = _memoryRegionStep.GetSelectedSegments()
            };

            // Create job
            var created = await _jobService.CreateAsync(jobProfile);

            StatusMessage = UIStrings.Status_JobCreated;

            // Close wizard with success
            CloseDialog(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job");
            StatusMessage = string.Format(UIStrings.Error_CreateJobFailed, ex.Message);
        }
    }
}
```

**Step Implementation Example** (Memory Region Step):
```csharp
public class JobWizardMemoryRegionStepViewModel : WizardStepViewModel
{
    private readonly IMemoryRegionProfileService _profileService;
    private ObservableCollection<MemoryRegionProfile> _profiles = new();
    private MemoryRegionProfile? _selectedProfile;

    public override string StepTitle => "Memory Region";
    public override int StepNumber => 4;

    public async override Task LoadDataAsync()
    {
        try
        {
            StatusMessage = UIStrings.Status_LoadingMemoryRegionProfiles;

            var profiles = await _profileService.GetAllAsync();

            Profiles.Clear();
            Profiles.AddRange(profiles);

            // Auto-select default
            SelectedProfile = profiles.FirstOrDefault(p => p.IsDefault)
                           ?? profiles.FirstOrDefault();

            if (Profiles.Count == 0)
            {
                StatusMessage = UIStrings.Status_NoProfilesAvailable;
                IsValid = false;
            }
            else
            {
                StatusMessage = string.Empty;
                ValidateStep();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles");
            StatusMessage = string.Format(UIStrings.Error_LoadingProfiles, ex.Message);
            IsValid = false;
        }
    }

    public override void ValidateStep()
    {
        var errors = new List<string>();

        if (SelectedProfile == null)
        {
            errors.Add(UIStrings.Validation_MemoryRegionRequired);
            IsValid = false;
            return;
        }

        var selectedSegments = SelectedProfile.GetSelectedSegments().ToList();

        if (selectedSegments.Count == 0)
        {
            errors.Add(UIStrings.Validation_AtLeastOneSegmentRequired);
        }
        else if (selectedSegments.Count > 1)
        {
            // Warn if non-contiguous (not an error)
            if (!SelectedProfile.AreSelectedSegmentsContiguous())
            {
                ValidationWarning = UIStrings.Warning_NonContiguousSegments;
            }
        }

        IsValid = errors.Count == 0;
        ValidationErrors = string.Join(Environment.NewLine, errors);
    }

    public override Task<bool> SaveStepDataAsync()
    {
        // Save selection to shared wizard state
        WizardState.MemoryRegionProfileId = SelectedProfile?.Id;
        WizardState.SelectedSegments = SelectedProfile?.GetSelectedSegments().ToList();
        return Task.FromResult(true);
    }

    public List<MemorySegment> GetSelectedSegments()
        => SelectedProfile?.GetSelectedSegments().ToList() ?? new();
}
```

**Fallback Mechanism** (JobWizardPlaceholderViewModel):
```csharp
// Active fallback when step-specific ViewModels are unavailable
public class JobWizardPlaceholderViewModel : WizardStepViewModel
{
    public override string StepTitle => "Loading...";
    public override int StepNumber => 0;

    public override void ValidateStep()
    {
        IsValid = true; // Always valid (placeholder)
    }

    public override Task LoadDataAsync() => Task.CompletedTask;
    public override Task<bool> SaveStepDataAsync() => Task.FromResult(true);
}
```

**Key Features**:
- Step-by-step validation
- Automatic navigation control (Next/Previous/Finish)
- Shared wizard state across steps
- Async data loading per step
- Fallback mechanism for missing steps
- Integration with profile services
- Comprehensive error handling

**Key Files**:
- `src/S7Tools/ViewModels/Jobs/JobWizardViewModel.cs` - Coordinator
- `src/S7Tools/ViewModels/Jobs/JobWizard*StepViewModel.cs` - Step ViewModels
- `src/S7Tools/ViewModels/Jobs/JobWizardPlaceholderViewModel.cs` - Fallback
- `src/S7Tools/Views/Jobs/JobWizardView.axaml` - Wizard UI

---

## Settings Management Patterns

### 18. Application Settings Service Pattern

**Description**: Centralized settings management with change notification and path resolution

**Purpose**:
- Single source of truth for all application settings
- Type-safe setting access
- Change notification for reactive updates
- Path resolution (relative → absolute)
- Settings schema validation

**Service Interface**:
```csharp
public interface IApplicationSettingsService
{
    // Get/Set settings
    T? GetSetting<T>(string key, T? defaultValue = default);
    void SetSetting<T>(string key, T value);

    // Path resolution
    string ResolvePath(string path);
    string GetProfilePath(string settingKey, string defaultFileName);

    // Change notification
    event EventHandler<SettingChangedEventArgs> SettingChanged;

    // Bulk operations
    void ResetToDefaults();
    Task SaveAsync();
    Task LoadAsync();
}

public class SettingChangedEventArgs : EventArgs
{
    public string Key { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}
```

**Implementation**:
```csharp
public class ApplicationSettingsService : IApplicationSettingsService
{
    private readonly Dictionary<string, object> _settings = new();
    private readonly IPathService _pathService;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    public T? GetSetting<T>(string key, T? defaultValue = default)
    {
        _semaphore.Wait();
        try
        {
            if (_settings.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                // Try conversion
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void SetSetting<T>(string key, T value)
    {
        _semaphore.Wait();
        try
        {
            var oldValue = _settings.TryGetValue(key, out var existing)
                ? existing
                : null;

            _settings[key] = value!;

            // Raise change notification
            SettingChanged?.Invoke(this, new SettingChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = value
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public string ResolvePath(string path)
    {
        return _pathService.ResolvePath(path);
    }

    public string GetProfilePath(string settingKey, string defaultFileName)
    {
        var configuredPath = GetSetting<string>(settingKey);

        if (!string.IsNullOrEmpty(configuredPath))
        {
            var resolved = ResolvePath(configuredPath);
            if (File.Exists(resolved))
                return resolved;
        }

        // Fallback to default
        return ResolvePath(defaultFileName);
    }
}
```

**Settings Schema** (dot notation):
```csharp
public static class SettingKeys
{
    // Logging settings
    public const string LoggingEnabled = "logging.enabled";
    public const string LoggingLevel = "logging.level";
    public const string LoggingRetentionDays = "logging.retentionDays";

    // UI settings
    public const string UiShowDemoData = "ui.showDemoData";
    public const string UiShowPlaceholders = "ui.showPlaceholders";
    public const string UiTheme = "ui.theme";

    // Profile paths
    public const string ProfilesSerialPath = "profiles.serial.path";
    public const string ProfilesSocatPath = "profiles.socat.path";
    public const string ProfilesPowerSupplyPath = "profiles.powerSupply.path";
    public const string ProfilesMemoryRegionPath = "profiles.memoryRegion.path";
    public const string ProfilesJobPath = "profiles.job.path";
}
```

**ViewModel Integration** (Settings Refresh Pattern):
```csharp
public class SomeViewModel : ViewModelBase
{
    private readonly IApplicationSettingsService _settingsService;
    private readonly IPathService _pathService;
    private string _profilesPath = string.Empty;

    public SomeViewModel(IApplicationSettingsService settingsService)
    {
        _settingsService = settingsService;

        // Subscribe to settings changes (with key filter)
        _settingsService.SettingChanged += OnSettingChanged;

        // Initial load
        RefreshFromSettings();
    }

    private void OnSettingChanged(object? sender, SettingChangedEventArgs e)
    {
        // Filter by relevant keys
        if (e.Key.StartsWith("profiles."))
        {
            RefreshFromSettings();
        }
    }

    private void RefreshFromSettings()
    {
        // Three-tier fallback:
        // 1. Resolved path from settings
        // 2. Profile-specific default
        // 3. Global default

        var configuredPath = _settingsService.GetSetting<string>(
            SettingKeys.ProfilesSerialPath);

        if (!string.IsNullOrEmpty(configuredPath))
        {
            ProfilesPath = _pathService.ResolvePath(configuredPath);
        }
        else
        {
            ProfilesPath = _pathService.ResolvePath(
                "src/resources/SerialProfiles/profiles.json");
        }
    }

    public override void Dispose()
    {
        // CRITICAL: Unsubscribe to prevent memory leaks
        _settingsService.SettingChanged -= OnSettingChanged;
        base.Dispose();
    }
}
```

**Path Resolution Pattern**:
```csharp
public interface IPathService
{
    string ResolvePath(string path);
    string GetBasePath();
}

public class PathService : IPathService
{
    private readonly string _basePath;

    public PathService()
    {
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    public string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _basePath;

        // Already absolute
        if (Path.IsPathRooted(path))
            return path;

        // Relative - combine with base
        return Path.GetFullPath(Path.Combine(_basePath, path));
    }

    public string GetBasePath() => _basePath;
}
```

**Key Features**:
- Type-safe setting access with generics
- Change notification with event filtering
- Three-tier path fallback (configured → profile default → global default)
- Relative/absolute path resolution
- Thread-safe with semaphore
- Bulk save/load operations
- Settings schema documentation

**Key Files**:
- `src/S7Tools/Services/ApplicationSettingsService.cs` - Service implementation
- `src/S7Tools/Services/PathService.cs` - Path resolution
- `src/S7Tools/Constants/SettingKeys.cs` - Schema constants
- `docs/SETTINGS_SCHEMA.md` - Full documentation

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

1. **Architecture**: Clean Architecture, DI, ViewModels/Views categorization
2. **Profiles**: Unified management with StandardProfileManager<T>, Memory Region profiling
3. **Concurrency**: Internal Method Pattern, UI marshaling, resource coordination
4. **MVVM**: Reactive properties, commands, disposal
5. **Services**: Decorator pattern, retry logic, Application Settings Service
6. **Tasks**: State management, priority scheduling
7. **Dialogs**: ProfileEditDialogService for consistent CRUD workflows
8. **Wizards**: Multi-step wizards with validation and navigation (Job Wizard)
9. **Settings**: Centralized settings with change notification and path resolution
10. **Errors**: Custom exception hierarchy
11. **Testing**: AAA pattern, async tests, exception tests

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

## Related Documentation

- [Index](../INDEX.md)
- [_Index](../architecture/_index.md)
- [Ai Agent Guide](../guides/ai-agent-guide.md)
- [Code Style](../guides/code-style.md)
- [Development Workflow](../guides/development-workflow.md)
- [Memory Bank Usage](../guides/memory-bank-usage.md)
- [_Index](../guides/migration/_index.md)
- [Deprecated Patterns](../guides/migration/deprecated-patterns.md)
- [Onboarding](../guides/onboarding.md)
- [Testing Guide](../guides/testing-guide.md)
- [Custom Exceptions](custom-exceptions.md)
- [Internal Method](internal-method.md)
- [Profile Management](profile-management.md)
- [Resource Coordination](resource-coordination.md)
- [Reusable Controls](reusable-controls.md)
- [Pattern Template](../templates/pattern-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
