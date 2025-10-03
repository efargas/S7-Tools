<!-- markdownlint-disable-file -->
# Task Details: Core Architecture Refactor

## Research Reference

**Source Research**: #file: ./.copilot-tracking/research/20240729-core-architecture-refactor-research.md

## Phase 1: Project Restructuring

### Task 1.1: Create the `S7Tools.Core` class library project.

Create a new C# class library project.

- **Files**:
  - `src/S7Tools.Core/S7Tools.Core.csproj` - Create this file.
  - `src/S7Tools.Core/Class1.cs` - Delete this default file.
- **Success**:
  - The `S7Tools.Core` project is created in the `src` directory.

### Task 1.2: Add a project reference from `S7Tools` to `S7Tools.Core`.

Modify the main application's project file to include a reference to the new core library.

- **Files**:
  - `src/S7Tools/S7Tools.csproj` - Modify this file.
- **Success**:
  - The `S7Tools.csproj` file contains a `<ProjectReference>` to `S7Tools.Core.csproj`.

### Task 1.3: Move `Models` and `Exceptions` folders to `S7Tools.Core`.

Move the existing `Models` and `Exceptions` directories from the `S7Tools` project to the `S7Tools.Core` project.

- **Files**:
  - `src/S7Tools/Models` - Move this folder to `src/S7Tools.Core/Models`.
  - `src/S7Tools/Exceptions` - Move this folder to `src/S7Tools.Core/Exceptions`.
- **Success**:
  - The folders no longer exist in `src/S7Tools`.
  - The folders now exist in `src/S7Tools.Core`.

## Phase 2: Core Service and Interface Definition

### Task 2.1: Create the `Tag` model in `S7Tools.Core`.

Create a simple model to represent an S7 tag.

- **Files**:
  - `src/S7Tools.Core/Models/Tag.cs` - Create this file.
- **Success**:
  - The `Tag.cs` file is created with properties for `Name`, `Address`, and `Value`.
  - The namespace is `S7Tools.Core.Models`.

### Task 2.2: Create the `IS7ConnectionProvider` and `ITagRepository` interfaces.

Define the contracts for the S7 communication services.

- **Files**:
  - `src/S7Tools.Core/Services/Interfaces/IS7ConnectionProvider.cs` - Create this file.
  - `src/S7Tools.Core/Services/Interfaces/ITagRepository.cs` - Create this file.
- **Success**:
  - `IS7ConnectionProvider.cs` is created in the correct namespace (`S7Tools.Core.Services.Interfaces`) and contains methods like `ConnectAsync` and `DisconnectAsync`.
  - `ITagRepository.cs` is created in the correct namespace (`S7Tools.Core.Services.Interfaces`) and contains a method like `Task<Tag> ReadTagAsync(string address)`.

## Phase 3: ViewModel and DI Integration

### Task 3.1: Update `MainWindowViewModel` to use a new `ReactiveCommand` with core services.

Inject the new core services into the main view model and create a command to demonstrate their use.

- **Files**:
  - `src/S7Tools/ViewModels/MainWindowViewModel.cs` - Modify this file.
- **Success**:
  - The constructor is updated to accept `ITagRepository`.
  - A new `ReactiveCommand` named `ReadTagCommand` is added.
  - The command's execution logic calls the `_tagRepository.ReadTagAsync` method.
  - A property to hold the result of the tag read is added.

### Task 3.2: Register new services in `Program.cs`.

Register the new interfaces and create placeholder implementations for them in the DI container.

- **Files**:
  - `src/S7Tools/Program.cs` - Modify this file.
  - `src/S7Tools/Services/PlcDataService.cs` - Create this file as a placeholder implementation.
- **Success**:
  - A new `PlcDataService.cs` file is created that implements `ITagRepository` and `IS7ConnectionProvider`.
  - The `ConfigureServices` method in `Program.cs` registers `PlcDataService` as a singleton for both `ITagRepository` and `IS7ConnectionProvider`.
