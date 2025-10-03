<!-- markdownlint-disable-file -->
# Research: Core Architecture Refactor

This research document outlines the plan to refactor the `S7Tools` project to establish a clean, scalable architecture by separating core business logic from the UI project.

## 1. Objective

The goal is to implement the first three points from the initial code review:
1.  Create a `S7Tools.Core` class library for business logic, models, and services.
2.  Implement the Provider and Repository patterns for S7 PLC communication.
3.  Ensure `ReactiveCommand` is used for user actions in the ViewModels.

## 2. Project Analysis

- **Current State**: The application is a single `WinExe` project (`S7Tools`).
- **Folders**: `Models` and `Exceptions` are currently in the UI project but should be part of a core library.
- **Dependencies**: The project correctly uses `Microsoft.Extensions.DependencyInjection` and `ReactiveUI`.
- **`MainWindowViewModel.cs`**: Already uses `ReactiveCommand` for UI actions, which is a good pattern to continue.

## 3. Implementation Strategy

### Phase 1: Project Restructuring

1.  **Create `S7Tools.Core` Project**: A new `.NET 8` class library will be created at `src/S7Tools.Core`.
2.  **Add Project Reference**: The main `S7Tools` project will add a reference to `S7Tools.Core`.
3.  **Move Folders**: The `Models` and `Exceptions` folders will be moved from `S7Tools` to `S7Tools.Core`.
4.  **Update Namespaces**: Namespaces of the moved files will be updated to reflect the new project structure (e.g., `S7Tools.Core.Models`).

### Phase 2: Core Service and Interface Definition

1.  **Create `Services` Folder**: A new `Services` folder will be created inside `S7Tools.Core`.
2.  **Define Interfaces**:
    -   `S7Tools.Core/Services/Interfaces/IS7ConnectionProvider.cs`: An interface to abstract the management of the S7 PLC connection.
    -   `S7Tools.Core/Services/Interfaces/ITagRepository.cs`: An interface to abstract reading and writing data (tags) from the PLC.
3.  **Define Models**:
    -   `S7Tools.Core/Models/Tag.cs`: A simple model to represent an S7 Tag (e.g., with Name, Address, Value).

### Phase 3: ViewModel and DI Integration

1.  **Update `MainWindowViewModel.cs`**:
    -   Inject the new core services (`ITagRepository`, `IS7ConnectionProvider`).
    -   Create a new `ReactiveCommand` (e.g., `ReadTagCommand`) that will use the injected services to perform its work. This demonstrates the integration between the UI and Core layers.
2.  **Update `Program.cs`**:
    -   Register the new interfaces and their (placeholder) implementations with the dependency injection container.

## 4. Success Criteria

- The solution builds successfully after restructuring.
- The `S7Tools.Core` project contains the `Models`, `Exceptions`, and core service interfaces.
- The `S7Tools` project references `S7Tools.Core`.
- The `MainWindowViewModel` correctly uses a `ReactiveCommand` that depends on a service from `S7Tools.Core`.
