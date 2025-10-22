# Quick Start: Resources and Settings Paths Management

**Feature**: 007-resources-paths-management
**Date**: 2025-10-22
**Estimated Effort**: 2-3 days

## Implementation Overview

This feature implements dynamic path management for S7Tools to eliminate hardcoded paths, ensure proper resource initialization, and provide settings management. The implementation focuses on the specific folder structure from the executable location without testing projects initially.

## Specific Folder Structure (from executable)

```
Resources/
├── AppSettings/AppSettings.json
├── Profiles/
│   ├── Serial/SerialProfiles.json
│   ├── Socat/SocatProfiles.json
│   ├── PowerSupply/PowerSupplyProfiles.json
│   └── MemoryRegions/**
├── Logs/
│   ├── Main/MainLog_{timestamp}_{RollingNumber}.json
│   └── Exported/
│       ├── CSV/s7tools_logs_{timestamp}.csv
│       ├── TXT/s7tools_logs_{timestamp}.txt
│       └── JSON/s7tools_logs_{timestamp}.json
├── Jobs/Jobs.json
├── Tasks/Tasks.json
├── Payloads/**
└── Dumps/**
```

## Key Components

### 1. Core Models (S7Tools.Core/Models/Configuration/)

- `PathConfiguration.cs` - Main path resolution model
- `ApplicationSettings.cs` - Settings hierarchy management
- `ResourceManifest.cs` - Resource requirements definition
- Supporting models: `DirectoryInfo`, `FileInfo`, `ResourceInfo`

### 2. Service Interfaces (S7Tools.Core/Interfaces/Services/)

- `IPathService.cs` - Path resolution and validation
- `IApplicationSettingsService.cs` - Settings management with user overrides
- `IResourceManagerService.cs` - Resource creation and validation

### 3. Service Implementations (S7Tools/Services/)

- `PathService.cs` - Implements dynamic path resolution
- `ApplicationSettingsService.cs` - Implements layered settings management
- `ResourceManagerService.cs` - Implements resource initialization

### 4. Service Registration (S7Tools/Extensions/)

Update `ServiceCollectionExtensions.cs` to register new services:

```csharp
public static IServiceCollection AddS7ToolsPathManagement(this IServiceCollection services)
{
    services.TryAddSingleton<IPathService, PathService>();
    services.TryAddSingleton<IApplicationSettingsService, ApplicationSettingsService>();
    services.TryAddSingleton<IResourceManagerService, ResourceManagerService>();
    return services;
}
```

## Implementation Steps

### Phase 1: Core Infrastructure (Day 1)

1. **Create Core Models**
   - Implement `PathConfiguration` with validation
   - Implement `ApplicationSettings` with merge logic
   - Implement `ResourceManifest` with creation strategies

2. **Define Service Contracts**
   - Create service interfaces with proper async patterns
   - Define result types for operations
   - Include proper XML documentation

3. **Basic Path Service**
   - Implement executable-relative path resolution
   - Add directory creation with error handling
   - Include structured logging for all operations

### Phase 2: Settings Management (Day 2)

1. **Settings Service Implementation**
   - Implement layered configuration loading
   - Add user settings override logic
   - Include change detection and notifications

2. **Resource Manager Service**
   - Implement resource manifest loading
   - Add missing resource creation logic
   - Include validation and error recovery

3. **Service Integration**
   - Register services in DI container
   - Add initialization during app startup
   - Update existing services to use new path resolution

### Phase 3: Testing & Polish (Day 3)

1. **Unit Tests**
   - Test path resolution with various scenarios
   - Test settings merging and persistence
   - Test resource creation and validation

2. **Integration Tests**
   - Test with actual file system operations
   - Test error scenarios (permissions, missing directories)
   - Test cross-platform compatibility

3. **Documentation & Cleanup**
   - Update existing code to use new services
   - Remove any remaining hardcoded paths
   - Update application initialization flow

## Testing Strategy

### No Testing Projects Initially

As specified, we will not implement testing projects yet. Focus is on building a solid base implementation first.

### Manual Verification

- Verify all folder structure is created correctly on first run
- Test path resolution in different deployment scenarios
- Verify settings loading and resource access
- Test error handling with permission issues

### Future Testing Considerations

Once the base implementation is solid:
- Unit tests for path resolution logic
- Integration tests for file operations
- Cross-platform compatibility tests

## Risk Mitigation

### File System Permissions

- Graceful degradation when directories cannot be created
- Clear error messages for permission issues
- Fallback strategies for read-only environments

### Cross-Platform Compatibility

- Use .NET path utilities for all path operations
- Test on Windows, Linux, and macOS
- Handle platform-specific path limitations

### Migration from Existing Code

- Gradual migration approach to minimize risk
- Maintain backward compatibility during transition
- Comprehensive testing of existing functionality

## Success Criteria

1. ✅ All hardcoded paths eliminated from codebase
2. ✅ Application works correctly when moved to different directories
3. ✅ All required Resources folder structure created automatically on first run
4. ✅ Specific file paths (AppSettings.json, profile JSONs, etc.) resolve correctly
5. ✅ Comprehensive error logging for debugging
6. ✅ Cross-platform path handling works correctly
7. ✅ No hardcoded strings used - all paths dynamically resolved
8. ✅ Build process completes successfully with 30-second wait verification

## Dependencies

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Json
- Microsoft.Extensions.Logging
- System.Text.Json (for settings serialization)

## Follow-up Tasks

- Update deployment scripts to work with new path structure
- Create user documentation for settings customization
- Consider adding settings validation in UI
- Plan migration strategy for existing installations
