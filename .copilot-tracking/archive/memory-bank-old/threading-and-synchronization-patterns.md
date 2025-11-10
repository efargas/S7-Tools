# Threading and Synchronization Patterns: S7Tools

**Last Updated**: 2025-10-10  
**Context Type**: Concurrency, Thread Safety, and Synchronization  
**Critical Lessons**: Semaphore Deadlock Resolution (BUG001)

---

## Overview

This document captures critical patterns for thread-safe operations, semaphore usage, and race condition prevention learned from production issues and their resolutions.

---

## Semaphore Patterns

### **1. Single Acquisition Per Call Chain (CRITICAL)**

**Rule**: Each public method should acquire a semaphore **once**. Private helper methods should **assume the semaphore is already held**.

**❌ WRONG - Causes Deadlock**:
```csharp
public async Task<Profile> CreateProfileAsync(Profile profile)
{
    await _semaphore.WaitAsync(); // ✅ Acquire semaphore
    try
    {
        var uniqueName = await EnsureUniqueNameAsync(profile.Name); // ❌ Calls method that tries to acquire semaphore again
        // ...
    }
    finally
    {
        _semaphore.Release();
    }
}

private async Task<string> EnsureUniqueNameAsync(string name)
{
    // ❌ DEADLOCK: Trying to acquire semaphore that's already held
    if (await IsNameAvailableAsync(name)) // This method tries to acquire _semaphore
    {
        return name;
    }
    // ...
}

public async Task<bool> IsNameAvailableAsync(string name)
{
    await _semaphore.WaitAsync(); // ❌ DEADLOCK HERE
    try
    {
        return !_profiles.Any(p => p.Name == name);
    }
    finally
    {
        _semaphore.Release();
    }
}
```

**✅ CORRECT - No Nested Acquisition**:
```csharp
public async Task<Profile> CreateProfileAsync(Profile profile)
{
    await _semaphore.WaitAsync(); // ✅ Acquire semaphore once
    try
    {
        // ✅ Call private helper that assumes semaphore is held
        var uniqueName = await EnsureUniqueNameAsync(profile.Name);
        // ...
    }
    finally
    {
        _semaphore.Release();
    }
}

/// <summary>
/// Ensures a unique profile name.
/// NOTE: This method must be called INSIDE a semaphore-protected block.
/// </summary>
private async Task<string> EnsureUniqueNameAsync(string name)
{
    // ✅ Direct collection access - no semaphore needed (already inside protected block)
    if (!_profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
    {
        return name;
    }
    
    // Try suffixes
    for (int i = 1; i <= 999; i++)
    {
        var candidateName = $"{name}_{i}";
        if (!_profiles.Any(p => string.Equals(p.Name, candidateName, StringComparison.OrdinalIgnoreCase)))
        {
            return candidateName;
        }
    }
    
    // Fallback to timestamp
    return $"{name}_{DateTime.UtcNow:yyyyMMddHHmmss}";
}

// ✅ Public method for external callers - acquires semaphore
public async Task<bool> IsNameAvailableAsync(string name, int? excludeId = null)
{
    await _semaphore.WaitAsync(); // ✅ Acquire for external callers
    try
    {
        return !_profiles.Any(p => 
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
    }
    finally
    {
        _semaphore.Release();
    }
}
```

### **2. Document Semaphore Requirements**

**Pattern**: Always document when a method requires semaphore protection

```csharp
/// <summary>
/// Ensures a unique profile name by adding suffixes if necessary.
/// NOTE: This method must be called INSIDE a semaphore-protected block.
/// </summary>
/// <param name="desiredName">The desired profile name.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>A unique profile name, or null if user cancelled.</returns>
private async Task<string?> EnsureUniqueProfileNameAsync(string desiredName, CancellationToken cancellationToken)
{
    // Implementation assumes _semaphore is already held
}
```

### **3. Direct Collection Access Inside Protected Blocks**

**Pattern**: Use direct collection queries (`_collection.Any()`, `_collection.FirstOrDefault()`) inside semaphore-protected blocks instead of calling other semaphore-protected methods.

```csharp
// ✅ CORRECT: Direct access inside protected block
await _semaphore.WaitAsync();
try
{
    // Direct collection access - no additional semaphore needed
    var exists = _profiles.Any(p => p.Name == name);
    var profile = _profiles.FirstOrDefault(p => p.Id == id);
    
    // Perform operations...
}
finally
{
    _semaphore.Release();
}

// ❌ WRONG: Calling semaphore-protected method inside protected block
await _semaphore.WaitAsync();
try
{
    // ❌ This will deadlock if IsNameAvailableAsync tries to acquire semaphore
    var available = await IsNameAvailableAsync(name);
}
finally
{
    _semaphore.Release();
}
```

### **4. ConfigureAwait(false) for All Async Calls**

**Pattern**: Always use `.ConfigureAwait(false)` in service/library code to prevent context switching issues

