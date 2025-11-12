# IResourceCoordinator Contract

**Interface**: IResourceCoordinator
**Location**: `src/S7Tools.Core/Services/Interfaces/IResourceCoordinator.cs`
**Purpose**: Resource conflict detection and locking for parallel job execution
**Layer**: Core (contract) → Application (implementation)

## Contract Definition

```csharp
namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Coordinates resource access across concurrent jobs to prevent conflicts.
/// Manages serial ports, TCP ports, and modbus connections with lock-free TryAcquire/Release pattern.
/// </summary>
public interface IResourceCoordinator
{
    /// <summary>
    /// Attempts to acquire all specified resources atomically.
    /// Either acquires ALL resources or acquires NONE (all-or-nothing semantics).
    /// </summary>
    /// <param name="resources">Resources to acquire</param>
    /// <returns>True if all resources acquired, false if any resource locked by another job</returns>
    bool TryAcquire(ResourceKey[] resources);

    /// <summary>
    /// Attempts to acquire all specified resources atomically with timeout.
    /// Retries acquisition for up to the specified timeout period.
    /// </summary>
    /// <param name="resources">Resources to acquire</param>
    /// <param name="timeout">Maximum time to wait for resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all resources acquired within timeout, false otherwise</returns>
    Task<bool> TryAcquireAsync(
        ResourceKey[] resources,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases all specified resources previously acquired via TryAcquire.
    /// Safe to call with resources not currently held (idempotent).
    /// </summary>
    /// <param name="resources">Resources to release</param>
    void Release(ResourceKey[] resources);

    /// <summary>
    /// Checks if specified resources are currently locked by any job.
    /// </summary>
    /// <param name="resources">Resources to check</param>
    /// <returns>True if all resources available, false if any locked</returns>
    bool AreAvailable(ResourceKey[] resources);

    /// <summary>
    /// Retrieves all currently locked resources with job information.
    /// </summary>
    /// <returns>Dictionary mapping resource keys to job IDs holding the lock</returns>
    IReadOnlyDictionary<ResourceKey, int> GetLockedResources();

    /// <summary>
    /// Event raised when resource lock state changes (acquired or released).
    /// Subscribe for real-time resource availability monitoring.
    /// </summary>
    event EventHandler<ResourceLockChangedEventArgs>? ResourceLockChanged;
}
```

## Event Argument Types

```csharp
/// <summary>
/// Event arguments for resource lock state changes.
/// </summary>
public class ResourceLockChangedEventArgs : EventArgs
{
    public ResourceKey Resource { get; init; }
    public ResourceLockAction Action { get; init; } // Acquired or Released
    public int? JobId { get; init; } // Job holding lock (null if Released)
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Resource lock action type.
/// </summary>
public enum ResourceLockAction
{
    Acquired = 0,
    Released = 1
}
```

## All-or-Nothing Atomicity

The `TryAcquire` method guarantees **atomic acquisition** of all resources:

```csharp
// Job requires 3 resources
var resources = new[]
{
    new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0"),
    new ResourceKey(ResourceType.Tcp, "10102"),
    new ResourceKey(ResourceType.Modbus, "192.168.1.10:502")
};

if (_coordinator.TryAcquire(resources))
{
    // GUARANTEED: All 3 resources are now locked
    try
    {
        await ExecuteJob();
    }
    finally
    {
        _coordinator.Release(resources); // Release all
    }
}
else
{
    // GUARANTEED: Zero resources acquired (none partially locked)
    // Queue job for retry when resources become available
}
```

**Why All-or-Nothing Matters**:
- Prevents deadlocks (Job A holds serial, waits for TCP; Job B holds TCP, waits for serial)
- Simplifies error handling (no partial cleanup needed)
- Enables fairness (first job to acquire ALL resources wins)

## Implementation Requirements

### 1. Thread-Safe Lock-Free Design (Article IV)

**Implementation Pattern**: Use `ConcurrentDictionary<ResourceKey, int>` to track locks without semaphores.

