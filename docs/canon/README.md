---
title: "Canon Index — Architectural Foundation Documents"
version: "1.0.0"
created: "2026-03-23"
last-updated: "2026-03-23"
status: "current"
tags: ["canon", "index", "architecture"]
---

# S7Tools Architectural Canon — Index

> The **Canon** is the prescriptive architectural foundation for S7Tools. These documents define
> the laws that **must** be followed by all code in the system. They are not descriptions of how
> code currently works, but **mandates** for how it must be written.
>
> Generated through a comprehensive source code audit on 2026-03-23.

---

## Universal Canon (All Projects)

| Document | Description |
|----------|-------------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | **The Constitution** — Core philosophy, hexagonal architecture, SOLID principles, 10 commandments, mandatory patterns, anti-patterns |
| [DATA_FLOW.md](./DATA_FLOW.md) | **The Blueprint** — Unidirectional data flow diagrams: UI→ViewModel→Service→Domain→Infrastructure, configuration flow, logging flow, profile CRUD flow |
| [STATE_MANAGEMENT.md](./STATE_MANAGEMENT.md) | **The Ledger Rules** — State taxonomy, ownership rules, ViewModel reactive patterns, settings/profile/job/connection/log state lifecycle |
| [CORE_API_CONTRACT.md](./CORE_API_CONTRACT.md) | **The Engine Manual** — Formal internal API: `IApplicationSettingsService`, `IProfileManager<T>`, `IJobScheduler`, `IBootloaderService`, `ILogDataStore`, all hardware interfaces |

---

## System Blueprints (Project-Specific)

| Document | Process | Complexity |
|----------|---------|-----------|
| [PLC_MEMORY_DUMP_PIPELINE.md](./PLC_MEMORY_DUMP_PIPELINE.md) | 14-stage PLC memory extraction workflow | Critical |
| [JOB_EXECUTION_PIPELINE.md](./JOB_EXECUTION_PIPELINE.md) | Job lifecycle: enqueue → resource coordination → parallel execution → persistence | Critical |
| [CONFIGURATION_AND_LOGGING_SYSTEM.md](./CONFIGURATION_AND_LOGGING_SYSTEM.md) | Complete appsettings schema, loading pipeline, logging architecture, anti-patterns audit | Critical |

---

## Audit Summary

### Audit Date
2026-03-23

### Scope
414 C# source files across 4 projects:
- `S7Tools` (UI + Application Services)
- `S7Tools.Core` (Domain)
- `S7Tools.Infrastructure.Logging` (Logging Infrastructure)
- `S7Tools.Diagnostics` (Dev Tooling)

### Key Findings

**Architecture Compliance**: ✅ High
- Clean separation of Domain / Infrastructure / Presentation layers
- All service interfaces defined in `S7Tools.Core`
- No UI framework references in Core or Infrastructure

**Configuration**: ✅ Compliant
- Strongly-typed `AppSettings` with `[Range]`/`[Required]` validation
- `WritableOptions<T>` pattern for atomic persistence
- `IApplicationSettingsService` as single point of access
- `ValidateOnStart()` prevents startup with invalid config

**Logging**: ✅ Compliant
- Custom `DataStoreLoggerProvider` (in-memory UI viewer)
- `UnifiedLoggerProvider` + `FileLogSink` (persistent file output)
- Per-task isolated log stores via `ITaskLoggerFactory`
- Structured log templates used throughout

**Active Refactoring in Progress**:
- Phase 1 (SerialPort): ✅ Complete — decomposed into 3 specialized services + facade
- Phase 2 (Socat): 🔄 In Progress — 4 specialized services + facade registered

**No Legacy Code Found**:
- Zero `TODO`, `FIXME`, `HACK`, `DEPRECATED` markers
- No orphaned interfaces or dead code paths
- No `Console.WriteLine` in service code (only in `--diag` path of `Program.cs`)
- No string interpolation in log messages

### Recommendations Encoded in Canon

1. **Ensure `appsettings.json` is present** in repository root (`/src/S7Tools/`) as a build artifact.
   Without it, application starts with all C# defaults (no error), which may not reflect intended production settings.

2. **Add CI architecture test** to enforce that `S7Tools.Core` has zero `ProjectReference` to other S7Tools projects.

3. **Complete Phase 2 Socat refactoring**: Remove legacy `SocatService` monolithic entry path after `SocatFacadeService` is fully validated.

4. **Standardize log level for business events**: Ensure all "job started/completed" type messages use `Information`, not `Debug`.
