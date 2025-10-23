# Service Contracts: Resources and Settings Paths Management

This directory contains the service interface contracts for the path management feature.

## Contract Files

- `IPathService.cs` - Core path resolution service interface
- `IApplicationSettingsService.cs` - Settings management service interface
- `IResourceManagerService.cs` - Resource creation and validation service interface

## Design Principles

All contracts follow Clean Architecture principles:

- Interfaces are defined in Core layer
- No dependencies on infrastructure concerns
- Return types use domain models
- Async patterns for file operations
- Proper error handling with specific exceptions

## Usage

These contracts will be implemented in the Infrastructure layer and registered via dependency injection in ServiceCollectionExtensions.cs.