```csharp
public async Task<Profile> CreateProfileAsync(Profile profile, CancellationToken cancellationToken = default)
{
    await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
    
    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
        var uniqueName = await EnsureUniqueNameAsync(profile.Name, cancellationToken).ConfigureAwait(false);
        await SaveProfilesAsync(cancellationToken).ConfigureAwait(false);
        
        return profile.Clone();
    }
    finally
    {
        _semaphore.Release();
    }
}
```

---

## Clone Pattern for Thread Safety

### **Why Clones Matter**

The service uses a **clone pattern** to ensure thread safety and immutability:

1. **Service stores master copies** in internal collections
2. **All public methods return clones** via `.Clone()` or `.ClonePreserveId()`
3. **ViewModels work with clones**, not original objects
4. **Updates create new clones** and replace originals atomically

### **Implementation Pattern**

```csharp
public class ProfileService
{
    private readonly List<Profile> _profiles = new(); // Master copies
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    // ✅ Return clones to external callers
    public async Task<IEnumerable<Profile>> GetAllProfilesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _profiles.Select(p => p.ClonePreserveId()).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    // ✅ Accept input, clone it, work with clone, return clone
    public async Task<Profile> CreateProfileAsync(Profile profile)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Clone input to avoid external mutations
            var newProfile = profile.Clone();
            newProfile.Id = _nextId++;
            newProfile.CreatedAt = DateTime.UtcNow;
            
            // Store master copy
            _profiles.Add(newProfile);
            await SaveProfilesAsync();
            
            // Return clone to caller
            return newProfile.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    // ✅ Update by replacing entire object
    public async Task<Profile> UpdateProfileAsync(Profile profile)
    {
        await _semaphore.WaitAsync();
        try
        {
            var existingProfile = _profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existingProfile == null)
            {
                throw new InvalidOperationException($"Profile with ID {profile.Id} does not exist");
            }
            
            // Create updated clone
            var updatedProfile = profile.Clone();
            updatedProfile.CreatedAt = existingProfile.CreatedAt; // Preserve creation time
            updatedProfile.ModifiedAt = DateTime.UtcNow;
            
            // Replace atomically
            var index = _profiles.IndexOf(existingProfile);
            _profiles[index] = updatedProfile;
            
            await SaveProfilesAsync();
            
            // Return clone
            return updatedProfile.Clone();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### **Benefits of Clone Pattern**

- ✅ **Thread-safe**: No shared mutable state between service and consumers
- ✅ **Immutable from consumer perspective**: ViewModels can't accidentally mutate service data
- ✅ **Service controls all mutations**: Clear ownership and responsibility
- ✅ **Easy change tracking**: Compare clones to detect modifications
- ✅ **Prevents race conditions**: Each caller gets independent copy

---

## UI Thread Marshalling

### **Pattern**: All UI updates must occur on the UI thread

```csharp
public interface IUIThreadService
{
    Task InvokeOnUIThreadAsync(Action action);
    Task<T> InvokeOnUIThreadAsync<T>(Func<T> function);
    bool IsOnUIThread { get; }
}

// Usage in services/ViewModels
await _uiThreadService.InvokeOnUIThreadAsync(() =>
{
    // Update ObservableCollection
    Profiles.Clear();
    foreach (var profile in profiles)
    {
        Profiles.Add(profile);
    }
});
```

### **Common UI Thread Violations**

```csharp
// ❌ WRONG: Updating UI collection from background thread
public async Task LoadProfilesAsync()
{
    var profiles = await _service.GetAllProfilesAsync(); // Background thread
    
    // ❌ CRASH: ObservableCollection can only be modified on UI thread
    Profiles.Clear();
    foreach (var profile in profiles)
    {
        Profiles.Add(profile);
    }
}

// ✅ CORRECT: Marshal to UI thread
public async Task LoadProfilesAsync()
{
    var profiles = await _service.GetAllProfilesAsync(); // Background thread
    
    // ✅ Marshal to UI thread
    await _uiThreadService.InvokeOnUIThreadAsync(() =>
    {
        Profiles.Clear();
        foreach (var profile in profiles)
        {
            Profiles.Add(profile);
        }
    });
}
```

---

## Race Condition Detection

### **Symptoms of Race Conditions**

1. **Hanging Operations**: Logs show method starts but never completes
2. **Intermittent Failures**: Works sometimes, fails other times
3. **Data Corruption**: Inconsistent state after concurrent operations
4. **Deadlocks**: Application freezes, no progress
5. **Cross-thread Exceptions**: "Collection was modified" errors

### **Detection Techniques**

#### **1. Log Analysis**
```csharp
// Add comprehensive logging to track execution flow
_logger.LogInformation("=== MethodName STARTED ===");
_logger.LogDebug("About to acquire semaphore");

await _semaphore.WaitAsync();
_logger.LogDebug("Semaphore acquired");

try
{
    _logger.LogDebug("Performing operation X");
    // Operation
    _logger.LogDebug("Operation X completed");
}
finally
{
    _semaphore.Release();
    _logger.LogDebug("Semaphore released");
}

