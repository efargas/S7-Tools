# Service Contracts: Bootloader Integration

**Feature**: 011-bootloader-integration
**Phase**: Phase 1 (Design)
**Created**: 2025-11-12
**Status**: Complete

## Overview

This directory contains the service interface definitions for bootloader integration. All interfaces follow S7Tools Clean Architecture patterns with contracts defined in Core layer (`S7Tools.Core/Services/Interfaces/`) and implementations in Application/Infrastructure layers.

## Contract Organization

| Contract | Purpose | Implementation Layer | File |
|----------|---------|---------------------|------|
| IPlcTransport | Low-level serial/network I/O | Infrastructure (Adapter) | [IPlcTransport.md](./IPlcTransport.md) |
| IPlcProtocol | Protocol-level packet handling | Infrastructure (Adapter) | [IPlcProtocol.md](./IPlcProtocol.md) |
| IPlcClient | High-level bootloader client | Infrastructure (Adapter) | [IPlcClient.md](./IPlcClient.md) |
| IPayloadProvider | Binary payload loading | Infrastructure (Adapter) | [IPayloadProvider.md](./IPayloadProvider.md) |
| IBootloaderService | Orchestration service | Application | [IBootloaderService.md](./IBootloaderService.md) |
| IJobScheduler | Job queue management | Application | [IJobScheduler.md](./IJobScheduler.md) |
| IResourceCoordinator | Resource conflict detection | Application | [IResourceCoordinator.md](./IResourceCoordinator.md) |
| IPowerSupplyService | Power control extension | Application (extends existing) | [IPowerSupplyService.md](./IPowerSupplyService.md) |
| ISocatService | TCP/serial bridge extension | Application (extends existing) | [ISocatService.md](./ISocatService.md) |
| Domain Events | Job state change events | Core (Events) | [domain-events.md](./domain-events.md) |

## Architecture Patterns

### Dependency Inversion (Article I)

All interfaces defined in Core layer with **zero external dependencies**:

```csharp
// ✅ CORRECT: Core interface with BCL types only
namespace S7Tools.Core.Services.Interfaces;

public interface IBootloaderService
{
    Task<byte[]> DumpMemoryAsync(JobProfileSet profileSet, CancellationToken ct = default);
}

// ❌ INCORRECT: Core interface depending on infrastructure type
namespace S7Tools.Core.Services.Interfaces;

public interface IBootloaderService
{
    Task<byte[]> DumpMemoryAsync(NModbus.IModbusClient modbusClient); // External dependency!
}
```

### Adapter Pattern for Reference Code Integration

Reference implementation (SiemensS7-Bootloader) wrapped by adapters without modification:

```
S7Tools.Core/Services/Interfaces/
    IPlcTransport.cs          ← Core contract (our abstraction)
    IPlcProtocol.cs
    IPlcClient.cs

S7Tools/Services/Adapters/
    PlcTransportAdapter.cs    ← Wraps reference ICommunicationChannel
    PlcProtocolAdapter.cs     ← Wraps reference PlcProtocol
    PlcClientAdapter.cs       ← Wraps reference PlcClient

External (SiemensS7-Bootloader NuGet/Reference):
    ICommunicationChannel     ← Reference abstraction (unchanged)
    PlcProtocol               ← Reference implementation (unchanged)
    PlcClient                 ← Reference implementation (unchanged)
```

### Async/Await Standards (Article IV)

All I/O operations use async methods with CancellationToken support:

```csharp
// ✅ CORRECT: Async with cancellation support
Task<byte[]> ReadAsync(int length, CancellationToken ct = default);

// ❌ INCORRECT: Synchronous blocking operation
byte[] Read(int length);
```

### Progress Reporting (Article V Observability)

Long-running operations accept `IProgress<T>` for UI responsiveness:

```csharp
// ✅ CORRECT: Progress reporting for long operations
Task<byte[]> DumpMemoryAsync(
    JobProfileSet profileSet,
    IProgress<BootloaderProgress>? progress = null,
    CancellationToken ct = default);

// ❌ INCORRECT: No progress reporting for 5-300 second operation
Task<byte[]> DumpMemoryAsync(JobProfileSet profileSet);
```

## Contract Validation Checklist

Before implementation, verify each interface:

- [ ] Defined in `S7Tools.Core/Services/Interfaces/` namespace
- [ ] Contains only BCL types in method signatures (no external dependencies)
- [ ] All I/O methods are async with `CancellationToken` parameter
- [ ] Long-running methods (>2s) accept `IProgress<T>` parameter
- [ ] All methods have XML documentation comments
- [ ] Return types use Task<T> for async, never async void
- [ ] Follows established S7Tools naming conventions (IServiceName pattern)
- [ ] Compatible with existing DI registration patterns (ServiceCollectionExtensions)

## Next Steps

After contract approval:

1. Review each interface definition in detail
2. Generate quickstart.md with sample usage
3. Update agent context with new interfaces
4. Proceed to Phase 2 (Implementation) with /speckit.tasks command

## References

- **Data Model**: `specs/011-bootloader-integration/data-model.md` (entities used in contracts)
- **Research**: `specs/011-bootloader-integration/research.md` (architectural decisions)
- **Constitution**: `.specify/memory/constitution.md` Article I (Clean Architecture boundaries)
- **Existing Patterns**: `docs/patterns/system-patterns.md` (service patterns, DI registration)
