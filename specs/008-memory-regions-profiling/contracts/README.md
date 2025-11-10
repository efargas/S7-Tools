# Service Contracts: Memory Regions Profiling System

This directory contains the service interface contracts for the memory regions profiling feature.

## Contract Files

- `IMemoryRegionProfileService.cs` - Core profile management service interface
- `IMemorySegmentValidator.cs` - Memory segment validation service interface
- `api-contracts.md` - Public API contracts and integration points

## Design Principles

All contracts follow Clean Architecture principles:

- Interfaces are defined in Core layer
- No dependencies on infrastructure concerns
- Return types use domain models
- Async patterns for file operations
- Proper error handling with domain-specific exceptions
- Thread safety using semaphore patterns
- Unified profile management with StandardProfileManager<T>

## Usage

These contracts will be implemented using the StandardProfileManager<T> pattern and registered via dependency injection in ServiceCollectionExtensions.cs.

Service registration follows established patterns:
```csharp
services.TryAddSingleton<IMemoryRegionProfileService, MemoryRegionProfileService>();
services.TryAddSingleton<IMemorySegmentValidator, MemorySegmentValidator>();
```
