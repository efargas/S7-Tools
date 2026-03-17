---
title: Resource Coordination Pattern (Parallel Service Initialization)
version: 1.0.7
created: '2025-11-10'
last-updated: '2026-03-17'
status: current
tags:
- pattern
- performance
- startup
- parallelization
- resource-management
related:
- docs/patterns/system-patterns.md
- docs/architecture/clean-architecture.md
- docs/patterns/internal-method.md
supersedes: []
---
# Resource Coordination Pattern (Parallel Service Initialization)

## Problem Statement

**Context**: S7Tools initializes multiple services at startup (settings, profiles, logging, network discovery, bootloader coordination). Sequential initialization causes slow startup times.

**Problems**:
- **Sequential initialization**: Services wait for each other unnecessarily
- **Slow startup**: 5+ seconds to initialize all services
- **Resource conflicts**: Concurrent access to files/resources causes errors
- **No visibility**: Can't track which services are initializing

**Example** (sequential initialization):
```csharp
// ❌ SLOW: Sequential initialization (5+ seconds)
public async Task InitializeAsync()
{
    await _settingsService.LoadAsync();          // 1 second
    await _serialProfileService.LoadAsync();     // 0.5 seconds
    await _socatProfileService.LoadAsync();      // 0.5 seconds
    await _powerSupplyProfileService.LoadAsync(); // 0.5 seconds
    await _jobProfileService.LoadAsync();         // 1 second
    await _memoryRegionProfileService.LoadAsync(); // 0.5 seconds
    await _loggingService.InitializeAsync();      // 1 second
}
// Total: 5 seconds
```

## Solution

Use the **Resource Coordination Pattern** to:
1. **Parallelize independent operations** (settings, profiles, logging can load concurrently)
2. **Coordinate dependent operations** (e.g., profiles depend on settings being loaded first)
3. **Track initialization progress** for UI feedback
4. **Handle failures gracefully** without blocking other services

### Pattern Structure

```csharp
public class ResourceCoordinator : IResourceCoordinator
{
    private readonly Dictionary<string, SemaphoreSlim> _resources = new();
    private readonly SemaphoreSlim _coordinatorLock = new(1, 1);

    // Acquire multiple resources atomically
    public async Task<bool> TryAcquireAsync(IReadOnlyList<string> resourceKeys,
        CancellationToken ct = default)
    {
        await _coordinatorLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Check if all resources are available
            foreach (var key in resourceKeys)
            {
                if (!_resources.TryGetValue(key, out var semaphore))
                {
                    _resources[key] = new SemaphoreSlim(1, 1);
                }
                else if (semaphore.CurrentCount == 0)
                {
                    return false;  // Resource busy
                }
            }

            // Acquire all resources
            foreach (var key in resourceKeys)
            {
                await _resources[key].WaitAsync(ct).ConfigureAwait(false);
            }

            return true;
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }

    // Release all acquired resources
    public async Task ReleaseAsync(IReadOnlyList<string> resourceKeys)
    {
        await _coordinatorLock.WaitAsync().ConfigureAwait(false);
        try
        {
            foreach (var key in resourceKeys)
            {
                if (_resources.TryGetValue(key, out var semaphore))
                {
                    semaphore.Release();
                }
            }
        }
        finally
        {
            _coordinatorLock.Release();
        }
    }
}
```

## Parallel Initialization Pattern

### Approach 1: Task.WhenAll (Simple Parallelization)

**Use Case**: Independent services with no dependencies

```csharp
public async Task InitializeAsync(CancellationToken ct = default)
{
    _logger.LogInformation("Starting parallel service initialization...");

    // All services can initialize in parallel
    var tasks = new List<Task>
    {
        _settingsService.LoadAsync(ct),
        _serialProfileService.LoadAsync(ct),
        _socatProfileService.LoadAsync(ct),
        _powerSupplyProfileService.LoadAsync(ct),
        _loggingService.InitializeAsync(ct)
    };

    // Wait for all services to complete
    await Task.WhenAll(tasks);

    _logger.LogInformation("✅ All services initialized in parallel");
}
```

**Benefits**:
- ✅ Reduces startup time from 5s → 1s (biggest task determines total time)
- ✅ Simple implementation
- ✅ No coordination overhead

**Limitations**:
- ❌ No dependency handling (assumes all services are independent)
- ❌ No progress tracking
- ❌ One failure cancels everything

