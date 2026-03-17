# S7Tools Source Code & Architecture Audit Report

## 1. Executive Summary
This report summarizes the findings of an extensive audit performed on the `S7Tools` codebase—a cross-platform desktop application built for Siemens S7-1200 PLC communication and bootloader payload execution.

The codebase exhibits a mature adoption of **Clean Architecture** and **MVVM** principles using Avalonia UI and ReactiveUI. Design patterns like Unified Profile Management and Resource Coordination are effectively implemented.

During the audit, critical security flaws regarding shell command execution were identified and successfully mitigated, alongside a suite of associated failing tests.

## 2. Application Functionalities
`S7Tools` provides sophisticated workflows for embedded systems and security researchers:
- **PLC Memory Dumping:** Orchestrates a 7-stage bootloader workflow including Socat setup, PLC power cycling, bootloader handshaking, stager/dumper payload installation, and memory extraction.
- **Task & Job Management:** Implements an advanced Job Scheduler with Resource Coordination to enqueue, prioritize, and execute tasks without hardware contention (e.g., locking a `/dev/ttyUSB0` serial port).
- **Profile Management:** A Unified Profile Management system allows users to create and manage configurations for Serial Ports, Socat servers, Power Supplies (Modbus TCP), and Memory Regions.
- **Real-Time Logging:** Features an in-memory DataStore provider allowing users to view categorized, real-time logs directly in the UI.

## 3. Architecture & Patterns Observed
### 3.1 Clean Architecture
The application strictly enforces separation of concerns across projects:
- `S7Tools.Core`: Pure domain models, exceptions, validation, and service interfaces. Zero dependencies on infrastructure or UI.
- `S7Tools.Infrastructure.Logging`: Implementation details for logging (DataStore provider) and file I/O operations.
- `S7Tools`: The UI layer (Avalonia) containing Views, ViewModels, and Application Services.

### 3.2 Key Design Patterns
- **MVVM + ReactiveUI:** Ensures View and ViewModel decoupling. Reactive properties handle state changes effectively.
- **Unified Profile Management (`StandardProfileManager<T>`):** Eliminates duplicate CRUD code for different profile types, ensuring thread safety through an "Internal Method Pattern".
- **Adapter Pattern:** Integrates existing native bootloader reference code (Python/C) via `.NET` wrapper services (`ISocatService`, `IPowerSupplyService`, `ISerialPortService`).
- **Resource Coordinator Pattern:** Prevents parallel execution conflicts over shared hardware resources like serial ports or Modbus connections.

## 4. Code Quality & SOLID Principles
Overall, the codebase adheres strongly to SOLID principles:
- **Single Responsibility Principle (SRP):** Classes like `JobScheduler` and `BootloaderService` correctly delegate low-level responsibilities to injected dependencies rather than handling them directly.
- **Dependency Inversion Principle (DIP):** UI and Application layers depend purely on abstractions (`ISocatService`, `IPlcClient`) defined in the Core domain.
- **Liskov Substitution Principle (LSP):** Base classes like `BaseBootloaderService` are successfully extended and tested without unexpected behavior.

### Areas of Improvement / Addressed Issues
1. **Security Vulnerability (Shell Injection):**
   - **Finding:** The `ShellCommandExecutor` previously passed user-controlled input and configuration parameters directly to `/bin/bash -c`. This allowed arbitrary shell execution (e.g., via backticks or command substitution `$(...)`).
   - **Resolution:** Refactored `ShellCommandExecutor.cs` to execute binaries directly by using `ProcessStartInfo.ArgumentList`. A `SplitCommandLine` utility was implemented to properly parse arguments. This eliminates shell injection entirely.

2. **Broken Tests / Outdated Mock Interfaces:**
   - **Finding:** Modifying the models (e.g., `SerialProfileRef`, `BootloaderResult`) or core dependencies led to compilation errors in `S7Tools.Tests` and `S7Tools.Core.Tests`.
   - **Resolution:** Test suites were updated to pass correct parameters, instantiate missing configurations, and map properly to the latest domain models. Additionally, the `ApplicationSettingsServiceTests` were updated to correctly mock `IWritableOptions<T>`.

## 5. Documentation Review
The documentation structure in `docs/website/docs/` is well-maintained and follows a structured lifecycle.
- **Active Documentation:** Comprehensive guides exist for Clean Architecture, MVC/MVVM patterns, the Task Logging System, and Settings Schema.
- **Deprecated Documentation:** Obsolete documents such as `Project_Architecture_Blueprint.md` and `Project_Folders_Structure_Blueprint.md` use frontmatter tags to clearly point users to their successors in the `architecture/overview.md` file. This is an excellent practice for maintaining historical context while avoiding confusion.

## 6. UI & Theming Engine
The UI is constructed using Avalonia with a VSCode-inspired interface consisting of an Activity Bar, Sidebar, Main Content Area, and a Bottom Panel.
- **Semantic Theming:** Color variables are not hard-coded but bound to dynamic semantic tokens (e.g., `BrandAccentBrush`, `AppBackgroundBrush`). This facilitates instant theme switching (Light/Dark/System) across the entire application without needing a restart.

## 7. Conclusion
`S7Tools` represents a highly structured, scalable, and robust approach to embedded systems tooling. The architecture effectively abstracts hardware constraints allowing for a sophisticated user interface. Addressing the shell execution vulnerability significantly strengthens the application's security posture, particularly relevant in OT/ICS contexts. The strict reliance on DI, comprehensive testing (`xUnit`, `Moq`, `FluentAssertions`), and excellent documentation make it a maintainable and modern `.NET` application.
