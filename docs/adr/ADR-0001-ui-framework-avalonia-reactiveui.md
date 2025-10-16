# ADR-0001: UI Framework — Avalonia + ReactiveUI

- Status: Accepted
- Date: 2025-10-16

## Context

S7Tools is a cross-platform desktop application targeting Linux, macOS, and Windows. We require MVVM, strong data binding, and a modern, flexible UI framework that works well with .NET 8 and supports our Clean Architecture approach.

Alternatives considered:

- WPF (.NET-only, Windows-only)
- .NET MAUI (mobile-first, desktop support maturing)
- Electron + web stack (heavy runtime, different skillset)

## Decision

Adopt Avalonia UI for cross-platform desktop UI and ReactiveUI as the MVVM framework. Integrate with Microsoft.Extensions.DependencyInjection and Splat for container alignment.

## Consequences

- Positive:
    - True cross-platform desktop support
    - First-class MVVM patterns with ReactiveUI (`ReactiveCommand`, `Interaction`, `RaiseAndSetIfChanged`)
    - Good developer ergonomics; testability and separation of concerns
- Negative:
    - Learning curve for Avalonia-specific patterns and styling
    - Packaging/distribution requires Avalonia workflows per OS

## References

- Registration centralized in `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`
- Bootstrapped in `src/S7Tools/Program.cs` with ReactiveUI and Splat integration
