---
applyTo: './.copilot-tracking/changes/20251009-s7-bootloader-integration-changes.md'
---
<!-- markdownlint-disable-file -->
# Task Checklist: Siemens S7 Bootloader Integration (DDD + MVVM + Clean Architecture)

## Overview

Refactor and integrate the SiemensS7-Bootloader reference into S7Tools using SRP-driven adapters, a Bootloader orchestration service, and a Task Manager for queued/parallel jobs with resource locking.

## Objectives

- Define Core contracts (IPlcClient, IPlcProtocol, IPlcTransport, IPayloadProvider, IPowerSupplyService, ISocatService, IBootloaderService, IJobScheduler, IResourceCoordinator)
- Implement Adapters over the reference project without behavior changes
- Provide BootloaderService orchestration for handshake→stager→dump flow
- Implement Task Manager (scheduling, locks, events) and Task Manager UI (VM + Views)
- Add tests for scheduler, orchestration, adapters; ensure DI registration and build pass

## Research Summary

### Project Files
- .github/agents/workspace/referent-projects/SiemensS7-Bootloader/src/S7_Csharp_Core/S7.Net/PlcClient.cs - current tight coupling, core operations
- .../S7.Net/PlcProtocol.cs - packet encoding/decoding, raw ops
- .../S7.Net/Channels/* - SerialChannel/TcpChannel implementations
- .../S7.Net/PayloadManager.cs - file-based payload provider
- .../S7.Core.Commands/* - orchestration via command handlers + power adapter

### External References
- Memory bank instructions - planning flow and documentation discipline
- .NET 8, Avalonia, ReactiveUI patterns from project standards

### Standards References
- /AGENTS.md - Clean Architecture, MVVM, DI, logging conventions
- .editorconfig - code style enforcement

## Implementation Checklist

### [ ] Phase 1: Core Contracts & Models

- [ ] Task 1.1: Add interfaces in S7Tools.Core/Services/Interfaces
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 40-142)

- [ ] Task 1.2: Add Job and Profile models in S7Tools.Core/Models
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 144-222)

### [ ] Phase 2: Adapters (Reference Wrappers)

- [ ] Task 2.1: Implement PlcTransport/Protocol/Client adapters in S7Tools/Services/Adapters
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 224-404)

- [ ] Task 2.2: Implement Payload, Power, Socat adapters
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 406-520)

### [ ] Phase 3: Bootloader Orchestration

- [ ] Task 3.1: Implement IBootloaderService and BootloaderService
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 522-662)

### [ ] Phase 4: Task Manager (Scheduling & UI)

- [ ] Task 4.1: Implement ResourceCoordinator and JobScheduler
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 664-846)

- [ ] Task 4.2: Implement Task Manager ViewModels and Views
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 848-1006)

### [ ] Phase 5: DI, Tests, and Verification

- [ ] Task 5.1: Register services in ServiceCollectionExtensions.cs
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 1008-1084)

- [ ] Task 5.2: Add unit/integration tests
  - Details: ../future-plans/20251009-s7-bootloader-integration-details.md (Lines 1086-1208)

## Dependencies

- .NET 8 SDK, Avalonia UI, ReactiveUI, Microsoft.Extensions.Logging/DI
- Access to SiemensS7-Bootloader reference code (read-only)

## Success Criteria

- End-to-end memory dump on S7-1200 via Task Manager (with logs and artifact file)
- Multiple jobs parallelized when resources disjoint; queued otherwise
- No changes to reference behavior; adapters pass tests
- Build succeeds; EditorConfig clean; DI resolves; UI responsive

## Risk Assessment

- Tight coupling in reference (mitigate with adapters/factories)
- Timing sensitivities (configurable delays, retries)
- Resource leaks (ensure teardown, StopBridge, Dispose)

## Time Estimates

- P1: 8–12h, P2: 8–12h, P3: 6–10h, P4: 12–18h, P5: 10–14h; Buffer 25%

## Quality Gates

- Unit tests for scheduler, adapters, orchestration; integration smoke test
- Static analysis, zero warnings, XML docs for public APIs, logging coverage

## .NET Specific Considerations

- ConfigureAwait(false) in services; async disposal; DI lifetimes; cross-platform serial