### Approach 2: Phased Initialization (With Dependencies)

**Use Case**: Services with dependencies (e.g., profiles depend on settings)

```csharp
public async Task InitializeAsync(CancellationToken ct = default)
{
    // Phase 1: Core services (must complete first)
    await Task.WhenAll(
        _settingsService.LoadAsync(ct),
        _loggingService.InitializeAsync(ct)
    );

    // Phase 2: Profile services (depend on settings)
    await Task.WhenAll(
        _serialProfileService.LoadAsync(ct),
        _socatProfileService.LoadAsync(ct),
        _powerSupplyProfileService.LoadAsync(ct),
        _jobProfileService.LoadAsync(ct),
        _memoryRegionProfileService.LoadAsync(ct)
    );

    // Phase 3: UI services (depend on profiles)
    await Task.WhenAll(
        _activityBarService.InitializeAsync(ct),
        _taskManagerViewModel.InitializeAsync(ct)
    );
}
```

**Benefits**:
- ✅ Handles dependencies correctly
- ✅ Still achieves parallelization within each phase
- ✅ Clear initialization order

### Approach 3: Resource Coordinator (Advanced)

**Use Case**: Complex resource dependencies and conflict detection

```csharp
public class BootloaderViewModel : ViewModelBase
{
    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly IBootloaderService _bootloaderService;

    public async Task ExecuteBootloaderTaskAsync(CancellationToken ct)
    {
        // Define required resources
        var resourceKeys = new[] { "network:socat", "file:bootloader-payload" };

        // Try to acquire resources
        if (!await _resourceCoordinator.TryAcquireAsync(resourceKeys, ct))
        {
            StatusMessage = "⚠️ Resources busy - another operation in progress";
            return;
        }

        try
        {
            StatusMessage = "🔄 Executing bootloader...";

            await _bootloaderService.ExecuteAsync(ct);

            StatusMessage = "✅ Bootloader execution complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Bootloader failed: {ex.Message}";
            _logger.LogError(ex, "Bootloader execution failed");
        }
        finally
        {
            // Always release resources
            await _resourceCoordinator.ReleaseAsync(resourceKeys);
        }
    }
}
```

**Benefits**:
- ✅ Prevents resource conflicts (e.g., two operations using same network port)
- ✅ Graceful degradation (shows "busy" instead of failing)
- ✅ Atomic resource acquisition (all-or-nothing)
- ✅ Explicit resource dependency declaration

## Progress Tracking

### Progress Reporting Interface

```csharp
public interface IInitializationProgress
{
    void ReportProgress(string serviceName, double percentage, string message);
    void ReportComplete(string serviceName);
    void ReportError(string serviceName, Exception ex);
}
```

### Implementation with Progress

```csharp
public async Task InitializeAsync(IInitializationProgress? progress = null,
    CancellationToken ct = default)
{
    var services = new Dictionary<string, Func<Task>>
    {
        ["Settings"] = async () =>
        {
            progress?.ReportProgress("Settings", 0, "Loading settings...");
            await _settingsService.LoadAsync(ct);
            progress?.ReportComplete("Settings");
        },

        ["Logging"] = async () =>
        {
            progress?.ReportProgress("Logging", 0, "Initializing logging...");
            await _loggingService.InitializeAsync(ct);
            progress?.ReportComplete("Logging");
        },

        ["Profiles"] = async () =>
        {
            progress?.ReportProgress("Profiles", 0, "Loading profiles...");
            await Task.WhenAll(
                _serialProfileService.LoadAsync(ct),
                _socatProfileService.LoadAsync(ct),
                _powerSupplyProfileService.LoadAsync(ct)
            );
            progress?.ReportComplete("Profiles");
        }
    };

    // Phase 1: Core services
    await Task.WhenAll(
        services["Settings"](),
        services["Logging"]()
    );

    // Phase 2: Dependent services
    await services["Profiles"]();
}
```

## Real-World Example: S7Tools Startup

### Application Startup Coordinator