```csharp
public class ResourceCoordinator : IResourceCoordinator
{
    private readonly ConcurrentDictionary<ResourceKey, int> _locks = new();
    private readonly object _atomicLock = new(); // Only for atomic multi-resource acquisition

    public bool TryAcquire(ResourceKey[] resources)
    {
        lock (_atomicLock) // Single lock for atomicity, NOT nested semaphores
        {
            // Step 1: Check if all resources available
            if (resources.Any(r => _locks.ContainsKey(r)))
            {
                return false; // At least one resource locked
            }

            // Step 2: Acquire all resources atomically
            foreach (var resource in resources)
            {
                _locks.TryAdd(resource, jobId); // All succeed because checked above
            }

            // Step 3: Notify listeners
            foreach (var resource in resources)
            {
                OnResourceLockChanged(new ResourceLockChangedEventArgs
                {
                    Resource = resource,
                    Action = ResourceLockAction.Acquired,
                    JobId = jobId,
                    Timestamp = DateTime.UtcNow
                });
            }

            return true;
        }
    }

    public void Release(ResourceKey[] resources)
    {
        foreach (var resource in resources)
        {
            if (_locks.TryRemove(resource, out var jobId))
            {
                OnResourceLockChanged(new ResourceLockChangedEventArgs
                {
                    Resource = resource,
                    Action = ResourceLockAction.Released,
                    JobId = null,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
```

**Critical**: The `_atomicLock` is the **ONLY** lock in the entire resource coordination system. No nested semaphores, no per-resource locks, no deadlock risk.

### 2. Async Retry Pattern

```csharp
public async Task<bool> TryAcquireAsync(
    ResourceKey[] resources,
    TimeSpan timeout,
    CancellationToken ct)
{
    var stopwatch = Stopwatch.StartNew();

    while (stopwatch.Elapsed < timeout && !ct.IsCancellationRequested)
    {
        if (TryAcquire(resources))
        {
            return true; // Acquired successfully
        }

        // Wait 100ms before retry
        await Task.Delay(100, ct).ConfigureAwait(false);
    }

    return false; // Timeout or canceled
}
```

### 3. Resource Availability Check (Non-Blocking)

```csharp
public bool AreAvailable(ResourceKey[] resources)
{
    // Lock-free read (ConcurrentDictionary is thread-safe for reads)
    return resources.All(r => !_locks.ContainsKey(r));
}

public IReadOnlyDictionary<ResourceKey, int> GetLockedResources()
{
    // Snapshot of current locks (thread-safe)
    return new Dictionary<ResourceKey, int>(_locks);
}
```

## Resource Conflict Scenarios

| Scenario | Job A Resources | Job B Resources | Result |
|----------|----------------|----------------|--------|
| **No Conflict** | Serial: /dev/ttyUSB0<br>TCP: 10102<br>Modbus: 192.168.1.10:502 | Serial: /dev/ttyUSB1<br>TCP: 10103<br>Modbus: 192.168.1.11:502 | ✅ Both acquire, run parallel |
| **Serial Conflict** | Serial: /dev/ttyUSB0 | Serial: /dev/ttyUSB0 | ❌ Job B blocks, retries when A releases |
| **TCP Conflict** | TCP: 10102 | TCP: 10102 | ❌ Job B blocks, retries when A releases |
| **Modbus Conflict** | Modbus: 192.168.1.10:502 | Modbus: 192.168.1.10:502 | ❌ Job B blocks, retries when A releases |
| **Partial Conflict** | Serial: /dev/ttyUSB0<br>TCP: 10102 | Serial: /dev/ttyUSB1<br>TCP: 10102 | ❌ Job B blocks (TCP conflict), acquires ZERO resources |

## Usage Example

```csharp
// In JobScheduler
private async Task ExecuteJobAsync(Job job, CancellationToken ct)
{
    var resources = new[]
    {
        new ResourceKey(ResourceType.Serial, job.ProfileSet.Serial.Device),
        new ResourceKey(ResourceType.Tcp, job.ProfileSet.Socat.Port.ToString()),
        new ResourceKey(ResourceType.Modbus, job.ProfileSet.Power.ModbusAddress)
    };

    // Try acquire with 30 second timeout
    if (!await _resourceCoordinator.TryAcquireAsync(resources, TimeSpan.FromSeconds(30), ct))
    {
        _logger.LogWarning("Job {JobId} unable to acquire resources after 30s timeout", job.Id);
        job.State = JobState.Failed;
        job.ErrorMessage = "Resource acquisition timeout";
        return;
    }

    try
    {
        _logger.LogInformation("Job {JobId} acquired resources: {Resources}",
            job.Id,
            string.Join(", ", resources.Select(r => $"{r.Type}:{r.Identifier}")));

        await _bootloaderService.DumpMemoryAsync(job.ProfileSet, progress, ct);
    }
    finally
    {
        // ALWAYS release, even on exception
        _resourceCoordinator.Release(resources);
        _logger.LogInformation("Job {JobId} released resources", job.Id);
    }
}
```

## Testing Strategy