_logger.LogInformation("=== MethodName COMPLETED ===");
```

#### **2. Timeout Testing**
```csharp
// Add timeout to detect deadlocks during testing
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await _semaphore.WaitAsync(cts.Token);
    // If this times out, there's likely a deadlock
}
catch (OperationCanceledException)
{
    _logger.LogError("Semaphore acquisition timed out - possible deadlock");
    throw;
}
```

#### **3. Semaphore Usage Audit**
```bash
# Search for all semaphore acquisitions
grep -r "WaitAsync" --include="*.cs"

# Check for nested calls
# Look for methods that acquire semaphore and call other methods that also acquire it
```

### **Prevention Strategies**

1. **Single Acquisition Rule**: One semaphore acquisition per call chain
2. **Document Requirements**: Mark methods that require semaphore protection
3. **Direct Access Pattern**: Use direct collection access inside protected blocks
4. **Code Reviews**: Check for nested semaphore acquisitions
5. **Testing**: Add timeout-based tests to detect deadlocks early

---

## Initialization Patterns

### **Lazy Initialization with Double-Check Locking**

```csharp
private bool _isInitialized;
private readonly SemaphoreSlim _semaphore = new(1, 1);

public async Task InitializeAsync(CancellationToken cancellationToken = default)
{
    // First check without lock (fast path)
    if (_isInitialized)
    {
        return;
    }
    
    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
        // Second check with lock (ensure only one initialization)
        if (_isInitialized)
        {
            return;
        }
        
        // Perform initialization
        await LoadDataAsync(cancellationToken).ConfigureAwait(false);
        
        _isInitialized = true;
    }
    finally
    {
        _semaphore.Release();
    }
}

private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
{
    if (!_isInitialized)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
```

---

## Concurrent Operations Best Practices

### **1. Minimize Lock Scope**

```csharp
// ✅ GOOD: Minimal lock scope
public async Task<Profile> GetProfileAsync(int id)
{
    Profile profile;
    
    await _semaphore.WaitAsync();
    try
    {
        // Only the collection access is protected
        profile = _profiles.FirstOrDefault(p => p.Id == id);
    }
    finally
    {
        _semaphore.Release();
    }
    
    // Expensive operations outside lock
    if (profile != null)
    {
        await LogAccessAsync(profile);
    }
    
    return profile?.Clone();
}

// ❌ BAD: Unnecessarily large lock scope
public async Task<Profile> GetProfileAsync(int id)
{
    await _semaphore.WaitAsync();
    try
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == id);
        
        // ❌ Expensive operation inside lock
        if (profile != null)
        {
            await LogAccessAsync(profile); // Holds lock during I/O
        }
        
        return profile?.Clone();
    }
    finally
    {
        _semaphore.Release();
    }
}
```

### **2. Avoid Blocking Calls in Async Methods**

```csharp
// ❌ WRONG: Blocking call in async method
public async Task DoWorkAsync()
{
    await _semaphore.WaitAsync();
    try
    {
        var result = SomeAsyncMethod().Result; // ❌ Blocking call - can cause deadlock
    }
    finally
    {
        _semaphore.Release();
    }
}

// ✅ CORRECT: Fully async
public async Task DoWorkAsync()
{
    await _semaphore.WaitAsync();
    try
    {
        var result = await SomeAsyncMethod().ConfigureAwait(false); // ✅ Fully async
    }
    finally
    {
        _semaphore.Release();
    }
}
```

### **3. Proper Disposal**

```csharp
public class ProfileService : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}
```

---

## Debugging Checklist

When investigating threading issues:

- [ ] Check for nested semaphore acquisitions
- [ ] Verify all async calls use `ConfigureAwait(false)`
- [ ] Confirm UI updates are marshalled to UI thread
- [ ] Look for blocking calls (`.Result`, `.Wait()`) in async methods
- [ ] Review log timestamps for hanging operations
- [ ] Check if private methods document semaphore requirements
- [ ] Verify clone pattern is used for shared data
- [ ] Test with timeouts to detect deadlocks
- [ ] Audit all `WaitAsync()` calls for proper pairing with `Release()`
- [ ] Ensure proper disposal of semaphores

---

## Real-World Example: BUG001 Resolution

**Problem**: Socat profile CRUD operations hung indefinitely

**Root Cause**: `EnsureUniqueProfileNameAsync` called `IsProfileNameAvailableAsync`, which tried to acquire a semaphore already held by `CreateProfileAsync`

**Solution**: Changed `EnsureUniqueProfileNameAsync` to use direct `_profiles.Any()` checks instead of calling `IsProfileNameAvailableAsync`

**Files Modified**:
- `src/S7Tools/Services/SocatProfileService.cs`
- `src/S7Tools/ViewModels/SocatSettingsViewModel.cs`

**Key Lesson**: Always use direct collection access inside semaphore-protected blocks. Never call other semaphore-protected methods from within a protected block.

---

**Document Status**: Living document - update after threading issues  
**Last Major Update**: 2025-10-10 (BUG001 Resolution)  
**Next Review**: After any concurrency-related changes
