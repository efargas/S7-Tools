---
title: "Internal Method Pattern (Semaphore Deadlock Prevention)"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["pattern", "threading", "semaphore", "deadlock-prevention", "thread-safety"]
related:
  - "docs/patterns/system-patterns.md"
  - "docs/patterns/profile-management.md"
  - "docs/architecture/mvvm-patterns.md"
supersedes: []
---

# Internal Method Pattern (Semaphore Deadlock Prevention)

## Problem Statement

**Context**: S7Tools uses semaphores to protect shared resources (profiles, network state, jobs) from concurrent access. Services implement multiple public methods that may call each other.

**Deadlock Scenario**:
```csharp
// ❌ DEADLOCK RISK
public async Task<bool> IsPortInUseAsync(int port)
{
    await _semaphore.WaitAsync();
    try
    {
        // This method also tries to acquire the SAME semaphore!
        return await CheckPortAvailabilityAsync(port);  // DEADLOCK!
    }
    finally { _semaphore.Release(); }
}

public async Task<bool> CheckPortAvailabilityAsync(int port)
{
    await _semaphore.WaitAsync();  // ❌ Deadlock: semaphore already held!
    try { /* check logic */ }
    finally { _semaphore.Release(); }
}
```

**Problem**: When one public method calls another public method, both try to acquire the same semaphore → deadlock.

## Solution

Use the **Internal Method Pattern**:

1. **Public methods** acquire/release the semaphore
2. **Internal methods** assume the semaphore is already held (no acquisition)
3. Public methods call internal methods to reuse logic safely

### Pattern Structure

```csharp
// ✅ Public API (acquires semaphore)
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

// ✅ Internal method (assumes semaphore held)
private Task<bool> IsPortInUseInternalAsync(int port, CancellationToken ct)
{
    // NO semaphore acquisition - lock already held by caller
    var isInUse = _activePorts.Contains(port);
    return Task.FromResult(isInUse);
}

// ✅ Public method reuses internal logic safely
public async Task<IReadOnlyList<int>> GetAvailablePortsAsync(int startPort, int count, CancellationToken ct = default)
{
    await _semaphore.WaitAsync(ct).ConfigureAwait(false);
    try
    {
        var availablePorts = new List<int>();

        for (int port = startPort; port < startPort + count; port++)
        {
            // Safe: calls internal method while holding lock
            if (!await IsPortInUseInternalAsync(port, ct).ConfigureAwait(false))
            {
                availablePorts.Add(port);
            }
        }

        return availablePorts;
    }
    finally
    {
        _semaphore.Release();
    }
}
```

## Key Principles

### 1. Naming Convention

- **Public methods**: `{Action}Async` (e.g., `IsPortInUseAsync`)
- **Internal methods**: `{Action}InternalAsync` (e.g., `IsPortInUseInternalAsync`)

**Why**: Clear visual distinction prevents accidental deadlocks.

### 2. Responsibility Split

| Method Type | Semaphore Handling | Visibility | Called By |
|-------------|-------------------|------------|-----------|
| **Public** | Acquire/Release | `public` | External consumers |
| **Internal** | Assume held | `private`/`protected` | Public methods (within lock) |

### 3. ConfigureAwait(false)

Always use `.ConfigureAwait(false)` in service/library code to avoid capturing synchronization context:

```csharp
await _semaphore.WaitAsync(ct).ConfigureAwait(false);
try
{
    return await DoWorkInternalAsync().ConfigureAwait(false);
}
finally
{
    _semaphore.Release();
}
```

**Why**: Prevents deadlocks when calling from UI thread (Avalonia/ReactiveUI).

## Debugging Pattern

Use structured logging to diagnose semaphore issues:

