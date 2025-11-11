---
title: "Resource Coordinator Pattern - Example"
created: "2025-11-10"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - example
  - pattern
  - resource-management
  - coordination
  - code
related:
  - docs/patterns/resource-coordination.md
  - docs/patterns/internal-method.md
---

# Resource Coordinator Pattern - Example

Concrete example demonstrating `IResourceCoordinator` usage for managing concurrent access to hardware resources.

## Overview

The Resource Coordinator ensures:
- Only one operation accesses hardware at a time
- Serial port/PLC connections are properly managed
- Background jobs don't interfere with user operations
- Clean resource cleanup on application shutdown

## Example: Using Resource Coordinator in Job Execution

```csharp
using S7Tools.Core.Interfaces;
using S7Tools.Core.Models;

namespace S7Tools.Services.Jobs;

public class JobExecutionService : IJobExecutionService
{
    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly ILoggingService _loggingService;

    public JobExecutionService(
        IResourceCoordinator resourceCoordinator,
        ILoggingService loggingService)
    {
        _resourceCoordinator = resourceCoordinator;
        _loggingService = loggingService;
    }

    public async Task<bool> ExecuteJobAsync(JobProfile job, CancellationToken cancellationToken)
    {
        _loggingService.LogInformation($"Job {job.Name}: Requesting resource access");

        // Acquire exclusive access to hardware resources
        await _resourceCoordinator.AcquireAsync(cancellationToken);

        try
        {
            _loggingService.LogInformation($"Job {job.Name}: Resource acquired, starting execution");

            // Perform job operations with exclusive hardware access
            await ConnectToPlcAsync(job, cancellationToken);
            await ExecuteMemoryDumpAsync(job, cancellationToken);
            await DisconnectAsync(cancellationToken);

            _loggingService.LogInformation($"Job {job.Name}: Completed successfully");
            return true;
        }
        catch (OperationCanceledException)
        {
            _loggingService.LogWarning($"Job {job.Name}: Canceled");
            throw;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, $"Job {job.Name}: Failed");
            return false;
        }
        finally
        {
            // Always release resource
            _resourceCoordinator.Release();
            _loggingService.LogDebug($"Job {job.Name}: Resource released");
        }
    }

    private async Task ConnectToPlcAsync(JobProfile job, CancellationToken ct)
    {
        // Resource Coordinator ensures no other operation is using serial port
        _loggingService.LogDebug($"Connecting to PLC via {job.SerialProfileName}");
        await Task.Delay(100, ct); // Simulate connection
    }

    private async Task ExecuteMemoryDumpAsync(JobProfile job, CancellationToken ct)
    {
        _loggingService.LogDebug("Executing memory dump");
        await Task.Delay(500, ct); // Simulate dump operation
    }

    private async Task DisconnectAsync(CancellationToken ct)
    {
        _loggingService.LogDebug("Disconnecting from PLC");
        await Task.Delay(50, ct); // Simulate disconnect
    }
}
```

## Example: Coordinating User Operations with Background Jobs

```csharp
public class PlcConnectionViewModel : ReactiveObject
{
    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly ILoggingService _loggingService;

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public PlcConnectionViewModel(
        IResourceCoordinator resourceCoordinator,
        ILoggingService loggingService)
    {
        _resourceCoordinator = resourceCoordinator;
        _loggingService = loggingService;

        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync);
    }

    private async Task ConnectAsync()
    {
        _loggingService.LogInformation("User: Connecting to PLC");

        // Wait for resource availability (respects background jobs)
        await _resourceCoordinator.AcquireAsync();

        try
        {
            // Exclusive access - no jobs can interfere
            _loggingService.LogInformation("Establishing PLC connection");
            await Task.Delay(1000); // Actual connection logic

            _loggingService.LogInformation("Connected successfully");
        }
        finally
        {
            _resourceCoordinator.Release();
        }
    }
}
```

## Pattern Benefits

### Mutual Exclusion
```csharp
// Job 1 acquires resource
await _resourceCoordinator.AcquireAsync();
// ... Job 1 using hardware ...

// Job 2 waits here until Job 1 releases
await _resourceCoordinator.AcquireAsync();
```

### Cancellation Support
```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    await _resourceCoordinator.AcquireAsync(cts.Token);
    // ... operations ...
}
catch (OperationCanceledException)
{
    // Timeout or explicit cancellation
}
```

### Resource Leak Prevention
Always use try/finally:
```csharp
await _resourceCoordinator.AcquireAsync();
try
{
    // ... operations ...
}
finally
{
    _resourceCoordinator.Release(); // ✅ Always released
}
```

## Real-World Usage

- **JobManager**: Coordinates scheduled job execution
- **PlcConnectionService**: Prevents concurrent PLC access
- **MemoryDumpService**: Ensures exclusive hardware access during dumps
- **PortDiscoveryService**: Coordinates port scanning operations

## Related Patterns

- [Resource Coordination Pattern](../resource-coordination.md) - Full documentation
- [Internal Method Pattern](../internal-method.md) - Thread safety
- [Semaphore Pattern Example](./semaphore-pattern-example.md) - Related concurrency

## See Also

- [Development Workflow](../../guides/development-workflow.md)
- [Testing Guide](../../guides/testing-guide.md)

## Related Documentation

- [Development Workflow](../../guides/development-workflow.md)
- [Testing Guide](../../guides/testing-guide.md)
- [Semaphore Pattern Example](semaphore-pattern-example.md)
- [Internal Method](../internal-method.md)
- [Resource Coordination](../resource-coordination.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
