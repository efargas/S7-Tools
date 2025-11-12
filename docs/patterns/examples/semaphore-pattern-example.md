---
title: "Semaphore Internal Method Pattern - Example"
created: "2025-11-10"
last-updated: "2025-11-11"
version: "1.0.0"
status: "current"
tags:
  - example
  - pattern
  - threading
  - semaphore
  - code
related:
  - docs/patterns/internal-method.md
  - docs/patterns/system-patterns.md
---

# Semaphore Internal Method Pattern - Example

Concrete example demonstrating the Internal Method Pattern to prevent semaphore deadlocks in S7Tools.

## The Problem

**DEADLOCK SCENARIO**: Calling a semaphore-acquiring method from within a semaphore lock:

```csharp
// ❌ WRONG - This will deadlock!
public class BadPortService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<bool> IsPortInUseAsync(int port)
    {
        await _semaphore.WaitAsync();
        try
        {
            // ... check port ...
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> FindFreePortAsync(int startPort)
    {
        await _semaphore.WaitAsync();  // Acquires semaphore
        try
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                // DEADLOCK! IsPortInUseAsync tries to acquire same semaphore
                if (!await IsPortInUseAsync(port))
                {
                    return port;
                }
            }
            return -1;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

## The Solution: Internal Method Pattern

**Separate public API (acquires semaphore) from internal methods (assume semaphore held)**:

```csharp
// ✅ CORRECT - Uses Internal Method Pattern
public class PortDiscoveryService : IPortDiscoveryService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILoggingService _loggingService;

    public PortDiscoveryService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    #region Public API (Acquires Semaphore)

    /// <summary>
    /// Public API: Check if port is in use (acquires semaphore).
    /// </summary>
    public async Task<bool> IsPortInUseAsync(int port)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await IsPortInUseInternalAsync(port);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Public API: Find first free port in range (acquires semaphore).
    /// </summary>
    public async Task<int> FindFreePortAsync(int startPort, int endPort)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await FindFreePortInternalAsync(startPort, endPort);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Public API: Get all free ports in range (acquires semaphore).
    /// </summary>
    public async Task<List<int>> GetFreePortsAsync(int startPort, int endPort)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await GetFreePortsInternalAsync(startPort, endPort);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Internal Methods (Assume Semaphore Held)

    /// <summary>
    /// Internal: Check if port is in use (assumes semaphore already held).
    /// NEVER call this from public code - use IsPortInUseAsync instead.
    /// </summary>
    private async Task<bool> IsPortInUseInternalAsync(int port)
    {
        try
        {
            using var tcpListener = new TcpListener(IPAddress.Loopback, port);
            tcpListener.Start();
            tcpListener.Stop();
            return false; // Port is free
        }
        catch (SocketException)
        {
            return true; // Port is in use
        }
    }

    /// <summary>
    /// Internal: Find first free port (assumes semaphore already held).
    /// Safe to call IsPortInUseInternalAsync because we already hold the semaphore.
    /// </summary>
    private async Task<int> FindFreePortInternalAsync(int startPort, int endPort)
    {
        _loggingService.LogDebug($"Searching for free port in range {startPort}-{endPort}");

        for (int port = startPort; port <= endPort; port++)
        {
            // ✅ SAFE - Calls internal method, semaphore already held
            if (!await IsPortInUseInternalAsync(port))
            {
                _loggingService.LogInformation($"Found free port: {port}");
                return port;
            }
        }

        _loggingService.LogWarning($"No free ports found in range {startPort}-{endPort}");
        return -1;
    }

    /// <summary>
    /// Internal: Get all free ports (assumes semaphore already held).
    /// </summary>
    private async Task<List<int>> GetFreePortsInternalAsync(int startPort, int endPort)
    {
        var freePorts = new List<int>();

        for (int port = startPort; port <= endPort; port++)
        {
            // ✅ SAFE - Calls internal method, semaphore already held
            if (!await IsPortInUseInternalAsync(port))
            {
                freePorts.Add(port);
            }
        }

        _loggingService.LogDebug($"Found {freePorts.Count} free ports in range {startPort}-{endPort}");
        return freePorts;
    }

    #endregion
}
```

## Usage Examples

### Example 1: Check Single Port

```csharp
var portService = serviceProvider.GetRequiredService<IPortDiscoveryService>();

// Public API - safe to call from anywhere
bool inUse = await portService.IsPortInUseAsync(8080);
Console.WriteLine($"Port 8080 in use: {inUse}");
```

### Example 2: Find Free Port

```csharp
// Find first free port in range 8000-9000
int freePort = await portService.FindFreePortAsync(8000, 9000);
if (freePort != -1)
{
    Console.WriteLine($"Use port: {freePort}");
}
```

### Example 3: Get All Free Ports

```csharp
// Get all free ports in range
var freePorts = await portService.GetFreePortsAsync(8000, 8100);
foreach (var port in freePorts)
{
    Console.WriteLine($"Available: {port}");
}
```

## Pattern Benefits

### Deadlock Prevention
- **Public methods** acquire semaphore once
- **Internal methods** assume semaphore held
- **Never** acquire semaphore twice on same call stack

### Clear API Contract
```csharp
// Public API - Thread-safe, acquires semaphore
public async Task<bool> IsPortInUseAsync(int port) { ... }

// Internal - NOT thread-safe, assumes semaphore held
private async Task<bool> IsPortInUseInternalAsync(int port) { ... }
```

### Composability
Internal methods can safely call each other:
```csharp
private async Task<int> FindFreePortInternalAsync(...)
{
    // ✅ SAFE - Both are internal methods
    if (!await IsPortInUseInternalAsync(port)) { ... }
}
```

## Anti-Patterns to Avoid

### ❌ Don't: Call Public from Internal
```csharp
private async Task FindFreePortInternalAsync(...)
{
    // ❌ DEADLOCK - Public method tries to acquire semaphore again
    if (!await IsPortInUseAsync(port)) { ... }
}
```

### ❌ Don't: Make Internal Methods Public
```csharp
// ❌ DANGEROUS - Exposes non-thread-safe method
public async Task<bool> IsPortInUseInternalAsync(int port) { ... }
```

### ❌ Don't: Skip Semaphore Release
```csharp
public async Task<bool> IsPortInUseAsync(int port)
{
    await _semaphore.WaitAsync();
    return await IsPortInUseInternalAsync(port);
    // ❌ LEAK - Semaphore never released on exception!
}
```

Always use `try/finally`:
```csharp
await _semaphore.WaitAsync();
try
{
    return await IsPortInUseInternalAsync(port);
}
finally
{
    _semaphore.Release();  // ✅ ALWAYS released
}
```

## Real-World Usage in S7Tools

This pattern is used in:
- **PortDiscoveryService** - Port availability checks
- **ProfileManagers** - Profile CRUD operations
- **ResourceCoordinator** - Resource allocation
- **JobManager** - Job queue operations

## Related Patterns

- [Internal Method Pattern](../internal-method.md) - Full pattern documentation
- [System Patterns](../system-patterns.md) - Comprehensive concurrency patterns
- [Resource Coordinator Example](./resource-coordinator-example.md) - Another semaphore usage

## See Also

- [Development Workflow](../../guides/development-workflow.md)
- [Code Style Guide](../../guides/code-style.md)

## Related Documentation

- [Code Style](../../guides/code-style.md)
- [Development Workflow](../../guides/development-workflow.md)
- [Resource Coordinator Example](resource-coordinator-example.md)
- [Internal Method](../internal-method.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