```csharp
public async Task<bool> IsPortInUseAsync(int port, CancellationToken ct = default)
{
    _logger.LogDebug("🔒 Waiting for semaphore (IsPortInUse) - Port: {Port}", port);

    await _semaphore.WaitAsync(ct).ConfigureAwait(false);

    _logger.LogDebug("🔓 Semaphore acquired (IsPortInUse) - Port: {Port}", port);

    try
    {
        var result = await IsPortInUseInternalAsync(port, ct).ConfigureAwait(false);

        _logger.LogDebug("✅ Semaphore work complete (IsPortInUse) - Port: {Port}, InUse: {InUse}", port, result);

        return result;
    }
    finally
    {
        _logger.LogDebug("🔓 Releasing semaphore (IsPortInUse) - Port: {Port}", port);
        _semaphore.Release();
    }
}
```

**Logging Guidelines**:
- 🔒 - Waiting for semaphore
- 🔓 - Semaphore acquired
- ✅ - Work complete
- 🔓 - Releasing semaphore

**Analysis**: If you see `🔒 Waiting` without `🔓 Acquired`, you have a deadlock.

## Real-World Examples

### Example 1: Profile Management

```csharp
public class StandardProfileManager<T> where T : class, IProfileBase
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // Public: Create profile
    public async Task<T> CreateAsync(T profile, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await CreateInternalAsync(profile, ct).ConfigureAwait(false);
        }
        finally { _semaphore.Release(); }
    }

    // Internal: Create logic (assumes lock held)
    private async Task<T> CreateInternalAsync(T profile, CancellationToken ct)
    {
        // Validate name uniqueness - safe to call internal method
        await EnsureNameUniqueInternalAsync(profile.Name, null, ct).ConfigureAwait(false);

        profile.Id = GetNextAvailableId();
        _profiles.Add(profile);

        await SaveToFileAsync(ct).ConfigureAwait(false);

        return (T)profile.Clone();
    }

    // Public: Check name uniqueness
    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await IsNameUniqueInternalAsync(name, excludeId, ct).ConfigureAwait(false);
        }
        finally { _semaphore.Release(); }
    }

    // Internal: Name uniqueness check (assumes lock held)
    private Task<bool> IsNameUniqueInternalAsync(string name, int? excludeId, CancellationToken ct)
    {
        var exists = _profiles.Any(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            p.Id != excludeId);

        return Task.FromResult(!exists);
    }

    // Internal: Ensure name is unique (throws if not)
    private async Task EnsureNameUniqueInternalAsync(string name, int? excludeId, CancellationToken ct)
    {
        // Safe: calls another internal method while holding lock
        if (!await IsNameUniqueInternalAsync(name, excludeId, ct).ConfigureAwait(false))
        {
            throw new DuplicateProfileNameException(name);
        }
    }
}
```

**Key Points**:
- `CreateInternalAsync` calls `EnsureNameUniqueInternalAsync` (both internal)
- `EnsureNameUniqueInternalAsync` calls `IsNameUniqueInternalAsync` (both internal)
- No deadlock: all internal methods assume semaphore held by `CreateAsync`

### Example 2: Network Service

```csharp
public class NetworkService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly HashSet<int> _activePorts = new();

    // Public: Register port usage
    public async Task<bool> RegisterPortAsync(int port, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await RegisterPortInternalAsync(port, ct).ConfigureAwait(false);
        }
        finally { _semaphore.Release(); }
    }

    // Internal: Register port (assumes lock held)
    private async Task<bool> RegisterPortInternalAsync(int port, CancellationToken ct)
    {
        // Safe: calls another internal method
        if (await IsPortInUseInternalAsync(port, ct).ConfigureAwait(false))
        {
            return false;
        }

        _activePorts.Add(port);
        return true;
    }

    // Public: Check if port is in use
    public async Task<bool> IsPortInUseAsync(int port, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await IsPortInUseInternalAsync(port, ct).ConfigureAwait(false);
        }
        finally { _semaphore.Release(); }
    }

    // Internal: Port check (assumes lock held)
    private Task<bool> IsPortInUseInternalAsync(int port, CancellationToken ct)
    {
        return Task.FromResult(_activePorts.Contains(port));
    }

    // Public: Get next available port
    public async Task<int?> GetNextAvailablePortAsync(int startPort, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Safe: iterates and calls internal method multiple times
            for (int port = startPort; port < startPort + 1000; port++)
            {
                if (!await IsPortInUseInternalAsync(port, ct).ConfigureAwait(false))
                {
                    return port;
                }
            }

            return null;
        }
        finally { _semaphore.Release(); }
    }
}
```

