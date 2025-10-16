<div align="center">

	<img src="src/S7Tools/Assets/avalonia-logo.ico" alt="S7Tools" width="64" height="64" />

	<h1>S7Tools</h1>
	<p>Cross‑platform desktop tools for Siemens S7‑1200 PLC communication and workflows.</p>

</div>

S7Tools is a .NET 8 + Avalonia UI application built with Clean Architecture and MVVM (ReactiveUI). It includes unified profile management (Serial, Socat, Power Supply), a real‑time log viewer, and a developer‑friendly DI setup for rapid extension.

> [!NOTE]
> For a deep dive into the system design, see the Architecture Blueprint: docs/Project_Architecture_Blueprint.md

## Features

- Cross‑platform desktop app (Linux, macOS, Windows) with Avalonia UI and ReactiveUI
- Real‑time in‑app logging via a custom in‑memory DataStore provider
- Unified Profile Management pattern for:
	- Serial Port profiles
	- Socat profiles (server settings)
	- Power Supply profiles (Modbus TCP)
- Clean Architecture, DI‑first composition, and testable service abstractions
- Diagnostic startup mode for quick environment validation (`--diag`)

## Quick start

> [!IMPORTANT]
> Prerequisite: .NET 8 SDK

Build the solution:

```bash
dotnet restore src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug
```

Run the desktop app:

```bash
dotnet run --project src/S7Tools/S7Tools.csproj
```

Run in diagnostic mode (initializes services, prints diagnostics, and exits):

```bash
dotnet run --project src/S7Tools/S7Tools.csproj -- --diag
```

## Project structure

```
src/
	S7Tools/                      # UI (Avalonia app, Views/ViewModels/Services)
		Extensions/ServiceCollectionExtensions.cs  # Central DI composition
	S7Tools.Core/                 # Domain interfaces, commands, validation, logging abstractions
	S7Tools.Infrastructure.Logging/  # Custom logging provider and in‑memory datastore
tests/                          # xUnit test projects per layer
docs/                           # Architecture and additional documentation
```

> [!TIP]
> All services are registered via DI in `src/S7Tools/Extensions/ServiceCollectionExtensions.cs`. Avoid adding registrations in `Program.cs`.

## Development

- Pattern highlights:
	- MVVM with ReactiveUI (`ReactiveCommand`, `Interaction`, `RaiseAndSetIfChanged`)
	- Service‑oriented design with interfaces in Core, implementations in UI/Infra
	- Centralized DI and background service initialization helpers
	- Unified `StandardProfileManager<T>` for thread‑safe, JSON‑backed CRUD with default/profile rules

- Build and run (Debug): see Quick start above
- Run tests:

```bash
dotnet test
```

> [!WARNING]
> When adding or modifying profile services, avoid nested semaphore acquisitions. Follow the internal helper pattern used in `StandardProfileManager<T>` to prevent deadlocks.

## Packaging (Avalonia publish)

> [!TIP]
> Replace `-c Release` with `-c Debug` if needed. Artifacts will be placed in `bin/Release/net8.0/<rid>/publish`.

Windows (self-contained, win-x64):

```bash
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Linux (self-contained, linux-x64):

```bash
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

macOS (self-contained, osx-x64):

```bash
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true
```

macOS (Apple Silicon, osx-arm64):

```bash
dotnet publish src/S7Tools/S7Tools.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true
```

> [!NOTE]
> For platform-specific packaging (DMG/MSI/AppImage), see Avalonia’s distribution guides. The above commands create portable, single-file binaries.

## Documentation

- Architecture Blueprint: docs/Project_Architecture_Blueprint.md
- Agent guidelines: AGENTS.md
- Architectural Decisions: docs/adr/_index.md

## Logging Viewer

The app includes a real-time Log Viewer backed by an in-memory DataStore provider.

- Open from the activity bar: “Log Viewer”
- Filter by level, category, or search text
- Toggle columns (timestamp, level, category) in the viewer
- Export logs: use the UI command to copy/export current logs (also available via the main window’s export logs command)

> [!NOTE]
> Logging is structured (`ILogger<T>` with scopes/properties). The in-memory store is bounded by `MaxEntries` configured during DI.

## Troubleshooting

- Linux serial access may require adding your user to the `dialout` group and re‑logging.
- For socat‑related features, ensure `socat` is installed and accessible on your system.