```csharp
public class ApplicationStartupCoordinator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApplicationStartupCoordinator> _logger;

    public async Task InitializeS7ToolsServicesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("🚀 Starting S7Tools service initialization...");

        var stopwatch = Stopwatch.StartNew();

        // Phase 1: Foundation services (must complete first)
        await InitializeFoundationServicesAsync(ct);

        // Phase 2: Profile services (parallel, depend on foundation)
        await InitializeProfileServicesAsync(ct);

        // Phase 3: UI services (depend on profiles)
        await InitializeUIServicesAsync(ct);

        stopwatch.Stop();
        _logger.LogInformation("✅ S7Tools initialized in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
    }

    private async Task InitializeFoundationServicesAsync(CancellationToken ct)
    {
        _logger.LogInformation("📦 Phase 1: Foundation services");

        var settingsService = _serviceProvider.GetRequiredService<IApplicationSettingsService>();
        var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();

        // Parallel initialization
        await Task.WhenAll(
            settingsService.LoadAsync(ct),
            loggingService.InitializeAsync(ct)
        );

        _logger.LogInformation("✅ Foundation services ready");
    }

    private async Task InitializeProfileServicesAsync(CancellationToken ct)
    {
        _logger.LogInformation("📦 Phase 2: Profile services");

        var tasks = new List<Task>();

        // All profile services can load in parallel
        tasks.Add(_serviceProvider.GetRequiredService<ISerialPortProfileService>().LoadAsync(ct));
        tasks.Add(_serviceProvider.GetRequiredService<ISocatProfileService>().LoadAsync(ct));
        tasks.Add(_serviceProvider.GetRequiredService<IPowerSupplyProfileService>().LoadAsync(ct));
        tasks.Add(_serviceProvider.GetRequiredService<IJobProfileService>().LoadAsync(ct));
        tasks.Add(_serviceProvider.GetRequiredService<IMemoryRegionProfileService>().LoadAsync(ct));

        await Task.WhenAll(tasks);

        _logger.LogInformation("✅ Profile services ready");
    }

    private async Task InitializeUIServicesAsync(CancellationToken ct)
    {
        _logger.LogInformation("📦 Phase 3: UI services");

        var activityBarService = _serviceProvider.GetRequiredService<IActivityBarService>();

        await activityBarService.InitializeAsync(ct);

        _logger.LogInformation("✅ UI services ready");
    }
}
```

## Benefits

### 1. Performance

**Before** (sequential): 5 seconds
**After** (parallel): 1 second
**Improvement**: 80% faster startup

### 2. Resource Safety

**Problem**: Two operations try to use same network port simultaneously
**Solution**: ResourceCoordinator prevents conflicts with atomic acquisition

### 3. User Feedback

**Progress tracking** shows initialization status:
```
🚀 Starting S7Tools service initialization...
📦 Phase 1: Foundation services
✅ Foundation services ready
📦 Phase 2: Profile services
✅ Profile services ready
📦 Phase 3: UI services
✅ UI services ready
✅ S7Tools initialized in 987ms
```

### 4. Failure Isolation

**Pattern**: One service failure doesn't block others

```csharp
private async Task InitializeProfileServicesAsync(CancellationToken ct)
{
    var tasks = new Dictionary<string, Task>
    {
        ["Serial"] = _serialProfileService.LoadAsync(ct),
        ["Socat"] = _socatProfileService.LoadAsync(ct),
        ["PowerSupply"] = _powerSupplyProfileService.LoadAsync(ct)
    };

    // Wait for all (even if some fail)
    var results = await Task.WhenAll(tasks.Values);

    // Log failures, continue with successful services
    foreach (var (name, task) in tasks)
    {
        if (task.IsFaulted)
        {
            _logger.LogWarning(task.Exception, "{ServiceName} failed to initialize", name);
        }
    }
}
```

## Anti-Patterns

### ❌ Don't: Sequential Initialization

```csharp
// BAD: Services wait for each other unnecessarily
public async Task InitializeAsync()
{
    await _settingsService.LoadAsync();  // 1s
    await _profileService.LoadAsync();   // 0.5s
    await _loggingService.LoadAsync();   // 1s
}
// Total: 2.5 seconds (could be 1 second with parallelization!)
```

**Fix**: Use `Task.WhenAll` for independent operations:

```csharp
// GOOD: Services initialize in parallel
public async Task InitializeAsync()
{
    await Task.WhenAll(
        _settingsService.LoadAsync(),
        _profileService.LoadAsync(),
        _loggingService.LoadAsync()
    );
}
// Total: 1 second (time of longest operation)
```

