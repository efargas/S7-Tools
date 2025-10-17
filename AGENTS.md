---
# AGENTS.md — Coding Agent Onboarding & Best Practices Guide

## Purpose
This document provides essential onboarding, architecture, and coding standards for all coding agents working on the S7Tools repository. It is designed to ensure consistency, maintainability, and compliance with project rules. **Do not include task-specific or session-specific notes here.**

---

## Project Overview

**S7Tools** is a cross-platform desktop application for Siemens S7-1200 PLC communication, built with .NET 8, Avalonia UI, and ReactiveUI. The project implements Clean Architecture, MVVM, and comprehensive logging.

---

## Key Agent Guidelines

1. **Follow Clean Architecture**: All dependencies flow inward. Core/domain has no external dependencies. Infrastructure and UI depend on Core only.
2. **MVVM Pattern**: Use ReactiveUI for ViewModels. All ViewModels must use `RaiseAndSetIfChanged` for properties and `ReactiveCommand` for commands.
3. **Service-Oriented Design**: All business logic is encapsulated in services, registered via DI in `ServiceCollectionExtensions.cs`.
4. **Dependency Injection**: Register all services/interfaces in the DI container. Never register services directly in `Program.cs`.
5. **Logging**: Use `ILogger<T>` for all logging. Integrate with the custom DataStore provider for real-time UI logs.
6. **Testing**: Follow AAA (Arrange–Act–Assert) for all unit tests. Place tests in the appropriate test project.
7. **EditorConfig**: Enforce code style and formatting with `.editorconfig`. Run `dotnet format` before commit.
8. **Documentation**: All public APIs must have XML documentation. Update the Memory Bank (`systemPatterns.md`) after significant changes.
9. **Error Handling**: Never swallow exceptions. Always log and rethrow or return a failure result.
10. **Thread Safety**: Use `IUIThreadService` for all UI thread operations. Never block the UI thread with I/O.

---

## Folder & File Structure

- `src/S7Tools/` — Main UI, ViewModels, Services
- `src/S7Tools.Core/` — Domain models, interfaces (no external deps)
- `src/S7Tools.Infrastructure.Logging/` — Logging infrastructure
- `tests/` — Unit and integration tests
- `.copilot-tracking/memory-bank/` — Project documentation, patterns, and Memory Bank

---

## Coding Standards (Summary)

- **C#**: 4-space indentation, PascalCase for types, camelCase for private fields with `_` prefix
- **XAML**: 2-space indentation, PascalCase for elements
- **Interfaces**: Prefix with `I` (e.g., `IActivityBarService`)
- **Async Methods**: Use `ConfigureAwait(false)` in library code
- **ViewModels**: Inherit from `ReactiveObject`, use `RaiseAndSetIfChanged`
- **Service Registration**: Use `ServiceCollectionExtensions.cs` only
- **Logging**: Use structured logging, never string interpolation in log messages
- **Validation**: Use centralized validation patterns (see `systemPatterns.md`)

---

## Agent Workflow

1. **Start by reading `.copilot-tracking/memory-bank/systemPatterns.md`** — This is the single source of truth for all architecture, patterns, and rules.
2. **Never duplicate documentation** — Update `systemPatterns.md` and related Memory Bank files after significant changes.
3. **Do not include session logs or task notes here** — Use the Memory Bank for all project intelligence and progress tracking.
4. **Use the agent workspace (`.github/agents/workspace/`) for temporary files only** — Never store permanent documentation or code here.

---

## References

- `.copilot-tracking/memory-bank/systemPatterns.md` — All patterns, rules, and templates
- `.copilot-tracking/memory-bank/` — Project documentation and Memory Bank
- `ServiceCollectionExtensions.cs` — Service registration
- `README.md` — Project summary and setup

---

## Current Baseline Notes (for continuity)

- Scheduler uses Local timezone; due scheduled tasks are auto-promoted to the queue.
- Job profiles path (Options): `src/resources/JobProfiles/profiles.json`.
- `PlcClientStub` is registered as a temporary implementation of `IPlcClient`; factory resolves it until the real client is provided.
- Next major focus: Implement Job Creator wizard in main content area, refactor Jobs details panel, and polish MemoryRegionProfile integration.

---

**For any coding agent: Always align with the latest `systemPatterns.md` and never introduce new patterns or rules without updating the Memory Bank.**
