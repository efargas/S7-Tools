# IBootloaderService Contract

**Interface**: IBootloaderService
**Location**: `src/S7Tools.Core/Services/Interfaces/IBootloaderService.cs`
**Purpose**: High-level orchestration service for complete bootloader workflow execution
**Layer**: Core (contract) → Application (implementation)

## Contract Definition

```csharp
namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Orchestrates complete S7-1200 PLC bootloader workflow for memory dumping.
/// Coordinates socat bridge, power cycling, handshaking, payload installation, and memory extraction.
/// </summary>
public interface IBootloaderService
{
    /// <summary>
    /// Executes complete memory dump workflow using provided profile set.
    /// </summary>
    /// <param name="profileSet">Configuration profiles (serial, socat, power, memory, payloads)</param>
    /// <param name="progress">Optional progress reporter for UI updates</param>
    /// <param name="cancellationToken">Cancellation token for workflow abort</param>
    /// <returns>Raw binary memory dump from specified region</returns>
    /// <exception cref="BootloaderException">Workflow failed at any stage</exception>
    /// <exception cref="ResourceUnavailableException">Required resource (serial/TCP/modbus) locked</exception>
    /// <exception cref="OperationCanceledException">User canceled via cancellationToken</exception>
    Task<byte[]> DumpMemoryAsync(
        JobProfileSet profileSet,
        IProgress<BootloaderProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates profile set configuration before workflow execution.
    /// Checks: serial port accessible, TCP port available, modbus reachable, payloads exist, memory region valid.
    /// </summary>
    /// <param name="profileSet">Configuration to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors if configuration invalid</returns>
    Task<ValidationResult> ValidateProfileSetAsync(
        JobProfileSet profileSet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates workflow duration based on memory region size and historical performance.
    /// </summary>
    /// <param name="memoryRegion">Memory region to dump</param>
    /// <returns>Estimated duration in seconds (range: 5-300s per SC-001)</returns>
    TimeSpan EstimateDuration(MemoryRegionProfile memoryRegion);
}
```

## Workflow Stages

The `DumpMemoryAsync` method orchestrates 7 sequential stages reported via `IProgress<BootloaderProgress>`:

| Stage | Description | Estimated Duration | Progress % |
|-------|-------------|-------------------|------------|
| 1. socat_setup | Start TCP/serial bridge (socat) | 2-5s | 0-10% |
| 2. power_cycle | Power off PLC → wait → power on | 5-10s | 10-20% |
| 3. handshake | Establish bootloader communication | 2-5s | 20-30% |
| 4. stager_install | Upload stager payload (small, ~4KB) | 3-8s | 30-50% |
| 5. memory_dump | Execute memory dump (varies by size) | 10-250s | 50-95% |
| 6. teardown | Clean up resources (socat, serial, modbus) | 1-3s | 95-99% |
| 7. complete | Save dump to OutputPath | 0.5-1s | 100% |

## Progress Reporting Structure

```csharp
/// <summary>
/// Progress information for bootloader workflow stages.
/// </summary>
public record BootloaderProgress(
    string Stage,              // Current stage name (e.g., "socat_setup", "memory_dump")
    double Percentage,         // Overall progress (0.0-100.0)
    string CurrentOperation,   // User-friendly description (e.g., "Uploading stager payload")
    Dictionary<string, object>? Data = null  // Optional stage-specific data (e.g., bytes transferred)
);
```

## Validation Result Structure

```csharp
/// <summary>
/// Result of profile set validation.
/// </summary>
public record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors
)
{
    public static ValidationResult Success() => new(true, Array.Empty<ValidationError>());
    public static ValidationResult Failure(params ValidationError[] errors) => new(false, errors);
}

/// <summary>
/// Individual validation error.
/// </summary>
public record ValidationError(
    string Field,        // Field name (e.g., "SerialProfile.Device")
    string Message       // Error description (e.g., "Port /dev/ttyUSB0 not accessible")
);
```

## Exception Hierarchy

```csharp
// Base exception for all bootloader errors
public class BootloaderException : S7ToolsException
{
    public BootloaderException(string message) : base(message) { }
    public BootloaderException(string message, Exception inner) : base(message, inner) { }
}

// Specific failure stages
public class HandshakeFailedException : BootloaderException { }
public class PayloadInstallException : BootloaderException { }
public class MemoryDumpException : BootloaderException { }
public class ResourceUnavailableException : BootloaderException { }
```

## Implementation Requirements

### 1. Resource Acquisition (Article IV Thread Safety)

```csharp
public async Task<byte[]> DumpMemoryAsync(JobProfileSet profileSet, ...)
{
    // Extract resource keys from profile set
    var resources = new[]
    {
        new ResourceKey(ResourceType.Serial, profileSet.Serial.Device),
        new ResourceKey(ResourceType.Tcp, profileSet.Socat.Port.ToString()),
        new ResourceKey(ResourceType.Modbus, profileSet.Power.ModbusAddress)
    };

    // Try acquire all resources (no nested semaphores!)
    if (!_resourceCoordinator.TryAcquire(resources))
    {
        throw new ResourceUnavailableException("Required resources locked by another job");
    }

    try
    {
        // Execute workflow stages...
        return await ExecuteWorkflowAsync(profileSet, progress, cancellationToken);
    }
    finally
    {
        // ALWAYS release resources
        _resourceCoordinator.Release(resources);
    }
}
```

### 2. Progress Reporting (Article V Observability)

```csharp
private async Task<byte[]> ExecuteWorkflowAsync(...)
{
    progress?.Report(new BootloaderProgress("socat_setup", 0, "Starting socat bridge"));
    await _socatService.StartBridgeAsync(profileSet.Socat);

    progress?.Report(new BootloaderProgress("power_cycle", 10, "Power cycling PLC"));
    await _powerSupplyService.PowerCycleAsync(profileSet.Power);

    progress?.Report(new BootloaderProgress("handshake", 20, "Establishing bootloader connection"));
    await _plcClient.HandshakeAsync(cancellationToken);

    // ... remaining stages
}
```