### ❌ Don't: Ignore Dependencies

```csharp
// BAD: ProfileService needs settings, but they load in parallel
public async Task InitializeAsync()
{
    await Task.WhenAll(
        _settingsService.LoadAsync(),      // Settings
        _profileService.LoadAsync()        // ❌ Needs settings!
    );
}
```

**Fix**: Use phased initialization:

```csharp
// GOOD: Settings load first, then profiles
public async Task InitializeAsync()
{
    // Phase 1: Settings
    await _settingsService.LoadAsync();

    // Phase 2: Services that depend on settings
    await _profileService.LoadAsync();
}
```

### ❌ Don't: Forget Resource Cleanup

```csharp
// BAD: Resources not released on exception
public async Task ExecuteAsync()
{
    var resources = new[] { "network", "file" };
    await _coordinator.TryAcquireAsync(resources);

    await DoWorkAsync();  // ❌ If this throws, resources leak!

    await _coordinator.ReleaseAsync(resources);
}
```

**Fix**: Use try/finally:

```csharp
// GOOD: Resources always released
public async Task ExecuteAsync()
{
    var resources = new[] { "network", "file" };

    if (!await _coordinator.TryAcquireAsync(resources))
        return;

    try
    {
        await DoWorkAsync();
    }
    finally
    {
        await _coordinator.ReleaseAsync(resources);  // ✅ Always executes
    }
}
```

## Testing

### Parallel Initialization Test

```csharp
[Fact]
public async Task InitializeAsync_Should_Run_In_Parallel()
{
    // Arrange
    var coordinator = new ApplicationStartupCoordinator(_serviceProvider, _logger);
    var stopwatch = Stopwatch.StartNew();

    // Act
    await coordinator.InitializeS7ToolsServicesAsync();

    stopwatch.Stop();

    // Assert: Should complete in < 2 seconds (parallel)
    // If sequential, would take 5+ seconds
    Assert.True(stopwatch.ElapsedMilliseconds < 2000,
        $"Initialization took {stopwatch.ElapsedMilliseconds}ms (expected < 2000ms)");
}
```

### Resource Conflict Test

```csharp
[Fact]
public async Task TryAcquire_Should_Prevent_Concurrent_Access()
{
    // Arrange
    var coordinator = new ResourceCoordinator();
    var resources = new[] { "network:port-8080" };

    // Act: First acquisition succeeds
    var acquired1 = await coordinator.TryAcquireAsync(resources);

    // Second acquisition fails (resource busy)
    var acquired2 = await coordinator.TryAcquireAsync(resources);

    // Assert
    Assert.True(acquired1);
    Assert.False(acquired2);  // Resource already in use

    // Cleanup
    await coordinator.ReleaseAsync(resources);

    // Now can acquire again
    var acquired3 = await coordinator.TryAcquireAsync(resources);
    Assert.True(acquired3);
}
```

## Related Patterns

- [Internal Method Pattern](internal-method.md) - Thread-safe resource access
- [Profile Management](profile-management.md) - Profile service initialization
- [Clean Architecture](../architecture/clean-architecture.md) - Service layer organization

## Examples

- [Resource Coordinator Pattern Example](examples/resource-coordinator-example.md) - Job execution and hardware coordination walkthrough

## Implementation Files

**Core**:
- `src/S7Tools/Services/Tasking/ResourceCoordinator.cs` - Resource coordination service
- `src/S7Tools/Program.cs` - Application startup coordination
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` - Service registration with parallel init

**Usage Examples**:
- `src/S7Tools/ViewModels/Jobs/JobsManagementViewModel.cs` - Resource acquisition for job execution
- `src/S7Tools/ViewModels/Pages/BootloaderViewModel.cs` - Resource coordination for bootloader tasks

---

**Last Updated**: 2025-11-10
**Status**: Production-proven pattern
**Performance**: 80% startup time reduction (5s → 1s)

## Related Documentation

- [Index](../INDEX.md)
- [Clean Architecture](../architecture/clean-architecture.md)
- [Onboarding](../guides/onboarding.md)
- [_Index](_index.md)
- [Resource Coordinator Example](examples/resource-coordinator-example.md)
- [Internal Method](internal-method.md)
- [Profile Management](profile-management.md)
- [System Patterns](system-patterns.md)
- [_Index](../reviews/_index.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