```csharp
[Fact]
public void TryAcquire_AvailableResources_ReturnsTrue()
{
    // Arrange
    var resources = new[]
    {
        new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0"),
        new ResourceKey(ResourceType.Tcp, "10102")
    };

    // Act
    var acquired = _coordinator.TryAcquire(resources);

    // Assert
    acquired.Should().BeTrue();
    _coordinator.GetLockedResources().Should().HaveCount(2);
}

[Fact]
public void TryAcquire_PartialConflict_AcquiresNothing()
{
    // Arrange
    var resources1 = new[] { new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0"), new ResourceKey(ResourceType.Tcp, "10102") };
    var resources2 = new[] { new ResourceKey(ResourceType.Serial, "/dev/ttyUSB1"), new ResourceKey(ResourceType.Tcp, "10102") }; // TCP conflict

    _coordinator.TryAcquire(resources1); // Lock Serial + TCP

    // Act
    var acquired = _coordinator.TryAcquire(resources2);

    // Assert
    acquired.Should().BeFalse(); // Fails due to TCP conflict
    _coordinator.GetLockedResources().Should().HaveCount(2); // Only resources1 locked, resources2 acquired ZERO
    _coordinator.GetLockedResources().Should().NotContainKey(new ResourceKey(ResourceType.Serial, "/dev/ttyUSB1")); // Verify /dev/ttyUSB1 NOT locked
}

[Fact]
public async Task TryAcquireAsync_ResourceBecomesAvailable_RetrySucceeds()
{
    // Arrange
    var resources = new[] { new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0") };
    _coordinator.TryAcquire(resources); // Initially locked

    // Act
    var acquireTask = _coordinator.TryAcquireAsync(resources, TimeSpan.FromSeconds(5), CancellationToken.None);
    await Task.Delay(500); // Wait 500ms
    _coordinator.Release(resources); // Release resource

    var acquired = await acquireTask;

    // Assert
    acquired.Should().BeTrue(); // Retry succeeded after release
}

[Fact]
public void Release_LockedResources_RaisesEvent()
{
    // Arrange
    var eventRaised = false;
    ResourceLockChangedEventArgs? eventArgs = null;
    _coordinator.ResourceLockChanged += (s, e) => { eventRaised = true; eventArgs = e; };

    var resource = new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0");
    _coordinator.TryAcquire(new[] { resource });

    // Act
    _coordinator.Release(new[] { resource });

    // Assert
    eventRaised.Should().BeTrue();
    eventArgs.Should().NotBeNull();
    eventArgs!.Action.Should().Be(ResourceLockAction.Released);
    eventArgs.Resource.Should().Be(resource);
}

[Fact]
public async Task ConcurrentAcquire_SameResource_OnlyOneSucceeds()
{
    // Arrange
    var resource = new ResourceKey(ResourceType.Serial, "/dev/ttyUSB0");
    var successCount = 0;

    // Act - 10 concurrent attempts to acquire same resource
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => Task.Run(() =>
        {
            if (_coordinator.TryAcquire(new[] { resource }))
            {
                Interlocked.Increment(ref successCount);
            }
        }))
        .ToArray();

    await Task.WhenAll(tasks);

    // Assert
    successCount.Should().Be(1); // Only ONE acquisition succeeds
    _coordinator.GetLockedResources().Should().HaveCount(1);
}
```

## Performance Requirements

| Metric | Target | Justification |
|--------|--------|---------------|
| TryAcquire latency | <1ms | Lock-free read of ConcurrentDictionary |
| Release latency | <1ms | ConcurrentDictionary.TryRemove is O(1) |
| Concurrent acquisitions | 100+ req/s | Bottleneck is bootloader execution (minutes), not locking |
| Memory overhead | ~100 bytes/lock | ResourceKey (16 bytes) + int jobId (4 bytes) + dictionary overhead |

## Error Handling

### Idempotent Release

```csharp
// Safe to call Release multiple times with same resources
_coordinator.Release(resources); // First release
_coordinator.Release(resources); // Second release (no-op, no exception)
```

### Graceful Degradation

```csharp
// If TryAcquire fails, job remains in queue for retry
if (!_coordinator.TryAcquire(resources))
{
    _logger.LogInformation("Job {JobId} resources locked, will retry", job.Id);
    // Job stays in Queued state, scheduler retries on next cycle
    return;
}
```

## Dependencies

- None (pure coordination service with ConcurrentDictionary)
- ILogger<ResourceCoordinator> (structured logging - optional)

## References

- **Specification**: FR-004 (parallel execution), FR-005 (resource conflict prevention), SC-006 (100% conflict prevention)
- **Constitution**: Article IV (Thread Safety - lock-free where possible, single lock for atomicity)
- **Research**: RQ2 (Event-Driven Scheduler decision - TryAcquire/Release pattern)
- **Data Model**: ResourceKey value object definition
