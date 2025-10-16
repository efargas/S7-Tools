# ADR-0002: Logging — In-memory DataStore Provider

- Status: Accepted
- Date: 2025-10-16

## Context

We need real-time logs visible in the UI for diagnostics and developer workflows. Standard providers (Console, File) do not expose a simple, queryable in-memory stream well-suited for UI binding.

Alternatives considered:

- Serilog with sinks (requires custom UI binding or signalization)
- ETW/EventSource (platform-specific and heavier to integrate with Avalonia UI)
- File-only logging (not real-time UI-friendly)

## Decision

Implement a custom in-memory log DataStore provider integrated with `Microsoft.Extensions.Logging`. Expose configuration via DI and surface structured logs to the UI.

## Consequences

- Positive:
    - Real-time logs in the UI without polling files
    - Structured logging with scopes and properties
    - Pluggable provider that can coexist with others (Console/File)
- Negative:
    - Memory usage bounded by `MaxEntries`
    - Maintenance of custom provider code

## References

- Infrastructure project: `src/S7Tools.Infrastructure.Logging`
- Registration: `AddS7ToolsLogging` and `AddDataStore` in DI setup