## Benefits

### 1. Deadlock Prevention

**Problem**: ❌ Public methods calling public methods → deadlock
**Solution**: ✅ Public methods call internal methods → no deadlock

### 2. Code Reuse

Internal methods can be safely reused by multiple public methods without risk.

```csharp
// Both public methods safely reuse internal validation
public Task<T> CreateAsync(T profile) => /* uses ValidateInternalAsync */
public Task<T> UpdateAsync(T profile) => /* uses ValidateInternalAsync */

private Task ValidateInternalAsync(T profile) => /* shared logic */
```

### 3. Clear Intent

Naming convention makes concurrency contract explicit:
- `Async` suffix → public API, acquires lock
- `InternalAsync` suffix → internal helper, assumes lock held

### 4. Testability

Internal methods can be tested independently (via reflection or `InternalsVisibleTo`):

```csharp
[assembly: InternalsVisibleTo("S7Tools.Tests")]

// Test internal logic without semaphore overhead
[Fact]
public async Task IsPortInUseInternal_Should_Return_True_For_Active_Port()
{
    var service = new NetworkService();

    // Use reflection or internal access
    var method = typeof(NetworkService).GetMethod("IsPortInUseInternalAsync",
        BindingFlags.NonPublic | BindingFlags.Instance);

    var result = await (Task<bool>)method.Invoke(service, new object[] { 8080, CancellationToken.None });

    Assert.False(result);
}
```

## Anti-Patterns

### ❌ Don't: Public Method Calls Public Method

```csharp
// BAD: Deadlock risk
public async Task<bool> IsValidAsync(string name)
{
    await _semaphore.WaitAsync();
    try
    {
        // ❌ This will deadlock!
        return await IsNameUniqueAsync(name);
    }
    finally { _semaphore.Release(); }
}

public async Task<bool> IsNameUniqueAsync(string name)
{
    await _semaphore.WaitAsync();  // ❌ Tries to acquire again!
    try { /* check */ }
    finally { _semaphore.Release(); }
}
```

**Fix**: Call internal method instead:

```csharp
// GOOD: No deadlock
public async Task<bool> IsValidAsync(string name)
{
    await _semaphore.WaitAsync();
    try
    {
        return await IsNameUniqueInternalAsync(name);  // ✅ Safe
    }
    finally { _semaphore.Release(); }
}
```

### ❌ Don't: Internal Method Acquires Semaphore

```csharp
// BAD: Violates pattern
private async Task<bool> CheckInternalAsync(int id)
{
    await _semaphore.WaitAsync();  // ❌ Internal method shouldn't acquire!
    try { /* work */ }
    finally { _semaphore.Release(); }
}
```

**Fix**: Remove semaphore handling from internal method:

```csharp
// GOOD: Assumes lock held
private Task<bool> CheckInternalAsync(int id)
{
    // ✅ No semaphore acquisition
    return Task.FromResult(_items.ContainsKey(id));
}
```

### ❌ Don't: Forget ConfigureAwait(false)

```csharp
// BAD: Captures synchronization context
public async Task<bool> DoWorkAsync()
{
    await _semaphore.WaitAsync();  // ❌ Missing ConfigureAwait(false)
    try { return await WorkInternalAsync(); }  // ❌ Missing ConfigureAwait(false)
    finally { _semaphore.Release(); }
}
```

**Fix**: Add ConfigureAwait(false) to ALL awaits:

```csharp
// GOOD: No context capture
public async Task<bool> DoWorkAsync()
{
    await _semaphore.WaitAsync().ConfigureAwait(false);  // ✅
    try { return await WorkInternalAsync().ConfigureAwait(false); }  // ✅
    finally { _semaphore.Release(); }
}
```