### 3. ConfigureAwait Pattern (Article IV)

```csharp
// ✅ CORRECT: ConfigureAwait(false) in service layer
await _socatService.StartBridgeAsync(profile).ConfigureAwait(false);

// ❌ INCORRECT: Missing ConfigureAwait in library code
await _socatService.StartBridgeAsync(profile);
```

### 4. Structured Logging (Article V)

```csharp
private readonly ILogger<BootloaderService> _logger;

public async Task<byte[]> DumpMemoryAsync(...)
{
    _logger.LogInformation(
        "Starting memory dump: Region={RegionName}, Size={Size} bytes, Job={JobId}",
        profileSet.MemoryRegion.Name,
        profileSet.MemoryRegion.Length,
        jobId);

    try
    {
        var dump = await ExecuteWorkflowAsync(...);
        _logger.LogInformation("Memory dump completed successfully: {BytesRetrieved} bytes", dump.Length);
        return dump;
    }
    catch (BootloaderException ex)
    {
        _logger.LogError(ex, "Bootloader workflow failed at stage: {Stage}", ex.Stage);
        throw;
    }
}
```

## Usage Example

```csharp
// In JobScheduler or TaskManagerViewModel
var profileSet = new JobProfileSet
{
    Serial = new SerialProfileRef { ProfileId = 5, Device = "/dev/ttyUSB0" },
    Socat = new SocatProfileRef { ProfileId = 2, Port = 10102 },
    Power = new PowerProfileRef { ProfileId = 1, ModbusAddress = "192.168.1.10:502" },
    MemoryRegion = new MemoryRegionProfile { StartAddress = 0x20000000, Length = 65536 },
    PayloadSet = new PayloadSetProfile { BasePath = "/path/to/payloads" }
};

// Validate before queuing
var validation = await _bootloaderService.ValidateProfileSetAsync(profileSet);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        _logger.LogWarning("Validation failed: {Field} - {Message}", error.Field, error.Message);
    }
    return;
}

// Execute with progress reporting
var progress = new Progress<BootloaderProgress>(p =>
{
    await _uiThreadService.InvokeAsync(() =>
    {
        ProgressPercentage = p.Percentage;
        CurrentOperation = p.CurrentOperation;
    });
});

try
{
    var dumpData = await _bootloaderService.DumpMemoryAsync(profileSet, progress, cts.Token);
    await File.WriteAllBytesAsync(job.OutputPath, dumpData);
}
catch (OperationCanceledException)
{
    _logger.LogInformation("User canceled memory dump");
}
catch (BootloaderException ex)
{
    _logger.LogError(ex, "Memory dump failed");
}
```

## Testing Strategy

### Unit Tests (Service Logic)

```csharp
[Fact]
public async Task DumpMemoryAsync_ValidProfileSet_ReturnsMemoryDump()
{
    // Arrange
    var mockPlcClient = new Mock<IPlcClient>();
    mockPlcClient.Setup(x => x.DumpMemoryAsync(It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new byte[65536]);

    var service = new BootloaderService(mockPlcClient.Object, ...);

    // Act
    var dump = await service.DumpMemoryAsync(ValidProfileSet);

    // Assert
    dump.Should().NotBeNull();
    dump.Length.Should().Be(65536);
}

[Fact]
public async Task DumpMemoryAsync_ResourcesLocked_ThrowsResourceUnavailableException()
{
    // Arrange
    var mockCoordinator = new Mock<IResourceCoordinator>();
    mockCoordinator.Setup(x => x.TryAcquire(It.IsAny<ResourceKey[]>())).Returns(false);

    var service = new BootloaderService(..., mockCoordinator.Object);

    // Act & Assert
    await Assert.ThrowsAsync<ResourceUnavailableException>(() =>
        service.DumpMemoryAsync(ValidProfileSet));
}
```

### Integration Tests (End-to-End with Fakes)

```csharp
[Fact]
public async Task DumpMemoryAsync_CompleteWorkflow_ProgressReported()
{
    // Arrange
    var progressReports = new List<BootloaderProgress>();
    var progress = new Progress<BootloaderProgress>(p => progressReports.Add(p));

    // Act
    await _bootloaderService.DumpMemoryAsync(ValidProfileSet, progress);

    // Assert
    progressReports.Should().HaveCountGreaterThan(5); // At least 5 stages reported
    progressReports.Last().Percentage.Should().Be(100.0);
    progressReports.Last().Stage.Should().Be("complete");
}
```

## Performance Requirements

| Metric | Target | Source |
|--------|--------|--------|
| 4KB dump | <30s | SC-001 (boot sector scenario) |
| 64KB dump | <5 min | SC-001 (full firmware scenario) |
| Resource cleanup | <3s | SC-010 (teardown latency) |
| Progress updates | <500ms | SC-008 (UI responsiveness) |

## Dependencies

- IPlcClient (bootloader client adapter)
- ISocatService (TCP/serial bridge)
- IPowerSupplyService (power control)
- IResourceCoordinator (resource locking)
- IPayloadProvider (payload loading)
- ILogger<BootloaderService> (structured logging)

## References

- **Specification**: FR-001, FR-002, FR-010 (bootloader workflow, progress reporting, error handling)
- **Success Criteria**: SC-001 (dump time), SC-008 (UI responsiveness), SC-010 (cleanup time)
- **Constitution**: Article IV (Thread Safety), Article V (Observability)
- **Research**: RQ2 (Event-Driven Scheduler), RQ4 (Progress Reporting)
