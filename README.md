<div align="center">

	<img src="src/S7Tools/Assets/avalonia-logo.ico" alt="S7Tools" width="64" height="64" />

	<h1>S7Tools</h1>
	<p>Cross‑platform desktop tools for Siemens S7‑1200 PLC communication and workflows.</p>

</div>

S7Tools is a .NET 8 + Avalonia UI application built with Clean Architecture and MVVM (ReactiveUI). It includes unified profile management, a real‑time log viewer, and a robust job scheduling system.

> [!TIP]
> **For AI Agents & Developers**: Start with [AGENTS.md](AGENTS.md) for a rapid context download.

## 📚 Documentation

The documentation is organized in the `docs/` directory:

*   **[Index](docs/INDEX.md)**: Master list of all documentation.
*   **[Architecture](docs/architecture/overview.md)**: System design, layers, and decisions.
*   **[Patterns](docs/patterns/_index.md)**: Reusable coding patterns and standards.
*   **[Guides](docs/guides/_index.md)**: Workflows, migration guides, and testing.

## Features

- **Cross‑platform**: Linux, macOS, Windows.
- **Job Wizard**: Multi-step wizard for creating complex PLC tasks.
- **Profile Management**: Unified system for Serial, Socat, Power Supply, and Memory Region profiles.
- **Task Logging**: Comprehensive logging system with main, protocol, and process channels.
- **Diagnostics**: Built-in diagnostic tools (`--diag`).

## Quick Start

### Prerequisites
*   .NET 8 SDK

### Build & Run
```bash
# Clean and Build
dotnet clean src/S7Tools.sln
dotnet build src/S7Tools.sln --configuration Debug

# Run
dotnet run --project src/S7Tools/S7Tools.csproj

# Run Tests
dotnet test src/S7Tools.sln
```

## Project Structure

*   `src/S7Tools`: Main UI Application (Avalonia).
*   `src/S7Tools.Core`: Domain models, interfaces, and business rules (No external dependencies).
*   `src/S7Tools.Infrastructure.Logging`: Logging implementation.
*   `src/S7Tools.Diagnostics`: Diagnostic console tool.
*   `tests/`: Unit and integration tests.

For detailed folder structure, see [docs/architecture/overview.md](docs/architecture/overview.md).

---
*Last updated: 2025-11-22*