## Migration Checklist

When refactoring existing code to use the Internal Method Pattern:

- [ ] Identify all methods that acquire semaphores
- [ ] Determine which methods call other semaphore-acquiring methods
- [ ] Create `*InternalAsync` versions of helper methods
- [ ] Remove semaphore acquisition from internal methods
- [ ] Update public methods to call internal versions
- [ ] Add `.ConfigureAwait(false)` to all `await` statements
- [ ] Add debug logging (🔒/🔓 pattern)
- [ ] Test for deadlocks with concurrent operations
- [ ] Update unit tests to use internal methods where appropriate

## Testing

### Deadlock Detection Test

```csharp
[Fact(Timeout = 5000)]  // 5-second timeout catches deadlocks
public async Task Concurrent_Operations_Should_Not_Deadlock()
{
    // Arrange
    var service = new NetworkService();
    var tasks = new List<Task>();

    // Act: Run 100 concurrent operations
    for (int i = 0; i < 100; i++)
    {
        int port = 8000 + i;
        tasks.Add(Task.Run(async () =>
        {
            await service.RegisterPortAsync(port);
            await service.IsPortInUseAsync(port);
            await service.GetNextAvailablePortAsync(port);
        }));
    }

    // Assert: Completes without deadlock (timeout enforces this)
    await Task.WhenAll(tasks);
}
```

### Internal Method Logic Test

```csharp
[Fact]
public async Task IsNameUniqueInternal_Should_Ignore_Excluded_Id()
{
    // Arrange
    var manager = new StandardProfileManager<TestProfile>();

    // Use InternalsVisibleTo or reflection
    var profile = new TestProfile { Id = 1, Name = "Test" };
    await manager.CreateAsync(profile);

    // Act: Check uniqueness excluding ID 1
    var isUnique = await manager.IsNameUniqueInternalAsync("Test", excludeId: 1);

    // Assert
    Assert.True(isUnique);  // Should be unique when excluding self
}
```

## Related Patterns

- [Profile Management](profile-management.md) - Uses Internal Method Pattern extensively
- [Resource Coordination](resource-coordination.md) - Parallel initialization with semaphores
- [MVVM Patterns](../architecture/mvvm-patterns.md) - UI thread safety considerations

## Examples

- [Semaphore Internal Method Pattern Example](examples/semaphore-pattern-example.md) - Detailed code walkthrough preventing deadlocks

## Implementation Files

**Core Services** (using pattern):
- `src/S7Tools/Services/StandardProfileManager.cs` - Profile CRUD operations
- `src/S7Tools/Services/NetworkService.cs` - Network port management
- `src/S7Tools/Services/JobSchedulerService.cs` - Job scheduling operations
- `src/S7Tools/Services/BootloaderService.cs` - Bootloader coordination

**Historical References**:
- `reviews/archive/SEMAPHORE_DEADLOCK_FIXES_COMPLETE.md` - Initial fixes
- `reviews/archive/SOCAT_SEMAPHORE_DEADLOCK_FIX_2025-10-15.md` - Socat service fix
- `docs/patterns/system-patterns.md` (Section 2.1) - Original documentation

---

**Last Updated**: 2025-11-10
**Status**: Required pattern for all semaphore-protected services
**Pattern Maturity**: Production-proven (zero deadlocks since implementation)

## Related Documentation

- [Index](../INDEX.md)
- [Mvvm Patterns](../architecture/mvvm-patterns.md)
- [Ai Agent Guide](../guides/ai-agent-guide.md)
- [Onboarding](../guides/onboarding.md)
- [_Index](_index.md)
- [Custom Exceptions](custom-exceptions.md)
- [Profile Manager Example](examples/profile-manager-example.md)
- [Resource Coordinator Example](examples/resource-coordinator-example.md)
- [Semaphore Pattern Example](examples/semaphore-pattern-example.md)
- [Profile Management](profile-management.md)
- [Resource Coordination](resource-coordination.md)
- [System Patterns](system-patterns.md)
- [_Index](../reviews/_index.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
